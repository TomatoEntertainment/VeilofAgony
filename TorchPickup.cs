using UnityEngine;

public class TorchPickup : MonoBehaviour
{
    public void Interact(TorchController torchController)
    {
        if (torchController != null)
        {
            // Notificar ManualInteractionSystem que foi pego
            ManualInteractionSystem manualSystem = GetComponent<ManualInteractionSystem>();
            if (manualSystem != null)
            {
                manualSystem.OnObjectPickedUp();
            }
            
            // Chama a nova função para equipar a tocha apagada.
            torchController.EquipUnlitTorch();
        }
        Destroy(gameObject);
    }
}