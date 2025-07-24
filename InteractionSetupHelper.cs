using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class InteractionSetupHelper : MonoBehaviour
{
    [Header("Configuração Automática")]
    [Tooltip("Prefab do ícone de interação")]
    public GameObject interactionIconPrefab;
    
    [Tooltip("Altura padrão para posicionar o ícone")]
    public float defaultIconHeight = 1.5f;
    
    [Tooltip("Configurar automaticamente no Start")]
    public bool autoSetupOnStart = false;
    
    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupAllInteractableObjects();
        }
    }
    
    [ContextMenu("Setup All Interactable Objects")]
    public void SetupAllInteractableObjects()
    {
        if (interactionIconPrefab == null)
        {
            Debug.LogError("InteractionSetupHelper: Prefab do ícone não foi atribuído!");
            return;
        }
        
        int setupCount = 0;
        
        // Configurar objetos Interactable
        Interactable[] interactables = FindObjectsOfType<Interactable>();
        foreach (var interactable in interactables)
        {
            if (SetupInteractableObject(interactable.gameObject, "Pegar"))
            {
                setupCount++;
            }
        }
        
        // Configurar objetos TorchPickup
        TorchPickup[] torches = FindObjectsOfType<TorchPickup>();
        foreach (var torch in torches)
        {
            if (SetupInteractableObject(torch.gameObject, "Pegar"))
            {
                setupCount++;
            }
        }
        
        // Configurar objetos Altar
        Altar[] altars = FindObjectsOfType<Altar>();
        foreach (var altar in altars)
        {
            if (SetupInteractableObject(altar.gameObject, "Usar"))
            {
                setupCount++;
            }
        }
        
        Debug.Log($"InteractionSetupHelper: {setupCount} objetos configurados com sucesso!");
    }
    
    bool SetupInteractableObject(GameObject obj, string actionText)
    {
        // Verificar se já tem ManualInteractionSystem
        ManualInteractionSystem existingSystem = obj.GetComponent<ManualInteractionSystem>();
        if (existingSystem != null)
        {
            Debug.Log($"Objeto {obj.name} já possui ManualInteractionSystem. Pulando...");
            return false;
        }
        
        // Verificar se já tem um ícone filho
        Transform existingIcon = obj.transform.Find("InteractionIcon");
        if (existingIcon != null)
        {
            Debug.Log($"Objeto {obj.name} já possui ícone filho. Pulando...");
            return false;
        }
        
        // Criar o ícone como filho
        Vector3 iconPosition = GetOptimalIconPosition(obj);
        GameObject iconInstance = Instantiate(interactionIconPrefab, obj.transform);
        iconInstance.name = "InteractionIcon";
        iconInstance.transform.localPosition = iconPosition;
        iconInstance.SetActive(false); // Começar desativado
        
        // Adicionar e configurar ManualInteractionSystem
        ManualInteractionSystem manualSystem = obj.AddComponent<ManualInteractionSystem>();
        manualSystem.interactionIcon = iconInstance;
        manualSystem.actionText = actionText;
        
        Debug.Log($"Configurado: {obj.name} com ação '{actionText}'");
        return true;
    }
    
    Vector3 GetOptimalIconPosition(GameObject obj)
    {
        Vector3 position = Vector3.up * defaultIconHeight;
        
        // Tentar usar bounds do objeto para posicionamento mais preciso
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            float objectHeight = renderer.bounds.size.y;
            position = Vector3.up * (objectHeight + 0.5f);
        }
        else
        {
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null)
            {
                float objectHeight = collider.bounds.size.y;
                position = Vector3.up * (objectHeight + 0.5f);
            }
        }
        
        return position;
    }
    
    [ContextMenu("Remove All Manual Systems")]
    public void RemoveAllManualSystems()
    {
        ManualInteractionSystem[] systems = FindObjectsOfType<ManualInteractionSystem>();
        int removeCount = 0;
        
        foreach (var system in systems)
        {
            // Remover ícone filho se existir
            if (system.interactionIcon != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(system.interactionIcon);
                }
                else
                {
                    DestroyImmediate(system.interactionIcon);
                }
            }
            
            // Remover o componente
            if (Application.isPlaying)
            {
                Destroy(system);
            }
            else
            {
                DestroyImmediate(system);
            }
            
            removeCount++;
        }
        
        Debug.Log($"InteractionSetupHelper: {removeCount} sistemas manuais removidos!");
    }
    
    [ContextMenu("Update All Action Texts")]
    public void UpdateAllActionTexts()
    {
        ManualInteractionSystem[] systems = FindObjectsOfType<ManualInteractionSystem>();
        
        foreach (var system in systems)
        {
            // Determinar texto baseado no tipo de objeto
            string newActionText = "Pegar";
            
            if (system.GetComponent<Interactable>() != null)
            {
                Interactable.ItemType itemType = system.GetComponent<Interactable>().itemType;
                switch (itemType)
                {
                    case Interactable.ItemType.Healing:
                        newActionText = "Pegar";
                        break;
                    case Interactable.ItemType.Key:
                        newActionText = "Pegar";
                        break;
                    case Interactable.ItemType.Generic:
                        newActionText = "Pegar";
                        break;
                }
            }
            else if (system.GetComponent<TorchPickup>() != null)
            {
                newActionText = "Pegar";
            }
            else if (system.GetComponent<Altar>() != null)
            {
                newActionText = "Usar";
            }
            
            system.SetActionText(newActionText);
        }
        
        Debug.Log("Todos os textos de ação foram atualizados!");
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(InteractionSetupHelper))]
public class InteractionSetupHelperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        
        InteractionSetupHelper helper = (InteractionSetupHelper)target;
        
        if (GUILayout.Button("Setup All Interactable Objects", GUILayout.Height(30)))
        {
            helper.SetupAllInteractableObjects();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Update All Action Texts"))
        {
            helper.UpdateAllActionTexts();
        }
        
        EditorGUILayout.Space();
        
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Remove All Manual Systems"))
        {
            if (EditorUtility.DisplayDialog("Confirmar", 
                "Tem certeza que deseja remover todos os sistemas manuais?", 
                "Sim", "Cancelar"))
            {
                helper.RemoveAllManualSystems();
            }
        }
        GUI.backgroundColor = Color.white;
    }
}
#endif