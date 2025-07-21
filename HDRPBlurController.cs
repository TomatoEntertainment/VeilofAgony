using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class HDRPBlurController : MonoBehaviour
{
    [Header("HDRP Volume")]
    [Tooltip("Volume component da câmera")]
    public Volume volume;
    
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
    private float blurredFocusDistance = 0.3f; // Máximo blur
    
    void Start()
    {
        SetupHDRPVolume();
    }
    
    void SetupHDRPVolume()
    {
        // Encontrar Volume automaticamente se não foi atribuído
        if (volume == null)
        {
            volume = GetComponent<Volume>();
            
            if (volume == null)
            {
                // Procurar na câmera
                Camera cam = GetComponent<Camera>();
                if (cam != null)
                {
                    volume = cam.GetComponent<Volume>();
                }
            }
        }
        
        if (volume == null)
        {
            Debug.LogError("❌ HDRPBlurController: Volume não encontrado! Adicione o componente Volume à câmera.");
            return;
        }
        else
        {
            Debug.Log("✅ HDRP Volume encontrado!");
        }
        
        if (volume.profile == null)
        {
            Debug.LogError("❌ HDRPBlurController: Volume Profile não atribuído! Crie um Volume Profile (HDRP) e atribua ao campo 'Profile'.");
            return;
        }
        else
        {
            Debug.Log("✅ Volume Profile encontrado: " + volume.profile.name);
        }
        
        // Verificar se o volume está ativo
        if (!volume.isGlobal)
        {
            Debug.LogWarning("⚠️ Volume não está marcado como 'Is Global'. Marque essa opção!");
        }
        
        // Obter referência do Depth of Field
        if (volume.profile.TryGet(out depthOfField))
        {
            Debug.Log("✅ Depth of Field encontrado no Profile!");
            
            // Configurar valores iniciais
            depthOfField.active = false; // Começar desabilitado
            
            // Configurar parâmetros se não estão overriden
            if (!depthOfField.focusMode.overrideState)
            {
                depthOfField.focusMode.overrideState = true;
                depthOfField.focusMode.value = DepthOfFieldMode.UsePhysicalCamera;
            }
            
            if (!depthOfField.focusDistance.overrideState)
            {
                depthOfField.focusDistance.overrideState = true;
                depthOfField.focusDistance.value = clearFocusDistance;
            }
            
            if (!depthOfField.nearFocusStart.overrideState)
            {
                depthOfField.nearFocusStart.overrideState = true;
                depthOfField.nearFocusStart.value = 0f;
            }
            
            if (!depthOfField.nearFocusEnd.overrideState)
            {
                depthOfField.nearFocusEnd.overrideState = true;
                depthOfField.nearFocusEnd.value = 4f;
            }
            
            Debug.Log("✅ Depth of Field (HDRP) configurado com sucesso!");
        }
        else
        {
            Debug.LogError("❌ HDRPBlurController: Depth of Field não encontrado no Volume Profile! \n" +
                          "SOLUÇÃO: \n" +
                          "1. Selecione o Volume Profile no Project\n" +
                          "2. Clique em 'Add Override'\n" +
                          "3. Escolha Post-processing → Depth of Field");
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
            if (!depthOfField.active)
            {
                depthOfField.active = true;
                if (showDebugInfo) Debug.Log("Blur HDRP ativado!");
            }
            
            // Interpolar entre foco claro e borrado
            float focusDistance = Mathf.Lerp(clearFocusDistance, blurredFocusDistance, currentBlurIntensity);
            depthOfField.focusDistance.value = focusDistance;
            
            // Ajustar intensidade do near blur
            float nearBlurEnd = Mathf.Lerp(4f, 8f, currentBlurIntensity);
            depthOfField.nearFocusEnd.value = nearBlurEnd;
        }
        else
        {
            // Desativar efeito
            if (depthOfField.active)
            {
                depthOfField.active = false;
                if (showDebugInfo) Debug.Log("Blur HDRP desativado!");
            }
        }
        
        // Debug info
        if (showDebugInfo && Time.time % 1f < Time.deltaTime)
        {
            Debug.Log($"HDRP Blur: {currentBlurIntensity:F2} → {targetBlurIntensity:F2} | Focus: {depthOfField.focusDistance.value:F1} | Ativo: {depthOfField.active}");
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
            depthOfField.active = false;
        }
    }
    
    /// <summary>
    /// Método de teste manual - força blur máximo
    /// </summary>
    [ContextMenu("TESTE: Blur Máximo HDRP")]
    public void ForceMaxBlurTest()
    {
        if (depthOfField != null)
        {
            Debug.Log("🔥 TESTE: Aplicando blur máximo HDRP!");
            depthOfField.active = true;
            depthOfField.focusDistance.value = 0.3f;
            depthOfField.nearFocusEnd.value = 8f;
            targetBlurIntensity = 1f;
            currentBlurIntensity = 1f;
        }
        else
        {
            Debug.LogError("❌ TESTE FALHOU: Depth of Field é null!");
        }
    }
    
    [ContextMenu("TESTE: Verificar Configuração HDRP")]
    public void DebugConfiguration()
    {
        Debug.Log("=== DEBUG CONFIGURAÇÃO HDRP ===");
        Debug.Log($"Volume: {(volume != null ? "✅" : "❌")}");
        Debug.Log($"Profile: {(volume?.profile != null ? "✅ " + volume.profile.name : "❌")}");
        Debug.Log($"Is Global: {(volume?.isGlobal == true ? "✅" : "❌")}");
        Debug.Log($"Depth of Field: {(depthOfField != null ? "✅" : "❌")}");
        
        if (depthOfField != null)
        {
            Debug.Log($"DOF Active: {depthOfField.active}");
            Debug.Log($"Focus Distance: {depthOfField.focusDistance.value}");
            Debug.Log($"Focus Mode: {depthOfField.focusMode.value}");
            Debug.Log($"Near Focus End: {depthOfField.nearFocusEnd.value}");
        }
    }
    
    // Getters para debug
    public float GetCurrentBlurIntensity() { return currentBlurIntensity; }
    public float GetTargetBlurIntensity() { return targetBlurIntensity; }
    public bool IsBlurActive() { return depthOfField != null && depthOfField.active; }
}