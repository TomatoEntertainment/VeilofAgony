using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Configurações de Vida (Golpes)")]
    public int maxHits = 3;
    private int currentHits;

    [Header("UI de Dano Imersivo")]
    public Image damageOverlay; 
    public float[] damageOpacityLevels;

    [Header("Efeitos")]
    public float opacityTransitionSpeed = 5f;
    private float targetOpacity = 0f;

    // --- NOVA ADIÇÃO ---
    [Header("Conexões de Cena")]
    [Tooltip("(Opcional) Arraste a porta da cela para aqui para a cena de introdução.")]
    public CellDoorController cellDoor; // Referência para a porta da cela.
    // -----------------

    void Awake() // Mudado de Start para Awake para garantir que currentHits é definido antes de outros scripts o usarem.
    {
        currentHits = maxHits;
    }

    void Start()
    {
        if (damageOverlay != null)
        {
            damageOverlay.enabled = true;
            Color startColor = damageOverlay.color;
            startColor.a = 0f;
            damageOverlay.color = startColor;
        }
    }

    void Update()
    {
        if (damageOverlay == null) return;

        // 1. Verificamos se a diferença entre a opacidade atual e a alvo é maior que um valor muito pequeno.
        if (Mathf.Abs(damageOverlay.color.a - targetOpacity) > 0.01f)
        {
            // Se a diferença for grande, continuamos a suavizar a transição.
            Color currentColor = damageOverlay.color;
            float newAlpha = Mathf.Lerp(currentColor.a, targetOpacity, Time.deltaTime * opacityTransitionSpeed);
            currentColor.a = newAlpha;
            damageOverlay.color = currentColor;
        }
        // 2. Se a opacidade já estiver muito perto do alvo, mas não for exatamente igual...
        else if (damageOverlay.color.a != targetOpacity)
        {
            // ...forçamo-la a ir para o valor final para garantir que a transição termina.
            Color finalColor = damageOverlay.color;
            finalColor.a = targetOpacity;
            damageOverlay.color = finalColor;
        }
    }

    // --- NOVA FUNÇÃO ---
    /// <summary>
    /// Aplica uma quantidade de dano inicial ao jogador no começo do jogo.
    /// </summary>
    public void ApplyInitialDamage(int damageHits)
    {
        for (int i = 0; i < damageHits; i++)
        {
            TakeHit();
        }
    }
    // -----------------

    public void HealToFull()
    {
        Debug.Log("Vida completamente restaurada!");
        currentHits = maxHits;
        targetOpacity = 0f;

        // --- NOVA ADIÇÃO ---
        // Se houver uma porta de cela ligada a este script, manda-a abrir.
        if (cellDoor != null)
        {
            cellDoor.UnlockAndOpen();
        }
        // -----------------
    }

    // O resto do script (TakeHit, Die, etc.) permanece igual.
    public void TakeHit()
    {
        if (currentHits <= 0) return;
        currentHits--;
        Debug.Log("Jogador levou um golpe! Vidas restantes: " + currentHits);
        UpdateDamageVisuals();
        if (currentHits <= 0) { Die(); }
    }

    private void UpdateDamageVisuals()
    {
        if (damageOpacityLevels.Length == 0) return;
        int opacityIndex = maxHits - currentHits - 1;
        if (opacityIndex >= 0 && opacityIndex < damageOpacityLevels.Length)
        {
            targetOpacity = damageOpacityLevels[opacityIndex];
        }
    }

    private void Die()
    {
        Debug.Log("O jogador morreu!");
        if (GetComponent<PlayerController>() != null) { GetComponent<PlayerController>().enabled = false; }
    }
}