using UnityEngine;

public class TorchController : MonoBehaviour
{
    public enum TorchState { Unequipped, Unlit, Lit }
    
    [Header("Estado Atual (Apenas para Debug)")]
    [SerializeField] private TorchState currentState = TorchState.Unequipped;

    [Header("Objetos da Tocha")]
    public GameObject unlitTorchInHand;
    public GameObject[] torchLevels;

    [Header("Configurações de Degradação")]
    public float[] degradationTimes = { 120f, 90f, 60f };
    public int CurrentLitLevel => currentLitLevel;
    private int currentLitLevel = 0;
    private float degradationTimer;

    public TorchState CurrentState => currentState;

    

    void Start()
    {
        UpdateVisuals();
    }

    void Update()
    {
        if (currentState == TorchState.Lit)
        {
            if (currentLitLevel >= torchLevels.Length - 1) return;
            degradationTimer -= Time.deltaTime;
            if (degradationTimer <= 0)
            {
                DowngradeTorch();
            }
        }
    }

    private void UpdateVisuals()
    {
        if (unlitTorchInHand != null) unlitTorchInHand.SetActive(false);
        foreach (var torch in torchLevels)
        {
            if (torch != null) torch.SetActive(false);
        }

        switch (currentState)
        {
            case TorchState.Unlit:
                if (unlitTorchInHand != null) unlitTorchInHand.SetActive(true);
                break;
            case TorchState.Lit:
                if (torchLevels.Length > currentLitLevel && torchLevels[currentLitLevel] != null)
                {
                    torchLevels[currentLitLevel].SetActive(true);
                }
                break;
        }
    }

    public void EquipUnlitTorch()
    {
        if (currentState != TorchState.Unequipped) return;
        currentState = TorchState.Unlit;
        UpdateVisuals();
    }
    
    public void LightAndResetTorch()
    {
        currentState = TorchState.Lit;
        currentLitLevel = 0;
        degradationTimer = degradationTimes[currentLitLevel];
        UpdateVisuals();
    }
    
    private void DowngradeTorch()
    {
        currentLitLevel++;
        if (currentLitLevel < degradationTimes.Length)
        {
            degradationTimer = degradationTimes[currentLitLevel];
        }
        UpdateVisuals();
    }
}