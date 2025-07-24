using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class StaminaSystem : MonoBehaviour
{
    [Header("Configurações de Stamina")]
    [Tooltip("Tempo total que pode correr (em segundos)")]
    public float maxStaminaTime = 10f;
    
    [Tooltip("Tempo para recuperar totalmente a stamina")]
    public float recoveryTime = 8f;
    
    [Tooltip("Tempo correndo antes dos efeitos começarem")]
    public float timeBeforeEffects = 3f;
    
    [Header("Referências")]
    [Tooltip("PlayerController para controlar velocidade")]
    public PlayerController playerController;
    
    [Header("Audio")]
    [Tooltip("Som de respiração pesada")]
    public AudioClip breathingSound;
    
    [Tooltip("Som de recuperação")]
    public AudioClip recoverySound;
    
    [Tooltip("Volume do som de respiração")]
    [Range(0f, 1f)]
    public float breathingVolume = 0.8f;
    
    [Tooltip("Volume do som de recuperação")]
    [Range(0f, 1f)]
    public float recoveryVolume = 0.6f;
    
    [Header("Efeito Visual")]
    [Tooltip("Volume HDRP para vinheta (bordas pretas)")]
    public Volume postProcessVolume;
    
    [Tooltip("Intensidade máxima da vinheta")]
    [Range(0f, 1f)]
    public float maxVignetteIntensity = 0.6f;
    
    [Tooltip("Velocidade da transição da vinheta (0 = sincronia com recovery time)")]
    public float vignetteTransitionSpeed = 0f;
    
    [Header("Debug")]
    public bool showDebug = false;
    
    // Estado interno
    private float currentStamina;
    private float runningTime;
    private bool isExhausted;
    private bool canStartEffects;
    
    // Componentes
    private AudioSource breathingAudio;
    private AudioSource recoveryAudio;
    private Vignette vignette;
    
    // Controle da transição da vinheta
    private float currentVignetteIntensity = 0f;
    private float targetVignetteIntensity = 0f;
    
    // Velocidades originais
    private float originalWalkSpeed;
    private float originalRunSpeed;
    
    void Start()
    {
        // Inicializar stamina
        currentStamina = maxStaminaTime;
        runningTime = 0f;
        isExhausted = false;
        canStartEffects = false;
        
        // Configurar PlayerController
        SetupPlayerController();
        
        // Configurar Audio
        SetupAudio();
        
        // Configurar HDRP Vinheta
        SetupVignette();
    }
    
    void SetupPlayerController()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
        
        if (playerController != null)
        {
            originalWalkSpeed = playerController.walkingSpeed;
            originalRunSpeed = playerController.runningSpeed;
        }
        else
        {
            Debug.LogError("StaminaSystem: PlayerController não encontrado!");
        }
    }
    
    void SetupAudio()
    {
        // Criar AudioSource para respiração
        GameObject breathingObj = new GameObject("BreathingAudio");
        breathingObj.transform.SetParent(transform);
        breathingAudio = breathingObj.AddComponent<AudioSource>();
        breathingAudio.clip = breathingSound;
        breathingAudio.volume = breathingVolume;
        breathingAudio.loop = false;
        breathingAudio.playOnAwake = false;
        breathingAudio.spatialBlend = 0f; // 2D
        
        // Criar AudioSource para recuperação
        GameObject recoveryObj = new GameObject("RecoveryAudio");
        recoveryObj.transform.SetParent(transform);
        recoveryAudio = recoveryObj.AddComponent<AudioSource>();
        recoveryAudio.clip = recoverySound;
        recoveryAudio.volume = recoveryVolume;
        recoveryAudio.loop = false;
        recoveryAudio.playOnAwake = false;
        recoveryAudio.spatialBlend = 0f; // 2D
    }
    
    void SetupVignette()
    {
        if (postProcessVolume == null)
        {
            // Tentar encontrar na câmera
            Camera cam = Camera.main;
            if (cam != null)
                postProcessVolume = cam.GetComponent<Volume>();
        }
        
        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            if (postProcessVolume.profile.TryGet(out vignette))
            {
                vignette.active = false;
                vignette.intensity.value = 0f;
                currentVignetteIntensity = 0f;
                targetVignetteIntensity = 0f;
                Debug.Log("✅ HDRP Vinheta configurada!");
            }
            else
            {
                Debug.LogWarning("⚠️ Vinheta não encontrada no Volume Profile! Adicione: Add Override → Post-processing → Vignette");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Post Process Volume não configurado!");
        }
    }
    
    void Update()
    {
        // Detectar se está correndo
        bool isRunning = IsPlayerRunning();
        
        // Atualizar stamina
        UpdateStamina(isRunning);
        
        // Atualizar efeitos
        UpdateEffects();
        
        // Debug
        if (showDebug)
            ShowDebugInfo(isRunning);
    }
    
    bool IsPlayerRunning()
    {
        if (playerController == null) return false;
        
        bool wantsToRun = Input.GetKey(KeyCode.LeftShift);
        bool hasMovement = Input.GetAxis("Vertical") != 0 || Input.GetAxis("Horizontal") != 0;
        bool canMove = playerController.canMove;
        
        return wantsToRun && hasMovement && canMove && !isExhausted;
    }
    
    void UpdateStamina(bool isRunning)
    {
        if (isRunning && !isExhausted)
        {
            // SEMPRE contar tempo de corrida quando está correndo
            runningTime += Time.deltaTime;
            
            // Ativar efeitos após tempo mínimo (INDEPENDENTE da stamina)
            if (runningTime >= timeBeforeEffects && !canStartEffects)
            {
                canStartEffects = true;
                Debug.Log("🚨 Efeitos de cansaço ativados após " + timeBeforeEffects + " segundos correndo!");
            }
            
            // Consumir stamina
            currentStamina -= Time.deltaTime;
            
            // Ficar exausto quando stamina acaba
            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                isExhausted = true;
                
                // Forçar velocidade de caminhada
                if (playerController != null)
                    playerController.runningSpeed = originalWalkSpeed;
                
                Debug.Log("😵 Player exausto!");
            }
        }
        else
        {
            // Recuperar stamina
            if (isExhausted)
            {
                // Recuperação lenta quando exausto
                currentStamina += (maxStaminaTime / recoveryTime) * Time.deltaTime;
            }
            else
            {
                // Recuperação rápida quando não exausto
                currentStamina += (maxStaminaTime / recoveryTime) * 2f * Time.deltaTime;
            }
            
            // Limitar stamina máxima
            currentStamina = Mathf.Min(currentStamina, maxStaminaTime);
            
            // Sair do estado exausto quando stamina volta ao máximo
            if (isExhausted && currentStamina >= maxStaminaTime)
            {
                isExhausted = false;
                runningTime = 0f;
                canStartEffects = false;
                
                // Resetar vinheta gradualmente (não instantâneo)
                targetVignetteIntensity = 0f;
                
                // Restaurar velocidade de corrida
                if (playerController != null)
                    playerController.runningSpeed = originalRunSpeed;
                
                Debug.Log("😌 Player recuperado! Vinheta desaparecerá gradualmente.");
            }
            
            // Se stamina recuperou totalmente e não está correndo, resetar efeitos gradualmente
            if (!isExhausted && currentStamina >= maxStaminaTime && !isRunning)
            {
                // Resetar flags para permitir que efeitos parem naturalmente
                canStartEffects = false;
                runningTime = 0f;
            }
        }
    }
    
    void UpdateEffects()
    {
        float staminaPercent = currentStamina / maxStaminaTime;
        bool isCurrentlyRunning = IsPlayerRunning();
        
        // Audio de respiração: toca se passou do tempo mínimo E (está correndo OU stamina não está cheia) E não está em recovery
        bool shouldShowBreathing = canStartEffects && (isCurrentlyRunning || staminaPercent < 1f) && !isExhausted;
        
        // Audio de recuperação: só quando exausto
        bool shouldShowRecovery = isExhausted && !isCurrentlyRunning;
        
        // Vinheta: toca se passou do tempo mínimo E stamina não está cheia
        bool shouldShowVignette = canStartEffects && staminaPercent < 1f && !isExhausted;
        
        // Audio de respiração
        UpdateBreathingAudio(shouldShowBreathing, staminaPercent, isCurrentlyRunning);
        
        // Audio de recuperação
        UpdateRecoveryAudio(shouldShowRecovery, staminaPercent);
        
        // Efeito de vinheta (bordas pretas)
        UpdateVignetteEffect(shouldShowVignette, staminaPercent);
    }
    
    void UpdateBreathingAudio(bool shouldPlay, float staminaPercent, bool isCurrentlyRunning)
    {
        if (breathingAudio == null || breathingSound == null) return;
        
        if (shouldPlay)
        {
            // Se stamina está 100% e não está correndo, NÃO tocar o audio
            if (staminaPercent >= 1f && !isCurrentlyRunning)
            {
                if (breathingAudio.isPlaying)
                {
                    breathingAudio.Stop();
                    Debug.Log("🔇 Respiração pesada parada - stamina recuperada totalmente");
                }
                return;
            }
            
            if (!breathingAudio.isPlaying)
            {
                breathingAudio.Play();
                Debug.Log("🎵 Respiração pesada iniciada - tempo correndo: " + runningTime.ToString("F1") + "s");
            }
            
            // Ajustar volume baseado no cansaço e se está correndo
            float fatigue = 1f - staminaPercent;
            
            if (isCurrentlyRunning)
            {
                // Se está correndo, volume baseado no cansaço (mínimo 40%)
                float volumeMultiplier = Mathf.Max(0.4f, fatigue);
                breathingAudio.volume = breathingVolume * volumeMultiplier;
            }
            else
            {
                // Se parou de correr mas ainda não recuperou totalmente, volume diminui gradualmente
                float recoveryFactor = 1f - staminaPercent; // Quanto mais stamina, menor o volume
                float volumeMultiplier = Mathf.Max(0.1f, recoveryFactor * 0.8f);
                breathingAudio.volume = breathingVolume * volumeMultiplier;
            }
        }
        else
        {
            if (breathingAudio.isPlaying)
            {
                breathingAudio.Stop();
                Debug.Log("🔇 Respiração pesada parada");
            }
        }
    }
    
    void UpdateRecoveryAudio(bool shouldPlay, float staminaPercent)
    {
        if (recoveryAudio == null || recoverySound == null) return;
        
        if (shouldPlay)
        {
            if (!recoveryAudio.isPlaying)
            {
                recoveryAudio.Play();
                Debug.Log("🎵 Som de recuperação iniciado");
            }
            
            // Volume diminui conforme recupera
            recoveryAudio.volume = recoveryVolume * Mathf.Lerp(1f, 0.3f, staminaPercent);
        }
        else
        {
            if (recoveryAudio.isPlaying)
            {
                recoveryAudio.Stop();
                Debug.Log("🔇 Som de recuperação parado");
            }
        }
    }
    
    void UpdateVignetteEffect(bool shouldShow, float staminaPercent)
    {
        if (vignette == null) return;
        
        // Calcular intensidade alvo
        if (shouldShow)
        {
            // Intensidade baseada no cansaço
            float fatigue = 1f - staminaPercent;
            targetVignetteIntensity = maxVignetteIntensity * fatigue;
        }
        else
        {
            // Intensidade alvo = 0 (desaparecer)
            targetVignetteIntensity = 0f;
        }
        
        // Calcular velocidade de transição
        float transitionSpeed;
        if (vignetteTransitionSpeed > 0f)
        {
            // Usar velocidade manual definida
            transitionSpeed = vignetteTransitionSpeed;
        }
        else
        {
            // Sincronizar com recovery time
            // Se está recuperando stamina, usar velocidade baseada no recovery time
            if (targetVignetteIntensity == 0f && currentVignetteIntensity > 0f)
            {
                // Calculando para que a vinheta desapareça no mesmo tempo que a stamina recupera
                transitionSpeed = maxVignetteIntensity / recoveryTime;
            }
            else
            {
                // Para aparecer, usar velocidade mais rápida (2x mais rápido que o desaparecimento)
                transitionSpeed = (maxVignetteIntensity / recoveryTime) * 2f;
            }
        }
        
        // Transição suave para a intensidade alvo
        currentVignetteIntensity = Mathf.MoveTowards(currentVignetteIntensity, targetVignetteIntensity, transitionSpeed * Time.deltaTime);
        
        // Ativar/desativar vinheta baseado na intensidade atual
        if (currentVignetteIntensity > 0.01f)
        {
            // Ativar se não estiver ativa
            if (!vignette.active)
            {
                vignette.active = true;
                Debug.Log("🖤 Vinheta (bordas pretas) ativada");
            }
            
            // Aplicar intensidade atual
            vignette.intensity.value = currentVignetteIntensity;
            
            // Ajustar suavidade das bordas baseado na intensidade
            float normalizedIntensity = currentVignetteIntensity / maxVignetteIntensity;
            vignette.smoothness.value = Mathf.Lerp(0.2f, 0.6f, normalizedIntensity);
        }
        else
        {
            // Desativar quando intensidade chegou próximo de 0
            if (vignette.active)
            {
                vignette.active = false;
                vignette.intensity.value = 0f;
                currentVignetteIntensity = 0f; // Garantir que chegou a zero
                Debug.Log("🖤 Vinheta (bordas pretas) desativada gradualmente - sincronizada com recovery");
            }
        }
    }
    
    void ShowDebugInfo(bool isRunning)
    {
        if (Time.time % 0.5f < Time.deltaTime) // A cada 0.5s
        {
            float staminaPercent = (currentStamina / maxStaminaTime) * 100f;
            float vignetteSpeed = vignetteTransitionSpeed > 0f ? vignetteTransitionSpeed : (maxVignetteIntensity / recoveryTime);
            
            Debug.Log($"🏃 Correndo: {isRunning} | " +
                     $"⚡ Stamina: {staminaPercent:F0}% | " +
                     $"⏱️ Tempo Corrida: {runningTime:F1}s | " +
                     $"🎯 Efeitos Ativos: {canStartEffects} | " +
                     $"😵 Exausto: {isExhausted} | " +
                     $"🎵 Audio Breathing: {(breathingAudio != null && breathingAudio.isPlaying)} (Vol: {(breathingAudio != null ? breathingAudio.volume.ToString("F2") : "0")}) | " +
                     $"🔄 Audio Recovery: {(recoveryAudio != null && recoveryAudio.isPlaying)} | " +
                     $"🖤 Vinheta: {(vignette != null && vignette.active)} (Atual: {currentVignetteIntensity:F2} → Alvo: {targetVignetteIntensity:F2}) Speed: {vignetteSpeed:F2}");
        }
    }
    
    // Métodos públicos para outros scripts
    public float GetStaminaPercent()
    {
        return currentStamina / maxStaminaTime;
    }
    
    public bool IsExhausted()
    {
        return isExhausted;
    }
    
    public bool CanRun()
    {
        return !isExhausted;
    }
    
    // Métodos de teste
    [ContextMenu("Teste: Forçar Exaustão")]
    void TestExhaustion()
    {
        currentStamina = 0f;
        isExhausted = true;
        canStartEffects = true;
        if (playerController != null)
            playerController.runningSpeed = originalWalkSpeed;
        Debug.Log("🧪 TESTE: Exaustão forçada!");
    }
    
    [ContextMenu("Teste: Recuperar Stamina")]
    void TestRecover()
    {
        currentStamina = maxStaminaTime;
        isExhausted = false;
        canStartEffects = false;
        runningTime = 0f;
        targetVignetteIntensity = 0f; // Iniciar fade out da vinheta
        if (playerController != null)
            playerController.runningSpeed = originalRunSpeed;
        Debug.Log("🧪 TESTE: Stamina recuperada! Vinheta desaparecerá gradualmente.");
    }
    
    [ContextMenu("Teste: Fade Out Vinheta")]
    void TestFadeOutVignette()
    {
        targetVignetteIntensity = 0f;
        float estimatedTime = vignetteTransitionSpeed > 0f ? 
            (currentVignetteIntensity / vignetteTransitionSpeed) : 
            (currentVignetteIntensity / (maxVignetteIntensity / recoveryTime));
        Debug.Log($"🧪 TESTE: Iniciando fade out da vinheta. Intensidade atual: {currentVignetteIntensity:F2} - Tempo estimado: {estimatedTime:F1}s (Recovery: {recoveryTime}s)");
    }
    
    [ContextMenu("Teste: Ativar Efeitos")]
    void TestEffects()
    {
        canStartEffects = true;
        runningTime = timeBeforeEffects + 1f; // Simular que já correu tempo suficiente
        currentStamina = maxStaminaTime * 0.5f; // 50% stamina para testar vinheta
        isExhausted = false;
        Debug.Log("🧪 TESTE: Efeitos ativados! Breathing e vinheta devem aparecer.");
    }
}