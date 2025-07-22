using UnityEngine;

public class SkinScene_UI_Controller : MonoBehaviour
{
    // Esta função será pública para que o nosso botão a possa encontrar.
    public void OnRestorePurchasesClicked()
    {
        // Verifica se o IAPManager existe antes de o chamar
        if (IAPManager.Instance != null)
        {
            Debug.Log("Botão de restaurar compras clicado. A chamar o IAPManager...");
            // Chama a função RestorePurchases() através da instância global (Singleton)
            IAPManager.Instance.RestorePurchases();
        }
        else
        {
            Debug.LogError("Não foi possível restaurar. Instância do IAPManager não encontrada!");
        }
    }
}