using UnityEngine;

public class ManualInteractionSystem : MonoBehaviour
{
    [Header("Configurações de Distância")]
    [Tooltip("Distância para mostrar o ícone (sincronizada com PlayerInteraction)")]
    public float iconDistance = 6f;
    
    [Tooltip("Ângulo máximo da câmera para mostrar o ícone")]
    public float maxAngleFromCamera = 60f;
    
    [Header("Referências")]
    [Tooltip("Ícone de interação (filho deste objeto)")]
    public GameObject interactionIcon;
    
    [Tooltip("Texto da ação para este objeto")]
    public string actionText = "Pegar";
    
    [Header("Configurações de HandSlot")]
    [Tooltip("Desativar ícone quando objeto estiver na mão do player")]
    public bool disableWhenInHand = true;
    
    [Header("Debug")]
    [Tooltip("Mostrar informações de debug no Console")]
    public bool showDebugInfo = false;
    
    [Tooltip("Mostrar gizmos de distância")]
    public bool showGizmos = true;

    private Camera playerCamera;
    private InteractionIconController iconController;
    private bool isIconActive = false;
    private bool isPromptActive = false;
    private Transform originalParent;
    private Vector3 originalPosition;
    private bool wasInHand = false;
    private PlayerInteraction playerInteractionRef;
    private bool isPickedUp = false; // Flag específica para controle de estado
    
