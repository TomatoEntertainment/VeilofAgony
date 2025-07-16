using UnityEngine;
using System.Collections;

public class MedievalGate : MonoBehaviour
{
    [Header("Configurações do Portão")]
    [Tooltip("Nome do portão para identificação")]
    public string gateName = "Portão Medieval";
    
    [Header("Configurações de Movimento")]
    [Tooltip("Altura que o portão irá subir")]
    public float openHeight = 5f;
    
    [Tooltip("Velocidade de abertura/fechamento")]
    public float moveSpeed = 2f;
    
    [Tooltip("Tempo que o portão fica aberto antes de fechar")]
    public float openDuration = 10f;
    
    [Tooltip("Fechar automaticamente após o tempo")]
    public bool autoClose = true;
    
    [Header("Efeitos Sonoros")]
    [Tooltip("Som ao abrir o portão")]
    public AudioSource openSound;
    
    [Tooltip("Som ao fechar o portão")]
    public AudioSource closeSound;
    
    [Tooltip("Som do mecanismo movendo")]
    public AudioSource mechanismSound;
    
    [Header("Estado")]
    [SerializeField] private bool isOpen = false;
    [SerializeField] private bool isMoving = false;
    [SerializeField] private bool isClosing = false;
    
    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Coroutine movementCoroutine;
    private Coroutine autoCloseCoroutine;
    
    void Start()
    {
        closedPosition = transform.position;
        openPosition = closedPosition + Vector3.up * openHeight;
        
        Debug.Log($"Portão {gateName} inicializado. Posição fechada: {closedPosition}");
    }
    
    public void ToggleGate()
    {
        Debug.Log($"ToggleGate chamado para {gateName}. Estado atual: isOpen={isOpen}, isMoving={isMoving}");
        
        if (isMoving)
        {
            Debug.Log($"Portão {gateName} já está em movimento! Operação cancelada.");
            return;
        }
        
        if (isOpen)
        {
            Debug.Log($"Portão {gateName} está aberto, chamando CloseGate()");
            CloseGate();
        }
        else
        {
            Debug.Log($"Portão {gateName} está fechado, chamando OpenGate()");
            OpenGate();
        }
    }
    
    public void OpenGate()
    {
        Debug.Log($"OpenGate chamado para {gateName}. Estado: isOpen={isOpen}, isMoving={isMoving}");
        
        if (isOpen || isMoving)
        {
            Debug.Log($"Portão {gateName} já está aberto ou em movimento! Operação cancelada.");
            return;
        }
        
        Debug.Log($"Iniciando abertura do portão {gateName}...");
        isClosing = false; // Limpar flag de fechamento quando abrir
        
        if (autoCloseCoroutine != null)
        {
            Debug.Log($"Parando corrotina de auto-close existente");
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }
        
        if (movementCoroutine != null)
        {
            Debug.Log($"Parando corrotina de movimento existente");
            StopCoroutine(movementCoroutine);
        }
        
        Debug.Log($"Iniciando corrotina de movimento para posição: {openPosition}");
        movementCoroutine = StartCoroutine(MoveGate(openPosition, true));
    }
    
    public void CloseGate()
    {
        if (!isOpen || isMoving)
        {
            Debug.Log($"Portão {gateName} já está fechado ou em movimento!");
            return;
        }
        
        Debug.Log($"Fechando portão {gateName}...");
        isClosing = true;
        
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }
        
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }
        movementCoroutine = StartCoroutine(MoveGate(closedPosition, false));
    }
    
    IEnumerator MoveGate(Vector3 targetPosition, bool opening)
    {
        Debug.Log($"Corrotina MoveGate iniciada. Opening={opening}, Target={targetPosition}, Start={transform.position}");
        
        isMoving = true;
        Vector3 startPosition = transform.position;
        float journey = 0f;
        
        if (opening && openSound != null)
        {
            Debug.Log($"Tocando som de abertura");
            openSound.Play();
        }
        else if (!opening && closeSound != null)
        {
            Debug.Log($"Tocando som de fechamento");
            closeSound.Play();
        }
        
        if (mechanismSound != null)
        {
            Debug.Log($"Tocando som do mecanismo");
            mechanismSound.Play();
        }
        
        while (journey <= 1f)
        {
            journey += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(startPosition, targetPosition, journey);
            
            yield return null;
        }
        
        Debug.Log($"Movimento concluído. Posição final: {transform.position}");
        
        transform.position = targetPosition;
        isMoving = false;
        isOpen = opening;
        
        if (!opening)
        {
            isClosing = false;
            Debug.Log($"Portão {gateName} terminou de fechar. isClosing = false");
        }
        else
        {
            Debug.Log($"Portão {gateName} terminou de abrir. Aguardando {openDuration} segundos para auto-close.");
        }
        
        if (mechanismSound != null)
        {
            mechanismSound.Stop();
        }
        
        Debug.Log($"Portão {gateName} {(opening ? "aberto" : "fechado")} com sucesso!");
        
        if (opening && autoClose)
        {
            Debug.Log($"Iniciando auto-close em {openDuration} segundos");
            autoCloseCoroutine = StartCoroutine(AutoCloseAfterDelay());
        }
        
        movementCoroutine = null;
    }
    
    IEnumerator AutoCloseAfterDelay()
    {
        Debug.Log($"Portão {gateName} iniciando contagem regressiva para auto-close: {openDuration} segundos");
        yield return new WaitForSeconds(openDuration);
        
        if (isOpen && !isMoving)
        {
            Debug.Log($"Auto-fechando portão {gateName} após {openDuration} segundos");
            CloseGate();
        }
        else
        {
            Debug.Log($"Auto-close cancelado para portão {gateName} (isOpen: {isOpen}, isMoving: {isMoving})");
        }
        
        autoCloseCoroutine = null;
    }
    
    public bool IsOpen()
    {
        return isOpen;
    }
    
    public bool IsMoving()
    {
        return isMoving;
    }
    
    public bool IsClosing()
    {
        return isClosing;
    }
    
    public string GetGateName()
    {
        return gateName;
    }
    
    public void SetOpenHeight(float height)
    {
        openHeight = height;
        openPosition = closedPosition + Vector3.up * openHeight;
    }
    
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }
    
    public void SetAutoClose(bool autoCloseEnabled, float duration = 10f)
    {
        autoClose = autoCloseEnabled;
        openDuration = duration;
    }
    
    public void StopMovement()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }
        
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }
        
        isMoving = false;
        
        if (mechanismSound != null)
        {
            mechanismSound.Stop();
        }
        
        Debug.Log($"Movimento do portão {gateName} interrompido!");
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 closedPos = Application.isPlaying ? closedPosition : transform.position;
        Gizmos.DrawWireCube(closedPos, transform.localScale);
        
        Gizmos.color = Color.green;
        Vector3 openPos = closedPos + Vector3.up * openHeight;
        Gizmos.DrawWireCube(openPos, transform.localScale);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(closedPos, openPos);
        
        Gizmos.color = Color.blue;
        Vector3 arrowStart = closedPos + Vector3.up * (openHeight * 0.5f);
        Vector3 arrowEnd = arrowStart + Vector3.up * 0.5f;
        Gizmos.DrawLine(arrowStart, arrowEnd);
        
        Vector3 arrowTip1 = arrowEnd + Vector3.left * 0.2f + Vector3.down * 0.2f;
        Vector3 arrowTip2 = arrowEnd + Vector3.right * 0.2f + Vector3.down * 0.2f;
        Gizmos.DrawLine(arrowEnd, arrowTip1);
        Gizmos.DrawLine(arrowEnd, arrowTip2);
    }
}