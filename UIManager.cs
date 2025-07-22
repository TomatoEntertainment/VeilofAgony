// UIManager.cs
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // --- EVENTOS ESTÁTICOS GLOBAIS ---
    public static event Action OnWatchAdClicked;
    public static event Action OnSkipReviveClicked;
    public static event Action OnRetryClicked;
    public static event Action OnMenuClicked;
    public static event Action OnShieldAdClicked;

    [Header("UI Panels")]
    public GameObject menuPanel;
    public GameObject revivePanel;
    public GameObject gameOverPanel;
    public GameObject noAdsPanel; // NOVO: Painel para a mensagem de "sem anúncios"

    [Header("GameOver UI")]
    public TMP_Text finalScoreText;

    [Header("In-Game UI")]
    public GameObject shieldButtonPanel;
    
    [Header("Buttons")]
    public Button watchAdButton;
    public Button skipAdButton;
    public Button retryButton;
    public Button menuButton;
    public Button shieldAdButton;

    [Header("Distance UI (6 dígitos)")]
    public TMP_Text[] distanceDigits;

    [Header("Coin UI (exibida em jogo)")]
    public TMP_Text coinText;

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
        if (noAdsPanel != null) noAdsPanel.SetActive(false); // Garante que começa desativado
    }

    /// <summary>
    /// Exibe a mensagem de "sem anúncios" por um tempo determinado.
    /// </summary>
    public void ShowNoAdsMessageFor(float seconds)
    {
        StartCoroutine(ShowNoAdsRoutine(seconds));
    }

    private IEnumerator ShowNoAdsRoutine(float duration)
    {
        if (noAdsPanel != null) noAdsPanel.SetActive(true);
        yield return new WaitForSeconds(duration);
        if (noAdsPanel != null) noAdsPanel.SetActive(false);
    }
    
    // --- FUNÇÕES QUE SERÃO CHAMADAS PELOS BOTÕES NA PREFAB ---
    public void HandleWatchAdClick() => OnWatchAdClicked?.Invoke();
    public void HandleSkipReviveClick() => OnSkipReviveClicked?.Invoke();
    public void HandleRetryClick() => OnRetryClicked?.Invoke();
    public void HandleMenuClick() => OnMenuClicked?.Invoke();
    public void HandleShieldAdClick() => OnShieldAdClicked?.Invoke();
}