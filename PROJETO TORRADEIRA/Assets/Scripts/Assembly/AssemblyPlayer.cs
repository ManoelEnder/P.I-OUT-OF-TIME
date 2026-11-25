using UnityEngine;
using UnityEngine.InputSystem;

// Jogador simples da sala de montagem: anda (WASD), olha (mouse) e interage (E) mirando no centro.
public class AssemblyPlayer : MonoBehaviour
{
    public float speed = 3.2f;
    public float sensitivity = 0.12f;
    public float eyeHeight = 1.75f;

    public Camera Cam { get; private set; }
    public bool Locked { get; set; }

    private CharacterController controller;
    private float pitch;
    private float yVel;
    private float bob;

    private void Awake()
    {
        controller = gameObject.AddComponent<CharacterController>();
        controller.height = 1.9f;
        controller.radius = 0.3f;
        controller.center = new Vector3(0f, 0.95f, 0f);

        GameObject camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(transform, false);
        camGo.transform.localPosition = new Vector3(0f, eyeHeight, 0f);
        Cam = camGo.GetComponent<Camera>();
        Cam.fieldOfView = 70f;
        Cam.nearClipPlane = 0.05f;
        Cam.backgroundColor = Color.black;
        Cam.clearFlags = CameraClearFlags.SolidColor;
        var data = camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (Keyboard.current == null || Mouse.current == null)
            return;
        float dt = Time.deltaTime;

        if (!Locked)
        {
            Vector2 look = Mouse.current.delta.ReadValue() * sensitivity;
            transform.Rotate(0f, look.x, 0f);
            pitch = Mathf.Clamp(pitch - look.y, -80f, 80f);

            Vector2 input = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
            input = Vector2.ClampMagnitude(input, 1f);
            Vector3 move = (transform.right * input.x + transform.forward * input.y) * speed;
            yVel = controller.isGrounded ? -2f : yVel - 9.81f * dt;
            move.y = yVel;
            controller.Move(move * dt);

            bob += input.magnitude * dt * 8f;
            float b = Mathf.Sin(bob) * 0.03f * input.magnitude;
            Cam.transform.localPosition = new Vector3(0f, eyeHeight + b, 0f);
        }

        Cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    // Vira a camera suavemente para olhar um ponto (cena da garra montando).
    public void LookAtPoint(Vector3 point, float t)
    {
        Vector3 dir = point - Cam.transform.position;
        Vector3 flat = new Vector3(dir.x, 0f, dir.z);
        if (flat.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), t);
        float targetPitch = -Mathf.Atan2(dir.y, flat.magnitude) * Mathf.Rad2Deg;
        pitch = Mathf.Lerp(pitch, targetPitch, t);
    }
}
