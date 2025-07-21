using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class StaminaBlurController : MonoBehaviour
{
    [Header("Post Processing")]
    [Tooltip("Post Process Volume da câmera")]
    public PostProcessVolume postProcessVolume;
    
    [Header("Configurações do Blur")]
    [Tooltip("Intensidade máxima do blur")]
    [Range(0f, 1f)]
    public float maxBlurIntensity = 0.8f;
    
    [Tooltip("Velocidade da transição do blur")]
    public float blurTransitionSpeed = 2f;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    // Referências internas
    private DepthOfField depthOfField;
    private float currentBlurIntensity = 0f;
    private float targetBlurIntensity = 0f;
    
    // Configurações do Depth of Field
    private float clearFocusDistance = 10f;    // Sem blur
    private float blurredFocusDistance = 0.1f; // Máximo blur
    
    void Start()
    {
        SetupPostProcessing();
    }
    
    void SetupPostProcessing()
    {
        // Encontrar Post Process Volume automaticamente se não foi atribuído
        if (postProcessVolume == null)
        {
            postProcessVolume = GetComponent<PostProcessVolume>();
            
            if (postProcessVolume == null)
            {
                // Procurar na câmera
                Camera cam = GetComponent<Camera>();
                if (cam != null)
                {
                    postProcessVolume = cam.GetComponent<PostProcessVolume>();
                }
            }
        }
        
        if (postProcessVolume == null)
        {
            Debug.LogError("❌ StaminaBlurController: Post Process Volume não encontrado! Adicione o componente PostProcessVolume à câmera.");
            return;
        }
        else
        {
            Debug.Log("✅ Post Process Volume encontrado!");
        }
        
        if (postProcessVolume.profile == null)
        {
            Debug.LogError("❌ StaminaBlurController: Post Process Profile não atribuído ao Volume! Crie um Profile e atribua ao campo 'Profile'.");
            return;
        }
        else
        {
            Debug.Log("✅ Post Process Profile encontrado: " + postProcessVolume.profile.name);
        }
        
        // Verificar se o volume está ativo
        if (!postProcessVolume.isGlobal)
        {
            Debug.LogWarning("⚠️ Post Process Volume não está marcado como 'Is Global'. Marque essa opção!");
        }
        
        // Obter referência do Depth of Field
        if (postProcessVolume.profile.TryGetSettings(out depthOfField))
        {
            Debug.Log("✅ Depth of Field encontrado no Profile!");
            
            // Configurar valores iniciais
            depthOfField.enabled.value = false; // Começar desabilitado
            depthOfField.focusDistance.value = clearFocusDistance;
            depthOfField.aperture.value = 0.1f;
            depthOfField.focalLength.value = 50f;
            depthOfField.kernelSize.value = KernelSize.Medium;
            
            Debug.Log("✅ Depth of Field configurado com sucesso!");
        }
        else
        {
            Debug.LogError("❌ StaminaBlurController: Depth of Field não encontrado no Profile! \n" +
                          "SOLUÇÃO: \n" +
                          "1. Selecione o Profile no Project\n" +
                          "2. Clique em 'Add effect...'\n" +
                          "3. Escolha Unity → Depth of Field");
        }
        
        // Verificar se a câmera tem Post-process Layer
        PostProcessLayer layer = GetComponent<PostProcessLayer>();
        if (layer == null)
        {
            Debug.LogError("❌ PostProcessLayer não encontrado na câmera! Adicione o componente PostProcessLayer.");
        }
        else
        {
            Debug.Log("✅ PostProcessLayer encontrado!");
        }
    }
    
    void Update()
    {
        UpdateBlurTransition();
    }
    
    void UpdateBlurTransition()
    {
        if (depthOfField == null) return;
        
        // Suavizar transição
        currentBlurIntensity = Mathf.Lerp(currentBlurIntensity, targetBlurIntensity, Time.deltaTime * blurTransitionSpeed);
        
        // Aplicar blur
        if (currentBlurIntensity > 0.01f)
        {
            // Ativar efeito
            if (!depthOfField.enabled.value)
            {
                depthOfField.enabled.value = true;
                if (showDebugInfo) Debug.Log("Blur ativado!");
            }
            
            // Interpolar entre foco claro e borrado
            float focusDistance = Mathf.Lerp(clearFocusDistance, blurredFocusDistance, currentBlurIntensity);
            depthOfField.focusDistance.value = focusDistance;
            
            // Ajustar intensidade do blur
            float aperture = Mathf.Lerp(5.6f, 0.1f, currentBlurIntensity);
            depthOfField.aperture.value = aperture;
        }
        else
        {
            // Desativar efeito
            if (depthOfField.enabled.value)
            {
                depthOfField.enabled.value = false;
                if (showDebugInfo) Debug.Log("Blur desativado!");
            }
        }
        
        // Debug info
        if (showDebugInfo && Time.time % 1f < Time.deltaTime)
        {
            Debug.Log($"Blur: {currentBlurIntensity:F2} → {targetBlurIntensity:F2} | Focus: {depthOfField.focusDistance.value:F1} | Ativo: {depthOfField.enabled.value}");
        }
    }
    
    /// <summary>
    /// Define a intensidade do blur (0 = sem blur, 1 = blur máximo)
    /// </summary>
    public void SetBlurIntensity(float intensity)
    {
        targetBlurIntensity = Mathf.Clamp01(intensity * maxBlurIntensity);
    }
    
    /// <summary>
    /// Ativa/desativa o blur instantaneamente
    /// </summary>
    public void SetBlurImmediate(float intensity)
    {
        targetBlurIntensity = Mathf.Clamp01(intensity * maxBlurIntensity);
        currentBlurIntensity = targetBlurIntensity;
    }
    
    /// <summary>
    /// Para o blur gradualmente
    /// </summary>
    public void StopBlur()
    {
        targetBlurIntensity = 0f;
    }
    
    /// <summary>
    /// Para o blur instantaneamente
    /// </summary>
    public void StopBlurImmediate()
    {
        targetBlurIntensity = 0f;
        currentBlurIntensity = 0f;
        
        if (depthOfField != null)
        {
            depthOfField.enabled.value = false;
        }
    }
    
    /// <summary>
    /// Método de teste manual - força blur máximo
    /// </summary>
    [ContextMenu("TESTE: Blur Máximo Forçado")]
    public void ForceMaxBlurTest()
    {
        if (depthOfField != null)
        {
            Debug.Log("🔥 TESTE: Aplicando blur máximo!");
            depthOfField.enabled.value = true;
            depthOfField.focusDistance.value = 0.1f;
            depthOfField.aperture.value = 0.05f;
            targetBlurIntensity = 1f;
            currentBlurIntensity = 1f;
        }
        else
        {
            Debug.LogError("❌ TESTE FALHOU: Depth of Field é null!");
        }
    }
    
    [ContextMenu("TESTE: Verificar Configuração")]
    public void DebugConfiguration()
    {
        Debug.Log("=== DEBUG CONFIGURAÇÃO ===");
        Debug.Log($"Post Process Volume: {(postProcessVolume != null ? "✅" : "❌")}");
        Debug.Log($"Profile: {(postProcessVolume?.profile != null ? "✅ " + postProcessVolume.profile.name : "❌")}");
        Debug.Log($"Is Global: {(postProcessVolume?.isGlobal == true ? "✅" : "❌")}");
        Debug.Log($"Depth of Field: {(depthOfField != null ? "✅" : "❌")}");
        
        if (depthOfField != null)
        {
            Debug.Log($"DOF Enabled: {depthOfField.enabled.value}");
            Debug.Log($"Focus Distance: {depthOfField.focusDistance.value}");
            Debug.Log($"Aperture: {depthOfField.aperture.value}");
        }
        
        PostProcessLayer layer = GetComponent<PostProcessLayer>();
        Debug.Log($"PostProcessLayer: {(layer != null ? "✅" : "❌")}");
    }
    
    // Getters para debug
    public float GetCurrentBlurIntensity() { return currentBlurIntensity; }
    public float GetTargetBlurIntensity() { return targetBlurIntensity; }
    public bool IsBlurActive() { return depthOfField != null && depthOfField.enabled.value; }
}