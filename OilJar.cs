// OilJar.cs
using UnityEngine;
using System.Collections;

public class OilJar : MonoBehaviour
{
    [Header("Configurações do Jarro")]
    [Tooltip("Nome do jarro para exibição")]
    public string jarName = "Jarro de Óleo";
    
    [Header("Configuração na Mão")]
    public Vector3 heldPosition = new Vector3(0.3f, 0.3f, 0.5f);
    public Vector3 heldRotation = new Vector3(0, 0, 0);
    
    [Header("Configurações de Animação de Despejo")]
    [Tooltip("Ângulo de inclinação ao despejar (em graus)")]
    public float pourAngle = 45f;
    
    [Tooltip("Eixo de rotação para inclinar (local)")]
    public Vector3 pourAxis = Vector3.forward;
    
    [Tooltip("Velocidade da animação de inclinar")]
    public float pourAnimationSpeed = 3f;
    
    [Tooltip("Delay antes de começar a inclinar")]
    public float pourStartDelay = 0.5f;
    
    [Tooltip("Som opcional ao despejar")]
    public AudioSource pourSound;
    
    private Rigidbody rb;
    private Collider col;
    private bool isPickedUp = false;
    private bool isPouring = false;
    private Quaternion originalRotation;
    private Quaternion pourRotation;
    private Coroutine pourAnimationCoroutine;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }
    
    void Start()
    {
        // Configurar ManualInteractionSystem se existir
        ManualInteractionSystem manualSystem = GetComponent<ManualInteractionSystem>();
        if (manualSystem != null)
        {
            manualSystem.actionText = "Pegar Jarro";
        }
        
        // Salvar rotação original
        originalRotation = transform.localRotation;
        pourRotation = originalRotation * Quaternion.AngleAxis(pourAngle, pourAxis);
    }
    
    public bool CanPickup()
    {
        return !isPickedUp && !isPouring;
    }
    
    public void Pickup(Transform handSlot)
    {
        if (!CanPickup()) return;
        
        isPickedUp = true;
        transform.SetParent(handSlot);
        transform.localPosition = heldPosition;
        transform.localEulerAngles = heldRotation;
        
        // Recalcular rotações baseado na nova orientação
        originalRotation = transform.localRotation;
        pourRotation = originalRotation * Quaternion.AngleAxis(pourAngle, pourAxis);
        
        if (rb != null) rb.isKinematic = true;
        if (col != null) col.enabled = false;
        
        // Notificar ManualInteractionSystem
        ManualInteractionSystem manualSystem = GetComponent<ManualInteractionSystem>();
        if (manualSystem != null)
        {
            manualSystem.OnObjectPickedUp();
        }
        
        Debug.Log($"Jarro {jarName} foi pego");
    }
    
    public void Drop()
    {
        if (!isPickedUp) return;
        
        // Parar animação se estiver despejando
        if (isPouring)
        {
            StopPouringAnimation();
        }
        
        isPickedUp = false;
        transform.SetParent(null);
        
        if (rb != null) rb.isKinematic = false;
        if (col != null) col.enabled = true;
        
        // Notificar ManualInteractionSystem
        ManualInteractionSystem manualSystem = GetComponent<ManualInteractionSystem>();
        if (manualSystem != null)
        {
            manualSystem.OnObjectDropped();
        }
        
        Debug.Log($"Jarro {jarName} foi largado");
    }
    
    /// <summary>
    /// Inicia a animação de despejo
    /// </summary>
    public void StartPouringAnimation()
    {
        if (!isPickedUp || isPouring) return;
        
        isPouring = true;
        
        // Garantir que está na rotação original no início
        transform.localRotation = originalRotation;
        
        if (pourAnimationCoroutine != null)
        {
            StopCoroutine(pourAnimationCoroutine);
        }
        
        pourAnimationCoroutine = StartCoroutine(PourAnimationCoroutine());
        
        Debug.Log($"Jarro {jarName} iniciou animação de despejo");
    }
    
    /// <summary>
    /// Para a animação de despejo
    /// </summary>
    public void StopPouringAnimation()
    {
        if (!isPouring) return;
        
        isPouring = false;
        
        if (pourAnimationCoroutine != null)
        {
            StopCoroutine(pourAnimationCoroutine);
            pourAnimationCoroutine = null;
        }
        
        // Parar som se estiver tocando
        if (pourSound != null && pourSound.isPlaying)
        {
            pourSound.Stop();
        }
        
        // Voltar à rotação original suavemente
        StartCoroutine(ReturnToOriginalRotation());
        
        Debug.Log($"Jarro {jarName} parou animação de despejo");
    }
    
    /// <summary>
    /// Corrotina que gerencia a animação de despejo
    /// </summary>
    IEnumerator PourAnimationCoroutine()
    {
        // Delay inicial antes de começar a inclinar
        yield return new WaitForSeconds(pourStartDelay);
        
        // Verificar se ainda está despejando (pode ter sido cancelado durante o delay)
        if (!isPouring) yield break;
        
        // Tocar som de despejo no início
        if (pourSound != null)
        {
            pourSound.Play();
        }
        
        // A animação agora será controlada pelo UpdatePouringAnimation
        // Esta corrotina apenas aguarda e toca o som
        pourAnimationCoroutine = null;
    }
    
    /// <summary>
    /// Corrotina para voltar à rotação original
    /// </summary>
    IEnumerator ReturnToOriginalRotation()
    {
        float animationTime = 0f;
        float animationDuration = 1f / pourAnimationSpeed;
        
        Quaternion startRotation = transform.localRotation;
        
        while (animationTime < animationDuration)
        {
            animationTime += Time.deltaTime;
            float progress = animationTime / animationDuration;
            
            // Usar easing para uma animação mais suave
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            
            transform.localRotation = Quaternion.Lerp(startRotation, originalRotation, easedProgress);
            
            yield return null;
        }
        
        // Garantir que voltou à rotação original
        transform.localRotation = originalRotation;
    }
    
    /// <summary>
    /// Atualiza a animação baseado no progresso do despejo
    /// </summary>
    /// <param name="progress">Progresso de 0 a 1</param>
    public void UpdatePouringAnimation(float progress)
    {
        if (!isPouring || !isPickedUp) return;
        
        // Aplicar delay inicial - não inclinar até 10% do progresso
        float delayProgress = 0.1f;
        
        if (progress < delayProgress)
        {
            // Manter na posição original durante o delay
            transform.localRotation = originalRotation;
            return;
        }
        
        // Calcular progresso ajustado após o delay
        float adjustedProgress = (progress - delayProgress) / (1f - delayProgress);
        adjustedProgress = Mathf.Clamp01(adjustedProgress);
        
        // Usar easing para uma animação mais suave
        float easedProgress = Mathf.SmoothStep(0f, 1f, adjustedProgress);
        
        // Interpolar entre rotação original e rotação de despejo
        transform.localRotation = Quaternion.Lerp(originalRotation, pourRotation, easedProgress);
    }
    
    public void Use()
    {
        // O jarro será destruído quando usado no altar
        Debug.Log($"Jarro {jarName} foi usado");
        
        // Parar qualquer animação em andamento
        if (isPouring)
        {
            StopPouringAnimation();
        }
    }
    
    public bool IsPickedUp() { return isPickedUp; }
    public bool IsPouring() { return isPouring; }
    
    void OnDestroy()
    {
        // Limpar corrotinas ao destruir o objeto
        if (pourAnimationCoroutine != null)
        {
            StopCoroutine(pourAnimationCoroutine);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Mostrar preview da rotação de despejo no editor
        if (Application.isPlaying && isPouring)
        {
            Gizmos.color = Color.blue;
        }
        else
        {
            Gizmos.color = Color.yellow;
        }
        
        // Desenhar uma seta indicando a direção do despejo
        Vector3 pourDirection = transform.TransformDirection(pourAxis);
        Gizmos.DrawRay(transform.position, pourDirection * 0.5f);
    }
}