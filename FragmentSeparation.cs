using UnityEngine;

public class FragmentSeparation : MonoBehaviour
{
    private Vector3 velocity;
    private float   duration;
    private float   timer;

    /// <summary>
    /// Velocidade (unidades/segundo) e duração do movimento.
    /// </summary>
    public void Initialize(Vector3 velocity, float duration)
    {
        this.velocity = velocity;
        this.duration = duration;
        this.timer    = 0f;
    }

    void Update()
    {
        // enquanto não atingir a duração, move
        if (timer < duration)
        {
            transform.position += velocity * Time.deltaTime;
            timer += Time.deltaTime;
        }
        else
        {
            // opcional: remover este componente quando acabar
            Destroy(this);
        }
    }
}
