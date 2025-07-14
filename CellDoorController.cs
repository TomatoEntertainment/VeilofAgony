using UnityEngine;
using System.Collections; // Necessário para usar Coroutines (para o delay)

public class CellDoorController : MonoBehaviour
{
    [Header("Configurações da Porta")]
    [Tooltip("O tempo em segundos que a porta demora a abrir depois de o jogador se curar.")]
    public float openDelay = 3.0f;

    [Tooltip("O som da porta a destrancar/abrir. Opcional.")]
    public AudioSource doorSound;

    [Header("Referências")]
    [Tooltip("Arraste o objeto do Jogador para aqui.")]
    public PlayerHealth playerHealth;

    private Animator doorAnimator;
    private bool isLocked = true;

    void Start()
    {
        doorAnimator = GetComponent<Animator>();

        // Garante que o jogador começa ferido.
        if (playerHealth != null)
        {
            // Aplica 2 hits de dano para deixar o jogador no "nível 2 de ferido".
            playerHealth.ApplyInitialDamage(2); 
        }
    }

    /// <summary>
    /// Método público que será chamado pelo jogador quando ele se curar.
    /// </summary>
    public void UnlockAndOpen()
    {
        if (isLocked)
        {
            isLocked = false;
            Debug.Log("A porta da cela foi destrancada! A aguardar para abrir...");
            StartCoroutine(OpenDoorAfterDelay());
        }
    }

    private IEnumerator OpenDoorAfterDelay()
    {
        // 1. Espera pelo tempo definido na variável openDelay.
        yield return new WaitForSeconds(openDelay);

        // 2. Toca o som da porta (se houver).
        if (doorSound != null)
        {
            doorSound.Play();
        }

        // 3. Ativa a animação de abertura da porta.
        if (doorAnimator != null)
        {
            Debug.Log("A abrir a porta da cela...");
            doorAnimator.SetTrigger("Open");
        }
    }
}