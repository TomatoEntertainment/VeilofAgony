using UnityEngine;

public class SkyboxRotator : MonoBehaviour
{
    [Tooltip("Grau de rotação por segundo")]
    public float rotationSpeed = 1f;

    void Update()
    {
        // Rotaciona o skybox no eixo Y
        float rot = Time.time * rotationSpeed;
        RenderSettings.skybox.SetFloat("_Rotation", rot);
    }
}
