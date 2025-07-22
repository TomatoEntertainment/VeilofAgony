using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
public class DailyLoginUI : MonoBehaviour
{
    [Header("Referências")]
    public DailyLoginManager loginManager;

    [Header("Painel de Recompensa")]
    public GameObject rewardPanel;
    public Image      rewardIcon;
    public TMP_Text       rewardText;

    [Header("UI Quantidades")]
    public TMP_Text coinText;
    public TMP_Text capsuleText;

    [Header("Displays de Saldo")]
    public MenuCoinDisplay    coinDisplay;     // seu script para exibir TotalCoins
    public MenuCapsuleDisplay capsuleDisplay;  // script para exibir TotalCapsules

    [Header("Botões")]
    public Button collectButton;
    public Button backButton;

    void Awake()
    {
        // Garantir referências
        rewardPanel.SetActive(false);
        rewardIcon.preserveAspect = true;

        collectButton.onClick.AddListener(OnCollectClicked);
        backButton  .onClick.AddListener(OnBackClicked);
    }

    void OnEnable()
    {
        loginManager.OnLoginChecked += ShowDailyReward;
        loginManager.CheckLogin();
    }

    void OnDisable()
    {
        loginManager.OnLoginChecked -= ShowDailyReward;
    }

    private void ShowDailyReward(int day, bool wasReset)
    {
        var data = Array.Find(loginManager.rewards, r => r.day == day);
        if (data == null) return;

        rewardIcon.sprite = data.icon;
        rewardText.text   = data.description;

        bool showCoin    = data.coinAmount > 0 && data.capsuleAmount == 0;
        bool showCapsule = data.capsuleAmount > 0 && data.coinAmount == 0;

        coinText.gameObject.SetActive(showCoin);
        capsuleText.gameObject.SetActive(showCapsule);

        if (showCoin)    coinText   .text = $"+{data.coinAmount} Coins";
        if (showCapsule) capsuleText.text = $"+{data.capsuleAmount} Cápsulas";

        collectButton.interactable = true;
        rewardPanel.SetActive(true);
    }

    private void OnCollectClicked()
    {
        int day = loginManager.GetCurrentStreak();
        // Primeiro, conceder a recompensa
        loginManager.GiveReward(day);

        // Em seguida, atualizar imediatamente o display adequado
        var data = Array.Find(loginManager.rewards, r => r.day == day);
        if (data != null)
        {
            if (data.coinAmount > 0)
                coinDisplay?.Refresh();
            if (data.capsuleAmount > 0)
                capsuleDisplay?.Refresh();
        }

        // Fecha o painel
        rewardPanel.SetActive(false);
    }

    private void OnBackClicked()
    {
        rewardPanel.SetActive(false);
    }
}
