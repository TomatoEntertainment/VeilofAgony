using UnityEngine;
using UnityEngine.UI;

public class InteractionIcon : MonoBehaviour
{
    [Header("Configurações do Ícone")]
    [Tooltip("Ícone que será exibido sobre o objeto")]
    public Image iconImage;
    
    [Tooltip("Animação do ícone (opcional)")]
    public Animator iconAnimator;
    
    [Header("Configurações Visuais")]
    [Tooltip("Escala do ícone")]
    public float iconScale = 1f;
    
    [Tooltip("Cor do ícone")]
    public Color iconColor = Color.white;
    
    [Tooltip("Fazer o ícone pulsar")]
    public bool enablePulse = true;
    
    [Tooltip("Velocidade da pulsação")]
    public float pulseSpeed = 2f;
    
    [Tooltip("Intensidade da pulsação")]
    public float pulseIntensity = 0.2f;
    
    private float originalScale;
    private Color originalColor;
    private float pulseTimer = 0f;
    
    void Start()
    {
        if (iconImage != null)
        {
            originalColor = iconImage.color;
            iconImage.color = iconColor;
        }
        
        originalScale = transform.localScale.x;
        transform.localScale = Vector3.one * iconScale;
    }
    
    void Update()
    {
        if (enablePulse)
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            float pulseValue = 1f + Mathf.Sin(pulseTimer) * pulseIntensity;
            transform.localScale = Vector3.one * iconScale * pulseValue;
            
            if (iconImage != null)
            {
                Color currentColor = iconColor;
                currentColor.a = iconColor.a * pulseValue;
                iconImage.color = currentColor;
            }
        }
    }
    
    public void SetIconSprite(Sprite sprite)
    {
        if (iconImage != null)
            iconImage.sprite = sprite;
    }
    
    public void SetIconColor(Color color)
    {
        iconColor = color;
        if (iconImage != null)
            iconImage.color = color;
    }
    
    public void PlayAnimation(string animationName)
    {
        if (iconAnimator != null)
            iconAnimator.Play(animationName);
    }
}