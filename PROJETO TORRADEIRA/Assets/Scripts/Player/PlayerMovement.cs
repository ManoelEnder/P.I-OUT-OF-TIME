using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    public float gravity = -9.81f;
    public float jumpForce = 2f;

    public Transform cameraTransform;
    public CharacterController controller;

    public TextMeshProUGUI textoInteracao;

    float xRotation = 0f;
    float yVelocity = 0f;

    private ItemColetavel itemPerto;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        textoInteracao.gameObject.SetActive(false);
    }

    void Update()
    {
        Vector2 moveInput = Vector2.zero;

        if (Keyboard.current.wKey.isPressed) moveInput.y += 1;
        if (Keyboard.current.sKey.isPressed) moveInput.y -= 1;
        if (Keyboard.current.dKey.isPressed) moveInput.x += 1;
        if (Keyboard.current.aKey.isPressed) moveInput.x -= 1;

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        if (controller.isGrounded && yVelocity < 0)
            yVelocity = -2f;

        if (Keyboard.current.spaceKey.wasPressedThisFrame && controller.isGrounded)
            yVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);

        yVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * moveSpeed;
        velocity.y = yVelocity;

        controller.Move(velocity * Time.deltaTime);

        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;

        xRotation -= mouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseDelta.x);

        if (itemPerto != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Destroy(itemPerto.gameObject);
            itemPerto = null;
            textoInteracao.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ItemColetavel item = other.GetComponentInParent<ItemColetavel>();

        if (item != null)
        {
            itemPerto = item;

            textoInteracao.text = "Aperte [E] para coletar " + item.itemNome;
            textoInteracao.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        ItemColetavel item = other.GetComponentInParent<ItemColetavel>();

        if (item != null && item == itemPerto)
        {
            itemPerto = null;
            textoInteracao.gameObject.SetActive(false);
        }
    }
}