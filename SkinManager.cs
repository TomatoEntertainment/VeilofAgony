using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public enum UnlockType { Distance, Purchase, RealMoney }
public enum CurrencyType { Coins, Capsules }

[System.Serializable]
public class SkinSlot
{
    public GameObject instance;
    public UnlockType unlockType;

    [Header("Desbloqueio por Distância")]
    public int unlockDistance;

    [Header("Desbloqueio com Moeda do Jogo")]
    public int purchaseCost;
    public CurrencyType purchaseCurrency;

    [Header("Desbloqueio com Dinheiro Real")]
    [Tooltip("ID do produto. DEVE ser idêntico ao cadastrado no IAPManager e nas lojas.")]
    public string productId;

    [Header("Referências de UI")]
    public TMP_Text unlockText;
    public Button purchaseButton;
}

public class SkinManager : MonoBehaviour
{
    [Header("Skins na cena")]
    public SkinSlot[] skins;

    [Header("Referências de Moedas")]
    public Wallet wallet;
    public MenuCoinDisplay coinDisplay;
    public MenuCapsuleDisplay capsuleDisplay;

    [Header("Botão Voltar")]
    public Button backButton;

    void OnEnable()
    {
        IAPManager.OnPurchaseSuccess += HandleSuccessfulPurchase;
    }

    void OnDisable()
    {
        IAPManager.OnPurchaseSuccess -= HandleSuccessfulPurchase;
    }

    void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(ReturnToMenu);

        RefreshAllSkinsUI();
    }

    private void HandleSuccessfulPurchase(string purchasedId)
    {
        Debug.Log($"[SkinManager] Recebeu confirmação de compra para '{purchasedId}'. Atualizando a UI.");
        RefreshAllSkinsUI();

        // Salva na nuvem após uma compra com dinheiro real ser confirmada.
        /* if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerCloudSave();
        } */
    }

    private void RefreshAllSkinsUI()
    {
        coinDisplay?.Refresh();
        capsuleDisplay?.Refresh();

        int highScore = PlayerPrefs.GetInt("HighScore", 0);

        foreach (var slot in skins)
        {
            if (slot.instance == null) continue;

            bool unlocked = false;
            switch (slot.unlockType)
            {
                case UnlockType.Distance:
                    unlocked = highScore >= slot.unlockDistance;
                    break;
                case UnlockType.Purchase:
                    unlocked = PlayerPrefs.GetInt("SkinPurchased_" + slot.instance.name, 0) == 1;
                    break;
                case UnlockType.RealMoney:
                    if (IAPManager.Instance != null)
                        unlocked = IAPManager.Instance.IsProductPurchased(slot.productId);
                    break;
            }

            bool isDistanceOrCurrency = slot.unlockType == UnlockType.Distance || slot.unlockType == UnlockType.Purchase;
            bool isRealMoney = slot.unlockType == UnlockType.RealMoney;

            if (slot.unlockText != null)
            {
                bool showText = !unlocked && isDistanceOrCurrency;
                slot.unlockText.gameObject.SetActive(showText);
                if (showText)
                {
                    if (slot.unlockType == UnlockType.Distance)
                    {
                        slot.unlockText.text = $"Alcance {slot.unlockDistance}m";
                    }
                    else
                    {
                        slot.unlockText.text = slot.purchaseCost.ToString();
                    }
                }
            }

            if (slot.purchaseButton != null)
            {
                bool showButton = !unlocked && isRealMoney;
                slot.purchaseButton.gameObject.SetActive(showButton);
                if (showButton)
                {
                    slot.purchaseButton.onClick.RemoveAllListeners();
                    SkinSlot currentSlot = slot;
                    slot.purchaseButton.onClick.AddListener(() => TryUnlock(currentSlot));
                }
            }

            if (slot.instance.GetComponent<Collider>() == null)
                slot.instance.AddComponent<BoxCollider>();
        }
    }

    void Update()
    {
        if (!DetectClick(out Vector2 sp)) return;

        if (EventSystem.current.IsPointerOverGameObject()) return;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                return;
            }
#endif

        Ray ray = Camera.main.ScreenPointToRay(sp);
        if (!Physics.Raycast(ray, out var hit)) return;

        for (int i = 0; i < skins.Length; i++)
        {
            var slot = skins[i];
            if (slot.instance == null) continue;
            if (hit.collider.gameObject != slot.instance && !hit.collider.transform.IsChildOf(slot.instance.transform))
                continue;

            bool isAlreadyUnlocked = false;
            switch (slot.unlockType)
            {
                case UnlockType.Distance: isAlreadyUnlocked = PlayerPrefs.GetInt("HighScore", 0) >= slot.unlockDistance; break;
                case UnlockType.Purchase: isAlreadyUnlocked = PlayerPrefs.GetInt("SkinPurchased_" + slot.instance.name, 0) == 1; break;
                case UnlockType.RealMoney: if (IAPManager.Instance != null) isAlreadyUnlocked = IAPManager.Instance.IsProductPurchased(slot.productId); break;
            }

            if (isAlreadyUnlocked)
            {
                EquipAndReturn(i);
            }
            else
            {
                if (slot.unlockType == UnlockType.Purchase)
                {
                    TryUnlock(slot);
                }
            }
            return;
        }
    }

    bool TryUnlock(SkinSlot slot)
    {
        switch (slot.unlockType)
        {
            case UnlockType.Purchase:
                bool purchaseSuccess = false;
                if (slot.purchaseCurrency == CurrencyType.Coins && wallet.coins >= slot.purchaseCost)
                {
                    wallet.AddCoins(-slot.purchaseCost);
                    purchaseSuccess = true;
                }
                else if (slot.purchaseCurrency == CurrencyType.Capsules && wallet.capsules >= slot.purchaseCost)
                {
                    wallet.AddCapsules(-slot.purchaseCost);
                    purchaseSuccess = true;
                }
                if (purchaseSuccess)
                {
                    PlayerPrefs.SetInt("SkinPurchased_" + slot.instance.name, 1);
                    PlayerPrefs.Save();
                    RefreshAllSkinsUI();

                    /* if (GameManager.Instance != null)
                    {
                        GameManager.Instance.TriggerCloudSave();
                    } */

                    return true;
                }
                break;
            case UnlockType.RealMoney:
                if (IAPManager.Instance != null)
                {
                    IAPManager.Instance.BuyProductID(slot.productId);
                }
                else
                {
                    Debug.LogError("[SkinManager] Instância do IAPManager não encontrada!");
                }
                return false;
        }
        return false;
    }

    void EquipAndReturn(int skinIndex)
    {
        PlayerPrefs.SetInt("EquippedSkin", skinIndex);
        PlayerPrefs.Save();
        // Salva na nuvem ao equipar uma nova skin
        /* if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerCloudSave();
        } */
        ReturnToMenu();
    }

    private bool DetectClick(out Vector2 screenPos)
    {
        screenPos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Mouse.current?.leftButton.wasPressedThisFrame == true) { screenPos = Mouse.current.position.ReadValue(); return true; }
        if (Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true) { screenPos = Touchscreen.current.primaryTouch.position.ReadValue(); return true; }
        return false;
#else
        if (Input.GetMouseButtonDown(0)) { screenPos = Input.mousePosition; return true; }
        return false;
#endif
    }

    private void ReturnToMenu()
    {
        SceneManager.LoadScene("Menu");
    }
    
    public SkinSlot[] GetAllSkinSlots()
    {
        return skins;
    }
}