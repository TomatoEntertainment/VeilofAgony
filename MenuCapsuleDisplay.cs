// MenuCapsuleDisplay.cs
using UnityEngine;
using TMPro;

public class MenuCapsuleDisplay : MonoBehaviour
{
    [Tooltip("Arraste aqui o TMP_Text que exibirá o total de cápsulas")]
    public TMP_Text totalCapsulesText;

    void Start() => Refresh();

    public void Refresh()
    {
        int total = PlayerPrefs.GetInt("TotalCapsules", 0);
        totalCapsulesText.text = total.ToString();
    }
}
