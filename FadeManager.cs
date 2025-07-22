using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance { get; private set; }

    [Tooltip("Image preta full-screen usada para o fade")]
    public Image fadeImage;

    [Tooltip("Duração em segundos do fade-out/in")]
    public float fadeDuration = 1f;

    void Awake()
    {
        // Singleton + Persistência
        if (Instance == null)
        {
            Instance = this;
            // Garante que TODO este GameObject (incluindo Canvas e fadeImage) persista
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Começa transparente
        fadeImage.color = new Color(0, 0, 0, 0);
    }

    /// <summary>
    /// Inicia o fade-out, carrega a cena e faz fade-in.
    /// </summary>
    public void FadeToScene(string sceneName)
    {
        // Se não houver imagem (por algum motivo), carrega direto
        if (fadeImage == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        StartCoroutine(FadeOutIn(sceneName));
    }

    private IEnumerator FadeOutIn(string sceneName)
    {
        // FADE-OUT
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, a);
            yield return null;
        }
        fadeImage.color = Color.black;

        // CARREGA A CENA
        SceneManager.LoadScene(sceneName);

        // ESPERA um frame para garantir que a cena carregou
        yield return null;

        // FADE-IN
        t = fadeDuration;
        while (t > 0f)
        {
            t -= Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, a);
            yield return null;
        }
        fadeImage.color = Color.clear;
    }
}
