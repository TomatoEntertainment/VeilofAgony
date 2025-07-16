// TotemAltar.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TotemAltar : MonoBehaviour
{
    [Header("Configurações do Altar")]
    [Tooltip("ID único deste altar (deve corresponder ao ID do totem)")]
    public string altarID = "totem_01";
    
    [Tooltip("Nome do altar para exibição")]
    public string altarName = "Altar Antigo";
    
    [Header("Textos de Interação")]
    public string placeTotemText = "Colocar Totem";
    public string pourOilText = "Derramar Óleo";
    public string burnTotemText = "Queimar Totem";
    public string needOilText = "Precisa de um jarro de óleo";
    public string wrongTotemText = "Totem incorreto para este altar";
    
    [Header("Configurações de Óleo")]
    [Tooltip("Tempo para derramar o óleo")]
    public float pourOilTime = 3f;
    
    [Tooltip("Tempo para queimar o totem")]
    public float burnTime = 2f;
    
    [Header("Referências do Altar")]
    [Tooltip("Transform onde o totem será colocado")]
    public Transform totemSlot;
    
    [Tooltip("Plane que simula o óleo")]
    public Transform oilPlane;
    
    [Tooltip("Altura inicial do plane de óleo")]
    public float oilPlaneStartY = -0.5f;
    
    [Tooltip("Altura final do plane de óleo")]
    public float oilPlaneEndY = 0f;
    
    [Tooltip("Partículas de óleo sendo derramado")]
    public ParticleSystem oilPourParticles;
    
    [Tooltip("Partículas de fogo")]
    public ParticleSystem fireParticles;
    
    [Header("Estados do Altar")]
    [SerializeField] private bool hasTotem = false;
    [SerializeField] private bool hasOil = false;
    [SerializeField] private bool isBurned = false;
    [SerializeField] private bool isPouring = false;
    [SerializeField] private bool isBurning = false;
    
    private TotemPickup currentTotem;
    private float currentActionProgress = 0f;
    private ManualInteractionSystem manualSystem;
    
    void Start()
    {
        manualSystem = GetComponent<ManualInteractionSystem>();
        
        // Configurar estado inicial
        if (oilPlane != null)
        {
            Vector3 pos = oilPlane.localPosition;
            pos.y = oilPlaneStartY;
            oilPlane.localPosition = pos;
            oilPlane.gameObject.SetActive(false);
        }
        
        if (oilPourParticles != null)
            oilPourParticles.Stop();
            
        if (fireParticles != null)
            fireParticles.Stop();
        
        UpdateInteractionText();
    }
    
    void UpdateInteractionText()
    {
        if (manualSystem == null) return;
        
        if (isBurned)
        {
            manualSystem.ForceShowIcon(false);
            manualSystem.enabled = false;
        }
        else if (!hasTotem)
        {
            manualSystem.actionText = placeTotemText;
        }
        else if (!hasOil)
        {
            manualSystem.actionText = pourOilText;
        }
        else
        {
            manualSystem.actionText = burnTotemText;
        }
    }
    
    public bool TryPlaceTotem(TotemPickup totem)
    {
        if (hasTotem || totem == null) return false;
        
        if (totem.GetTotemID() != altarID)
        {
            Debug.Log($"{wrongTotemText} - {totem.GetTotemName()} não é compatível com {altarName}!");
            return false;
        }
        
        // Colocar totem no slot
        currentTotem = totem;
        hasTotem = true;
        
        totem.transform.SetParent(totemSlot);
        totem.transform.localPosition = Vector3.zero;
        totem.transform.localRotation = Quaternion.identity;
        
        // Desativar física do totem
        Rigidbody rb = totem.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        
        Collider col = totem.GetComponent<Collider>();
        if (col != null) col.enabled = false;
        
        // Esconder ícone do totem
        ManualInteractionSystem totemManual = totem.GetComponent<ManualInteractionSystem>();
        if (totemManual != null)
        {
            totemManual.ForceShowIcon(false);
            totemManual.enabled = false;
        }
        
        UpdateInteractionText();
        Debug.Log($"Totem {totem.GetTotemName()} colocado em {altarName}");
        
        return true;
    }
    
    public void StartPouringOil(GameObject progressHolder, RectTransform movingFillRect, OilJar oilJar)
    {
        if (!hasTotem || hasOil || isPouring || oilJar == null) return;
        
        isPouring = true;
        currentActionProgress = 0f;
        
        // Iniciar UI de progresso
        if (movingFillRect != null)
        {
            float fillHeight = movingFillRect.rect.height;
            float fillStartY = -fillHeight;
            movingFillRect.anchoredPosition = new Vector2(0, fillStartY);
        }
        
        if (progressHolder != null)
            progressHolder.SetActive(true);
        
        // Iniciar partículas de óleo
        if (oilPourParticles != null)
            oilPourParticles.Play();
        
        // Mostrar plane de óleo
        if (oilPlane != null)
        {
            oilPlane.gameObject.SetActive(true);
            Vector3 pos = oilPlane.localPosition;
            pos.y = oilPlaneStartY;
            oilPlane.localPosition = pos;
        }
        
        Debug.Log($"Iniciando derramamento de óleo em {altarName}");
    }
    
    public void UpdatePouringOil(GameObject progressHolder, RectTransform movingFillRect, OilJar oilJar)
    {
        if (!isPouring || oilJar == null) return;
        
        currentActionProgress += Time.deltaTime;
        float progress = Mathf.Clamp01(currentActionProgress / pourOilTime);
        
        // Atualizar UI de progresso
        if (movingFillRect != null)
        {
            float fillHeight = movingFillRect.rect.height;
            float fillStartY = -fillHeight;
            float newY = Mathf.Lerp(fillStartY, 0, progress);
            movingFillRect.anchoredPosition = new Vector2(0, newY);
        }
        
        // Atualizar altura do plane de óleo
        if (oilPlane != null)
        {
            Vector3 pos = oilPlane.localPosition;
            pos.y = Mathf.Lerp(oilPlaneStartY, oilPlaneEndY, progress);
            oilPlane.localPosition = pos;
        }
        
        // Completar derramamento
        if (progress >= 1f)
        {
            CompletePouringOil(progressHolder, oilJar);
        }
    }
    
    void CompletePouringOil(GameObject progressHolder, OilJar oilJar)
    {
        isPouring = false;
        hasOil = true;
        
        // Destruir o jarro de óleo
        if (oilJar != null)
        {
            oilJar.Use();
            Destroy(oilJar.gameObject);
        }
        
        // Parar partículas
        if (oilPourParticles != null)
            oilPourParticles.Stop();
        
        // Esconder UI de progresso
        if (progressHolder != null)
            progressHolder.SetActive(false);
        
        // Garantir que o plane está na posição final
        if (oilPlane != null)
        {
            Vector3 pos = oilPlane.localPosition;
            pos.y = oilPlaneEndY;
            oilPlane.localPosition = pos;
        }
        
        UpdateInteractionText();
        Debug.Log($"Óleo derramado com sucesso em {altarName}");
    }
    
    public void CancelPouringOil(GameObject progressHolder)
    {
        if (!isPouring) return;
        
        isPouring = false;
        currentActionProgress = 0f;
        
        // Parar partículas
        if (oilPourParticles != null)
            oilPourParticles.Stop();
        
        // Esconder UI de progresso
        if (progressHolder != null)
            progressHolder.SetActive(false);
        
        // Resetar plane de óleo
        if (oilPlane != null)
        {
            Vector3 pos = oilPlane.localPosition;
            pos.y = oilPlaneStartY;
            oilPlane.localPosition = pos;
            oilPlane.gameObject.SetActive(false);
        }
        
        Debug.Log($"Derramamento de óleo cancelado em {altarName}");
    }
    
    public void StartBurning(GameObject progressHolder, RectTransform movingFillRect)
    {
        if (!hasTotem || !hasOil || isBurning || isBurned) return;
        
        isBurning = true;
        currentActionProgress = 0f;
        
        // Iniciar UI de progresso
        if (movingFillRect != null)
        {
            float fillHeight = movingFillRect.rect.height;
            float fillStartY = -fillHeight;
            movingFillRect.anchoredPosition = new Vector2(0, fillStartY);
        }
        
        if (progressHolder != null)
            progressHolder.SetActive(true);
        
        Debug.Log($"Iniciando queima do totem em {altarName}");
    }
    
    public void UpdateBurning(GameObject progressHolder, RectTransform movingFillRect)
    {
        if (!isBurning) return;
        
        currentActionProgress += Time.deltaTime;
        float progress = Mathf.Clamp01(currentActionProgress / burnTime);
        
        // Atualizar UI de progresso
        if (movingFillRect != null)
        {
            float fillHeight = movingFillRect.rect.height;
            float fillStartY = -fillHeight;
            float newY = Mathf.Lerp(fillStartY, 0, progress);
            movingFillRect.anchoredPosition = new Vector2(0, newY);
        }
        
        // Completar queima
        if (progress >= 1f)
        {
            CompleteBurning(progressHolder);
        }
    }
    
    void CompleteBurning(GameObject progressHolder)
    {
        isBurning = false;
        isBurned = true;
        
        // Esconder UI de progresso
        if (progressHolder != null)
            progressHolder.SetActive(false);
        
        // Iniciar partículas de fogo
        if (fireParticles != null)
            fireParticles.Play();
        
        // Opcional: Destruir ou esconder o totem após um delay
        StartCoroutine(BurnTotemEffect());
        
        UpdateInteractionText();
        Debug.Log($"Totem queimado com sucesso em {altarName}!");
    }
    
    public void CancelBurning(GameObject progressHolder)
    {
        if (!isBurning) return;
        
        isBurning = false;
        currentActionProgress = 0f;
        
        // Esconder UI de progresso
        if (progressHolder != null)
            progressHolder.SetActive(false);
        
        Debug.Log($"Queima cancelada em {altarName}");
    }
    
    IEnumerator BurnTotemEffect()
    {
        // Esperar 2 segundos antes de fazer o totem desaparecer
        yield return new WaitForSeconds(2f);
        
        if (currentTotem != null)
        {
            // Fade out ou destruir o totem
            Destroy(currentTotem.gameObject);
        }
        
        // Opcional: Notificar o GameManager que este altar foi completado
        // GameManager.Instance.OnAltarCompleted(altarID);
    }
    
    // Getters para verificação de estado
    public bool HasTotem() { return hasTotem; }
    public bool HasOil() { return hasOil; }
    public bool IsBurned() { return isBurned; }
    public bool IsPouring() { return isPouring; }
    public bool IsBurning() { return isBurning; }
    public string GetAltarID() { return altarID; }
    
    void OnDrawGizmosSelected()
    {
        if (totemSlot != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(totemSlot.position, Vector3.one * 0.5f);
        }
    }
}