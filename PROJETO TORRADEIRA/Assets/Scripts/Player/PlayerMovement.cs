using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 3.5f;
    public float sprintSpeed = 6f;
    public float mouseSensitivity = 2f;
    public float gravity = -9.81f;
    public float jumpForce = 2f;

    public Transform cameraTransform;
    public CharacterController controller;

    public TextMeshProUGUI textoInteracao;

    public float headBobSpeed = 7f;
    public float headBobAmount = 0.15f;

    public float sprintHeadBobSpeed = 11f;
    public float sprintHeadBobAmount = 0.19f;

    public float headBobSmooth = 8f;

    float xRotation = 0f;
    float yVelocity = 0f;
    float headBobTimer = 0f;

    private Vector3 cameraStartPosition;
    private ItemColetavel itemPerto;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        textoInteracao.gameObject.SetActive(false);

        cameraStartPosition =
            cameraTransform.localPosition;
    }

    void Update()
    {
        Vector2 moveInput = Vector2.zero;

        if (Keyboard.current.wKey.isPressed)
            moveInput.y += 1;

        if (Keyboard.current.sKey.isPressed)
            moveInput.y -= 1;

        if (Keyboard.current.dKey.isPressed)
            moveInput.x += 1;

        if (Keyboard.current.aKey.isPressed)
            moveInput.x -= 1;

        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();

        Vector3 move =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;

        if (controller.isGrounded && yVelocity < 0)
            yVelocity = -2f;

        if (Keyboard.current.spaceKey.wasPressedThisFrame &&
            controller.isGrounded)
        {
            yVelocity =
                Mathf.Sqrt(
                    jumpForce * -2f * gravity
                );
        }

        yVelocity += gravity * Time.deltaTime;

        bool isSprinting =
            Keyboard.current.leftShiftKey.isPressed &&
            moveInput.sqrMagnitude > 0.01f &&
            controller.isGrounded;

        float currentSpeed =
            isSprinting
                ? sprintSpeed
                : moveSpeed;

        Vector3 velocity =
            move * currentSpeed;

        velocity.y = yVelocity;

        controller.Move(
            velocity * Time.deltaTime
        );

        HandleHeadBob(
            moveInput,
            isSprinting
        );

        Vector2 mouseDelta =
            Mouse.current.delta.ReadValue() *
            mouseSensitivity;

        xRotation -= mouseDelta.y;

        xRotation =
            Mathf.Clamp(
                xRotation,
                -90f,
                90f
            );

        cameraTransform.localRotation =
            Quaternion.Euler(
                xRotation,
                0f,
                0f
            );

        transform.Rotate(
            Vector3.up * mouseDelta.x
        );

        if (itemPerto != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            Destroy(itemPerto.gameObject);

            itemPerto = null;

            textoInteracao.gameObject.SetActive(false);
        }
    }

    void HandleHeadBob(
        Vector2 moveInput,
        bool isSprinting
    )
    {
        bool isMoving =
            moveInput.sqrMagnitude > 0.01f &&
            controller.isGrounded;

        if (isMoving)
        {
            float currentBobSpeed =
                isSprinting
                    ? sprintHeadBobSpeed
                    : headBobSpeed;

            float currentBobAmount =
                isSprinting
                    ? sprintHeadBobAmount
                    : headBobAmount;

            headBobTimer +=
                Time.deltaTime * currentBobSpeed;

            float verticalBob =
                Mathf.Sin(headBobTimer) *
                currentBobAmount;

            float horizontalBob =
                Mathf.Sin(headBobTimer * 2f) *
                currentBobAmount *
                0.25f;

            Vector3 targetPosition =
                cameraStartPosition;

            targetPosition.y += verticalBob;
            targetPosition.x += horizontalBob;

            cameraTransform.localPosition =
                Vector3.Lerp(
                    cameraTransform.localPosition,
                    targetPosition,
                    Time.deltaTime *
                    headBobSmooth
                );
        }
        else
        {
            headBobTimer = 0f;

            cameraTransform.localPosition =
                Vector3.Lerp(
                    cameraTransform.localPosition,
                    cameraStartPosition,
                    Time.deltaTime *
                    headBobSmooth
                );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ItemColetavel item =
            other.GetComponentInParent<ItemColetavel>();

        if (item != null)
        {
            itemPerto = item;

            textoInteracao.text =
                "Aperte [E] para coletar " +
                item.itemNome;

            textoInteracao.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        ItemColetavel item =
            other.GetComponentInParent<ItemColetavel>();

        if (item != null &&
            item == itemPerto)
        {
            itemPerto = null;

            textoInteracao.gameObject.SetActive(false);
        }
    }
}