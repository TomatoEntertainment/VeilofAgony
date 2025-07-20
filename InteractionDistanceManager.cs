using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Gerencia as distâncias de interação em toda a cena, mantendo PlayerInteraction e ManualInteractionSystem sincronizados
/// </summary>
public class InteractionDistanceManager : MonoBehaviour
{
    [Header("Configurações Globais de Distância")]
    [Tooltip("Distância para interagir com objetos (raio verde)")]
    [Range(1f, 10f)]
    public float globalInteractionDistance = 3f;
    
    [Tooltip("Distância para mostrar ícone de interação (raio amarelo)")]
    [Range(2f, 15f)]
    public float globalIconVisibilityDistance = 6f;
    
    [Header("Referências")]
    [Tooltip("Referência ao PlayerInteraction (auto-detectado se vazio)")]
    public PlayerInteraction playerInteraction;
    
    [Header("Sincronização Automática")]
    [Tooltip("Sincronizar automaticamente ao iniciar")]
    public bool autoSyncOnStart = true;
    
    [Tooltip("Sincronizar automaticamente quando valores mudarem no Inspector")]
    public bool autoSyncOnValidate = true;
    
    [Tooltip("Atualizar continuamente (custoso, usar apenas para debug)")]
    public bool continuousUpdate = false;
    
    [Header("Debug Visual")]
    [Tooltip("Mostrar gizmos globais de distância")]
    public bool showGlobalGizmos = true;
    
    [Tooltip("Cor do gizmo de interação")]
    public Color interactionColor = Color.green;
    
    [Tooltip("Cor do gizmo de visibilidade do ícone")]
    public Color iconColor = Color.yellow;
    
    private float lastInteractionDistance;
    private float lastIconDistance;
    
    void Start()
    {
        // Auto-detectar PlayerInteraction se não foi atribuído
        if (playerInteraction == null)
        {
            playerInteraction = FindObjectOfType<PlayerInteraction>();
            if (playerInteraction == null)
            {
                Debug.LogWarning("InteractionDistanceManager: PlayerInteraction não encontrado na cena!");
                return;
            }
        }
        
        // Validar distâncias
        ValidateDistances();
        
        // Sincronizar automaticamente se habilitado
        if (autoSyncOnStart)
        {
            SyncAllDistances();
        }
        
        // Salvar valores iniciais para detecção de mudança
        lastInteractionDistance = globalInteractionDistance;
        lastIconDistance = globalIconVisibilityDistance;
    }
    
    void Update()
    {
        if (!continuousUpdate) return;
        
        // Verificar se as distâncias mudaram
        if (HasDistancesChanged())
        {
            ValidateDistances();
            SyncAllDistances();
            UpdateLastValues();
        }
    }
    
    void OnValidate()
    {
        if (!autoSyncOnValidate || !Application.isPlaying) return;
        
        ValidateDistances();
        
        // Delay para garantir que a validação aconteça após a mudança
        if (gameObject.activeInHierarchy)
        {
            Invoke(nameof(SyncAllDistances), 0.1f);
        }
    }
    
    bool HasDistancesChanged()
    {
        return !Mathf.Approximately(lastInteractionDistance, globalInteractionDistance) ||
               !Mathf.Approximately(lastIconDistance, globalIconVisibilityDistance);
    }
    
    void UpdateLastValues()
    {
        lastInteractionDistance = globalInteractionDistance;
        lastIconDistance = globalIconVisibilityDistance;
    }
    
    void ValidateDistances()
    {
        // Garantir que a distância do ícone seja maior que a de interação
        if (globalIconVisibilityDistance <= globalInteractionDistance)
        {
            globalIconVisibilityDistance = globalInteractionDistance * 2f;
            Debug.LogWarning($"InteractionDistanceManager: globalIconVisibilityDistance ajustada para {globalIconVisibilityDistance} (deve ser maior que globalInteractionDistance)");
        }
        
        // Garantir valores mínimos
        globalInteractionDistance = Mathf.Max(globalInteractionDistance, 1f);
        globalIconVisibilityDistance = Mathf.Max(globalIconVisibilityDistance, 2f);
    }
    
