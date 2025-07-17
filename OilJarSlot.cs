using UnityEngine;

public class OilJarSlot : MonoBehaviour
{
    [Header("Configurações do Slot")]
    [Tooltip("Nome identificador deste slot")]
    public string slotName = "Oil Jar Slot";
    
    [Tooltip("Lado do mapa que este slot pertence")]
    public OilJarSpawnManager.MapSide mapSide = OilJarSpawnManager.MapSide.Left;
    
    [Header("Visual")]
    [Tooltip("Mostrar gizmo do slot no Scene View")]
    public bool showGizmo = true;
    
    [Tooltip("Cor do gizmo quando vazio")]
    public Color emptyColor = Color.cyan;
    
    [Tooltip("Cor do gizmo quando ocupado")]
    public Color occupiedColor = Color.magenta;
    
    // Estado interno
    private bool isOccupied = false;
    private GameObject spawnedItem = null;
    private OilJarSpawnManager manager = null;
    
    public void Initialize(OilJarSpawnManager spawnManager, OilJarSpawnManager.MapSide side)
    {
        manager = spawnManager;
        mapSide = side;
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
    
    public OilJarSpawnManager.MapSide GetMapSide()
    {
        return mapSide;
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmo) return;
        
        // Escolher cor baseado no estado e lado
        Color gizmoColor = isOccupied ? occupiedColor : emptyColor;
        
        // Modificar cor baseado no lado do mapa
        if (mapSide == OilJarSpawnManager.MapSide.Left)
        {
            gizmoColor = Color.Lerp(gizmoColor, Color.blue, 0.3f);
        }
        else
        {
            gizmoColor = Color.Lerp(gizmoColor, Color.red, 0.3f);
        }
        
        Gizmos.color = gizmoColor;
        
        // Desenhar esfera
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        
        // Desenhar seta para cima
        Vector3 arrowStart = transform.position;
        Vector3 arrowEnd = arrowStart + Vector3.up * 0.6f;
        Gizmos.DrawLine(arrowStart, arrowEnd);
        
        // Ponta da seta
        Vector3 arrowTip1 = arrowEnd + Vector3.left * 0.15f + Vector3.down * 0.15f;
        Vector3 arrowTip2 = arrowEnd + Vector3.right * 0.15f + Vector3.down * 0.15f;
        Gizmos.DrawLine(arrowEnd, arrowTip1);
        Gizmos.DrawLine(arrowEnd, arrowTip2);
        
        // Desenhar indicador do lado (L ou R)
        Vector3 sideIndicator = transform.position + Vector3.up * 0.8f;
        string sideText = mapSide == OilJarSpawnManager.MapSide.Left ? "L" : "R";
        
        #if UNITY_EDITOR
        UnityEditor.Handles.color = gizmoColor;
        UnityEditor.Handles.Label(sideIndicator, sideText);
        #endif
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        
        // Gizmo mais destacado quando selecionado
        Color gizmoColor = isOccupied ? occupiedColor : emptyColor;
        
        if (mapSide == OilJarSpawnManager.MapSide.Left)
        {
            gizmoColor = Color.Lerp(gizmoColor, Color.blue, 0.3f);
        }
        else
        {
            gizmoColor = Color.Lerp(gizmoColor, Color.red, 0.3f);
        }
        
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, 0.3f);
        
        // Label com informações detalhadas
        #if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        string sideText = mapSide == OilJarSpawnManager.MapSide.Left ? "ESQUERDA" : "DIREITA";
        string statusText = isOccupied ? "OCUPADO" : "VAZIO";
        
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.0f, 
            $"{slotName}\nLado: {sideText}\nStatus: {statusText}");
        #endif
    }
    
    // Para debug - forçar spawnar jarro neste slot específico
    [ContextMenu("Force Spawn Oil Jar Here")]
    void ForceSpawnOilJarHere()
    {
        if (Application.isPlaying && manager != null && !isOccupied)
        {
            manager.SpawnJarInSide(mapSide);
        }
    }
    
    // Método para mudar o lado do slot no editor
    [ContextMenu("Toggle Map Side")]
    void ToggleMapSide()
    {
        mapSide = mapSide == OilJarSpawnManager.MapSide.Left ? 
                  OilJarSpawnManager.MapSide.Right : 
                  OilJarSpawnManager.MapSide.Left;
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
}