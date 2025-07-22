using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class WarningIndicator : MonoBehaviour
{
    [Header("Prefab do Indicador")]
    [Tooltip("Uma Image desativada na hierarquia que servirá como template")]
    public Image warningPrefab;

    [Header("Posicionamento")]
    [Tooltip("Pixels a partir da borda direita")]
    public float horizontalMargin = 50f;
    [Tooltip("Ajuste vertical (pixels)")]
    public float verticalMargin = 0f;

    [Header("Tempo de Exibição")]
    [Tooltip("Segundos que o aviso fica visível")]
    public float displayDuration = 1f;

    private Canvas        canvas;
    private RectTransform canvasRect;

    void Awake()
    {
        canvas     = GetComponent<Canvas>();
        canvasRect = canvas.GetComponent<RectTransform>();
        // Garante que o template esteja invisível
        if (warningPrefab != null)
            warningPrefab.gameObject.SetActive(false);
    }

    /// <summary>
    /// Chame para cada spawn de meteoro:
    /// instancia um aviso e o exibe independentemente.
    /// </summary>
    public void ShowWarning(Vector3 worldPos)
    {
        if (warningPrefab == null) return;

        // 1) Cria uma cópia do template
        Image img = Instantiate(warningPrefab, warningPrefab.transform.parent);
        img.gameObject.SetActive(true);

        // 2) Ajusta as ancoragens (direita, meio vertical)
        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot     = new Vector2(1f, 0.5f);

        // 3) Dispara a rotina que posiciona, espera e destrói
        StartCoroutine(WarningRoutine(img, worldPos));
    }

    private IEnumerator WarningRoutine(Image img, Vector3 worldPos)
    {
        // converte do mundo para ponto na tela, depois para local do canvas
        Vector2 localPoint;
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localPoint
        );

        // X fixo na borda direita, Y alinhado ao meteoro + margem
        float xPos = -horizontalMargin;
        float yPos = localPoint.y + verticalMargin;
        img.rectTransform.anchoredPosition = new Vector2(xPos, yPos);

        // espera e destrói apenas este aviso
        yield return new WaitForSeconds(displayDuration);
        Destroy(img.gameObject);
    }
}