    [ContextMenu("Sincronizar Todas as Distâncias")]
    public void SyncAllDistances()
    {
        if (playerInteraction == null)
        {
            Debug.LogError("InteractionDistanceManager: PlayerInteraction não atribuído!");
            return;
        }
        
        int syncedCount = 0;
        
        // Sincronizar PlayerInteraction
        playerInteraction.UpdateInteractionDistances(globalInteractionDistance, globalIconVisibilityDistance);
        syncedCount++;
        
        // Sincronizar todos os ManualInteractionSystem
        ManualInteractionSystem[] allManualSystems = FindObjectsOfType<ManualInteractionSystem>();
        foreach (ManualInteractionSystem manualSystem in allManualSystems)
        {
            manualSystem.SyncDistances(globalIconVisibilityDistance, globalInteractionDistance);
            syncedCount++;
        }
        
        Debug.Log($"InteractionDistanceManager: {syncedCount} sistemas sincronizados! Interação: {globalInteractionDistance}m, Ícone: {globalIconVisibilityDistance}m");
        
        UpdateLastValues();
    }
    
    [ContextMenu("Detectar Distâncias Atuais")]
    public void DetectCurrentDistances()
    {
        if (playerInteraction != null)
        {
            globalInteractionDistance = playerInteraction.GetInteractionDistance();
            globalIconVisibilityDistance = playerInteraction.GetIconVisibilityDistance();
            
            Debug.Log($"InteractionDistanceManager: Distâncias detectadas - Interação: {globalInteractionDistance}m, Ícone: {globalIconVisibilityDistance}m");
        }
        else
        {
            Debug.LogWarning("InteractionDistanceManager: PlayerInteraction não encontrado para detectar distâncias!");
        }
    }
    
    [ContextMenu("Reset para Valores Padrão")]
    public void ResetToDefaults()
    {
        globalInteractionDistance = 3f;
        globalIconVisibilityDistance = 6f;
        
        if (Application.isPlaying)
        {
            SyncAllDistances();
        }
        
        Debug.Log("InteractionDistanceManager: Distâncias resetadas para valores padrão");
    }
    
    public void SetInteractionDistance(float newDistance)
    {
        globalInteractionDistance = Mathf.Max(newDistance, 1f);
        ValidateDistances();
        
        if (Application.isPlaying)
        {
            SyncAllDistances();
        }
    }
    
    public void SetIconVisibilityDistance(float newDistance)
    {
        globalIconVisibilityDistance = Mathf.Max(newDistance, 2f);
        ValidateDistances();
        
        if (Application.isPlaying)
        {
            SyncAllDistances();
        }
    }
    
    public void SetBothDistances(float interactionDist, float iconDist)
    {
        globalInteractionDistance = Mathf.Max(interactionDist, 1f);
        globalIconVisibilityDistance = Mathf.Max(iconDist, 2f);
        ValidateDistances();
        
        if (Application.isPlaying)
        {
            SyncAllDistances();
        }
    }
    
    // Métodos públicos para obter informações
    public float GetInteractionDistance() { return globalInteractionDistance; }
    public float GetIconVisibilityDistance() { return globalIconVisibilityDistance; }
    public int GetManualSystemsCount() 
    { 
        ManualInteractionSystem[] systems = FindObjectsOfType<ManualInteractionSystem>();
        return systems != null ? systems.Length : 0;
    }
    
    void OnDrawGizmos()
    {
        if (!showGlobalGizmos || playerInteraction == null) return;
        
        Vector3 playerPosition = playerInteraction.transform.position;
        
        // Desenhar esfera de interação (verde)
        Gizmos.color = interactionColor;
        Gizmos.DrawWireSphere(playerPosition, globalInteractionDistance);
        
        // Desenhar esfera de visibilidade do ícone (amarelo, mais transparente)
        Color iconColorWithAlpha = iconColor;
        iconColorWithAlpha.a = 0.3f;
        Gizmos.color = iconColorWithAlpha;
        Gizmos.DrawWireSphere(playerPosition, globalIconVisibilityDistance);
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showGlobalGizmos || playerInteraction == null) return;
        
