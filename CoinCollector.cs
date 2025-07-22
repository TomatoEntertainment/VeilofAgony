using UnityEngine;

public class CoinCollector : MonoBehaviour
{
    [Tooltip("Velocidade de abdução")]
    public float abductionSpeed    = 6f;
    [Tooltip("Distância mínima para coletar")]
    public float collectDistance   = 0.1f;

    void OnTriggerEnter(Collider other)
    {
        // tenta pegar o componente AbductableCoin
        AbductableCoin coin = other.GetComponent<AbductableCoin>();
        if (coin == null) return;

        // configura os valores que ainda existem
        coin.abductionSpeed   = abductionSpeed;
        coin.collectDistance  = collectDistance;

        // inicia a abdução
        coin.StartAbduction(transform.root);
    }
}
