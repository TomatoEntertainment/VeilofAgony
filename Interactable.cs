using UnityEngine;
using UnityEngine.UI;

public class Interactable : MonoBehaviour
{
    public enum ItemType { Healing, Key, Generic }
    [Header("Configuração do Item")]
    public ItemType itemType;
    public float useTime = 2.0f;

    [Header("Configuração na Mão do Jogador")]
    public Vector3 heldPosition;
    public Vector3 heldRotation;

    private GameObject progressHolder;
    private RectTransform movingFillRect;
    private float fillStartY, fillHeight;
    private Rigidbody rb;
    private Collider col;
    private bool isBeingUsed = false;
    private float currentUseTime = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void StartUse(GameObject holder, RectTransform movingFill)
    {
        progressHolder = holder;
        movingFillRect = movingFill;
        isBeingUsed = true;
        currentUseTime = 0f;

        if (movingFillRect != null)
        {
            fillHeight = movingFillRect.rect.height;
            fillStartY = -fillHeight;
            movingFillRect.anchoredPosition = new Vector2(0, fillStartY);
        }
        if (progressHolder != null) progressHolder.SetActive(true);
    }

    public bool UpdateUse(PlayerHealth playerHealth)
    {
        if (!isBeingUsed) return false;
        currentUseTime += Time.deltaTime;
        float progress = Mathf.Clamp01(currentUseTime / useTime);
        float newY = Mathf.Lerp(fillStartY, 0, progress);

        if (movingFillRect != null) movingFillRect.anchoredPosition = new Vector2(0, newY);

        if (progress >= 1f)
        {
            PerformAction(playerHealth);
            EndUse();
            return true;
        }
        return false;
    }

    public void EndUse()
    {
        isBeingUsed = false;
        if (movingFillRect != null) movingFillRect.anchoredPosition = new Vector2(0, fillStartY);
        if (progressHolder != null) progressHolder.SetActive(false);
    }
    
    private void PerformAction(PlayerHealth playerHealth)
    {
        if (itemType == ItemType.Healing)
        {
            playerHealth.HealToFull();
            Destroy(gameObject);
        }
    }
    
    public void Pickup(Transform handSlot)
    {
        this.transform.SetParent(handSlot);
        this.transform.localPosition = heldPosition;
        this.transform.localEulerAngles = heldRotation;
        if (rb != null) rb.isKinematic = true;
        if (col != null) col.enabled = false;
    }

    public void Drop()
    {
        this.transform.SetParent(null);
        if (rb != null) rb.isKinematic = false;
        if (col != null) col.enabled = true;
    }
}