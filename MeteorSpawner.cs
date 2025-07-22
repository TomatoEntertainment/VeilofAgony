using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnStage
{
    [Tooltip("A partir de qual distância usar estes prefabs")]
    public float minDistance;
    [Tooltip("Prefabs de meteoro para este estágio")]
    public GameObject[] prefabs;
}

[System.Serializable]
public class IntervalStage
{
    [Tooltip("A partir de qual distância usar este intervalo de spawn")]
    public float minDistance;
    [Tooltip("Novo intervalo (segundos) entre spawns")]
    public float spawnInterval;
}

public class MeteorSpawner : MonoBehaviour
{
    public static MeteorSpawner Instance { get; private set; }

    [Header("Spawn Config")]
    public Transform    spawnPoint;
    public float        minYOffset    = -3f;
    public float        maxYOffset    =  3f;
    [Tooltip("Intervalo padrão entre spawns")]
    public float        defaultInterval = 2f;
    [Header("Dynamic Interval by Distance")]
    [Tooltip("Configuração de intervalos baseados na distância")]
    public IntervalStage[] intervalStages;

    [Header("Movement")]
    [Tooltip("Velocidade base de movimento dos meteoros")]
    public float        moveSpeed     = 5f;

    [Header("Prefabs")]
    [Tooltip("Prefabs iniciais usados antes do primeiro estágio")]
    public GameObject[] defaultPrefabs;
    [Tooltip("Configuração de mudança de prefab por distância")]
    public SpawnStage[] spawnStages;

    [Header("Warning Indicator")]
    public WarningIndicator warningIndicator;
    [Tooltip("Ajuste vertical do indicador para centralizar")]
    public float        warningVerticalMargin = 0f;

    private List<GameObject> spawnedMeteors = new List<GameObject>();
    private Coroutine        spawnRoutine;
    private float            currentInterval;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void OnEnable()  => StartSpawning();
    void OnDisable() => StopSpawning();

    public void StartSpawning()
    {
        StopSpawning();
        currentInterval = defaultInterval;
        spawnRoutine    = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    public void ClearAllMeteors()
    {
        foreach (var m in spawnedMeteors)
            if (m != null) Destroy(m);
        spawnedMeteors.Clear();
    }

    public void IncreaseMeteorSpeed(float amount)
    {
        moveSpeed += amount;
    }

    private IEnumerator SpawnLoop()
    {
        // espera o jogo começar
        yield return new WaitUntil(() => GameManager.Instance.IsGameStarted);

        // enquanto o jogo não acabar
        while (!GameManager.Instance.IsGameOver)
        {
            SpawnMeteor();
            UpdateSpawnInterval();
            yield return new WaitForSeconds(currentInterval);
        }
    }

    private void UpdateSpawnInterval()
    {
        float dist = GameManager.Instance.DistanceTravelled;
        currentInterval = defaultInterval;

        // aplica o estágio de intervalo mais avançado
        foreach (var stage in intervalStages)
        {
            if (dist >= stage.minDistance)
                currentInterval = stage.spawnInterval;
            else
                break;
        }
    }

    private void SpawnMeteor()
    {
        if (spawnPoint == null || defaultPrefabs == null || defaultPrefabs.Length == 0)
            return;

        // 1) Escolhe a lista de prefabs conforme a distância
        float dist = GameManager.Instance.DistanceTravelled;
        GameObject[] chooseFrom = defaultPrefabs;
        foreach (var stage in spawnStages)
        {
            if (dist >= stage.minDistance && stage.prefabs != null && stage.prefabs.Length > 0)
                chooseFrom = stage.prefabs;
        }

        // 2) Calcula posição aleatória em Y
        float y = spawnPoint.position.y + Random.Range(minYOffset, maxYOffset);
        Vector3 spawnPos = new Vector3(spawnPoint.position.x, y, spawnPoint.position.z);

        // 3) Exibe o aviso alinhado ao spawn
        if (warningIndicator != null)
        {
            warningIndicator.verticalMargin = warningVerticalMargin;
            warningIndicator.ShowWarning(spawnPos);
        }

        // 4) Instancia o meteoro
        GameObject prefab = chooseFrom[Random.Range(0, chooseFrom.Length)];
        GameObject m = Instantiate(prefab, spawnPos, prefab.transform.rotation);
        spawnedMeteors.Add(m);

        // 5) Ajusta o componente de movimento
        var meteor = m.GetComponent<Meteor>() ?? m.AddComponent<Meteor>();
        meteor.moveSpeed = moveSpeed;
    }
}
