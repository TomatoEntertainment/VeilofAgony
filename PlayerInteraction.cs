using UnityEngine;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configurações de Interação")]
    public float interactionDistance = 3f;
    public Camera playerCamera;
    public Transform handSlot;
    
    [Header("UI de Progresso Personalizada")]
    [Tooltip("O objeto PAI que contém toda a UI de progresso.")]
    public GameObject progressHolder;
    [Tooltip("A imagem que será movida para o efeito de sangue (Blood_Moving).")]
    public RectTransform movingFillRect;

    private PlayerHealth playerHealth;
    private TorchController torchController;
    private Animator playerAnimator;
    private Interactable heldItem = null;
    private LeverHandle heldLever = null;
    private Altar currentAltar = null;
    private bool isInteractingWithAltar = false;
    private bool isUsingItem = false;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        torchController = GetComponent<TorchController>();
        playerAnimator = GetComponent<Animator>();
        if (progressHolder != null) progressHolder.SetActive(false);
    }

    void Update()
    {
        if (!isUsingItem && !isInteractingWithAltar) HandleRaycastDetection();
        HandleInput();
        // Removido UpdateAltarActionText() - textos agora são manuais
    }
    
    void HandleRaycastDetection()
    {
        RaycastHit hit;
        currentAltar = null;

        if (Physics.Raycast(playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2)), out hit, interactionDistance))
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
            if (Physics.Raycast(playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2)), out hit, interactionDistance))
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
                        // Em vez de ativar via base, pegar a alavanca e ativá-la diretamente
                        LeverHandle attachedLever = leverBase.GetAttachedLever();
                        if (attachedLever != null && attachedLever.CanUse())
                        {
                            attachedLever.UseLever();
                        }
                        else
                        {
                            Debug.Log("Alavanca não está disponível para uso!");
                        }
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
}