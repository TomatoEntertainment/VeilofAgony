using UnityEngine;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configurações de Interação")]
    [Tooltip("Distância para interagir com objetos e mostrar prompt")]
    public float interactionDistance = 3f;
    
    [Tooltip("Distância para mostrar ícone de interação (deve ser maior que interactionDistance)")]
    public float iconVisibilityDistance = 6f;
    
    public Camera playerCamera;
    public Transform handSlot;
    
    [Header("UI de Progresso Personalizada")]
    [Tooltip("O objeto PAI que contém toda a UI de progresso.")]
    public GameObject progressHolder;
    [Tooltip("A imagem que será movida para o efeito de sangue (Blood_Moving).")]
    public RectTransform movingFillRect;

    [Header("Debug Visual")]
    [Tooltip("Mostrar gizmos de distância no Scene View")]
    public bool showDistanceGizmos = true;
    
    [Tooltip("Cor do gizmo da distância de interação")]
    public Color interactionGizmoColor = Color.green;
    
    [Tooltip("Cor do gizmo de visibilidade do ícone")]
    public Color iconGizmoColor = Color.yellow;

    private PlayerHealth playerHealth;
    private TorchController torchController;
    private Animator playerAnimator;
    private Interactable heldItem = null;
    private LeverHandle heldLever = null;
    private Altar currentAltar = null;
    private bool isInteractingWithAltar = false;
    private bool isUsingItem = false;
    private float lastInteractionDistance;
    private float lastIconDistance;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        torchController = GetComponent<TorchController>();
        playerAnimator = GetComponent<Animator>();
        if (progressHolder != null) progressHolder.SetActive(false);
        
        lastInteractionDistance = interactionDistance;
        lastIconDistance = iconVisibilityDistance;
        
        // Validar e sincronizar distâncias
        ValidateDistances();
        SynchronizeInteractionDistances();
    }
    
    void Update()
    {
        // Verificar se as distâncias mudaram e sincronizar automaticamente
        if (!Mathf.Approximately(lastInteractionDistance, interactionDistance) ||
            !Mathf.Approximately(lastIconDistance, iconVisibilityDistance))
        {
            ValidateDistances();
            SynchronizeInteractionDistances();
            lastInteractionDistance = interactionDistance;
            lastIconDistance = iconVisibilityDistance;
        }
        
        if (!isUsingItem && !isInteractingWithAltar) HandleRaycastDetection();
        HandleInput();
    }
    
    void ValidateDistances()
    {
        // Garantir que a distância do ícone seja maior que a de interação
        if (iconVisibilityDistance <= interactionDistance)
        {
            iconVisibilityDistance = interactionDistance * 2f;
            Debug.LogWarning($"PlayerInteraction: iconVisibilityDistance ajustada para {iconVisibilityDistance} (deve ser maior que interactionDistance)");
        }
    }
    
    void SynchronizeInteractionDistances()
    {
        // Encontrar todos os ManualInteractionSystem na cena e sincronizar suas distâncias
        ManualInteractionSystem[] allManualSystems = FindObjectsOfType<ManualInteractionSystem>();
        
        foreach (ManualInteractionSystem manualSystem in allManualSystems)
        {
            // Apenas iconVisibilityDistance para ícone, prompt é por raycast
            manualSystem.SyncDistances(iconVisibilityDistance, 0f);
        }
        
        Debug.Log($"PlayerInteraction: {allManualSystems.Length} sistemas sincronizados - Ícone: {iconVisibilityDistance}m, Prompt: Por Raycast");
    }
    
    // Método público para atualizar distâncias em runtime
    public void UpdateInteractionDistances(float newInteractionDistance, float newIconDistance = -1f)
    {
        interactionDistance = Mathf.Max(newInteractionDistance, 0.5f);
        
        if (newIconDistance > 0f)
        {
            iconVisibilityDistance = newIconDistance;
        }
        else
        {
            iconVisibilityDistance = interactionDistance * 2f;
        }
        
        ValidateDistances();
        SynchronizeInteractionDistances();
    }

    void HandleRaycastDetection()
    {
        RaycastHit hit;
        currentAltar = null;

        // Usar o centro exato da tela da câmera
        Vector3 screenCenter = new Vector3(playerCamera.pixelWidth * 0.5f, playerCamera.pixelHeight * 0.5f, 0f);
        Ray ray = playerCamera.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            Altar altar = hit.collider.GetComponent<Altar>();
            if (altar != null && torchController != null)
            {
                bool podeAcenderPelaPrimeiraVez = torchController.CurrentState == TorchController.TorchState.Unlit;
                bool podeRecarregar = torchController.CurrentState == TorchController.TorchState.Lit && torchController.CurrentLitLevel > 0;

                if (podeAcenderPelaPrimeiraVez || podeRecarregar)
                {
                    currentAltar = altar;
                }
            }
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.E) && !isInteractingWithAltar)
        {
            RaycastHit hit;
            
            // Usar o centro exato da tela da câmera
            Vector3 screenCenter = new Vector3(playerCamera.pixelWidth * 0.5f, playerCamera.pixelHeight * 0.5f, 0f);
            Ray ray = playerCamera.ScreenPointToRay(screenCenter);
            
            if (Physics.Raycast(ray, out hit, interactionDistance))
            {
                // TorchPickup
                if (hit.collider.GetComponent<TorchPickup>() != null) 
                { 
                    hit.collider.GetComponent<TorchPickup>().Interact(torchController); 
                    return; 
                }
                
                // LeverHandle (alavanca solta)
                LeverHandle leverHandle = hit.collider.GetComponent<LeverHandle>();
                if (leverHandle != null && leverHandle.CanPickup() && heldItem == null && heldLever == null)
                {
                    PickupLever(leverHandle);
                    return;
                }
                
                // LeverBase (base para alavanca)
                LeverBase leverBase = hit.collider.GetComponent<LeverBase>();
                if (leverBase != null)
                {
                    if (!leverBase.HasLeverAttached() && heldLever != null)
                    {
                        // Anexar alavanca na base
                        if (leverBase.GetLeverID() == heldLever.GetLeverID())
                        {
                            if (leverBase.TryAttachLever(heldLever))
                            {
                                heldLever = null;
                            }
                        }
                        else
                        {
                            Debug.Log($"Alavanca {heldLever.GetLeverName()} não é compatível com esta base!");
                        }
                    }
                    else if (leverBase.HasLeverAttached())
                    {
                        // Usar alavanca através da base
                        leverBase.ActivateLever();
                    }
                    return;
                }
                
                // LeverHandle anexada (usar alavanca)
                if (leverHandle != null && leverHandle.IsAttachedToBase())
                {
                    if (leverHandle.CanUse())
                    {
                        leverHandle.UseLever();
                    }
                    else
                    {
                        Debug.Log("Alavanca não está disponível para uso!");
                    }
                    return;
                }
                
                // Interactable normal
                if (hit.collider.GetComponent<Interactable>() != null && heldItem == null && heldLever == null) 
                { 
                    PickupItem(hit.collider.GetComponent<Interactable>()); 
                    return; 
                }
            }
        }
        
        // Usar bandagem
        if (heldItem != null && heldItem.itemType == Interactable.ItemType.Healing)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                isUsingItem = true;
                if(playerAnimator) playerAnimator.SetBool("isHealing", true);
                heldItem.StartUse(progressHolder, movingFillRect);
            }
            if (isUsingItem)
            {
                if (Input.GetKey(KeyCode.R))
                {
                    if (heldItem.UpdateUse(playerHealth)) 
                    { 
                        heldItem = null; 
                        isUsingItem = false; 
                        if(playerAnimator) playerAnimator.SetBool("isHealing", false); 
                    }
                }
                if (Input.GetKeyUp(KeyCode.R)) 
                { 
                    isUsingItem = false; 
                    if(playerAnimator) playerAnimator.SetBool("isHealing", false); 
                    heldItem.EndUse(); 
                }
            }
        }
        
        // Largar itens
        if (Input.GetKeyDown(KeyCode.Q) && !isUsingItem)
        {
            if (heldItem != null)
            {
                DropItem();
            }
            else if (heldLever != null)
            {
                DropLever();
            }
        }
        
        // Interagir com altar
        if (currentAltar != null && !isUsingItem)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                isInteractingWithAltar = true;
                currentAltar.StartInteraction(progressHolder, movingFillRect);
            }
            if (isInteractingWithAltar)
            {
                if (Input.GetKey(KeyCode.E)) 
                { 
                    currentAltar.UpdateInteraction(torchController); 
                }
                if (Input.GetKeyUp(KeyCode.E)) 
                { 
                    isInteractingWithAltar = false; 
                    currentAltar.EndInteraction(); 
                }
            }
        }
        else if (isInteractingWithAltar) 
        { 
            isInteractingWithAltar = false; 
        }
    }
    
    void PickupItem(Interactable item) 
    { 
        heldItem = item; 
        item.Pickup(handSlot); 
    }
    
    void DropItem() 
    { 
        heldItem.Drop(); 
        heldItem = null; 
    }
    
    void PickupLever(LeverHandle lever)
    {
        heldLever = lever;
        lever.Pickup(handSlot);
    }
    
    void DropLever()
    {
        heldLever.Drop();
        heldLever = null;
    }
    
    // Desenhar gizmos para visualizar as distâncias
    void OnDrawGizmos()
    {
        if (!showDistanceGizmos) return;
        
        // Desenhar esfera da distância de interação (verde)
        Gizmos.color = interactionGizmoColor;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
        
        // Desenhar esfera da distância de visibilidade do ícone (amarelo, mais transparente)
        Color iconColor = iconGizmoColor;
        iconColor.a = 0.3f;
        Gizmos.color = iconColor;
        Gizmos.DrawWireSphere(transform.position, iconVisibilityDistance);
        
        // Desenhar raycast da câmera quando em play mode
        if (Application.isPlaying && playerCamera != null)
        {
            // Usar o centro exato da tela da câmera
            Vector3 screenCenter = new Vector3(playerCamera.pixelWidth * 0.5f, playerCamera.pixelHeight * 0.5f, 0f);
            Ray ray = playerCamera.ScreenPointToRay(screenCenter);
            
            // Raycast de interação (linha sólida verde)
            Gizmos.color = interactionGizmoColor;
            Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * interactionDistance);
            
            // Indicar ponto final do raycast
            Gizmos.DrawWireSphere(ray.origin + ray.direction * interactionDistance, 0.1f);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showDistanceGizmos) return;
        
        // Círculo no chão para distância de interação
        Gizmos.color = interactionGizmoColor;
        DrawGroundCircle(transform.position, interactionDistance);
        
        // Círculo no chão para distância de ícone
        Color iconColor = iconGizmoColor;
        iconColor.a = 0.5f;
        Gizmos.color = iconColor;
        DrawGroundCircle(transform.position, iconVisibilityDistance);
        
        // Labels com as distâncias (apenas no editor)
        #if UNITY_EDITOR
        UnityEditor.Handles.color = interactionGizmoColor;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"Interação: {interactionDistance}m");
        
        UnityEditor.Handles.color = iconGizmoColor;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, $"Ícone: {iconVisibilityDistance}m");
        #endif
    }
    
    void DrawGroundCircle(Vector3 center, float radius)
    {
        Vector3 prevPos = Vector3.zero;
        for (int i = 0; i <= 360; i += 10)
        {
            float angle = i * Mathf.Deg2Rad;
            Vector3 newPos = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            
            if (i > 0)
            {
                Gizmos.DrawLine(prevPos, newPos);
            }
            prevPos = newPos;
        }
    }
    
    // Métodos públicos para obter as distâncias
    public float GetInteractionDistance()
    {
        return interactionDistance;
    }
    
    public float GetIconVisibilityDistance()
    {
        return iconVisibilityDistance;
    }
}