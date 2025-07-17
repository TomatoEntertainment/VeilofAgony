using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float walkingSpeed = 7.5f;
    public float runningSpeed = 11.5f;
    public float gravity = 20.0f;
    public Camera playerCamera; // Campo para arrastar a sua câmara.
    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;

    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    private PlayerHealth playerHealth; // Referência para o nosso script de vida.

    [HideInInspector]
    public bool canMove = true;
    [HideInInspector]
    public bool canRotateBody = true;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerHealth = GetComponent<PlayerHealth>(); // Pega a referência do script de vida.

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // --- LÓGICA DE MOVIMENTO (WASD) ---
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float curSpeedX = (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Vertical");
        float curSpeedY = (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Horizontal");

        if (canMove)
        {
            moveDirection = (forward * curSpeedX) + (right * curSpeedY);
        }

        // Aplicar gravidade
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        // Mover o controller
        characterController.Move(moveDirection * Time.deltaTime);

        // --- LÓGICA DE OLHAR (CÂMARA) ---
        rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);

        if (canRotateBody)
        {
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }

        // --- CÓDIGO DE TESTE PARA O SISTEMA DE VIDA ---
        // Se a tecla "T" for pressionada, o jogador leva 1 golpe.
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (playerHealth != null)
            {
                playerHealth.TakeHit();
            }
        }
    }
}