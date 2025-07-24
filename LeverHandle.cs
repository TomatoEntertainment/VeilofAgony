using UnityEngine;

public class LeverHandle : MonoBehaviour
{
    [Header("Configurações da Alavanca")]
    [Tooltip("ID único desta alavanca (deve ser igual ao da base)")]
    public string leverID = "lever_01";
    
    [Tooltip("Nome da alavanca para exibição")]
    public string leverName = "Alavanca do Portão";
    
    [Header("Textos de Interação")]
    [Tooltip("Texto quando a alavanca está solta")]
    public string pickupText = "Pegar Alavanca";
    
    [Tooltip("Texto quando a alavanca está encaixada")]
    public string useText = "Usar Alavanca";
    
    [Header("Configuração na Mão do Jogador")]
    public Vector3 heldPosition;
    public Vector3 heldRotation;
    
    [Header("Configurações de Animação")]
    [Tooltip("Ângulo de rotação quando alavanca é puxada")]
    public float pullAngle = 45f;
    
    [Tooltip("Velocidade da animação da alavanca")]
    public float animationSpeed = 5f;
    
    [Tooltip("Tempo que a alavanca fica puxada antes de voltar")]
    public float holdTime = 2f;
    
    [Tooltip("Eixo de rotação da alavanca (local)")]
    public Vector3 rotationAxis = Vector3.right;
    
    [Header("Configuração Inicial")]
    [Tooltip("Se esta alavanca deve começar anexada à base no início do jogo")]
    public bool startAttachedToBase = false;
    
    [Tooltip("Base onde esta alavanca deve ser anexada (se startAttachedToBase = true)")]
    public LeverBase initialBase;
    
    [Header("Estado")]
    [SerializeField] private bool isPickedUp = false;
    [SerializeField] private bool isAttachedToBase = false;
    [SerializeField] private bool isActivated = false;
    [SerializeField] private bool isAnimating = false;
    [SerializeField] private bool isReturning = false;
    
