// GameManager.cs - VERSÃO FINAL COM EVENTOS E UIMANAGER
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI; // Adicionado para aceder a componentes de UI como 'Selectable'
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Cenários e Prefabs")]
    public string[] scenarioScenes;
    public GameObject uiPrefab; // Arraste aqui a sua UI_Canvas_Prefab

    [Header("Referências do Jogo")]
    public PlayerController playerController;
    public Transform reviveSpawnPoint;
    
    [Header("Elementos de UI 3D")]
    public GameObject gameOver3DTextObject; // Arraste seu GameObject de Texto 3D de "Game Over" aqui

    [Header("Configurações do Jogo")]
    public float playerSpeed = 5f;
    public float maxPlayerSpeed = 15f;
    public float gameOverDelay = 1f;
    public float[] distanceMilestones;
    public float speedIncrement = 1f;

    [Header("Configurações do Contador de Pontos")]
    public float counterSpeedInterval = 100f;
    public float counterSpeedIncrement = 1f;

    [Header("Configurações de Reviver")]
    public float revivePanelDelay = 1f;

    // --- Variáveis privadas ---
    private const int MAX_COINS = 999999;
    private bool isGameStarted;
    private bool isGameOver;
    private float distanceTravelled;
    private int nextMilestoneIndex;
    private float nextCounterThreshold;
    private int coinCount;
    private bool hasRevived = false;
    private bool isReviving = false;

    // --- Propriedades Públicas ---
    public float DistanceTravelled => distanceTravelled;
    public bool IsGameStarted => isGameStarted;
    public bool IsGameOver => isGameOver;

    // --- MÉTODOS DE CICLO DE VIDA ---
    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        if (UIManager.Instance == null && uiPrefab != null)
        {
            Instantiate(uiPrefab);
        }

        if (AdManager.Instance == null)
        {
            new GameObject("AdManager").AddComponent<AdManager>();
        }
    }

    void OnEnable()
    {
        UIManager.OnWatchAdClicked += OnWatchAd;
        UIManager.OnSkipReviveClicked += OnSkipRevive;
        UIManager.OnRetryClicked += RestartLevel;
        UIManager.OnMenuClicked += ReturnToMenu;
        UIManager.OnShieldAdClicked += OnShieldAdClicked;
    }

    void OnDisable()
    {
        UIManager.OnWatchAdClicked -= OnWatchAd;
        UIManager.OnSkipReviveClicked -= OnSkipRevive;
        UIManager.OnRetryClicked -= RestartLevel;
        UIManager.OnMenuClicked -= ReturnToMenu;
        UIManager.OnShieldAdClicked -= OnShieldAdClicked;
    }

    void Start()
    {
        isGameStarted = false;
        isGameOver = false;
        hasRevived = false;
        distanceTravelled = 0f;
        nextMilestoneIndex = 0;
        nextCounterThreshold = counterSpeedInterval;
        coinCount = PlayerPrefs.GetInt("TotalCoins", 0);

        if (UIManager.Instance == null)
        {
            Debug.LogError("UIManager não encontrado! A UI não funcionará.");
            return;
        }

        UIManager.Instance.menuPanel.SetActive(true);
        UIManager.Instance.revivePanel.SetActive(false);
        UIManager.Instance.gameOverPanel.SetActive(false);
        UIManager.Instance.finalScoreText.gameObject.SetActive(false);
        
        if (UIManager.Instance.noAdsPanel != null) UIManager.Instance.noAdsPanel.SetActive(false);
        
        if (gameOver3DTextObject != null) gameOver3DTextObject.SetActive(false);

        foreach (var d in UIManager.Instance.distanceDigits)
            if (d != null) d.gameObject.SetActive(false);

        if (UIManager.Instance.coinText != null)
        {
            UIManager.Instance.coinText.gameObject.SetActive(true);
            UIManager.Instance.coinText.text = coinCount.ToString();
        }

        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        if (MeteorSpawner.Instance != null)
        {
            MeteorSpawner.Instance.StopSpawning();
        }
    }

    void Update()
    {
        if (isGameOver || isReviving) return;

        if (!isGameStarted)
        {
            if (AnyStartInput())
            {
                Vector2 clickPosition = Vector2.zero;
                bool isPointerInput = false;

                // Determina se foi um input de ponteiro (rato/toque) e obtém a sua posição
                #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                if (Mouse.current?.leftButton.wasPressedThisFrame == true) {
                    clickPosition = Mouse.current.position.ReadValue();
                    isPointerInput = true;
                } else if (Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true) {
                    clickPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                    isPointerInput = true;
                }
                #else
                if (Input.GetMouseButtonDown(0)) {
                    clickPosition = Input.mousePosition;
                    isPointerInput = true;
                } else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) {
                    clickPosition = Input.GetTouch(0).position;
                    isPointerInput = true;
                }
                #endif

                // Se foi um input de ponteiro, verifica a UI.
                // Caso contrário (teclado), inicia o jogo diretamente.
                if (isPointerInput)
                {
                    if (!IsPointerOverInteractiveUI(clickPosition))
                    {
                        StartGame();
                    }
                }
                else // Foi um input de teclado
                {
                    StartGame();
                }
            }
            return;
        }

        distanceTravelled += playerSpeed * Time.deltaTime;

        while (distanceTravelled >= nextCounterThreshold)
        {
            playerSpeed = Mathf.Min(playerSpeed + counterSpeedIncrement, maxPlayerSpeed);
            nextCounterThreshold += counterSpeedInterval;
        }

        UpdateDistanceUI();

        if (nextMilestoneIndex < distanceMilestones.Length &&
            distanceTravelled >= distanceMilestones[nextMilestoneIndex])
        {
            if (MeteorSpawner.Instance != null)
            {
                MeteorSpawner.Instance.IncreaseMeteorSpeed(speedIncrement);
            }
            nextMilestoneIndex++;
        }
    }
    
    /// <summary>
    /// Verifica se o ponteiro está sobre um elemento de UI interativo (como um botão).
    /// </summary>
    private bool IsPointerOverInteractiveUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;

        var eventData = new PointerEventData(EventSystem.current) { position = screenPosition };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            // Se o objeto tiver um componente "Selectable" (Button, Slider, Toggle, etc.),
            // significa que é interativo.
            if (result.gameObject.GetComponent<Selectable>() != null)
            {
                return true; // Clique foi sobre UI interativa, então bloqueia.
            }
        }

        return false; // Clique não foi sobre UI interativa.
    }

    private void OnShieldAdClicked()
    {
        AdManager.Instance.ShowRewardedAd(
            () => { // On Success
                SetShieldPanelState(false);
                if (playerController != null)
                {
                    playerController.ActivateShield();
                }
            },
            () => { // On Failure
                UIManager.Instance.ShowNoAdsMessageFor(2f);
            }
        );
    }

    private bool AnyStartInput()
    {
        #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        return (Mouse.current?.leftButton.wasPressedThisFrame == true)
            || (Keyboard.current?.anyKey.wasPressedThisFrame == true)
            || (Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true);
        #else
        return Input.GetMouseButtonDown(0)
            || Input.anyKeyDown
            || (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began);
        #endif
    }

    private void StartGame()
    {
        isGameStarted = true;
        UIManager.Instance.menuPanel.SetActive(false);
        UIManager.Instance.revivePanel.SetActive(false);
        UIManager.Instance.gameOverPanel.SetActive(false);

        foreach (var d in UIManager.Instance.distanceDigits)
            if (d != null) d.gameObject.SetActive(true);

        SetShieldPanelState(true);

        if (MeteorSpawner.Instance != null)
        {
            MeteorSpawner.Instance.ClearAllMeteors();
            MeteorSpawner.Instance.StartSpawning();
        }
    }

    private void UpdateDistanceUI()
    {
        if (UIManager.Instance.distanceDigits == null || UIManager.Instance.distanceDigits.Length == 0) return;
        int dist = Mathf.FloorToInt(distanceTravelled);
        string s = dist.ToString().PadLeft(UIManager.Instance.distanceDigits.Length, '0');
        for (int i = 0; i < UIManager.Instance.distanceDigits.Length; i++)
            if (UIManager.Instance.distanceDigits[i] != null)
                UIManager.Instance.distanceDigits[i].text = s[i].ToString();
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        SetShieldPanelState(false);
        if (playerController != null) playerController.enabled = false;
        if (MeteorSpawner.Instance != null)
        {
            MeteorSpawner.Instance.StopSpawning();
        }
        if (!hasRevived)
        {
            StartCoroutine(ShowRevivePanelAfterDelay());
        }
        else
        {
            OnSkipRevive();
        }
    }

    private IEnumerator ShowRevivePanelAfterDelay()
    {
        yield return new WaitForSeconds(revivePanelDelay);
        UIManager.Instance.revivePanel.SetActive(true);
    }

    public void OnWatchAd()
    {
        AdManager.Instance.ShowRewardedAd(
            () => { // On Success
                if (UIManager.Instance.watchAdButton != null) UIManager.Instance.watchAdButton.interactable = false;
                StartCoroutine(RevivePlayerSequence());
            },
            () => { // On Failure
                UIManager.Instance.ShowNoAdsMessageFor(2f);
            }
        );
    }

    public void OnSkipRevive()
    {
        UIManager.Instance.revivePanel.SetActive(false);
        StartCoroutine(ShowGameOverUI());
    }

    private IEnumerator RevivePlayerSequence()
    {
        isReviving = true;
        Debug.Log("Jogador ganhou recompensa! Resetando para o estado inicial.");
        UIManager.Instance.revivePanel.SetActive(false);

        if (MeteorSpawner.Instance != null)
        {
            MeteorSpawner.Instance.ClearAllMeteors();
            MeteorSpawner.Instance.StopSpawning();
        }

        if (playerController != null && reviveSpawnPoint != null)
        {
            playerController.transform.position = reviveSpawnPoint.position;
            playerController.transform.rotation = reviveSpawnPoint.rotation;
            if(playerController.gameObject.activeSelf == false)
            {
                playerController.gameObject.SetActive(true);
            }
            playerController.ResetToPreStartState();
        }
        else
        {
            Debug.LogError("PlayerController ou ReviveSpawnPoint não foram definidos no Inspector!");
        }
        
        SetShieldPanelState(true);

        foreach (var d in UIManager.Instance.distanceDigits)
            if (d != null) d.gameObject.SetActive(false);

        UIManager.Instance.menuPanel.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        hasRevived = true;
        isGameOver = false;
        isGameStarted = false;
        if (UIManager.Instance.watchAdButton != null) UIManager.Instance.watchAdButton.interactable = true;
        isReviving = false;
    }

    private IEnumerator ShowGameOverUI()
    {
        yield return new WaitForSeconds(gameOverDelay);
        
        UIManager.Instance.gameOverPanel.SetActive(true);
        UIManager.Instance.finalScoreText.gameObject.SetActive(true);

        int finalScore = Mathf.FloorToInt(distanceTravelled);
        UIManager.Instance.finalScoreText.text = finalScore.ToString();

        if (gameOver3DTextObject != null)
            gameOver3DTextObject.SetActive(true);
        
        SaveHighScore();
    }

    private void SaveHighScore()
    {
        int finalScore = Mathf.FloorToInt(distanceTravelled);
        int record = PlayerPrefs.GetInt("HighScore", 0);
        if (finalScore > record)
        {
            PlayerPrefs.SetInt("HighScore", finalScore);
            PlayerPrefs.Save();
        }
    }

    public void CollectCoin()
    {
        coinCount = Mathf.Min(PlayerPrefs.GetInt("TotalCoins", 0) + 1, MAX_COINS);
        PlayerPrefs.SetInt("TotalCoins", coinCount);
        PlayerPrefs.Save();
        if (UIManager.Instance?.coinText != null)
        {
            UIManager.Instance.coinText.text = coinCount.ToString();
        }
    }

    public void RestartLevel()
    {
        if (scenarioScenes != null && scenarioScenes.Length > 0)
        {
            int idx = Random.Range(0, scenarioScenes.Length);
            SceneManager.LoadScene(scenarioScenes[idx]);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("Menu");
    }

    public void SetShieldPanelState(bool active)
    {
        if (UIManager.Instance?.shieldButtonPanel != null)
        {
            UIManager.Instance.shieldButtonPanel.SetActive(active);
        }
    }
}