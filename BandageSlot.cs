using UnityEngine;

public class BandageSlot : MonoBehaviour
{
    [Header("Configurações do Slot")]
    [Tooltip("Nome identificador deste slot")]
    public string slotName = "Bandage Slot";
    
    [Header("Visual")]
    [Tooltip("Mostrar gizmo do slot no Scene View")]
    public bool showGizmo = true;
    
    [Tooltip("Cor do gizmo quando vazio")]
    public Color emptyColor = Color.green;
    
    [Tooltip("Cor do gizmo quando ocupado")]
    public Color occupiedColor = Color.red;
    
    // Estado interno
    private bool isOccupied = false;
    private GameObject spawnedItem = null;
    private BandageSpawnManager manager = null;
    
    public void Initialize(BandageSpawnManager spawnManager)
    {
        manager = spawnManager;
        isOccupied = false;
        spawnedItem = null;
        
        // Se o slot não tem nome, usar o nome do GameObject
        if (string.IsNullOrEmpty(slotName))
        {
            slotName = gameObject.name;
        }
    }
    
    public void SetOccupied(GameObject item)
    {
        isOccupied = true;
        spawnedItem = item;
    }
    
    public void SetEmpty()
    {
        isOccupied = false;
        spawnedItem = null;
    }
    
    public bool IsOccupied()
    {
        return isOccupied;
    }
    
    public GameObject GetSpawnedItem()
    {
        return spawnedItem;
    }
    
    public string GetSlotName()
    {
        return slotName;
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmo) return;
        
        // Escolher cor baseado no estado
        Gizmos.color = isOccupied ? occupiedColor : emptyColor;
        
        // Desenhar esfera
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        
        // Desenhar seta para cima
        Vector3 arrowStart = transform.position;
        Vector3 arrowEnd = arrowStart + Vector3.up * 0.5f;
        Gizmos.DrawLine(arrowStart, arrowEnd);
        
        // Ponta da seta
        Vector3 arrowTip1 = arrowEnd + Vector3.left * 0.1f + Vector3.down * 0.1f;
        Vector3 arrowTip2 = arrowEnd + Vector3.right * 0.1f + Vector3.down * 0.1f;
        Gizmos.DrawLine(arrowEnd, arrowTip1);
        Gizmos.DrawLine(arrowEnd, arrowTip2);
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        
        // Gizmo mais destacado quando selecionado
        Gizmos.color = isOccupied ? occupiedColor : emptyColor;
        Gizmos.DrawSphere(transform.position, 0.2f);
        
        // Label com informações
        #if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.8f, 
            $"{slotName}\n{(isOccupied ? "OCUPADO" : "VAZIO")}");
        #endif
    }
    
    // Para debug - forçar spawnar bandagem neste slot específico
    [ContextMenu("Force Spawn Bandage Here")]
    void ForceSpawnBandageHere()
    {
        if (Application.isPlaying && manager != null && !isOccupied)
        {
            // Temporariamente fazer este ser o único slot disponível
            // (hack para forçar spawn aqui)
            Vector3 originalPos = transform.position;
            
            // Mover temporariamente outros slots para longe
            BandageSlot[] allSlots = FindObjectsOfType<BandageSlot>();
            Vector3[] originalPositions = new Vector3[allSlots.Length];
            
            for (int i = 0; i < allSlots.Length; i++)
            {
                if (allSlots[i] != this)
                {
                    originalPositions[i] = allSlots[i].transform.position;
                    allSlots[i].transform.position = Vector3.up * 1000f; // Mover para longe
                }
            }
            
            // Forçar respawn
            manager.SpawnBandageInRandomSlot();
            
            // Restaurar posições
            for (int i = 0; i < allSlots.Length; i++)
            {
                if (allSlots[i] != this)
                {
                    allSlots[i].transform.position = originalPositions[i];
                }
            }
        }
    }
}