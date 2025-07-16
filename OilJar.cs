// OilJar.cs
using UnityEngine;

public class OilJar : MonoBehaviour
{
    [Header("Configurações do Jarro")]
    [Tooltip("Nome do jarro para exibição")]
    public string jarName = "Jarro de Óleo";
    
    [Header("Configuração na Mão")]
    public Vector3 heldPosition = new Vector3(0.3f, 0.3f, 0.5f);
    public Vector3 heldRotation = new Vector3(0, 0, 0);
    
    private Rigidbody rb;
    private Collider col;
    private bool isPickedUp = false;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }
    
    void Start()
    {
        // Configurar ManualInteractionSystem se existir
        ManualInteractionSystem manualSystem = GetComponent<ManualInteractionSystem>();
        if (manualSystem != null)
        {
            manualSystem.actionText = "Pegar Jarro";
        }
    }
    
    public bool CanPickup()
    {
        return !isPickedUp;
    }
    
    public void Pickup(Transform handSlot)
    {
        if (!CanPickup()) return;
        
        isPickedUp = true;
        transform.SetParent(handSlot);
        transform.localPosition = heldPosition;
        transform.localEulerAngles = heldRotation;
        
        if (rb != null) rb.isKinematic = true;
        if (col != null) col.enabled = false;
        
        // Notificar ManualInteractionSystem
        ManualInteractionSystem manualSystem = GetComponent<ManualInteractionSystem>();
        if (manualSystem != null)
        {
            manualSystem.OnObjectPickedUp();
        }
        
        Debug.Log($"Jarro {jarName} foi pego");
    }
    
    public void Drop()
    {
        if (!isPickedUp) return;
        
        isPickedUp = false;
        transform.SetParent(null);
        
        if (rb != null) rb.isKinematic = false;
        if (col != null) col.enabled = true;
        
        // Notificar ManualInteractionSystem
        ManualInteractionSystem manualSystem = GetComponent<ManualInteractionSystem>();
        if (manualSystem != null)
        {
            manualSystem.OnObjectDropped();
        }
        
        Debug.Log($"Jarro {jarName} foi largado");
    }
    
    public void Use()
    {
        // O jarro será destruído quando usado no altar
        Debug.Log($"Jarro {jarName} foi usado");
    }
    
    public bool IsPickedUp() { return isPickedUp; }
}
