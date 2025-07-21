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
    private PlayerAnimationController animationController; // NOVA REFERÊNCIA
    
    // Itens na mão
    private Interactable heldItem = null;
    private LeverHandle heldLever = null;
    private TotemPickup heldTotem = null;
    private OilJar heldOilJar = null;
    
    // Objetos interagíveis em foco
    private Altar currentAltar = null;
    private TotemAltar currentTotemAltar = null;
    
    // Estados de interação
    private bool isInteractingWithAltar = false;
    private bool isInteractingWithTotemAltar = false;
    private bool isUsingItem = false;
    
    // Controle de distâncias
    private float lastInteractionDistance;
    private float lastIconDistance;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        torchController = GetComponent<TorchController>();
        animationController = GetComponent<PlayerAnimationController>(); // NOVA LINHA
        
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
        
        if (!isUsingItem && !isInteractingWithAltar && !isInteractingWithTotemAltar) 
            HandleRaycastDetection();
            
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
        currentTotemAltar = null;

        // Usar o centro exato da tela da câmera
        Vector3 screenCenter = new Vector3(playerCamera.pixelWidth * 0.5f, playerCamera.pixelHeight * 0.5f, 0f);
        Ray ray = playerCamera.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            // Detectar Altar normal
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
            
            // Detectar TotemAltar - MELHORADA A DETECÇÃO
            TotemAltar totemAltar = hit.collider.GetComponent<TotemAltar>();
            
            // Se não encontrou diretamente, verificar se é um filho de um TotemAltar
            if (totemAltar == null)
            {
                totemAltar = hit.collider.GetComponentInParent<TotemAltar>();
            }
            
            if (totemAltar != null && !totemAltar.IsBurned())
            {
                currentTotemAltar = totemAltar;
            }
        }
    }

    void HandleInput()
    {
        // === INTERAÇÃO COM OBJETOS (PRESSIONAR E) ===
        if (Input.GetKeyDown(KeyCode.E) && !isInteractingWithAltar && !isInteractingWithTotemAltar)
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
                
                // OilJar - Pegar jarro de óleo
                OilJar oilJar = hit.collider.GetComponent<OilJar>();
                if (oilJar != null && oilJar.CanPickup() && !HasAnyItemInHand())
                {
                    PickupOilJar(oilJar);
                    return;
                }
                
                // TotemPickup - Pegar totem
                TotemPickup totemPickup = hit.collider.GetComponent<TotemPickup>();
                if (totemPickup != null && totemPickup.CanPickup() && !HasAnyItemInHand())
                {
                    PickupTotem(totemPickup);
                    return;
                }
                
                // NOVA ADIÇÃO: TotemAltar - Colocar totem quando estiver com totem na mão
                TotemAltar totemAltarInteract = hit.collider.GetComponent<TotemAltar>();
                
                // Se não encontrou diretamente, verificar se é um filho de um TotemAltar
                if (totemAltarInteract == null)
                {
                    totemAltarInteract = hit.collider.GetComponentInParent<TotemAltar>();
                }
                
                if (totemAltarInteract != null && !totemAltarInteract.IsBurned())
                {
                    // Colocar totem no altar se tiver totem na mão
                    if (!totemAltarInteract.HasTotem() && heldTotem != null)
                    {
                        if (totemAltarInteract.TryPlaceTotem(heldTotem))
                        {
                            heldTotem = null;
                        }
                        return;
                    }
                    // Ações de segurar E serão processadas abaixo para outras interações
                }
                
                // LeverHandle (alavanca solta)
                LeverHandle leverHandle = hit.collider.GetComponent<LeverHandle>();
                if (leverHandle != null && leverHandle.CanPickup() && !HasAnyItemInHand())
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
                if (hit.collider.GetComponent<Interactable>() != null && !HasAnyItemInHand()) 
                { 
                    PickupItem(hit.collider.GetComponent<Interactable>()); 
                    return; 
                }
            }
        }
        
        // === USAR BANDAGEM (SEGURAR R) ===
        if (heldItem != null && heldItem.itemType == Interactable.ItemType.Healing)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                isUsingItem = true;
                // MUDANÇA: Usar o novo sistema de animações
                if(animationController) animationController.StartHealing();
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
                        // MUDANÇA: Usar o novo sistema de animações
                        if(animationController) animationController.StopHealing(); 
                    }
                }
                if (Input.GetKeyUp(KeyCode.R)) 
                { 
                    isUsingItem = false; 
                    // MUDANÇA: Usar o novo sistema de animações
                    if(animationController) animationController.StopHealing(); 
                    heldItem.EndUse(); 
                }
            }
        }
        
        // === LARGAR ITENS (PRESSIONAR Q) ===
        if (Input.GetKeyDown(KeyCode.Q) && !isUsingItem && !isInteractingWithAltar && !isInteractingWithTotemAltar)
        {
            if (heldItem != null)
            {
                DropItem();
            }
            else if (heldLever != null)
            {
                DropLever();
            }
            else if (heldTotem != null)
            {
                DropTotem();
            }
            else if (heldOilJar != null)
            {
                DropOilJar();
            }
        }
        
        // === INTERAGIR COM ALTAR NORMAL (SEGURAR E) ===
        if (currentAltar != null && !isUsingItem && !isInteractingWithTotemAltar)
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
        
        // === INTERAGIR COM TOTEM ALTAR (SEGURAR E) ===
        if (currentTotemAltar != null && !isUsingItem && !isInteractingWithAltar)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (currentTotemAltar.HasTotem() && !currentTotemAltar.HasOil() && heldOilJar != null)
                {
                    // Iniciar derramamento de óleo
                    isInteractingWithTotemAltar = true;
                    currentTotemAltar.StartPouringOil(progressHolder, movingFillRect, heldOilJar);
                }
                else if (currentTotemAltar.HasTotem() && currentTotemAltar.HasOil() && !currentTotemAltar.IsBurned())
                {
                    // Iniciar queima
                    isInteractingWithTotemAltar = true;
                    currentTotemAltar.StartBurning(progressHolder, movingFillRect);
                }
                else if (currentTotemAltar.HasTotem() && !currentTotemAltar.HasOil() && heldOilJar == null)
                {
                    Debug.Log("Precisa de um jarro de óleo para derramar no altar!");
                }
            }
            
            if (isInteractingWithTotemAltar)
            {
                if (Input.GetKey(KeyCode.E))
                {
                    if (currentTotemAltar.IsPouring())
                    {
                        currentTotemAltar.UpdatePouringOil(progressHolder, movingFillRect, heldOilJar);
                        // Se completou o derramamento, o jarro será destruído pelo TotemAltar
                        if (!currentTotemAltar.IsPouring() && heldOilJar != null)
                        {
                            heldOilJar = null;
                        }
                    }
                    else if (currentTotemAltar.IsBurning())
                    {
                        currentTotemAltar.UpdateBurning(progressHolder, movingFillRect);
                    }
                }
                
                if (Input.GetKeyUp(KeyCode.E))
                {
                    isInteractingWithTotemAltar = false;
                    if (currentTotemAltar.IsPouring())
                    {
                        currentTotemAltar.CancelPouringOil(progressHolder);
                    }
                    else if (currentTotemAltar.IsBurning())
                    {
                        currentTotemAltar.CancelBurning(progressHolder);
                    }
                }
            }
        }
        else if (isInteractingWithTotemAltar)
        {
            isInteractingWithTotemAltar = false;
        }
    }
    
    // === MÉTODOS AUXILIARES PARA PEGAR ITENS ===
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
    
    void PickupTotem(TotemPickup totem)
    {
        heldTotem = totem;
        totem.Pickup(handSlot);
    }
    
    void DropTotem()
    {
        heldTotem.Drop();
        heldTotem = null;
    }
    
    void PickupOilJar(OilJar jar)
    {
        heldOilJar = jar;
        jar.Pickup(handSlot);
    }
    
    void DropOilJar()
    {
        heldOilJar.Drop();
        heldOilJar = null;
    }
    
    bool HasAnyItemInHand()
    {
        return heldItem != null || heldLever != null || heldTotem != null || heldOilJar != null;
    }
    
    // === DESENHAR GIZMOS PARA VISUALIZAÇÃO ===
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
    
    // === MÉTODOS PÚBLICOS PARA OBTER AS DISTÂNCIAS ===
    public float GetInteractionDistance()
    {
        return interactionDistance;
    }
    
    public float GetIconVisibilityDistance()
    {
        return iconVisibilityDistance;
    }
}