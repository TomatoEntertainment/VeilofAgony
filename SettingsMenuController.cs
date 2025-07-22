using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuController : MonoBehaviour
{
    [Header("Panels & Buttons")]
    [Tooltip("O painel flutuante de Settings")]
    public GameObject settingsPanel;
    [Tooltip("Botão que abre o painel de Settings (Options)")]
    public Button optionsButton;
    [Tooltip("Botão de Shop a ser ocultado")]
    public Button shopButton;
    [Tooltip("Botão de Voltar que fecha o painel de Settings")]
    public Button backButton;

    void Start()
    {
        // Inicialmente, o painel e o Back estão ocultos
        settingsPanel.SetActive(false);
        backButton.gameObject.SetActive(false);

        // Registra os callbacks
        optionsButton.onClick.AddListener(OpenSettings);
        backButton.onClick.AddListener(CloseSettings);
    }

    private void OpenSettings()
    {
        settingsPanel.SetActive(true);

        // Esconde o botão de options (que também é settings) e o shop
        optionsButton.gameObject.SetActive(false);
        shopButton.gameObject.SetActive(false);

        // Mostra apenas o Back dentro do painel
        backButton.gameObject.SetActive(true);
    }

    private void CloseSettings()
    {
        settingsPanel.SetActive(false);

        // Restaura o botão de options e shop
        optionsButton.gameObject.SetActive(true);
        shopButton.gameObject.SetActive(true);

        // Oculta o Back
        backButton.gameObject.SetActive(false);
    }
}
