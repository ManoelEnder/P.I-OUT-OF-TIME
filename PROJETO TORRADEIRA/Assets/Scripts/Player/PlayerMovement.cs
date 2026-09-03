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

    public AudioSource audioPassos;
    public AudioClip[] sonsPassos;
    public float intervaloPasso = 0.5f;
    public float intervaloPassoCorrendo = 0.3f;

    private float xRotation = 0f;
    private float yVelocity = 0f;
    private float headBobTimer = 0f;
    private float contadorPasso = 0f;

    private Vector3 cameraStartPosition;
    private ItemColetavel itemPerto;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (textoInteracao != null)
        {
            textoInteracao.gameObject.SetActive(false);
        }

        if (cameraTransform != null)
        {
            cameraStartPosition =
                cameraTransform.localPosition;
        }
    }

    private void Update()
    {
        if (PauseMenu.IsPaused)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

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
        {
            yVelocity = -2f;
        }

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

        HandleFootsteps(
            moveInput,
            isSprinting
        );

        if (Mouse.current != null &&
            cameraTransform != null)
        {
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
        }

        if (itemPerto != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            Destroy(itemPerto.gameObject);

            itemPerto = null;

            if (textoInteracao != null)
            {
                textoInteracao.gameObject.SetActive(false);
            }
        }
    }

    private void HandleHeadBob(
        Vector2 moveInput,
        bool isSprinting
    )
    {
        if (cameraTransform == null)
            return;

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

    private void HandleFootsteps(
        Vector2 moveInput,
        bool isSprinting
    )
    {
        bool isMoving =
            moveInput.sqrMagnitude > 0.01f &&
            controller.isGrounded;

        if (isMoving)
        {
            contadorPasso -= Time.deltaTime;

            if (contadorPasso <= 0f)
            {
                if (sonsPassos != null &&
                    sonsPassos.Length > 0 &&
                    audioPassos != null)
                {
                    int passo =
                        Random.Range(
                            0,
                            sonsPassos.Length
                        );

                    audioPassos.PlayOneShot(
                        sonsPassos[passo]
                    );
                }

                contadorPasso =
                    isSprinting
                        ? intervaloPassoCorrendo
                        : intervaloPasso;
            }
        }
        else
        {
            contadorPasso = 0f;

            if (audioPassos != null &&
                audioPassos.isPlaying)
            {
                audioPassos.Stop();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ItemColetavel item =
            other.GetComponentInParent<ItemColetavel>();

        if (item != null)
        {
            itemPerto = item;

            if (textoInteracao != null)
            {
                textoInteracao.text =
                    "Aperte [E] para coletar " +
                    item.itemNome;

                textoInteracao.gameObject.SetActive(true);
            }
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

            if (textoInteracao != null)
            {
                textoInteracao.gameObject.SetActive(false);
            }
        }
    }
}