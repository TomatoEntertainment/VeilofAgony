// PlayerInventory.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventário")]
    [SerializeField] private int oilAmount = 0;
    
    [Header("UI do Inventário")]
    [Tooltip("Texto para mostrar quantidade de óleo")]
    public TMP_Text oilCountText;
    
    [Tooltip("Ícone do óleo na UI")]
    public GameObject oilUIIcon;
    
    void Start()
    {
        UpdateUI();
    }
    
    public void AddOil(int amount)
    {
        oilAmount += amount;
        UpdateUI();
        Debug.Log($"Óleo adicionado. Total: {oilAmount}");
    }
    
    public bool UseOil(int amount)
    {
        if (oilAmount >= amount)
        {
            oilAmount -= amount;
            UpdateUI();
            Debug.Log($"Óleo usado. Restante: {oilAmount}");
            return true;
        }
        
        Debug.Log("Óleo insuficiente!");
        return false;
    }
    
    public int GetOilAmount()
    {
        return oilAmount;
    }
    
    void UpdateUI()
    {
        if (oilCountText != null)
        {
            oilCountText.text = oilAmount.ToString();
        }
        
        if (oilUIIcon != null)
        {
            oilUIIcon.SetActive(oilAmount > 0);
        }
    }
}