    private Rigidbody rb;
    private Collider col;
    private LeverBase currentBase;
    private Quaternion originalRotation;
    private Quaternion pulledRotation;
    private float holdTimer = 0f;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }
    
    void Start()
    {
        ManualInteractionSystem manualSystem = GetComponent<ManualInteractionSystem>();
        if (manualSystem != null)
        {
            manualSystem.actionText = pickupText;
        }
        
        if (startAttachedToBase && initialBase != null)
        {
            AttachToBaseAtStart();
        }
    }
    
    void AttachToBaseAtStart()
    {
        if (initialBase.GetLeverID() != leverID)
        {
            Debug.LogError($"ERRO: Alavanca {leverName} (ID: {leverID}) não é compatível com base {initialBase.baseName} (ID: {initialBase.GetLeverID()})!");
            return;
        }
        
        isAttachedToBase = true;
        isPickedUp = false;
        currentBase = initialBase;
        
        transform.SetParent(initialBase.GetAttachPoint());
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        
        originalRotation = transform.localRotation;
        pulledRotation = originalRotation * Quaternion.AngleAxis(pullAngle, rotationAxis);
        
        if (rb != null) rb.isKinematic = true;
        if (col != null) col.enabled = false;
        
        // DELETAR COMPLETAMENTE o ícone da alavanca quando já começa anexada
        DeleteInteractionIcon();
        
        initialBase.SetAttachedLeverAtStart(this);
        
        Debug.Log($"Alavanca {leverName} anexada automaticamente à base {initialBase.baseName} no início do jogo.");
    }
    
    void Update()
    {
        if (isAnimating)
        {
            AnimateLever();
        }
        
        if (isActivated && !isReturning && !isAnimating)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= holdTime)
            {
                StartReturn();
            }
        }
    }
    
    void AnimateLever()
    {
        if (!isAttachedToBase) return;
        
        Quaternion targetRotation = isReturning ? originalRotation : pulledRotation;
        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * animationSpeed);
        
        if (Quaternion.Angle(transform.localRotation, targetRotation) < 1f)
        {
            transform.localRotation = targetRotation;
            isAnimating = false;
            
            if (isReturning)
            {
                isActivated = false;
                isReturning = false;
                holdTimer = 0f;
                
                Debug.Log($"Alavanca {leverName} voltou à posição normal e está disponível novamente.");
            }
            else
            {
                Debug.Log($"Alavanca {leverName} puxada. Aguardando {holdTime} segundos para voltar.");
            }
        }
    }
    
    public bool CanPickup()
    {
        return !isPickedUp && !isAttachedToBase;
    }
    
    public void Pickup(Transform handSlot)
    {
        if (!CanPickup()) return;
        
        isPickedUp = true;
        
        transform.SetParent(handSlot);
        transform.localPosition = heldPosition;
        transform.localEulerAngles = heldRotation;
        
        if (rb != null) rb.isKinematic = true;
        if (col != null) col.enabled = false;
        
        ManualInteractionSystem manualSystem = GetComponent<ManualInteractionSystem>();
        if (manualSystem != null)
        {
            manualSystem.OnObjectPickedUp();
        }
        
        Debug.Log($"Alavanca {leverName} pega pelo jogador");
    }
    
    public void Drop()
    {
        if (!isPickedUp) return;
        
        isPickedUp = false;
        
        transform.SetParent(null);
        
        if (rb != null) rb.isKinematic = false;
        if (col != null) col.enabled = true;
        
        ManualInteractionSystem manualSystem = GetComponent<ManualInteractionSystem>();
        if (manualSystem != null)
        {
            manualSystem.OnObjectDropped();
        }
        
        Debug.Log($"Alavanca {leverName} largada pelo jogador");
    }
    
    public bool TryAttachToBase(LeverBase leverBase)
    {
        if (leverBase.GetLeverID() != leverID)
        {
            Debug.Log($"Alavanca {leverName} não é compatível com esta base!");
            return false;
        }
        
        if (leverBase.HasLeverAttached())
        {
            Debug.Log("Esta base já possui uma alavanca!");
            return false;
        }
        
        isAttachedToBase = true;
        isPickedUp = false;
        currentBase = leverBase;
        
        transform.SetParent(leverBase.GetAttachPoint());
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        
        originalRotation = transform.localRotation;
        pulledRotation = originalRotation * Quaternion.AngleAxis(pullAngle, rotationAxis);
        
        if (rb != null) rb.isKinematic = true;
        if (col != null) col.enabled = false;
        
        // DELETAR COMPLETAMENTE o ícone da alavanca quando anexada
        DeleteInteractionIcon();
        
        Debug.Log($"Alavanca {leverName} anexada à base com sucesso!");
        return true;
    }
    
    public void DetachFromBase()
    {
        if (!isAttachedToBase || currentBase == null) return;
        
        isAttachedToBase = false;
        currentBase = null;
        
        transform.SetParent(null);
        
        if (rb != null) rb.isKinematic = false;
        if (col != null) col.enabled = true;
        
        // RECRIAR o ícone da alavanca quando removida da base
        RecreateInteractionIcon();
        
        Debug.Log($"Alavanca {leverName} removida da base");
    }
    
    /// <summary>
    /// Deleta completamente o ícone de interação da alavanca
    /// </summary>
    private void DeleteInteractionIcon()
    {
        ManualInteractionSystem manualSystem = GetComponent<ManualInteractionSystem>();
        if (manualSystem != null)
        {
            // Deletar o ícone filho se existir
            if (manualSystem.interactionIcon != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(manualSystem.interactionIcon);
                }
                else
                {
                    DestroyImmediate(manualSystem.interactionIcon);
                }
                manualSystem.interactionIcon = null;
            }
            
            // Desabilitar o componente ManualInteractionSystem
            manualSystem.enabled = false;
        }
        
        Debug.Log($"Ícone de interação da alavanca {leverName} deletado completamente.");
    }
    
    /// <summary>
    /// Recria o ícone de interação quando a alavanca é removida da base
    /// </summary>
    private void RecreateInteractionIcon()
    {
        // Tentar encontrar um InteractionSetupHelper na cena para recriar o ícone
        InteractionSetupHelper setupHelper = FindObjectOfType<InteractionSetupHelper>();
        if (setupHelper != null && setupHelper.interactionIconPrefab != null)
        {
            // Criar novo ícone
            Vector3 iconPosition = Vector3.up * 1.5f; // Altura padrão
            
            // Tentar usar bounds do objeto para posicionamento mais preciso
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                float objectHeight = renderer.bounds.size.y;
                iconPosition = Vector3.up * (objectHeight + 0.5f);
            }
            
            GameObject iconInstance = Instantiate(setupHelper.interactionIconPrefab, transform);
            iconInstance.name = "InteractionIcon";
            iconInstance.transform.localPosition = iconPosition;
            iconInstance.SetActive(false); // Começar desativado
            
            // Reconfigurar ManualInteractionSystem
            ManualInteractionSystem manualSystem = GetComponent<ManualInteractionSystem>();
            if (manualSystem != null)
            {
                manualSystem.interactionIcon = iconInstance;
                manualSystem.actionText = pickupText;
                manualSystem.enabled = true;
            }
            
            Debug.Log($"Ícone de interação da alavanca {leverName} recriado com sucesso.");
        }
        else
        {
            // Fallback: apenas reabilitar o sistema manual se não conseguir recriar o ícone
            ManualInteractionSystem manualSystem = GetComponent<ManualInteractionSystem>();
            if (manualSystem != null)
            {
                manualSystem.enabled = true;
                manualSystem.actionText = pickupText;
            }
            
            Debug.LogWarning($"Não foi possível recriar o ícone de interação para a alavanca {leverName}. InteractionSetupHelper ou prefab não encontrado.");
        }
    }
    
    public void UseLever()
    {
        if (!IsAttachedToBase())
        {
            Debug.Log("Alavanca não está anexada a uma base!");
            return;
        }
        
        if (isActivated || isAnimating)
        {
            Debug.Log("Alavanca já está ativada ou em movimento!");
            return;
        }
        
        if (currentBase != null)
        {
            MedievalGate gate = currentBase.GetControlledGate();
            if (gate == null)
            {
                Debug.Log($"Nenhum portão atribuído à base {currentBase.baseName}!");
                return;
            }
            
            isActivated = true;
            isAnimating = true;
            isReturning = false;
            holdTimer = 0f;
            
            Debug.Log($"Alavanca {leverName} ativando portão {gate.GetGateName()}!");
            gate.ToggleGate();
            
            Debug.Log($"Alavanca {leverName} ativada! Iniciando animação de puxada...");
        }
    }
    
    void StartReturn()
    {
        isReturning = true;
        isAnimating = true;
        Debug.Log($"Alavanca {leverName} iniciando retorno à posição original...");
    }
    
    public bool IsAttachedToBase()
    {
        return isAttachedToBase && currentBase != null;
    }
    
    public bool IsPickedUp()
    {
        return isPickedUp;
    }
    
    public bool CanUse()
    {
        return IsAttachedToBase() && !isActivated && !isAnimating && !isReturning;
    }
    
    public bool IsActivated()
    {
        return isActivated;
    }
    
    public bool IsAnimating()
    {
        return isAnimating;
    }
    
    public string GetLeverID()
    {
        return leverID;
    }
    
    public string GetLeverName()
    {
        return leverName;
    }
    
    public LeverBase GetCurrentBase()
    {
        return currentBase;
    }
}