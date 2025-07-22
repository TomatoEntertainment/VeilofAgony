using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PlayerController : MonoBehaviour
{
    [Header("Flap Settings")]
    public float     flapForce          = 5f;
    public GameObject shockwavePrefab;
    public Vector3   shockwaveOffset   = new Vector3(0f, -1f, 0f);
    public float     shockwaveLifetime = 1f;
    public AudioClip flapClip;
    public AudioSource audioSource;

    [Header("Crash Sequence")]
    public GameObject shortCircuitPrefab;
    public float      shortCircuitDuration = 1f;

    [Header("Explosion Settings")]
    public GameObject explosionPrefab;
    public AudioClip explosionClip;

    [Header("Shield Settings")]
    [Tooltip("O prefab do objeto visual do escudo.")]
    public GameObject shieldPrefab;
    [Tooltip("Som que toca quando o escudo é quebrado.")]
    public AudioClip shieldBreakClip;
    
    private GameObject shieldInstance;
    private bool isShieldActive = false;

    private Rigidbody rb;
    private Collider  col;
    private bool      gravityRestored  = false;
    private bool      crashSequenceRun = false;

    void Awake()
    {
        rb  = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        rb.useGravity     = false;
        rb.linearVelocity       = Vector3.zero;
    }

    public void ActivateShield()
    {
        if (isShieldActive) return;

        isShieldActive = true;
        if (shieldPrefab != null && shieldInstance == null)
        {
            shieldInstance = Instantiate(shieldPrefab, transform.position, transform.rotation, transform);
        }
        shieldInstance?.SetActive(true);
        
        // Avisa o GameManager para DESATIVAR o painel
        GameManager.Instance.SetShieldPanelState(false);
    }

    private void DeactivateShield()
    {
        if (!isShieldActive) return;
        
        isShieldActive = false;
        shieldInstance?.SetActive(false);

        if (audioSource != null && shieldBreakClip != null)
        {
            audioSource.PlayOneShot(shieldBreakClip);
        }
        
        // Avisa o GameManager para REATIVAR o painel
        GameManager.Instance.SetShieldPanelState(true);
    }


    void Update()
    {
        // bloqueia flap/crash antes de iniciar ou após crash/GameOver
        if (crashSequenceRun ||
            GameManager.Instance == null ||
           !GameManager.Instance.IsGameStarted ||
            GameManager.Instance.IsGameOver)
            return;

        if (!gravityRestored)
        {
            rb.useGravity     = true;
            gravityRestored   = true;
        }

        // detecta flap
        bool flapInput = false;
    #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Mouse.current?.leftButton.wasPressedThisFrame == true ||
            Keyboard.current?.spaceKey.wasPressedThisFrame == true ||
            Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true)
            flapInput = true;
    #else
        if (Input.GetMouseButtonDown(0) ||
            Input.GetKeyDown(KeyCode.Space) ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            flapInput = true;
    #endif

        if (flapInput)
        {
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(Vector3.up * flapForce, ForceMode.Impulse);

            // shockwave
            if (shockwavePrefab != null)
            {
                Vector3 pos = transform.position + shockwaveOffset;
                var sw = Instantiate(shockwavePrefab, pos, transform.rotation);
                Destroy(sw, shockwaveLifetime);
            }
            // som de flap
            if (audioSource != null && flapClip != null)
                audioSource.PlayOneShot(flapClip);
        }
    }

    // ---------- LÓGICA DE COLISÃO ATUALIZADA ----------
    void OnCollisionEnter(Collision collision)
    {
        // 1. Ignora colisões se o jogo não estiver em um estado ativo (já caiu, não começou, etc.)
        if (crashSequenceRun || GameManager.Instance == null || !GameManager.Instance.IsGameStarted || GameManager.Instance.IsGameOver)
        {
            return;
        }

        // 2. Verifica se a colisão foi com um objeto perigoso (Meteoro ou Base)
        bool isDangerousCollision = collision.collider.CompareTag("Meteor") || collision.collider.CompareTag("Base");

        if (isDangerousCollision)
        {
            // 3. VERIFICAÇÃO PRINCIPAL: O escudo está ativo?
            if (isShieldActive)
            {
                // Se o escudo está ativo, protege o jogador
                DeactivateShield(); 
                
                // Se foi um meteoro, destrói o meteoro
                if(collision.collider.CompareTag("Meteor"))
                {
                    Destroy(collision.gameObject);
                }
                
                // IMPORTANTE: Interrompe a execução do método aqui para garantir que a lógica de GameOver não seja chamada.
                return; 
            }
            
            // 4. Se a colisão é perigosa E o escudo NÃO estava ativo, inicia a sequência de Game Over.
            crashSequenceRun = true;
            StartCoroutine(CrashSequence());
        }
    }

    private IEnumerator CrashSequence()
    {
        // trava a nave no ponto de colisão
        rb.linearVelocity   = Vector3.zero;
        rb.isKinematic = true;
        col.enabled    = false;

        // curto-circuito
        if (shortCircuitPrefab != null)
            Instantiate(shortCircuitPrefab, transform.position, transform.rotation);

        yield return new WaitForSeconds(shortCircuitDuration);

        // explosão
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, transform.rotation);

        if (audioSource != null && explosionClip != null)
            audioSource.PlayOneShot(explosionClip);

        // remove nave
        gameObject.SetActive(false);

        // sinaliza Game Over
        GameManager.Instance.GameOver();
    }

    public void ResetState()
    {
        // Reseta o estado para permitir que o jogador continue.
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero; // Zera a velocidade para evitar quedas bruscas.
        col.enabled = true;
        crashSequenceRun = false;
        gravityRestored = true;
    }

    public void ResetToPreStartState()
    {
        // Reseta as flags de controle
        crashSequenceRun = false;
        gravityRestored = false;

        // Desativa o escudo (isso também reativará o painel do botão)
        DeactivateShield();

        // Reativa o colisor e o próprio script
        col.enabled = true;
        this.enabled = true;

        // Reseta a física para o estado inicial "flutuante"
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}