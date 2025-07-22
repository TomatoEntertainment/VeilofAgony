using UnityEngine;
using TMPro;

public class MenuCoinDisplay : MonoBehaviour
{
    [Tooltip("Arraste aqui o TMP_Text que exibirá o total de moedas")]
    public TMP_Text totalCoinsText;

    void Start() => Refresh();

    public void Refresh()
    {
        int total = PlayerPrefs.GetInt("TotalCoins", 0);
        totalCoinsText.text = total.ToString();
    }
}
