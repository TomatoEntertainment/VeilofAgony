using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI; 
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public class MenuController : MonoBehaviour
{
    [Header("Cenas de Cenário")]
    [Tooltip("Nomes exatos das cenas de cenário (Build Settings)")]
    public string[] scenarioScenes;

    [Header("Painéis de UI")]
    [Tooltip("Arraste para aqui o seu painel de Opções/Settings")]
    public GameObject settingsPanel; // NOVA REFERÊNCIA

    [Header("Recorde")]
    [Tooltip("Texto que exibirá o recorde salvo")]
    public TMP_Text recordText;

    private bool hasLoaded = false;

    void OnEnable()
    {
        UpdateRecordText();
    }

    void Start()
    {
        UpdateRecordText();
    }

    private void UpdateRecordText()
    {
        if (recordText == null)
        {
            Debug.LogWarning("[MenuController] recordText não atribuído no Inspector.");
            return;
        }
        int record = PlayerPrefs.GetInt("HighScore", 0);
        recordText.text = record.ToString();
    }

    void Update()
    {
        if (hasLoaded) return;

        // *** ALTERAÇÃO PRINCIPAL AQUI ***
        // Se o painel de opções estiver ativo, o script não faz mais nada.
        if (settingsPanel != null && settingsPanel.activeInHierarchy)
        {
            return;
        }

        if (ClickDetected(out Vector2 screenPos))
        {
            if (!IsPointerOverInteractiveUI(screenPos))
            {
                LoadRandomScenario();
                hasLoaded = true;
            }
        }
    }

    private bool ClickDetected(out Vector2 screenPosition)
    {
        screenPosition = Vector2.zero;

    #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }
        return false;
    #else
        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }
        return false;
    #endif
    }
    
    private bool IsPointerOverInteractiveUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;

        var eventData = new PointerEventData(EventSystem.current) { position = screenPosition };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (result.gameObject.GetComponent<Selectable>() != null)
            {
                return true;
            }
        }

        return false;
    }

    public void LoadRandomScenario()
    {
        if (scenarioScenes == null || scenarioScenes.Length == 0)
        {
            Debug.LogError("[MenuController] scenarioScenes não configurado no Inspector!");
            return;
        }
        int idx = Random.Range(0, scenarioScenes.Length);
        string sceneName = scenarioScenes[idx];
        
        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.FadeToScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}