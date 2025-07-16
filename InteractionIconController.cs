using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class InteractionIconController : MonoBehaviour
{
    [Header("Referências UI")]
    [Tooltip("Ícone pequeno que sempre aparece")]
    public GameObject simpleIcon;
    
    [Tooltip("Prompt completo com botão e texto")]
    public GameObject fullPrompt;
    
    [Tooltip("Texto da ação (ex: 'Pegar')")]
    public TMP_Text actionText;
    
    [Tooltip("Imagem do botão (ex: tecla E)")]
    public Image buttonImage;
    
    [Header("Configurações Visuais")]
    [Tooltip("Fazer o ícone pulsar")]
    public bool enablePulse = false;
    
    [Tooltip("Velocidade da pulsação")]
    public float pulseSpeed = 2f;
    
    [Tooltip("Intensidade da pulsação")]
    public float pulseIntensity = 0.1f;
    
    [Header("Configurações de Escala Dinâmica")]
    [Tooltip("Usar escala dinâmica baseada na distância")]
    public bool useDynamicScale = true;
    
    [Tooltip("Escala mínima quando player está perto")]
    public float minScale = 0.8f;
    
    [Tooltip("Escala máxima quando player está longe")]
    public float maxScale = 1.5f;
    
    [Tooltip("Distância mínima para calcular escala")]
    public float minDistance = 2f;
    
    [Tooltip("Distância máxima para calcular escala")]
    public float maxDistance = 10f;
    
    [Header("Configurações de Escala Fixa")]
    [Tooltip("Escala fixa do ícone simples (quando dinâmica desabilitada)")]
    public float fixedIconScale = 1f;
    
    [Tooltip("Escala fixa do prompt completo")]
    public float fixedPromptScale = 1f;
    
    [Header("Configurações de Transição")]
    [Tooltip("Velocidade da transição do prompt")]
    public float transitionSpeed = 8f;
    
    [Header("Configurações de Layout")]
    [Tooltip("Offset do prompt em relação ao ícone simples")]
    public Vector3 promptOffset = new Vector3(0, -80, 0);
    
    private float pulseTimer = 0f;
    private Vector3 originalPromptPosition;
    private bool isShowingPrompt = false;
    private CanvasGroup promptCanvasGroup;
    private Camera playerCamera;
    
    void Start()
    {
        // Encontrar a câmera do player
        FindPlayerCamera();
        
        // Aplicar escalas iniciais
        if (simpleIcon != null)
        {
            if (useDynamicScale)
            {
                simpleIcon.transform.localScale = Vector3.one * maxScale; // Começar com escala máxima
            }
            else
            {
                simpleIcon.transform.localScale = Vector3.one * fixedIconScale;
            }
        }
        
        // Guardar posição original do prompt
        if (fullPrompt != null)
        {
            originalPromptPosition = fullPrompt.transform.localPosition;
            fullPrompt.transform.localScale = Vector3.one * fixedPromptScale;
        }
        
        // Configurar CanvasGroup para o prompt se não existir
        if (fullPrompt != null)
        {
            promptCanvasGroup = fullPrompt.GetComponent<CanvasGroup>();
            if (promptCanvasGroup == null)
            {
                promptCanvasGroup = fullPrompt.AddComponent<CanvasGroup>();
            }
            promptCanvasGroup.alpha = 0f;
        }
        
        // Garantir que apenas o ícone simples esteja visível inicialmente
        if (simpleIcon != null) simpleIcon.SetActive(true);
        if (fullPrompt != null) fullPrompt.SetActive(false);
    }
    
    void FindPlayerCamera()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerCamera = playerObj.GetComponentInChildren<Camera>();
            }
        }
    }
    
    void Update()
    {
        // Atualizar escala dinâmica baseada na distância
        if (useDynamicScale && playerCamera != null && simpleIcon != null && simpleIcon.activeInHierarchy)
        {
            UpdateDynamicScale();
        }
        
        // Pulsação opcional do ícone simples (aplicada sobre a escala dinâmica)
        if (enablePulse && simpleIcon != null && simpleIcon.activeInHierarchy)
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            float pulseValue = 1f + Mathf.Sin(pulseTimer) * pulseIntensity;
            
            if (useDynamicScale)
            {
                // Aplicar pulsação sobre a escala dinâmica
                float currentDynamicScale = GetDynamicScale();
                simpleIcon.transform.localScale = Vector3.one * currentDynamicScale * pulseValue;
            }
            else
            {
                // Aplicar pulsação sobre a escala fixa
                simpleIcon.transform.localScale = Vector3.one * fixedIconScale * pulseValue;
            }
        }
        else if (!enablePulse && simpleIcon != null && simpleIcon.activeInHierarchy)
        {
            // Aplicar apenas escala sem pulsação
            if (useDynamicScale)
            {
                float currentDynamicScale = GetDynamicScale();
                simpleIcon.transform.localScale = Vector3.one * currentDynamicScale;
            }
            else
            {
                simpleIcon.transform.localScale = Vector3.one * fixedIconScale;
            }
        }
        
        // Suavizar transição do prompt
        if (fullPrompt != null && promptCanvasGroup != null)
        {
            float targetAlpha = isShowingPrompt ? 1f : 0f;
            promptCanvasGroup.alpha = Mathf.Lerp(promptCanvasGroup.alpha, targetAlpha, Time.deltaTime * transitionSpeed);
            
            // Ativar/desativar o prompt baseado na transparência
            if (promptCanvasGroup.alpha < 0.01f && fullPrompt.activeInHierarchy)
            {
                fullPrompt.SetActive(false);
            }
            else if (promptCanvasGroup.alpha > 0.01f && !fullPrompt.activeInHierarchy)
            {
                fullPrompt.SetActive(true);
                // Aplicar offset e escala fixa do prompt quando ativado
                fullPrompt.transform.localPosition = originalPromptPosition + promptOffset;
                fullPrompt.transform.localScale = Vector3.one * fixedPromptScale;
            }
        }
    }
    
    void UpdateDynamicScale()
    {
        if (playerCamera == null)
        {
            FindPlayerCamera(); // Tentar encontrar novamente
            return;
        }
        
        float currentScale = GetDynamicScale();
        
        // Aplicar apenas se não há pulsação ativa
        if (!enablePulse)
        {
            simpleIcon.transform.localScale = Vector3.one * currentScale;
        }
    }
    
    float GetDynamicScale()
    {
        if (playerCamera == null)
        {
            return fixedIconScale;
        }
        
        // Calcular distância do player ao ícone
        float distance = Vector3.Distance(playerCamera.transform.position, transform.position);
        
        // Clampar distância entre min e max
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        
        // Calcular progresso da distância (0 = perto, 1 = longe)
        float distanceProgress = (distance - minDistance) / (maxDistance - minDistance);
        
        // Interpolar entre escala mínima e máxima
        // Quando perto (distanceProgress = 0) = minScale
        // Quando longe (distanceProgress = 1) = maxScale
        float dynamicScale = Mathf.Lerp(minScale, maxScale, distanceProgress);
        
        return dynamicScale;
    }
    
    public void SetActionText(string action)
    {
        if (actionText != null)
        {
            actionText.text = action;
        }
    }
    
    public void ShowPrompt(bool show)
    {
        isShowingPrompt = show;
        
        if (show)
        {
            // Mostrar prompt completo MAS manter ícone simples visível também
            if (simpleIcon != null) simpleIcon.SetActive(true);
            if (fullPrompt != null) fullPrompt.SetActive(true);
        }
        else
        {
            // Mostrar apenas ícone simples
            if (simpleIcon != null) simpleIcon.SetActive(true);
            // O fullPrompt será escondido gradualmente pelo Update()
        }
    }
    
    public void SetButtonImage(Sprite buttonSprite)
    {
        if (buttonImage != null)
        {
            buttonImage.sprite = buttonSprite;
        }
    }
    
    public void SetIconColor(Color color)
    {
        if (simpleIcon != null)
        {
            Image iconImg = simpleIcon.GetComponent<Image>();
            if (iconImg != null)
            {
                iconImg.color = color;
            }
        }
    }
    
    public void SetIconScale(float scale)
    {
        fixedIconScale = scale;
        if (!useDynamicScale && simpleIcon != null)
        {
            simpleIcon.transform.localScale = Vector3.one * fixedIconScale;
        }
    }
    
    public void SetPromptScale(float scale)
    {
        fixedPromptScale = scale;
        if (fullPrompt != null)
        {
            fullPrompt.transform.localScale = Vector3.one * fixedPromptScale;
        }
    }
    
    public void SetDynamicScaleRange(float min, float max)
    {
        minScale = min;
        maxScale = max;
    }
    
    public void SetDistanceRange(float min, float max)
    {
        minDistance = min;
        maxDistance = max;
    }
    
    public void EnableDynamicScale(bool enable)
    {
        useDynamicScale = enable;
        
        // Aplicar escala apropriada imediatamente
        if (simpleIcon != null)
        {
            if (useDynamicScale)
            {
                UpdateDynamicScale();
            }
            else
            {
                simpleIcon.transform.localScale = Vector3.one * fixedIconScale;
            }
        }
    }
    
    void OnDestroy()
    {
        // Cleanup se necessário
    }
}