using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class AbductableCoin : MonoBehaviour
{
    [Header("Abdução")]
    [Tooltip("Velocidade com que a moeda voa para a nave")]
    public float abductionSpeed = 6f;

    [Tooltip("Distância para considerar coleta completa")]
    public float collectDistance = 0.1f;

    [Header("Deslocamento antes da abdução")]
    [Tooltip("Velocidade de movimento horizontal (direita → esquerda)")]
    public float moveSpeed = 2f;

    private bool    isAbducting = false;
    private Transform target;

    void Update()
    {
        // enquanto não começa abdução, desliza para a esquerda
        if (!isAbducting)
            transform.Translate(Vector3.left * moveSpeed * Time.deltaTime, Space.World);
    }

    /// <summary>
    /// Inicia a abdução, chamada pelo CoinCollector.
    /// </summary>
    public void StartAbduction(Transform shipRoot)
    {
        if (isAbducting) return;
        isAbducting = true;
        target      = shipRoot;
        GetComponent<Collider>().enabled = false; // evita retrigger

        StartCoroutine(AbductRoutine());
    }

    private IEnumerator AbductRoutine()
    {
        while (true)
        {
            // move em direção à nave
            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                abductionSpeed * Time.deltaTime
            );

            // se chegou perto o suficiente, coleto e destruo
            if (Vector3.Distance(transform.position, target.position) <= collectDistance)
            {
                GameManager.Instance.CollectCoin();
                Destroy(gameObject);
                yield break;
            }

            yield return null;
        }
    }
}
