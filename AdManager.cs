// AdManager.cs
using System;
using UnityEngine;
using GoogleMobileAds.Api;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    private RewardedAd rewardedAd;

    // IDs de Bloco de Anúncios de Teste. Substitua pelos seus antes de publicar!
    #if UNITY_ANDROID
        private string rewardedUnitId = "ca-app-pub-3940256099942544/5224354917";
    #elif UNITY_IPHONE
        private string rewardedUnitId = "ca-app-pub-3940256099942544/1712485313";
    #else
        private string rewardedUnitId = "unused";
    #endif

    void Awake()
    {
        // Implementação do Singleton
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
        // Inicializa o SDK do Google Mobile Ads.
        // Isso deve ser feito apenas uma vez, idealmente no início.
        MobileAds.Initialize((InitializationStatus status) =>
        {
            Debug.Log("AdMob SDK inicializado.");
            // Carrega o primeiro anúncio recompensado na inicialização.
            LoadRewardedAd();
        });
    }

    /// <summary>
    /// Carrega um novo anúncio recompensado.
    /// </summary>
    public void LoadRewardedAd()
    {
        Debug.Log("Carregando anúncio recompensado...");

        // Limpa o anúncio antigo se existir, para evitar vazamento de memória.
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        var adRequest = new AdRequest();
        adRequest.Keywords.Add("unity-admob-sample");

        // Envia a requisição para carregar o anúncio.
        RewardedAd.Load(rewardedUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("Falha ao carregar anúncio recompensado: " + error?.GetMessage());
                return;
            }

            Debug.Log("Anúncio recompensado carregado com sucesso.");
            rewardedAd = ad;
            RegisterEventHandlers(rewardedAd);
        });
    }

    /// <summary>
    /// Mostra o anúncio recompensado se ele estiver carregado.
    /// </summary>
    /// <param name="onUserEarnedReward">Ação a ser executada quando o usuário ganhar a recompensa.</param>
    /// <param name="onAdFailedToShow">Ação a ser executada se o anúncio não puder ser exibido.</param>
    public void ShowRewardedAd(Action onUserEarnedReward, Action onAdFailedToShow)
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            Debug.Log("Mostrando anúncio recompensado.");
            rewardedAd.Show((Reward reward) =>
            {
                // Chamado quando o usuário ganha a recompensa.
                Debug.Log($"Recompensa ganha: {reward.Amount} {reward.Type}");
                onUserEarnedReward?.Invoke();
            });
        }
        else
        {
            Debug.LogError("O anúncio recompensado não está pronto para ser mostrado.");
            onAdFailedToShow?.Invoke();
            // Tenta carregar um novo anúncio em segundo plano para a próxima tentativa.
            LoadRewardedAd();
        }
    }

    private void RegisterEventHandlers(RewardedAd ad)
    {
        // Chamado quando o anúncio é mostrado.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Anúncio recompensado foi aberto (tela cheia).");
        };
        
        // Chamado quando o anúncio é fechado.
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Anúncio recompensado foi fechado.");
            // IMPORTANTE: Carregue o próximo anúncio assim que o atual for fechado.
            // Isso garante que sempre haverá um anúncio pronto para a próxima vez.
            LoadRewardedAd();
        };

        // Chamado quando há um erro ao mostrar o anúncio.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Falha ao mostrar o anúncio recompensado: " + error.GetMessage());
            LoadRewardedAd(); // Tenta carregar um novo em caso de falha.
        };
    }
}