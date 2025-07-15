using UnityEngine;

public class ManualInteractionSystem : MonoBehaviour
{
    [Header("Configurações de Distância")]
    [Tooltip("Distância para mostrar o ícone simples")]
    public float iconDistance = 8f;
    
    [Tooltip("Distância para mostrar o prompt completo")]
    public float promptDistance = 3f;
    
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
    
    private Camera playerCamera;
    private InteractionIconController iconController;
    private bool isIconActive = false;
    private bool isPromptActive = false;
    private Transform originalParent;
    private Vector3 originalPosition;
    private bool wasInHand = false;
    
    void Start()
    {
        // Salvar posição e parent originais
        originalParent = transform.parent;
        originalPosition = transform.position;
        
        // Encontrar a câmera do jogador
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerCamera = playerObj.GetComponentInChildren<Camera>();
            }
        }
        
        // Encontrar o controller do ícone
        if (interactionIcon != null)
        {
            iconController = interactionIcon.GetComponent<InteractionIconController>();
            
            // Garantir que o ícone está desativado no início
            interactionIcon.SetActive(false);
            isIconActive = false;
            isPromptActive = false;
        }
    }
    
    void Update()
    {
        if (playerCamera == null || interactionIcon == null) return;
        
        // Verificar se o objeto está na mão do player
        bool isInHand = IsObjectInPlayerHand();
        
        // Se deve desativar quando na mão e está na mão, esconder ícone
        if (disableWhenInHand && isInHand)
        {
            if (isIconActive)
            {
                HideIcon();
            }
            wasInHand = true;
            return;
        }
        
        // Se saiu da mão, reativar logica normal
        if (wasInHand && !isInHand)
        {
            wasInHand = false;
        }
        
        // Lógica normal de distância e visibilidade
        float distance = Vector3.Distance(playerCamera.transform.position, transform.position);
        bool isInRange = distance <= iconDistance;
        bool isInAngle = IsInCameraAngle();
        bool isVisible = isInRange && isInAngle && !IsObstructed();
        
        // Determinar se deve mostrar ícone
        bool shouldShowIcon = isVisible;
        
        // Determinar se deve mostrar prompt completo
        bool shouldShowPrompt = isVisible && distance <= promptDistance && IsLookingDirectly();
        
        // Atualizar estado do ícone
        UpdateIconState(shouldShowIcon, shouldShowPrompt);
    }
    
    bool IsObjectInPlayerHand()
    {
        // Método 1: Verificar se o parent mudou para algo com "Hand" no nome
        if (transform.parent != originalParent)
        {
            string parentName = transform.parent.name.ToLower();
            if (parentName.Contains("hand") || parentName.Contains("slot"))
            {
                return true;
            }
        }
        
        // Método 2: Verificar se está muito longe da posição original
        float distanceFromOriginal = Vector3.Distance(transform.position, originalPosition);
        if (distanceFromOriginal > 2f)
        {
            PlayerInteraction playerInteraction = FindObjectOfType<PlayerInteraction>();
            if (playerInteraction != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, playerInteraction.transform.position);
                if (distanceToPlayer < 3f)
                {
                    return true;
                }
            }
        }
        
        // Método 3: Verificar se o Rigidbody está kinematic
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && rb.isKinematic)
        {
            PlayerInteraction playerInteraction = FindObjectOfType<PlayerInteraction>();
            if (playerInteraction != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, playerInteraction.transform.position);
                if (distanceToPlayer < 2f)
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
    
    bool IsLookingDirectly()
    {
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2)), out hit, promptDistance))
        {
            return hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform);
        }
        
        return false;
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
            if (shouldShowPrompt != isPromptActive)
            {
                iconController.ShowPrompt(shouldShowPrompt);
                iconController.SetActionText(actionText);
                isPromptActive = shouldShowPrompt;
            }
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
        }
    }
    
    void HideIcon()
    {
        if (interactionIcon != null)
        {
            interactionIcon.SetActive(false);
            isIconActive = false;
            isPromptActive = false;
        }
    }
    
    public void SetActionText(string newActionText)
    {
        actionText = newActionText;
        if (iconController != null && isIconActive)
        {
            iconController.SetActionText(actionText);
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
        if (disableWhenInHand)
        {
            HideIcon();
            wasInHand = true;
        }
    }
    
    public void OnObjectDropped()
    {
        wasInHand = false;
        originalPosition = transform.position;
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, iconDistance);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, promptDistance);
    }
}