        Vector3 playerPosition = playerInteraction.transform.position;
        
        // Desenhar círculos no chão
        Gizmos.color = interactionColor;
        DrawGroundCircle(playerPosition, globalInteractionDistance);
        
        Color iconColorWithAlpha = iconColor;
        iconColorWithAlpha.a = 0.5f;
        Gizmos.color = iconColorWithAlpha;
        DrawGroundCircle(playerPosition, globalIconVisibilityDistance);
        
        // Mostrar labels com distâncias (apenas no editor)
        #if UNITY_EDITOR
        UnityEditor.Handles.color = interactionColor;
        UnityEditor.Handles.Label(playerPosition + Vector3.up * 3f, $"Interação Global: {globalInteractionDistance}m");
        
        UnityEditor.Handles.color = iconColor;
        UnityEditor.Handles.Label(playerPosition + Vector3.up * 3.5f, $"Ícone Global: {globalIconVisibilityDistance}m");
        
        // Mostrar contador de sistemas
        UnityEditor.Handles.color = Color.white;
        int systemCount = GetManualSystemsCount();
        UnityEditor.Handles.Label(playerPosition + Vector3.up * 4f, $"Sistemas: {systemCount}");
        #endif
    }
    
    void DrawGroundCircle(Vector3 center, float radius)
    {
        Vector3 prevPos = Vector3.zero;
        for (int i = 0; i <= 360; i += 10)
        {
            float angle = i * Mathf.Deg2Rad;
            Vector3 newPos = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            
            if (i > 0)
            {
                Gizmos.DrawLine(prevPos, newPos);
            }
            prevPos = newPos;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(InteractionDistanceManager))]
public class InteractionDistanceManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Controles Rápidos", EditorStyles.boldLabel);
        
        InteractionDistanceManager manager = (InteractionDistanceManager)target;
        
        // Botões de ação
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Sincronizar Tudo", GUILayout.Height(25)))
        {
            manager.SyncAllDistances();
        }
        if (GUILayout.Button("Detectar Atual", GUILayout.Height(25)))
        {
            manager.DetectCurrentDistances();
        }
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("Reset Padrão", GUILayout.Height(20)))
        {
            manager.ResetToDefaults();
        }
        
        EditorGUILayout.Space();
        
        // Informações da cena
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("Informações da Cena", EditorStyles.boldLabel);
            
            int manualSystemsCount = manager.GetManualSystemsCount();
            EditorGUILayout.LabelField($"Sistemas Manuais: {manualSystemsCount}");
            
            if (manager.playerInteraction != null)
            {
                EditorGUILayout.LabelField($"PlayerInteraction: Conectado");
                float currentInteraction = manager.playerInteraction.GetInteractionDistance();
                float currentIcon = manager.playerInteraction.GetIconVisibilityDistance();
                EditorGUILayout.LabelField($"Distâncias Atuais: {currentInteraction}m / {currentIcon}m");
            }
            else
            {
                EditorGUILayout.LabelField("PlayerInteraction: Não encontrado", EditorStyles.helpBox);
            }
        }
        
        EditorGUILayout.Space();
        
        // Presets rápidos
        EditorGUILayout.LabelField("Presets Rápidos", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Curto\n2m/4m"))
        {
            manager.SetBothDistances(2f, 4f);
        }
        if (GUILayout.Button("Médio\n3m/6m"))
        {
            manager.SetBothDistances(3f, 6f);
        }
        if (GUILayout.Button("Longo\n5m/10m"))
        {
            manager.SetBothDistances(5f, 10f);
        }
        EditorGUILayout.EndHorizontal();
        
        // Avisos
        EditorGUILayout.Space();
        if (manager.globalIconVisibilityDistance <= manager.globalInteractionDistance)
        {
            EditorGUILayout.HelpBox("Aviso: Distância do ícone deve ser maior que distância de interação!", MessageType.Warning);
        }
        
        if (!Application.isPlaying && manager.autoSyncOnValidate)
        {
            EditorGUILayout.HelpBox("Auto-sync habilitado. Mudanças serão aplicadas automaticamente durante o play.", MessageType.Info);
        }
    }
}
#endif