using UnityEngine;

[System.Serializable]
public class RewardData
{
    [Tooltip("Dia da sequência (1–7)")]
    public int day;

    [Tooltip("Quantidade de moedas grátis (coins)")]
    public int coinAmount;

    [Tooltip("Quantidade de cápsulas premium")]
    public int capsuleAmount;

    [Tooltip("Descrição opcional para UI")]
    public string description;

    [Tooltip("Ícone ilustrativo (opcional)")]
    public Sprite icon;
}
