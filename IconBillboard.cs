using UnityEngine;

public class IconBillboard : MonoBehaviour
{
    private Camera targetCamera;
    
    [Header("Configurações do Billboard")]
    [Tooltip("Se deve inverter a direção (útil para alguns casos)")]
    public bool reverse = false;
    
    [Tooltip("Se deve rotacionar apenas no eixo Y")]
    public bool lockY = false;
    
    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }
    
    void LateUpdate()
    {
        if (targetCamera == null) return;
        
        Vector3 directionToCamera;
        
        if (reverse)
        {
            directionToCamera = transform.position - targetCamera.transform.position;
        }
        else
        {
            directionToCamera = targetCamera.transform.position - transform.position;
        }
        
        if (lockY)
        {
            directionToCamera.y = 0;
        }
        
        if (directionToCamera != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(directionToCamera);
        }
    }
    
    public void SetCamera(Camera camera)
    {
        targetCamera = camera;
    }
}