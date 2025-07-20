using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class OilJarSpawnManager : MonoBehaviour
{
    public enum MapSide
    {
        Left,
        Right
    }
    
    [Header("Configurações de Spawn")]
    [Tooltip("Prefab do jarro de óleo que será spawnado")]
    public GameObject oilJarPrefab;
    
    [Tooltip("Quantidade total de jarros no mapa")]
    public int totalJarsInMap = 4;
    
    [Tooltip("Quantidade de jarros por lado (deve ser totalJarsInMap/2)")]
    public int jarsPerSide = 2;
    
    [Header("Slots do Lado Esquerdo")]
    [Tooltip("Todos os slots do lado esquerdo do mapa")]
    public OilJarSlot[] leftSideSlots;
    
    [Header("Slots do Lado Direito")]
    [Tooltip("Todos os slots do lado direito do mapa")]
    public OilJarSlot[] rightSideSlots;
    
    [Header("Debug")]
    [Tooltip("Mostrar logs de debug")]
    public bool showDebugLogs = true;
    
    // Singleton
    public static OilJarSpawnManager Instance { get; private set; }
    
    // Controle interno
    private List<OilJarSlot> availableLeftSlots = new List<OilJarSlot>();
    private List<OilJarSlot> availableRightSlots = new List<OilJarSlot>();
    private List<OilJarSlot> occupiedLeftSlots = new List<OilJarSlot>();
    private List<OilJarSlot> occupiedRightSlots = new List<OilJarSlot>();
    
    private int currentLeftJars = 0;
    private int currentRightJars = 0;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Validar configuração
        ValidateConfiguration();
    }
    
    void Start()
    {
        InitializeSlots();
        SpawnInitialJars();
    }
    
    void ValidateConfiguration()
    {
        if (totalJarsInMap % 2 != 0)
        {
            Debug.LogWarning($"OilJarSpawnManager: totalJarsInMap ({totalJarsInMap}) deve ser par para dividir igualmente entre os lados!");
        }
        
        jarsPerSide = totalJarsInMap / 2;
        
        if (showDebugLogs)
        {
            Debug.Log($"OilJarSpawnManager: {jarsPerSide} jarros por lado (total: {totalJarsInMap})");
        }
    }
    
    void InitializeSlots()
    {
        // Inicializar slots do lado esquerdo
        availableLeftSlots.Clear();
        if (leftSideSlots != null)
        {
            foreach (OilJarSlot slot in leftSideSlots)
            {
                if (slot != null)
                {
                    slot.Initialize(this, MapSide.Left);
                    availableLeftSlots.Add(slot);
                }
            }
        }
        
        // Inicializar slots do lado direito
        availableRightSlots.Clear();
        if (rightSideSlots != null)
        {
            foreach (OilJarSlot slot in rightSideSlots)
            {
                if (slot != null)
                {
                    slot.Initialize(this, MapSide.Right);
                    availableRightSlots.Add(slot);
                }
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"OilJarSpawnManager: {availableLeftSlots.Count} slots esquerdos, {availableRightSlots.Count} slots direitos inicializados");
        }
    }
    
    void SpawnInitialJars()
    {
        if (oilJarPrefab == null)
        {
            Debug.LogError("OilJarSpawnManager: oilJarPrefab não foi definido!");
            return;
        }
        
        // Spawnar jarros no lado esquerdo
        for (int i = 0; i < jarsPerSide; i++)
        {
            SpawnJarInSide(MapSide.Left);
        }
        
        // Spawnar jarros no lado direito
        for (int i = 0; i < jarsPerSide; i++)
        {
            SpawnJarInSide(MapSide.Right);
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"OilJarSpawnManager: {currentLeftJars} jarros spawnados à esquerda, {currentRightJars} à direita");
        }
    }
    
    public void SpawnJarInSide(MapSide side)
    {
        List<OilJarSlot> availableSlots = side == MapSide.Left ? availableLeftSlots : availableRightSlots;
        List<OilJarSlot> occupiedSlots = side == MapSide.Left ? occupiedLeftSlots : occupiedRightSlots;
        
        if (availableSlots.Count == 0)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"OilJarSpawnManager: Nenhum slot disponível no lado {side}");
            }
            return;
        }
        
        // Escolher slot aleatório
        int randomIndex = Random.Range(0, availableSlots.Count);
        OilJarSlot selectedSlot = availableSlots[randomIndex];
        
        // Spawnar jarro no slot
        GameObject jarInstance = Instantiate(oilJarPrefab, selectedSlot.transform.position, selectedSlot.transform.rotation);
        
        // Configurar o jarro
        OilJar jarComponent = jarInstance.GetComponent<OilJar>();
        if (jarComponent != null)
        {
            // Registrar callback para quando o jarro for usado
            StartCoroutine(WaitForJarDestroy(jarInstance, selectedSlot, side));
        }
        
        // Atualizar listas
        availableSlots.Remove(selectedSlot);
        occupiedSlots.Add(selectedSlot);
        selectedSlot.SetOccupied(jarInstance);
        
        // Atualizar contadores
        if (side == MapSide.Left)
        {
            currentLeftJars++;
        }
        else
        {
            currentRightJars++;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"OilJarSpawnManager: Jarro spawnado no lado {side} em {selectedSlot.name}. Total {side}: {(side == MapSide.Left ? currentLeftJars : currentRightJars)}");
        }
    }
    
    System.Collections.IEnumerator WaitForJarDestroy(GameObject jarInstance, OilJarSlot slot, MapSide side)
    {
        // Aguardar até o jarro ser destruído
        while (jarInstance != null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Jarro foi usado/destruído
        OnJarUsed(slot, side);
    }
    
    public void OnJarUsed(OilJarSlot slot, MapSide side)
    {
        if (slot == null) return;
        
        // Determinar listas baseado no lado
        List<OilJarSlot> availableSlots = side == MapSide.Left ? availableLeftSlots : availableRightSlots;
        List<OilJarSlot> occupiedSlots = side == MapSide.Left ? occupiedLeftSlots : occupiedRightSlots;
        
        // Liberar o slot
        occupiedSlots.Remove(slot);
        availableSlots.Add(slot);
        slot.SetEmpty();
        
        // Atualizar contadores
        if (side == MapSide.Left)
        {
            currentLeftJars--;
        }
        else
        {
            currentRightJars--;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"OilJarSpawnManager: Jarro usado no lado {side} em {slot.name}. Restam {side}: {(side == MapSide.Left ? currentLeftJars : currentRightJars)}");
        }
        
        // Lógica inteligente: Spawnar no lado com menos jarros
        MapSide sideToSpawn = currentLeftJars < currentRightJars ? MapSide.Left : 
                             currentRightJars < currentLeftJars ? MapSide.Right : 
                             (Random.value > 0.5f ? MapSide.Left : MapSide.Right);
        
        // Delay pequeno antes de spawnar novo
        StartCoroutine(DelayedSpawn(sideToSpawn, 0.5f));
    }
    
    System.Collections.IEnumerator DelayedSpawn(MapSide side, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Verificar se ainda precisa spawnar
        int currentTotal = currentLeftJars + currentRightJars;
        if (currentTotal < totalJarsInMap)
        {
            SpawnJarInSide(side);
        }
    }
    
    [ContextMenu("Respawn All Jars")]
    public void RespawnAllJars()
    {
        ClearAllJars();
        InitializeSlots();
        SpawnInitialJars();
    }
    
    [ContextMenu("Clear All Jars")]
    public void ClearAllJars()
    {
        // Destruir jarros do lado esquerdo
        foreach (OilJarSlot slot in occupiedLeftSlots.ToList())
        {
            if (slot != null && slot.GetSpawnedItem() != null)
            {
                Destroy(slot.GetSpawnedItem());
                slot.SetEmpty();
            }
        }
        
        // Destruir jarros do lado direito
        foreach (OilJarSlot slot in occupiedRightSlots.ToList())
        {
            if (slot != null && slot.GetSpawnedItem() != null)
            {
                Destroy(slot.GetSpawnedItem());
                slot.SetEmpty();
            }
        }
        
        // Resetar contadores
        currentLeftJars = 0;
        currentRightJars = 0;
        occupiedLeftSlots.Clear();
        occupiedRightSlots.Clear();
        
        // Restaurar slots disponíveis
        availableLeftSlots.Clear();
        availableRightSlots.Clear();
        
        if (leftSideSlots != null)
        {
            availableLeftSlots.AddRange(leftSideSlots.Where(s => s != null));
        }
        
        if (rightSideSlots != null)
        {
            availableRightSlots.AddRange(rightSideSlots.Where(s => s != null));
        }
        
        if (showDebugLogs)
        {
            Debug.Log("OilJarSpawnManager: Todos os jarros foram removidos");
        }
    }
    
    // Métodos públicos para informações
    public int GetCurrentLeftJars() { return currentLeftJars; }
    public int GetCurrentRightJars() { return currentRightJars; }
    public int GetTotalCurrentJars() { return currentLeftJars + currentRightJars; }
    public int GetAvailableLeftSlots() { return availableLeftSlots.Count; }
    public int GetAvailableRightSlots() { return availableRightSlots.Count; }
    
    void OnDrawGizmos()
    {
        // Desenhar conexões para slots esquerdos (azul)
        if (leftSideSlots != null)
        {
            Gizmos.color = Color.blue;
            foreach (OilJarSlot slot in leftSideSlots)
            {
                if (slot != null)
                {
                    Gizmos.DrawLine(transform.position, slot.transform.position);
                }
            }
        }
        
        // Desenhar conexões para slots direitos (vermelho)
        if (rightSideSlots != null)
        {
            Gizmos.color = Color.red;
            foreach (OilJarSlot slot in rightSideSlots)
            {
                if (slot != null)
                {
                    Gizmos.DrawLine(transform.position, slot.transform.position);
                }
            }
        }
    }
}