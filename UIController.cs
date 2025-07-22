using UnityEngine;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    [Tooltip("Nome exato da cena de skins conforme Build Settings")]
    public string skinSceneName = "Skins";

    /// <summary>
    /// Chame este método no OnClick() do seu botão para carregar a cena de skins.
    /// </summary>
    public void LoadSkinScene()
    {
        // Se você quiser sem fade:
        FadeManager.Instance.FadeToScene(skinSceneName);

        // Ou, se usar FadeManager:
        // FadeManager.Instance.FadeToScene(skinSceneName);
    }
}
