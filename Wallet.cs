using UnityEngine;

public class Wallet : MonoBehaviour
{
    public int coins;
    public int capsules;

    private const string CoinsKey    = "TotalCoins";
    private const string CapsulesKey = "TotalCapsules";

    void Awake()
    {
        Load();
    }

    public void Load()
    {
        coins    = PlayerPrefs.GetInt(CoinsKey, 0);
        capsules = PlayerPrefs.GetInt(CapsulesKey, 0);
    }

    public void Save()
    {
        PlayerPrefs.SetInt(CoinsKey,    coins);
        PlayerPrefs.SetInt(CapsulesKey, capsules);
        PlayerPrefs.Save();
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        Save();
    }

    public void AddCapsules(int amount)
    {
        capsules += amount;
        Save();
    }
}
