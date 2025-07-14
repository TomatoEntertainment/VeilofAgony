using UnityEngine;

public class TorchPickup : MonoBehaviour
{
    public void Interact(TorchController torchController)
    {
        if (torchController != null)
        {
            // Chama a nova função para equipar a tocha apagada.
            torchController.EquipUnlitTorch();
        }
        Destroy(gameObject);
    }
}