// TotemPickup.cs
using UnityEngine;

public class TotemPickup : MonoBehaviour
{
    [Header("Configurações do Totem")]
    [Tooltip("ID único deste totem (deve corresponder ao ID do altar)")]
    public string totemID = "totem_01";
    
    [Tooltip("Nome do totem para exibição")]
    public string totemName = "Totem Antigo";
    
    [Header("Configuração na Mão")]
    public Vector3 heldPosition = new Vector3(0.5f, 0.5f, 0.5f);
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
            manualSystem.actionText = "Pegar Totem";
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
        
        Debug.Log($"Totem {totemName} foi pego");
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
        
        Debug.Log($"Totem {totemName} foi largado");
    }
    
    public string GetTotemID() { return totemID; }
    public string GetTotemName() { return totemName; }
    public bool IsPickedUp() { return isPickedUp; }
}
