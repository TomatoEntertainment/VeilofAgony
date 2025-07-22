using UnityEngine;
using UnityEngine.SceneManagement;

public class AbductionLightController : MonoBehaviour
{
    [Header("Configuração da Luz")]
    [Tooltip("Este campo será preenchido automaticamente pelo ShipSkinLoader.")]
    public GameObject abductionLightObject;

    [Header("Cenas de Level")]
    [Tooltip("Digite o nome exato de todas as cenas onde a luz deve ficar VISÍVEL.")]
    public string[] levelSceneNames;

    // Removemos a lógica de Awake e a colocamos em um método público
    public void Initialize()
    {
        if (abductionLightObject == null)
        {
            Debug.LogWarning("[AbductionLightController] Nenhum objeto de luz de abdução foi encontrado nesta skin. A luz não funcionará.", this.gameObject);
            return;
        }

        abductionLightObject.SetActive(false);
        string currentSceneName = SceneManager.GetActiveScene().name;
        bool isLevelScene = false;

        foreach (string levelName in levelSceneNames)
        {
            if (currentSceneName == levelName)
            {
                isLevelScene = true;
                break;
            }
        }

        if (isLevelScene)
        {
            abductionLightObject.SetActive(true);
        }
    }
}