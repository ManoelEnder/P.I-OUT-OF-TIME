using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float sensibilidade = 80f;
    public Transform cameraTransform;

    float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * sensibilidade * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensibilidade * Time.deltaTime;

        // câmera
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -75f, 75f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // player gira só no Y
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y + mouseX, 0f);
    }
}