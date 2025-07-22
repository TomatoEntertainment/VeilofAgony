using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[System.Serializable]
public struct SkinProfile
{
    public GameObject prefab;
    public Vector3 gameplayRotation;
    public RuntimeAnimatorController animatorController;
}

public class ShipSkinLoader : MonoBehaviour
{
    [Header("Perfis de Skin")]
    public SkinProfile[] skinProfiles;
    
    [Header("Controle da Luz de Abdução")]
    [Tooltip("O nome do GameObject da luz de abdução dentro de cada prefab.")]
    public string abductionLightObjectName = "AbductionLight";
    
    [Tooltip("Adicione aqui o nome EXATO de todas as cenas de level.")]
    public List<string> levelSceneNames = new List<string>();

    private GameObject currentAbductionLight;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        UpdateLightVisibility();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Awake()
    {
        int equippedSkinIndex = PlayerPrefs.GetInt("EquippedSkin", 0);
        LoadSkin(equippedSkinIndex);
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateLightVisibility();
    }

    public void LoadSkin(int index)
    {
        if (skinProfiles == null || index < 0 || index >= skinProfiles.Length) return;

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        SkinProfile selectedProfile = skinProfiles[index];
        if (selectedProfile.prefab == null) return;

        GameObject newSkinInstance = Instantiate(selectedProfile.prefab, transform);
        newSkinInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(selectedProfile.gameplayRotation));
        newSkinInstance.transform.localScale = Vector3.one;

        var newAnimator = newSkinInstance.GetComponent<Animator>() ?? newSkinInstance.AddComponent<Animator>();
        // CORREÇÃO: A variável se chama 'selectedProfile', e não 'selected'.
        newAnimator.runtimeAnimatorController = selectedProfile.animatorController;

        // Tenta encontrar a luz, mas agora está preparado para não encontrá-la.
        Transform lightTransform = newSkinInstance.transform.Find(abductionLightObjectName);
        if (lightTransform != null)
        {
            currentAbductionLight = lightTransform.gameObject;
            var lightCollider = currentAbductionLight.GetComponent<Collider>() ?? currentAbductionLight.AddComponent<SphereCollider>();
            lightCollider.isTrigger = true;
        }
        else
        {
            // Se não encontrou, garante que a referência é nula e avisa no console.
            currentAbductionLight = null;
            Debug.LogWarning($"[ShipSkinLoader] Não foi possível encontrar o objeto da luz de abdução com o nome '{abductionLightObjectName}' no prefab '{selectedProfile.prefab.name}'. O collider de abdução não funcionará para esta skin.");
        }
        
        UpdateLightVisibility();
    }
    
    private void UpdateLightVisibility()
    {
        // Verifica se a luz foi encontrada ANTES de tentar usá-la.
        if (currentAbductionLight == null)
        {
            // Se a luz não existe, simplesmente não faz nada e evita o erro.
            return; 
        }
        
        // Força a desativação dos componentes visuais da luz.
        var lightComponent = currentAbductionLight.GetComponent<Light>();
        if (lightComponent != null)
        {
            lightComponent.enabled = false;
        }

        var rendererComponent = currentAbductionLight.GetComponent<Renderer>();
        if (rendererComponent != null)
        {
            rendererComponent.enabled = false;
        }
    }
}