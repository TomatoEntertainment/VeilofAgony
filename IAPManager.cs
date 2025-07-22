// IAPManager.cs - VERSÃO CORRIGIDA E COMPLETA
using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

// IDetailedStoreListener herda de IStoreListener, então precisamos implementar os métodos de ambas.
public class IAPManager : MonoBehaviour, IDetailedStoreListener
{
    public static IAPManager Instance { get; private set; }

    private static IStoreController storeController;
    private static IExtensionProvider storeExtensionProvider;

    public static event Action<string> OnPurchaseSuccess;

    [Header("IDs dos Produtos")]
    public ProductDefinition[] productDefinitions;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (IsInitialized()) return;
        InitializePurchasing();
    }

    public bool IsInitialized()
    {
        return storeController != null && storeExtensionProvider != null;
    }

    private void InitializePurchasing()
    {
        if (IsInitialized()) return;
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        foreach (var product in productDefinitions)
        {
            builder.AddProduct(product.id, product.type);
        }
        Debug.Log("[IAPManager] Inicializando o sistema de compras...");
        UnityPurchasing.Initialize(this, builder);
    }

    public void BuyProductID(string productId)
    {
        if (!IsInitialized())
        {
            Debug.LogError("[IAPManager] ERRO: Sistema de IAP não inicializado.");
            return;
        }
        Product product = storeController.products.WithID(productId);
        if (product != null && product.availableToPurchase)
        {
            Debug.Log($"[IAPManager] Iniciando compra do produto: '{product.definition.id}'...");
            storeController.InitiatePurchase(product);
        }
        else
        {
            Debug.LogError($"[IAPManager] ERRO: Produto '{productId}' não encontrado ou não disponível para compra.");
        }
    }

    public void RestorePurchases()
    {
        if (!IsInitialized())
        {
            Debug.LogError("Falha na restauração: O sistema de IAP não foi inicializado.");
            return;
        }

        if (Application.platform == RuntimePlatform.IPhonePlayer ||
            Application.platform == RuntimePlatform.OSXPlayer)
        {
            Debug.Log("Iniciando restauração de compras (Apple)...");
            var apple = storeExtensionProvider.GetExtension<IAppleExtensions>();
            apple.RestoreTransactions((result, error) => {
                if (result) { Debug.Log("Restauração concluída com sucesso."); }
                else { Debug.LogError("Falha na restauração: " + error); }
            });
        }
        else
        {
            Debug.LogWarning("Restauração manual não é necessária nesta plataforma (ex: Google Play).");
        }
    }

    // ---- MÉTODOS DA INTERFACE ----

    /// <summary>
    /// Chamado quando o Unity IAP é inicializado com sucesso.
    /// </summary>
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        Debug.Log("[IAPManager] Sistema de IAP inicializado com SUCESSO.");
        storeController = controller;
        storeExtensionProvider = extensions;
    }

    /// <summary>
    /// Chamado quando a compra é bem-sucedida.
    /// </summary>
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        string productId = args.purchasedProduct.definition.id;
        Debug.Log($"[IAPManager] SUCESSO na compra do produto '{productId}'.");
        OnPurchaseSuccess?.Invoke(productId);
        return PurchaseProcessingResult.Complete;
    }

    // ---------- INÍCIO DA CORREÇÃO ----------
    // Ambos os métodos OnInitializeFailed e OnPurchaseFailed são necessários
    // para satisfazer completamente as interfaces IDetailedStoreListener e IStoreListener.

    /// <summary>
    /// Chamado quando a inicialização falha (versão detalhada).
    /// </summary>
    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError($"[IAPManager] ERRO na inicialização do IAP: {error}. Mensagem: {message}");
    }

    /// <summary>
    /// Chamado quando a inicialização falha (versão simples, exigida pela interface base).
    /// </summary>
    public void OnInitializeFailed(InitializationFailureReason error)
    {
        OnInitializeFailed(error, "Nenhuma mensagem adicional.");
    }

    /// <summary>
    /// Chamado quando uma compra falha (versão detalhada).
    /// </summary>
    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        Debug.LogError($"[IAPManager] Falha na compra do produto '{product.definition.id}'. Motivo: {failureDescription.reason}. Mensagem: {failureDescription.message}");
    }

    /// <summary>
    /// Chamado quando uma compra falha (versão simples, exigida pela interface base).
    /// </summary>
    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogError($"[IAPManager] Falha na compra do produto '{product.definition.id}'. Motivo: {failureReason}");
    }
    // ---------- FIM DA CORREÇÃO ----------


    public bool IsProductPurchased(string productId)
    {
        if (!IsInitialized() || string.IsNullOrEmpty(productId)) return false;
        Product product = storeController.products.WithID(productId);
        if (product != null)
        {
            return product.hasReceipt;
        }
        return false;
    }
}

[System.Serializable]
public class ProductDefinition
{
    public string id = "com.suaempresa.seujogo.nomeproduto";
    public ProductType type = ProductType.NonConsumable;
}