using UnityEngine;

public class CoinSpawner : MonoBehaviour
{
    [Tooltip("Prefab da moeda (com AbductableCoin)")]
    public GameObject coinPrefab;

    [Tooltip("Tag usada pelos meteoros")]
    public string meteorTag = "Meteor";

    [Tooltip("Tempo (s) entre cada spawn de moeda")]
    public float spawnInterval = 2f;

    [Tooltip("Chance (0→1) de realmente spawnar uma moeda a cada intervalo")]
    [Range(0f, 1f)]
    public float spawnChance = 0.5f;

    [Tooltip("Raio de checagem para não spawnear dentro de um meteor")]
    public float avoidRadius = 1f;

    [Tooltip("Transform que indica o ponto de spawn (usa X/Y/Z desse transform)")]
    public Transform spawnPoint;

    [Tooltip("Posição X fixa de spawn (caso não tenha spawnPoint)")]
    public float spawnX = 10f;

    [Tooltip("Posição Z fixa de spawn (caso não tenha spawnPoint)")]
    public float spawnZ = 0f;

    [Tooltip("Limite mínimo e máximo de Y para spawn")]
    public float minY = -3.5f;
    public float maxY =  3.5f;

    private float timer;

    void Update()
    {
        // --- CORREÇÃO PRINCIPAL ---
        // Se o jogo ainda não começou ou se já terminou, o script não faz nada.
        if (GameManager.Instance == null || !GameManager.Instance.IsGameStarted || GameManager.Instance.IsGameOver)
            return;

        // verifica o timer
        timer += Time.deltaTime;
        if (timer < spawnInterval) return;
        timer = 0f;

        // chance de não spawnar
        if (Random.value > spawnChance)
            return;

        // escolhe posição de spawn
        float y = Random.Range(minY, maxY);
        float x = (spawnPoint != null) ? spawnPoint.position.x : spawnX;
        float z = (spawnPoint != null) ? spawnPoint.position.z : spawnZ;
        Vector3 pos = new Vector3(x, y, z);

        // aborta se estiver dentro do raio de um meteor
        Collider[] hits = Physics.OverlapSphere(pos, avoidRadius);
        foreach (var hit in hits)
            if (hit.CompareTag(meteorTag))
                return;

        // finalmente instancia a moeda
        Instantiate(coinPrefab, pos, Quaternion.identity);
    }

    // visualize o ponto de spawn e o raio de avoid no Scene view
    void OnDrawGizmosSelected()
    {
        float x = (spawnPoint != null) ? spawnPoint.position.x : spawnX;
        float z = (spawnPoint != null) ? spawnPoint.position.z : spawnZ;
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3(x, (minY + maxY) / 2f, z);
        Gizmos.DrawLine(new Vector3(x, minY, z), new Vector3(x, maxY, z));
        Gizmos.DrawWireSphere(center, avoidRadius);
    }
}