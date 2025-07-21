using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BandageSpawnManager : MonoBehaviour
{
    [Header("Configurações de Spawn")]
    [Tooltip("Prefab da bandagem que será spawnado")]
    public GameObject bandagePrefab;
    
    [Tooltip("Quantidade máxima de bandagens no mapa")]
    public int maxBandagesInMap = 4;
    
    [Header("Slots de Bandagem")]
    [Tooltip("Todos os slots onde bandagens podem spawnar")]
    public BandageSlot[] allBandageSlots;
    
    [Header("Debug")]
    [Tooltip("Mostrar logs de debug")]
    public bool showDebugLogs = true;
    
    // Singleton
    public static BandageSpawnManager Instance { get; private set; }
    
    // Controle interno
    private List<BandageSlot> availableSlots = new List<BandageSlot>();
    private List<BandageSlot> occupiedSlots = new List<BandageSlot>();
    private int currentBandageCount = 0;
    
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
    }
    
    void Start()
    {
        InitializeSlots();
        SpawnInitialBandages();
    }
    
    void InitializeSlots()
    {
        if (allBandageSlots == null || allBandageSlots.Length == 0)
        {
            Debug.LogError("BandageSpawnManager: Nenhum slot de bandagem foi definido!");
            return;
        }
        
        // Limpar listas
        availableSlots.Clear();
        occupiedSlots.Clear();
        
        // Adicionar todos os slots como disponíveis
        foreach (BandageSlot slot in allBandageSlots)
        {
            if (slot != null)
            {
                availableSlots.Add(slot);
                slot.Initialize(this);
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"BandageSpawnManager: {availableSlots.Count} slots de bandagem inicializados");
        }
    }
    
    void SpawnInitialBandages()
    {
        if (bandagePrefab == null)
        {
            Debug.LogError("BandageSpawnManager: bandagePrefab não foi definido!");
            return;
        }
        
        // Spawnar bandagens iniciais
        for (int i = 0; i < maxBandagesInMap; i++)
        {
            SpawnBandageInRandomSlot();
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"BandageSpawnManager: {currentBandageCount} bandagens spawnadas inicialmente");
        }
    }
    
    public void SpawnBandageInRandomSlot()
    {
        if (availableSlots.Count == 0)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("BandageSpawnManager: Nenhum slot disponível para spawnar bandagem");
            }
            return;
        }
        
        // Escolher slot aleatório
        int randomIndex = Random.Range(0, availableSlots.Count);
        BandageSlot selectedSlot = availableSlots[randomIndex];
        
        // Spawnar bandagem no slot
        GameObject bandageInstance = Instantiate(bandagePrefab, selectedSlot.transform.position, selectedSlot.transform.rotation);
        
        // Configurar a bandagem
        Interactable bandageComponent = bandageInstance.GetComponent<Interactable>();
        if (bandageComponent != null)
        {
            // Registrar callback para quando a bandagem for usada
            StartCoroutine(WaitForBandageDestroy(bandageInstance, selectedSlot));
        }
        
        // Atualizar listas
        availableSlots.Remove(selectedSlot);
        occupiedSlots.Add(selectedSlot);
        selectedSlot.SetOccupied(bandageInstance);
        currentBandageCount++;
        
        if (showDebugLogs)
        {
            Debug.Log($"BandageSpawnManager: Bandagem spawnada em {selectedSlot.name}. Total: {currentBandageCount}");
        }
    }
    
    System.Collections.IEnumerator WaitForBandageDestroy(GameObject bandageInstance, BandageSlot slot)
    {
        // Aguardar até a bandagem ser destruída
        while (bandageInstance != null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Bandagem foi usada/destruída
        OnBandageUsed(slot);
    }
    
    public void OnBandageUsed(BandageSlot slot)
    {
        if (slot == null) return;
        
        // Liberar o slot
        occupiedSlots.Remove(slot);
        availableSlots.Add(slot);
        slot.SetEmpty();
        currentBandageCount--;
        
        if (showDebugLogs)
        {
            Debug.Log($"BandageSpawnManager: Bandagem usada em {slot.name}. Restam: {currentBandageCount}");
        }
        
        // Spawnar nova bandagem em outro slot para manter sempre 4
        if (currentBandageCount < maxBandagesInMap)
        {
            // Delay pequeno antes de spawnar nova
            Invoke(nameof(SpawnBandageInRandomSlot), 0.5f);
        }
    }
    
    [ContextMenu("Respawn All Bandages")]
    public void RespawnAllBandages()
    {
        // Limpar bandagens existentes
        ClearAllBandages();
        
        // Reinicializar
        InitializeSlots();
        SpawnInitialBandages();
    }
    
    [ContextMenu("Clear All Bandages")]
    public void ClearAllBandages()
    {
        // Destruir todas as bandagens existentes
        foreach (BandageSlot slot in occupiedSlots.ToList())
        {
            if (slot != null && slot.GetSpawnedItem() != null)
            {
                Destroy(slot.GetSpawnedItem());
                slot.SetEmpty();
            }
        }
        
        // Resetar contadores
        currentBandageCount = 0;
        occupiedSlots.Clear();
        
        // Todos os slots ficam disponíveis
        availableSlots.Clear();
        foreach (BandageSlot slot in allBandageSlots)
        {
            if (slot != null)
            {
                availableSlots.Add(slot);
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log("BandageSpawnManager: Todas as bandagens foram removidas");
        }
    }
    
    // Métodos públicos para informações
    public int GetCurrentBandageCount() { return currentBandageCount; }
    public int GetAvailableSlotCount() { return availableSlots.Count; }
    public int GetTotalSlotCount() { return allBandageSlots != null ? allBandageSlots.Length : 0; }
    
    void OnDrawGizmos()
    {
        if (allBandageSlots == null) return;
        
        // Desenhar conexões para todos os slots
        Gizmos.color = Color.green;
        foreach (BandageSlot slot in allBandageSlots)
        {
            if (slot != null)
            {
                Gizmos.DrawLine(transform.position, slot.transform.position);
                Gizmos.DrawWireSphere(slot.transform.position, 0.3f);
            }
        }
    }
}