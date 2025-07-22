using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video; // Essencial para controlar o VideoPlayer

public class ControladorAbertura : MonoBehaviour
{
    [Header("Configurações do Vídeo")]
    
    // Campo para arrastar o componente Video Player no Inspector
    [Tooltip("Arraste o componente Video Player que está na cena aqui.")]
    public VideoPlayer videoPlayer;

    [Header("Configurações de Transição")]

    [Tooltip("Nome da cena a ser carregada após o vídeo. Ex: 'MainMenu'")]
    public string nomeDaProximaCena;
    
    [Tooltip("Tempo MÁXIMO de espera em segundos. Usado como segurança caso o vídeo falhe.")]
    public float tempoMaximoDeEspera = 30.0f;


    void Start()
    {
        // --- Validações Iniciais ---
        if (videoPlayer == null)
        {
            Debug.LogError("O componente Video Player não foi atribuído no Inspector!");
            return;
        }

        if (string.IsNullOrEmpty(nomeDaProximaCena))
        {
            Debug.LogError("O nome da próxima cena não foi definido no Inspector!");
            return;
        }

        // Garante que o vídeo não comece a tocar sozinho
        videoPlayer.playOnAwake = false;

        // "Inscreve" a nossa função PularParaProximaCena para ser chamada quando o vídeo terminar.
        // O evento 'loopPointReached' é acionado quando o vídeo chega ao fim (se não estiver em loop).
        videoPlayer.loopPointReached += PularParaProximaCena;
        
        // Inicia o vídeo
        videoPlayer.Play();

        // Inicia o nosso timer de segurança, que pulará a cena de qualquer jeito após o tempo máximo.
        Invoke("PularParaProximaCena", tempoMaximoDeEspera);
    }

    // Esta função será chamada pelo evento do VideoPlayer ou pelo Invoke de segurança.
    // O argumento "source" é o próprio VideoPlayer que terminou.
    public void PularParaProximaCena(VideoPlayer source = null)
    {
        // Se a função já foi chamada uma vez, não faz nada (evita chamadas duplas)
        if (this.enabled == false) return;

        // Desativa o script para garantir que a cena só seja carregada uma vez.
        this.enabled = false;

        // Cancela a chamada de segurança do Invoke, já que não precisamos mais dela.
        CancelInvoke("PularParaProximaCena");

        // Remove a inscrição do evento para limpar a memória
        if(source != null)
        {
            source.loopPointReached -= PularParaProximaCena;
        }
        
        Debug.Log("Vídeo terminado ou tempo esgotado. Carregando próxima cena...");
        SceneManager.LoadScene(nomeDaProximaCena);
    }
}