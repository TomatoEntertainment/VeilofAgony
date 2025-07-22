// CheatInspector.cs
using UnityEngine;

public class CheatInspector : MonoBehaviour
{
    [Header("Valores Desejados")]
    [Tooltip("Digite aqui a quantidade total de moedas que você quer ter.")]
    public int desiredCoins = 10000;

    [Tooltip("Digite aqui a quantidade total de cápsulas que você quer ter.")]
    public int desiredCapsules = 100;

    [Header("Controle")]
    [Tooltip("Marque esta caixa e rode o jogo para aplicar os valores. A caixa será desmarcada automaticamente.")]
    public bool applyCheatsOnStart = false;


    void Start()
    {
        // Se a caixa de 'applyCheatsOnStart' estiver marcada no Inspector...
        if (applyCheatsOnStart)
        {
            // ...aplica os valores e avisa no console.
            SetResources();

            // Desmarca a caixa para não aplicar os cheats toda vez que o jogo iniciar.
            applyCheatsOnStart = false;
        }
    }

    /// <summary>
    /// Esta função define os totais de moedas e cápsulas para os valores das variáveis públicas.
    /// </summary>
    public void SetResources()
    {
        PlayerPrefs.SetInt("TotalCoins", desiredCoins);
        PlayerPrefs.SetInt("TotalCapsules", desiredCapsules);
        PlayerPrefs.Save();

        Debug.Log($"[CheatInspector] CHEAT APLICADO! Moedas definidas para: {desiredCoins} | Cápsulas definidas para: {desiredCapsules}");

        // Tenta atualizar a UI se os scripts de display existirem na cena.
        var coinDisplay = FindObjectOfType<MenuCoinDisplay>();
        if (coinDisplay != null) coinDisplay.Refresh();

        var capsuleDisplay = FindObjectOfType<MenuCapsuleDisplay>();
        if (capsuleDisplay != null) capsuleDisplay.Refresh();
    }
}