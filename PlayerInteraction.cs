using UnityEngine;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configurações de Interação")]
    public float interactionDistance = 3f;
    public Camera playerCamera;
    public Transform handSlot;
    public Image interactionIcon;
    
    [Header("UI de Progresso Personalizada")]
    [Tooltip("O objeto PAI que contém toda a UI de progresso.")]
    public GameObject progressHolder;
    [Tooltip("A imagem que será movida para o efeito de sangue (Blood_Moving).")]
    public RectTransform movingFillRect;

    private PlayerHealth playerHealth;
    private TorchController torchController;
    private Animator playerAnimator;
    private Interactable heldItem = null;
    private Altar currentAltar = null;
    private bool isInteractingWithAltar = false;
    private bool isUsingItem = false;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        torchController = GetComponent<TorchController>();
        playerAnimator = GetComponent<Animator>();
        if (interactionIcon != null) interactionIcon.enabled = false;
        if (progressHolder != null) progressHolder.SetActive(false);
    }

    void Update()
    {
        if (!isUsingItem && !isInteractingWithAltar) HandleRaycastDetection();
        HandleInput();
    }
    
    void HandleRaycastDetection()
    {
        RaycastHit hit;
        bool canInteract = false;
        
        // Limpa a referência do altar a cada frame para evitar interações fantasma.
        currentAltar = null;

        if (Physics.Raycast(playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2)), out hit, interactionDistance))
        {
            // --- Lógica de Deteção de Altar (ATUALIZADA) ---
            Altar altar = hit.collider.GetComponent<Altar>();
            if (altar != null && torchController != null)
            {
                // Condição 1: A tocha está na mão, mas apagada.
                bool podeAcenderPelaPrimeiraVez = torchController.CurrentState == TorchController.TorchState.Unlit;
                
                // Condição 2: A tocha está acesa, mas já não está no nível 1 (índice 0).
                bool podeRecarregar = torchController.CurrentState == TorchController.TorchState.Lit && torchController.CurrentLitLevel > 0;

                if (podeAcenderPelaPrimeiraVez || podeRecarregar)
                {
                    canInteract = true;
                    currentAltar = altar;
                }
            }
            // --- Fim da Lógica de Altar ---

            else if (hit.collider.GetComponent<TorchPickup>() != null) 
            {
                canInteract = true; 
            }
            else if (hit.collider.GetComponent<Interactable>() != null && heldItem == null) 
            {
                canInteract = true; 
            }
        }
        
        if (interactionIcon != null)
        {
            interactionIcon.enabled = canInteract;
        }
    }

    void HandleInput()
    {
        // --- LÓGICA DE APANHAR ITENS (Tecla E) ---
        if (Input.GetKeyDown(KeyCode.E) && !isInteractingWithAltar)
        {
            RaycastHit hit;
            if (Physics.Raycast(playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2)), out hit, interactionDistance))
            {
                if (hit.collider.GetComponent<TorchPickup>() != null) { hit.collider.GetComponent<TorchPickup>().Interact(torchController); if (interactionIcon != null) interactionIcon.enabled = false; return; }
                if (hit.collider.GetComponent<Interactable>() != null && heldItem == null) { PickupItem(hit.collider.GetComponent<Interactable>()); if (interactionIcon != null) interactionIcon.enabled = false; return; }
            }
        }
        
        // --- LÓGICA DE USAR A BANDAGEM (Segurar R) ---
        if (heldItem != null && heldItem.itemType == Interactable.ItemType.Healing)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                isUsingItem = true;
                if(playerAnimator) playerAnimator.SetBool("isHealing", true);

                // --- LINHA ADICIONADA DE VOLTA ---
                if (interactionIcon != null) interactionIcon.enabled = false;

                heldItem.StartUse(progressHolder, movingFillRect);
            }
            if (isUsingItem)
            {
                if (Input.GetKey(KeyCode.R))
                {
                    if (heldItem.UpdateUse(playerHealth)) { heldItem = null; isUsingItem = false; if(playerAnimator) playerAnimator.SetBool("isHealing", false); }
                }
                if (Input.GetKeyUp(KeyCode.R)) { isUsingItem = false; if(playerAnimator) playerAnimator.SetBool("isHealing", false); heldItem.EndUse(); }
            }
        }
        
        // --- LÓGICA DE LARGAR ITENS (Tecla Q) ---
        if (Input.GetKeyDown(KeyCode.Q) && heldItem != null && !isUsingItem) DropItem();
        
        // --- LÓGICA DE INTERAGIR COM O ALTAR (Segurar E) ---
        if (currentAltar != null && !isUsingItem)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                isInteractingWithAltar = true;

                // --- LINHA ADICIONADA DE VOLTA ---
                if (interactionIcon != null) interactionIcon.enabled = false;

                currentAltar.StartInteraction(progressHolder, movingFillRect);
            }
            if (isInteractingWithAltar)
            {
                if (Input.GetKey(KeyCode.E)) { currentAltar.UpdateInteraction(torchController); }
                if (Input.GetKeyUp(KeyCode.E)) { isInteractingWithAltar = false; currentAltar.EndInteraction(); }
            }
        }
        else if (isInteractingWithAltar) { isInteractingWithAltar = false; }
    }
    
    void PickupItem(Interactable item) { heldItem = item; item.Pickup(handSlot); }
    void DropItem() { heldItem.Drop(); heldItem = null; }
}