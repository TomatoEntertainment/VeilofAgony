using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class ScrollRawImage : MonoBehaviour
{
    [Header("Scroll")]
    [Tooltip("Velocidade de deslocamento em UV por segundo")]
    public float scrollSpeed = 0.5f;

    private RawImage rawImage;
    private Rect originalUVRect;
    private Vector2 uvOffset;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();

        // Garante que a textura repita
        if (rawImage.texture != null)
            rawImage.texture.wrapMode = TextureWrapMode.Repeat;

        // Guarda o uvRect *exato* que você definiu no Inspector,
        // incluindo largura e altura (size.x e size.y)
        originalUVRect = rawImage.uvRect;
    }

    void Update()
    {
        // Pausa o scroll se o jogo acabou
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        // Move apenas no eixo X
        uvOffset.x += scrollSpeed * Time.deltaTime;
        uvOffset.x %= 1f;

        // Reaplica o tamanho original (inclusive a altura que você ajustou)
        rawImage.uvRect = new Rect(uvOffset, originalUVRect.size);
    }
}