    void Start()
    {
        // Verificações de segurança iniciais
        if (transform == null)
        {
            Debug.LogError("ManualInteractionSystem: Transform é null!");
            enabled = false;
            return;
        }
        
        // Salvar posição e parent originais
        originalParent = transform.parent;
        originalPosition = transform.position;
        
        // Encontrar referências
        FindPlayerReferences();
        
        // Encontrar o controller do ícone
        if (interactionIcon != null)
        {
            iconController = interactionIcon.GetComponent<InteractionIconController>();
            
            // Garantir que o ícone está desativado no início
            interactionIcon.SetActive(false);
            isIconActive = false;
            isPromptActive = false;
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"ManualInteractionSystem ({gameObject.name}): interactionIcon não foi atribuído!");
            }
        }
        
        // Tentar sincronizar com PlayerInteraction se disponível
        TrySyncWithPlayerInteraction();
    }
    
    void FindPlayerReferences()
    {
        // Encontrar a câmera do jogador
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerCamera = playerObj.GetComponentInChildren<Camera>();
                playerInteractionRef = playerObj.GetComponent<PlayerInteraction>();
            }
        }
        else
        {
            // Se encontrou a câmera, tentar encontrar PlayerInteraction
            playerInteractionRef = playerCamera.GetComponentInParent<PlayerInteraction>();
        }
    }
    
    void TrySyncWithPlayerInteraction()
    {
        if (playerInteractionRef != null)
        {
            // Sincronizar apenas a distância do ícone com PlayerInteraction
            iconDistance = playerInteractionRef.GetIconVisibilityDistance();
            
            if (showDebugInfo)
            {
                Debug.Log($"ManualInteractionSystem ({gameObject.name}): Sincronizado com PlayerInteraction - Ícone: {iconDistance}m");
            }
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning($"ManualInteractionSystem ({gameObject.name}): PlayerInteraction não encontrado para sincronização automática");
        }
    }
    
    // Método público para sincronização manual das distâncias
    public void SyncDistances(float newIconDistance, float unused)
    {
        iconDistance = newIconDistance;
        
        if (showDebugInfo)
        {
            Debug.Log($"ManualInteractionSystem ({gameObject.name}): Distância do ícone atualizada para: {iconDistance}m");
        }
    }
    
    void Update()
    {
        // Verificações de segurança antes de processar
        if (playerCamera == null || interactionIcon == null)
        {
            if (playerCamera == null)
            {
                FindPlayerReferences(); // Tentar encontrar novamente se perdeu a referência
            }
            return;
        }
        
        // Verificar se o objeto está na mão do player usando a nova lógica
        bool isInHand = false;
        try
        {
            isInHand = IsObjectInPlayerHand();
        }
        catch (System.Exception e)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"ManualInteractionSystem ({gameObject.name}): Erro ao verificar se está na mão: {e.Message}");
            }
            return;
        }
        
        // Se deve desativar quando na mão e está na mão, esconder ícone
        if (disableWhenInHand && (isInHand || isPickedUp))
        {
            if (isIconActive)
            {
                HideIcon();
                if (showDebugInfo) Debug.Log($"ManualInteractionSystem ({gameObject.name}): Ícone escondido - objeto na mão");
            }
            wasInHand = true;
            return;
        }
        
        // Se saiu da mão, reativar logica normal
        if (wasInHand && !isInHand && !isPickedUp)
        {
            wasInHand = false;
            if (showDebugInfo) Debug.Log($"ManualInteractionSystem ({gameObject.name}): Objeto saiu da mão, reativando lógica normal");
        }
        
        // Lógica de distância para o ícone
        float distance = Vector3.Distance(playerCamera.transform.position, transform.position);
        bool isInIconRange = distance <= iconDistance;
        bool isInAngle = IsInCameraAngle();
        bool isVisible = isInIconRange && isInAngle && !IsObstructed();
        
        // Determinar se deve mostrar ícone (baseado em distância)
        bool shouldShowIcon = isVisible;
        
        // Determinar se deve mostrar prompt (baseado em raycast direto do player)
        bool shouldShowPrompt = IsInPlayerRaycast();
        
        // Debug info
        if (showDebugInfo && (shouldShowIcon != isIconActive || shouldShowPrompt != isPromptActive))
        {
            Debug.Log($"ManualInteractionSystem ({gameObject.name}): Dist={distance:F1}, IconRange={isInIconRange}, InAngle={isInAngle}, ShowIcon={shouldShowIcon}, InRaycast={shouldShowPrompt}");
        }
        
        // Atualizar estado do ícone
        UpdateIconState(shouldShowIcon, shouldShowPrompt);
    }
    
    bool IsObjectInPlayerHand()
    {
        // Se já foi marcado como pego, retornar true
        if (isPickedUp) return true;
        
        // Verificações de segurança
        if (transform == null) return false;
        
        // Método melhorado: Verificar componentes específicos para determinar se está na mão
        
        // Para Interactable
        Interactable interactable = GetComponent<Interactable>();
        if (interactable != null)
        {
            // Se o rigidbody está kinematic E o collider está desabilitado, provavelmente está na mão
            Rigidbody rb = GetComponent<Rigidbody>();
            Collider col = GetComponent<Collider>();
            
            if (rb != null && rb.isKinematic && col != null && !col.enabled)
            {
                // Verificar se está próximo do player
                if (playerInteractionRef != null)
                {
                    float distanceToPlayer = Vector3.Distance(transform.position, playerInteractionRef.transform.position);
                    if (distanceToPlayer < 2f)
                    {
                        return true;
                    }
                }
            }
        }
        
        // Para LeverHandle
        LeverHandle leverHandle = GetComponent<LeverHandle>();
        if (leverHandle != null)
        {
            // Usar o método IsPickedUp() do próprio LeverHandle
            return leverHandle.IsPickedUp();
        }
        
        // Para LeverBase - NUNCA deve estar na mão
        LeverBase leverBase = GetComponent<LeverBase>();
        if (leverBase != null)
        {
            return false; // Bases nunca estão na mão
        }
        
        // Método de fallback: Verificar se o parent mudou para algo com "Hand" no nome
        if (transform.parent != originalParent)
        {
            if (transform.parent != null)
            {
                string parentName = transform.parent.name.ToLower();
                if (parentName.Contains("hand") || parentName.Contains("slot"))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    bool IsInCameraAngle()
    {
        Vector3 directionToObject = (transform.position - playerCamera.transform.position).normalized;
        float angle = Vector3.Angle(playerCamera.transform.forward, directionToObject);
        return angle <= maxAngleFromCamera;
    }
    
    bool IsObstructed()
    {
        Vector3 directionToObject = (transform.position - playerCamera.transform.position).normalized;
        float distance = Vector3.Distance(playerCamera.transform.position, transform.position);
        
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, directionToObject, out hit, distance))
        {
            return hit.collider.gameObject != gameObject && 
                   !hit.collider.transform.IsChildOf(transform);
        }
        
        return false;
    }
    
    bool IsInPlayerRaycast()
    {
        // Verificações de segurança
        if (playerInteractionRef == null || playerCamera == null || transform == null) 
            return false;
        
        try
        {
            // Usar exatamente o mesmo raycast que o PlayerInteraction usa
            float interactionDistance = playerInteractionRef.GetInteractionDistance();
            RaycastHit hit;
            
            // Usar o centro exato da tela da câmera
            Vector3 screenCenter = new Vector3(playerCamera.pixelWidth * 0.5f, playerCamera.pixelHeight * 0.5f, 0f);
            Ray ray = playerCamera.ScreenPointToRay(screenCenter);
            
            if (Physics.Raycast(ray, out hit, interactionDistance))
            {
                // Verificar se o raycast atingiu este objeto ou um de seus filhos
                return hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform);
            }
        }
        catch (System.Exception e)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"ManualInteractionSystem ({gameObject.name}): Erro no raycast: {e.Message}");
            }
        }
        
        return false;
    }
    
    bool IsLookingDirectly()
    {
        // Método mantido para compatibilidade, mas agora usa IsInPlayerRaycast
        return IsInPlayerRaycast();
    }
    
    void UpdateIconState(bool shouldShowIcon, bool shouldShowPrompt)
    {
        if (shouldShowIcon && !isIconActive)
        {
            ShowIcon();
        }
        else if (!shouldShowIcon && isIconActive)
        {
            HideIcon();
        }
        
        if (isIconActive && iconController != null)
        {
            // Sempre atualizar o texto quando o prompt mudar
            if (shouldShowPrompt != isPromptActive)
            {
                iconController.ShowPrompt(shouldShowPrompt);
                isPromptActive = shouldShowPrompt;
            }
            
            // Garantir que o texto está sempre atualizado
            iconController.SetActionText(actionText);
        }
    }
    
    void ShowIcon()
    {
        if (interactionIcon != null)
        {
            interactionIcon.SetActive(true);
            isIconActive = true;
            
            if (iconController != null)
            {
                iconController.SetActionText(actionText);
            }
            
            if (showDebugInfo) Debug.Log($"ManualInteractionSystem ({gameObject.name}): Ícone mostrado");
        }
    }
    
    void HideIcon()
    {
        if (interactionIcon != null)
        {
            interactionIcon.SetActive(false);
            isIconActive = false;
            isPromptActive = false;
            
            if (showDebugInfo) Debug.Log($"ManualInteractionSystem ({gameObject.name}): Ícone escondido");
        }
    }
    
    public void SetActionText(string newActionText)
    {
        if (actionText != newActionText)
        {
            actionText = newActionText;
            
            // Atualizar imediatamente se o ícone estiver ativo
            if (iconController != null && isIconActive)
            {
                iconController.SetActionText(actionText);
            }
        }
    }
    
    public void ForceShowIcon(bool show)
    {
        if (show)
        {
            ShowIcon();
        }
        else
        {
            HideIcon();
        }
    }
    
    public void OnObjectPickedUp()
    {
        isPickedUp = true; // Marcar como pego
        
        if (disableWhenInHand)
        {
            HideIcon();
            wasInHand = true;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"ManualInteractionSystem ({gameObject.name}): OnObjectPickedUp chamado");
        }
    }
    
    public void OnObjectDropped()
    {
        isPickedUp = false; // Marcar como não pego
        wasInHand = false;
        
        // Verificar se transform ainda é válido antes de usar
        if (transform != null)
        {
            originalPosition = transform.position;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"ManualInteractionSystem ({gameObject.name}): OnObjectDropped chamado");
        }
    }
    
    // Métodos públicos para obter informações
    public float GetIconDistance() { return iconDistance; }
    public bool IsIconCurrentlyActive() { return isIconActive; }
    public bool IsPromptCurrentlyActive() { return isPromptActive; }
    
    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        // Desenhar apenas a esfera de distância do ícone (amarelo)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, iconDistance);
        
        // Label com a distância do ícone
        #if UNITY_EDITOR
        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.Label(transform.position + Vector3.up * (iconDistance + 0.5f), $"Ícone: {iconDistance}m");
        
        // Mostrar que o prompt é por raycast
        UnityEditor.Handles.color = Color.green;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, "Prompt: Por Raycast");
        #endif
    }
}