using UnityEngine;
using UnityEngine.UI;

public class Altar : MonoBehaviour
{
    public float interactionTime = 2.5f;

    private GameObject progressHolder;
    private RectTransform bloodMovingRect;
    private float fillStartY, fillHeight;
    private bool isInteracting = false;
    private float currentInteractionTime = 0f;

    public void StartInteraction(GameObject holder, RectTransform movingRect)
    {
        progressHolder = holder;
        bloodMovingRect = movingRect;
        isInteracting = true;
        currentInteractionTime = 0f;

        if (bloodMovingRect != null)
        {
            fillHeight = bloodMovingRect.rect.height;
            fillStartY = -fillHeight;
            bloodMovingRect.anchoredPosition = new Vector2(0, fillStartY);
        }
        if (progressHolder != null) progressHolder.SetActive(true);
    }

    public void UpdateInteraction(TorchController torch)
    {
        if (!isInteracting) return;
        currentInteractionTime += Time.deltaTime;
        float progress = Mathf.Clamp01(currentInteractionTime / interactionTime);
        float newY = Mathf.Lerp(fillStartY, 0, progress);

        if (bloodMovingRect != null) bloodMovingRect.anchoredPosition = new Vector2(0, newY);
        
        if (progress >= 1f)
        {
            torch.LightAndResetTorch();
            EndInteraction();
        }
    }

    public void EndInteraction()
    {
        if (!isInteracting) return;
        isInteracting = false;
        if (bloodMovingRect != null) bloodMovingRect.anchoredPosition = new Vector2(0, fillStartY);
        if (progressHolder != null) progressHolder.SetActive(false);
    }
}