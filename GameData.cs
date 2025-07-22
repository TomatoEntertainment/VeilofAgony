using System.Collections.Generic;

[System.Serializable]
public class GameData
{
    // Moedas e Itens
    public int totalCoins;
    public int totalCapsules;
    public int highScore;

    // Skins
    public int equippedSkinIndex;
    public List<string> purchasedSkinNames = new List<string>();

    // Construtor com valores padrão para um novo jogador
    public GameData()
    {
        totalCoins = 0;
        totalCapsules = 0;
        highScore = 0;
        equippedSkinIndex = 0;
    }
}