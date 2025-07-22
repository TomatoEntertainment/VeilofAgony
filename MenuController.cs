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

    [Header("UI Ignorada (decorativa)")]
    [Tooltip("UI Elements que serão ignorados na detecção de clique")]
    public GameObject[] uiIgnoreList;

    [Header("Recorde")]
    [Tooltip("Texto que exibirá o recorde salvo")]
    public TMP_Text recordText;

    private bool hasLoaded = false;

    void OnEnable()
    {
        // Toda vez que o menu ficar ativo, recarrega o recorde
        UpdateRecordText();
    }

    void Start()
    {
        // Também no Start (caso seja a primeira ativação)
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

        if (ClickDetected(out Vector2 screenPos))
        {
            // Debug.Log($"[MenuController] Clique detectado em {screenPos}");

            if (IsPointerOverUI(screenPos, out string uiName))
            {
                // Debug.Log($"[MenuController] Clique sobre UI \"{uiName}\" – ignorando");
            }
            else
            {
                // Debug.Log("[MenuController] Clique fora da UI relevante – carregando cenário aleatório");
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

    private bool IsPointerOverUI(Vector2 screenPosition, out string clickedUIName)
    {
        clickedUIName = null;
        if (EventSystem.current == null)
            return false;

        var eventData = new PointerEventData(EventSystem.current) { position = screenPosition };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            var go = result.gameObject;
            clickedUIName = go.name;

            // Se for parte de uma UI ignorada, continue procurando
            bool isIgnored = false;
            foreach (var ignoreGO in uiIgnoreList)
            {
                if (ignoreGO != null && (go == ignoreGO || go.transform.IsChildOf(ignoreGO.transform)))
                {
                    isIgnored = true;
                    break;
                }
            }
            if (!isIgnored)
                return true; // clique sobre UI relevante
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
        // Debug.Log($"[MenuController] Carregando cena '{sceneName}' (índice {idx})");
        FadeManager.Instance.FadeToScene(sceneName);
    }

}
