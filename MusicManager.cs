using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    private static MusicManager instance;

    void Awake()
    {
        // Se já existir uma instância, destrói esta duplicata
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Esta é a instância única: impede que seja destruída ao trocar de cena
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
