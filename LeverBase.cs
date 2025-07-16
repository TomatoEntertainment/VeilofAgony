using UnityEngine;

public class LeverBase : MonoBehaviour
{
    [Header("Configurações da Base")]
    [Tooltip("ID único desta base (deve ser igual ao da alavanca)")]
    public string leverID = "lever_01";
    
    [Tooltip("Nome da base para exibição")]
    public string baseName = "Base da Alavanca";
    
    [Header("Textos de Interação")]
    [Tooltip("Texto quando a base está vazia")]
    public string attachText = "Encaixar Alavanca";
    
    [Tooltip("Texto quando a base tem alavanca")]
    public string activateText = "Usar Alavanca";
    
    [Header("Referências")]
    [Tooltip("Ponto onde a alavanca será anexada")]
    public Transform attachPoint;
    
    [Tooltip("Portão que será controlado por esta alavanca")]
    public MedievalGate controlledGate;
    
    [Header("Visual Feedback")]
    [Tooltip("Material quando não tem alavanca")]
    public Material emptyMaterial;
    
    [Tooltip("Material quando tem alavanca")]
    public Material occupiedMaterial;
    
    private LeverHandle attachedLever;
    private bool hasLeverAttached = false;
    private Renderer baseRenderer;
    
    void Start()
    {
        baseRenderer = GetComponent<Renderer>();
        UpdateVisualState();
        
        // Configurar texto inicial apenas se não tiver alavanca anexada no Start
        ManualInteractionSystem manualSystem = GetComponent<ManualInteractionSystem>();
        if (manualSystem != null && !hasLeverAttached)
        {
            manualSystem.actionText = attachText;
        }
        
        if (attachPoint == null)
        {
            Debug.LogWarning($"Base {baseName} não tem attach point definido!");
        }
        
        if (controlledGate == null)
        {
            Debug.LogWarning($"Base {baseName} não tem portão atribuído!");
        }
    }
    
    public void SetAttachedLeverAtStart(LeverHandle lever)
    {
        attachedLever = lever;
        hasLeverAttached = true;
        UpdateVisualState();
        
        // MANTER o ícone da base ativo quando alavanca já está anexada no início
        ManualInteractionSystem manualSystem = GetComponent<ManualInteractionSystem>();
        if (manualSystem != null)
        {
            manualSystem.actionText = activateText;
        }
        
        Debug.Log($"Base {baseName} configurada com alavanca {lever.GetLeverName()} no início do jogo.");
    }
    
    void Update()
    {
        // Apenas atualiza visual, texto não muda automaticamente
    }
    
    public bool TryAttachLever(LeverHandle leverInHand)
    {
        if (hasLeverAttached) return false;
        
        if (leverInHand.GetLeverID() != leverID)
        {
            Debug.Log($"Alavanca {leverInHand.GetLeverName()} não é compatível com {baseName}!");
            return false;
        }
        
        if (leverInHand.TryAttachToBase(this))
        {
            attachedLever = leverInHand;
            hasLeverAttached = true;
            UpdateVisualState();
            
            // MANTER o ícone da base ativo e mudar o texto para "usar alavanca"
            ManualInteractionSystem manualSystem = GetComponent<ManualInteractionSystem>();
            if (manualSystem != null)
            {
                manualSystem.actionText = activateText;
            }
            
            Debug.Log($"Alavanca {leverInHand.GetLeverName()} anexada a {baseName}!");
            return true;
        }
        
        return false;
    }
    
    public void DetachLever()
    {
        if (!hasLeverAttached || attachedLever == null) return;
        
        attachedLever.DetachFromBase();
        attachedLever = null;
        hasLeverAttached = false;
        UpdateVisualState();
        
        // Voltar texto para "encaixar alavanca"
        ManualInteractionSystem manualSystem = GetComponent<ManualInteractionSystem>();
        if (manualSystem != null)
        {
            manualSystem.actionText = attachText;
        }
        
        Debug.Log($"Alavanca removida de {baseName}");
    }
    
    public void ActivateLever()
    {
        // Reativar esta função para ser chamada pela PlayerInteraction
        Debug.Log($"ActivateLever chamado para {baseName}");
        
        if (!hasLeverAttached || attachedLever == null)
        {
            Debug.Log($"Erro: Não há alavanca anexada em {baseName}!");
            return;
        }
        
        // Usar a alavanca anexada
        if (attachedLever.CanUse())
        {
            attachedLever.UseLever();
        }
        else
        {
            Debug.Log($"Alavanca em {baseName} não está disponível para uso!");
        }
    }
    
    void UpdateVisualState()
    {
        if (baseRenderer == null) return;
        
        if (hasLeverAttached && occupiedMaterial != null)
        {
            baseRenderer.material = occupiedMaterial;
        }
        else if (!hasLeverAttached && emptyMaterial != null)
        {
            baseRenderer.material = emptyMaterial;
        }
    }
    
    public string GetLeverID()
    {
        return leverID;
    }
    
    public bool HasLeverAttached()
    {
        return hasLeverAttached;
    }
    
    public Transform GetAttachPoint()
    {
        return attachPoint;
    }
    
    public LeverHandle GetAttachedLever()
    {
        return attachedLever;
    }
    
    public MedievalGate GetControlledGate()
    {
        return controlledGate;
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, controlledGate != null ? controlledGate.transform.position : transform.position);
        
        if (attachPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attachPoint.position, 0.2f);
        }
    }
}