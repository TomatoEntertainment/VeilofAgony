using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Animator do player")]
    public Animator animator;
    
    [Tooltip("PlayerController para detectar movimento")]
    public PlayerController playerController;
    
    [Header("Configurações de Transição")]
    [Tooltip("Suavidade das transições de animação")]
    [Range(0.1f, 1f)]
    public float animationSmoothTime = 0.3f;
    
    [Tooltip("Velocidade mínima para considerar que está se movendo")]
    [Range(0.01f, 0.5f)]
    public float movementThreshold = 0.1f;
    
    [Header("Debug")]
    [Tooltip("Mostrar informações de debug no console")]
    public bool showDebugInfo = false;
    
    // Parâmetros do Animator (nomes dos parâmetros que você deve criar no Animator Controller)
    private static class AnimParams
    {
        public const string Speed = "Speed";
        public const string DirectionX = "DirectionX";
        public const string DirectionZ = "DirectionZ";
        public const string IsRunning = "IsRunning";
        public const string IsGrounded = "IsGrounded";
        public const string IsHealing = "IsHealing";
        public const string IsMoving = "IsMoving";
    }
    
    // Variáveis para suavização
    private Vector2 currentMovementInput;
    private Vector2 targetMovementInput;
    private bool wasMoving = false;
    
    // Cache de componentes
    private CharacterController characterController;
    
    void Start()
    {
        // Auto-detectar componentes se não foram atribuídos
        if (animator == null)
            animator = GetComponent<Animator>();
            
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
            
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
        
        // Validações
        if (animator == null)
        {
            Debug.LogError("PlayerAnimationController: Animator não encontrado! Adicione um Animator ao player.");
            enabled = false;
            return;
        }
        
        if (playerController == null)
        {
            Debug.LogError("PlayerAnimationController: PlayerController não encontrado!");
            enabled = false;
            return;
        }
        
        ValidateAnimatorParameters();
    }
    
    void Update()
    {
        if (animator == null || playerController == null) return;
        
        UpdateMovementAnimations();
        UpdateStateAnimations();
        
        if (showDebugInfo)
            ShowDebugInfo();
    }
    
    void UpdateMovementAnimations()
    {
        // Obter input de movimento
        float inputX = Input.GetAxis("Horizontal");
        float inputZ = Input.GetAxis("Vertical");
        
        // Detectar se está correndo
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        
        // Calcular movimento alvo
        targetMovementInput = new Vector2(inputX, inputZ);
        
        // Suavizar transições
        currentMovementInput = Vector2.Lerp(currentMovementInput, targetMovementInput, 
            Time.deltaTime / animationSmoothTime);
        
        // Calcular velocidade total
        float totalSpeed = currentMovementInput.magnitude;
        bool isMoving = totalSpeed > movementThreshold && playerController.canMove;
        
        // Aplicar modificador de corrida à velocidade
        if (isRunning && isMoving)
        {
            totalSpeed *= 2f; // Dobrar a velocidade para animação de corrida
        }
        
        // Atualizar parâmetros do Animator
        animator.SetFloat(AnimParams.Speed, totalSpeed);
        animator.SetFloat(AnimParams.DirectionX, currentMovementInput.x);
        animator.SetFloat(AnimParams.DirectionZ, currentMovementInput.y);
        animator.SetBool(AnimParams.IsRunning, isRunning && isMoving);
        animator.SetBool(AnimParams.IsMoving, isMoving);
        
        // Detectar mudanças de estado de movimento para debug
        if (isMoving != wasMoving)
        {
            if (showDebugInfo)
            {
                Debug.Log($"PlayerAnimationController: Movimento {(isMoving ? "iniciado" : "parado")}");
            }
            wasMoving = isMoving;
        }
    }
    
    void UpdateStateAnimations()
    {
        // Atualizar estado de grounded
        if (characterController != null)
        {
            animator.SetBool(AnimParams.IsGrounded, characterController.isGrounded);
        }
        
        // O parâmetro IsHealing já é gerenciado pelo PlayerInteraction
        // Então não precisamos atualizá-lo aqui
    }
    
    void ValidateAnimatorParameters()
    {
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("PlayerAnimationController: Nenhum Animator Controller atribuído ao Animator!");
            return;
        }
        
        // Lista de parâmetros que devem existir no Animator Controller
        string[] requiredParams = {
            AnimParams.Speed,
            AnimParams.DirectionX,
            AnimParams.DirectionZ,
            AnimParams.IsRunning,
            AnimParams.IsGrounded,
            AnimParams.IsHealing,
            AnimParams.IsMoving
        };
        
        foreach (string param in requiredParams)
        {
            bool paramExists = false;
            
            foreach (AnimatorControllerParameter animParam in animator.parameters)
            {
                if (animParam.name == param)
                {
                    paramExists = true;
                    break;
                }
            }
            
            if (!paramExists)
            {
                Debug.LogWarning($"PlayerAnimationController: Parâmetro '{param}' não encontrado no Animator Controller! " +
                               "Adicione este parâmetro ao seu Animator Controller.");
            }
        }
    }
    
    /// <summary>
    /// Força a reprodução de uma animação específica
    /// </summary>
    public void PlayAnimation(string animationName, int layer = 0)
    {
        if (animator != null)
        {
            animator.Play(animationName, layer);
        }
    }
    
    /// <summary>
    /// Define um parâmetro trigger no Animator
    /// </summary>
    public void SetTrigger(string triggerName)
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerName);
        }
    }
    
    /// <summary>
    /// Define um parâmetro bool no Animator
    /// </summary>
    public void SetBool(string paramName, bool value)
    {
        if (animator != null)
        {
            animator.SetBool(paramName, value);
        }
    }
    
    /// <summary>
    /// Define um parâmetro float no Animator
    /// </summary>
    public void SetFloat(string paramName, float value)
    {
        if (animator != null)
        {
            animator.SetFloat(paramName, value);
        }
    }
    
    /// <summary>
    /// Obtém o valor atual de um parâmetro float
    /// </summary>
    public float GetFloat(string paramName)
    {
        return animator != null ? animator.GetFloat(paramName) : 0f;
    }
    
    /// <summary>
    /// Obtém o valor atual de um parâmetro bool
    /// </summary>
    public bool GetBool(string paramName)
    {
        return animator != null && animator.GetBool(paramName);
    }
    
    /// <summary>
    /// Pausa/despausa as animações
    /// </summary>
    public void SetAnimationSpeed(float speed)
    {
        if (animator != null)
        {
            animator.speed = speed;
        }
    }
    
    /// <summary>
    /// Obtém informações sobre o estado atual da animação
    /// </summary>
    public AnimatorStateInfo GetCurrentAnimationState(int layer = 0)
    {
        return animator != null ? animator.GetCurrentAnimatorStateInfo(layer) : new AnimatorStateInfo();
    }
    
    /// <summary>
    /// Verifica se uma animação específica está tocando
    /// </summary>
    public bool IsPlayingAnimation(string animationName, int layer = 0)
    {
        if (animator == null) return false;
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(layer);
        return stateInfo.IsName(animationName);
    }
    
    void ShowDebugInfo()
    {
        if (Time.time % 0.5f < Time.deltaTime) // A cada 0.5 segundos
        {
            Debug.Log($"PlayerAnimation - Speed: {currentMovementInput.magnitude:F2} | " +
                     $"Direction: ({currentMovementInput.x:F2}, {currentMovementInput.y:F2}) | " +
                     $"Running: {animator.GetBool(AnimParams.IsRunning)} | " +
                     $"Moving: {animator.GetBool(AnimParams.IsMoving)} | " +
                     $"Grounded: {animator.GetBool(AnimParams.IsGrounded)}");
        }
    }
    
    // Métodos para integração com outros scripts
    
    /// <summary>
    /// Chamado pelo PlayerInteraction quando inicia cura
    /// </summary>
    public void StartHealing()
    {
        if (animator != null)
        {
            animator.SetBool(AnimParams.IsHealing, true);
        }
    }
    
    /// <summary>
    /// Chamado pelo PlayerInteraction quando para cura
    /// </summary>
    public void StopHealing()
    {
        if (animator != null)
        {
            animator.SetBool(AnimParams.IsHealing, false);
        }
    }
    
    /// <summary>
    /// Para uso em situações especiais (cutscenes, etc.)
    /// </summary>
    public void DisableMovementAnimations()
    {
        if (animator != null)
        {
            animator.SetFloat(AnimParams.Speed, 0f);
            animator.SetFloat(AnimParams.DirectionX, 0f);
            animator.SetFloat(AnimParams.DirectionZ, 0f);
            animator.SetBool(AnimParams.IsRunning, false);
            animator.SetBool(AnimParams.IsMoving, false);
        }
    }
    
    /// <summary>
    /// Reativa as animações de movimento
    /// </summary>
    public void EnableMovementAnimations()
    {
        // As animações voltarão a funcionar normalmente no próximo Update()
    }
    
    // Métodos para configuração em runtime
    
    [ContextMenu("Teste: Idle")]
    void TestIdle()
    {
        currentMovementInput = Vector2.zero;
        targetMovementInput = Vector2.zero;
        animator.SetFloat(AnimParams.Speed, 0f);
        animator.SetBool(AnimParams.IsMoving, false);
        animator.SetBool(AnimParams.IsRunning, false);
        Debug.Log("Teste: Animação Idle forçada");
    }
    
    [ContextMenu("Teste: Andar Frente")]
    void TestWalkForward()
    {
        currentMovementInput = Vector2.up;
        animator.SetFloat(AnimParams.Speed, 1f);
        animator.SetFloat(AnimParams.DirectionZ, 1f);
        animator.SetBool(AnimParams.IsMoving, true);
        animator.SetBool(AnimParams.IsRunning, false);
        Debug.Log("Teste: Animação Andar Frente forçada");
    }
    
    [ContextMenu("Teste: Correr")]
    void TestRun()
    {
        currentMovementInput = Vector2.up;
        animator.SetFloat(AnimParams.Speed, 2f);
        animator.SetFloat(AnimParams.DirectionZ, 1f);
        animator.SetBool(AnimParams.IsMoving, true);
        animator.SetBool(AnimParams.IsRunning, true);
        Debug.Log("Teste: Animação Correr forçada");
    }
    
    [ContextMenu("Validar Parâmetros do Animator")]
    void ValidateParameters()
    {
        ValidateAnimatorParameters();
    }
}