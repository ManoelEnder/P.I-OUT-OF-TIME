using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// Movimento em primeira pessoa:
// - acelera e freia suave (sem arrancar/parar seco), com pouco controle no ar;
// - passos medidos pela DISTANCIA andada: o balanco da camera e o som do passo andam juntos;
// - camera desce no apoio do pe, balanca de lado, inclina ao andar de lado e "afunda" ao pousar;
// - respiracao bem leve parado.
public class PlayerMovement : MonoBehaviour
{
    [Header("Velocidade")]
    public float moveSpeed = 3.5f;
    public float sprintSpeed = 6f;
    [Tooltip("m/s ganhos por segundo ao comecar a andar.")]
    public float acceleration = 18f;
    [Tooltip("m/s perdidos por segundo ao soltar as teclas.")]
    public float deceleration = 22f;
    [Range(0f, 1f)] public float airControl = 0.35f;

    [Header("Pulo e gravidade")]
    public float gravity = -9.81f;
    public float jumpForce = 2f;
    [Tooltip("Ainda da para pular logo depois de sair de um degrau.")]
    public float coyoteTime = 0.12f;

    [Header("Mouse")]
    public float mouseSensitivity = 2f;

    [Header("Referencias")]
    public Transform cameraTransform;
    public CharacterController controller;
    public TextMeshProUGUI textoInteracao;

    [Header("Passada")]
    [Tooltip("Metros por passo andando / correndo.")]
    public float walkStride = 0.75f;
    public float sprintStride = 1.05f;

    [Header("Balanco da camera")]
    public float bobWalk = 0.035f;
    public float bobSprint = 0.06f;
    public float swayAmount = 0.35f;
    public float stepRoll = 0.6f;
    public float strafeRoll = 1.6f;
    public float breathing = 0.006f;
    public float landingDip = 0.08f;

    [Header("Sons de passo")]
    public AudioSource audioPassos;
    public AudioClip[] sonsPassos;
    [Range(0f, 1f)] public float volumePassoAndando = 0.7f;
    [Range(0f, 1f)] public float volumePassoCorrendo = 1f;

    // Lidos pelo aparelho de radar (RadarDevice) para balancar na mao.
    public float StepPhase { get; private set; }
    public float MoveBlend { get; private set; }
    // Correndo faz barulho: o inimigo escuta de mais longe.
    public bool IsSprinting { get; private set; }
    public Vector2 LookDelta { get; private set; }
    public float LandingOffset => landOffset;

    private const string SensitivityKey = "MouseSensitivity";

    private float xRotation;
    private float yVelocity;
    private Vector3 planarVelocity;
    private float lastGroundedTime;
    private bool wasGrounded = true;
    private float fallSpeed;

    private float stepDistance;
    private int stepCount;
    private float bobWeight;
    private float rollCurrent;
    private float landOffset;
    private float landVelocity;
    private float sensitivityFactor = 1f;

    private Vector3 cameraStartPosition;
    private ItemColetavel itemPerto;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (textoInteracao != null)
            textoInteracao.gameObject.SetActive(false);

        if (cameraTransform != null)
            cameraStartPosition = cameraTransform.localPosition;

        RefreshSensitivity();
    }

    private void OnEnable()
    {
        RefreshSensitivity();
    }

    // O slider de sensibilidade (0-100%) salva em PlayerPrefs; 100% = velocidade original.
    public void RefreshSensitivity()
    {
        sensitivityFactor = Mathf.Lerp(0.15f, 1f, Mathf.Clamp01(PlayerPrefs.GetFloat(SensitivityKey, 1f)));
    }

    private void Update()
    {
        if (PauseMenu.IsPaused || DefeatController.IsDefeated || Keyboard.current == null)
            return;

        float dt = Time.deltaTime;
        Vector2 input = ReadMoveInput();
        bool grounded = controller.isGrounded;

        if (grounded)
            lastGroundedTime = Time.time;

        // ---------- Horizontal com aceleracao ----------
        bool sprinting = Keyboard.current.leftShiftKey.isPressed && input.y > 0.1f && grounded;
        IsSprinting = sprinting;
        float targetSpeed = sprinting ? sprintSpeed : moveSpeed;
        Vector3 wish = (transform.right * input.x + transform.forward * input.y) * targetSpeed;

        float rate = wish.sqrMagnitude > planarVelocity.sqrMagnitude ? acceleration : deceleration;
        if (!grounded)
            rate *= airControl;
        planarVelocity = Vector3.MoveTowards(planarVelocity, wish, rate * dt);

        // ---------- Vertical ----------
        if (grounded && yVelocity < 0f)
            yVelocity = -2f;

        if (Keyboard.current.spaceKey.wasPressedThisFrame && Time.time - lastGroundedTime <= coyoteTime)
        {
            yVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
            lastGroundedTime = -999f;
        }

        yVelocity += gravity * dt;
        if (!grounded)
            fallSpeed = Mathf.Max(fallSpeed, -yVelocity);

        Vector3 motion = planarVelocity;
        motion.y = yVelocity;
        CollisionFlags flags = controller.Move(motion * dt);

        // Bateu numa parede: perde a velocidade nessa direcao (nao "gruda" deslizando rapido).
        if ((flags & CollisionFlags.Sides) != 0)
        {
            Vector3 real = controller.velocity;
            real.y = 0f;
            planarVelocity = Vector3.Lerp(planarVelocity, real, 0.5f);
        }

        bool groundedNow = controller.isGrounded;
        if (groundedNow && !wasGrounded)
            Land();
        wasGrounded = groundedNow;

        Look();
        UpdateSteps(groundedNow, sprinting, dt);
        UpdateCamera(input, sprinting, dt);

        if (itemPerto != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Destroy(itemPerto.gameObject);
            itemPerto = null;

            if (textoInteracao != null)
                textoInteracao.gameObject.SetActive(false);
        }
    }

    private static Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) input.y += 1f;
        if (Keyboard.current.sKey.isPressed) input.y -= 1f;
        if (Keyboard.current.dKey.isPressed) input.x += 1f;
        if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        return input.sqrMagnitude > 1f ? input.normalized : input;
    }

    private void Look()
    {
        LookDelta = Vector2.zero;

        if (Mouse.current == null || cameraTransform == null)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity * sensitivityFactor;
        LookDelta = delta;

        xRotation = Mathf.Clamp(xRotation - delta.y, -89f, 89f);
        transform.Rotate(Vector3.up * delta.x);
    }

    // ---------- Passos ----------

    private void UpdateSteps(bool grounded, bool sprinting, float dt)
    {
        float speed = new Vector3(planarVelocity.x, 0f, planarVelocity.z).magnitude;
        float moving = grounded ? Mathf.InverseLerp(0.2f, moveSpeed, speed) : 0f;
        MoveBlend = Mathf.MoveTowards(MoveBlend, moving, dt * 4f);

        if (!grounded || speed < 0.2f)
        {
            // Parado: termina o passo atual devagar para a camera assentar no lugar.
            StepPhase = Mathf.MoveTowards(StepPhase, Mathf.Round(StepPhase), dt * 2f);
            return;
        }

        float stride = sprinting ? sprintStride : walkStride;
        stepDistance += speed * dt;
        StepPhase += speed * dt / stride;

        // Cada passo completo = um pe no chao.
        int step = Mathf.FloorToInt(StepPhase);
        if (step != stepCount)
        {
            stepCount = step;
            PlayFootstep(sprinting ? volumePassoCorrendo : volumePassoAndando);
        }
    }

    private void PlayFootstep(float volume)
    {
        if (audioPassos == null || sonsPassos == null || sonsPassos.Length == 0)
            return;

        audioPassos.pitch = Random.Range(0.9f, 1.08f);
        audioPassos.PlayOneShot(sonsPassos[Random.Range(0, sonsPassos.Length)], volume);
    }

    private void Land()
    {
        // Afunda proporcional a velocidade da queda (mola volta sozinha).
        float impact = Mathf.Clamp01((fallSpeed - 2f) / 8f);
        landVelocity -= landingDip * 12f * Mathf.Max(0.25f, impact);
        fallSpeed = 0f;

        if (impact > 0.1f)
            PlayFootstep(Mathf.Lerp(0.6f, 1f, impact));
    }

    // ---------- Camera ----------

    private void UpdateCamera(Vector2 input, bool sprinting, float dt)
    {
        if (cameraTransform == null)
            return;

        bobWeight = Mathf.MoveTowards(bobWeight, MoveBlend, dt * 3f);
        float amount = Mathf.Lerp(bobWalk, bobSprint, sprinting ? 1f : 0f) * bobWeight;

        // Fase 0..1 de cada passo: desce no apoio (fase 0/1), sobe no meio.
        float phase = StepPhase - Mathf.Floor(StepPhase);
        float vertical = -amount * (0.5f + 0.5f * Mathf.Cos(phase * Mathf.PI * 2f));
        // Lado alterna a cada passo (um ciclo lateral = dois passos).
        float side = Mathf.Sin(StepPhase * Mathf.PI);
        float lateral = side * amount * swayAmount;

        // Respiracao parado.
        float breath = Mathf.Sin(Time.time * 1.6f) * breathing * (1f - bobWeight);

        // Mola do pouso, em passos pequenos (um quadro longo nao pode deixar a mola explodir).
        float remaining = Mathf.Min(dt, 0.1f);
        while (remaining > 0f)
        {
            float step = Mathf.Min(remaining, 1f / 120f);
            landVelocity += (-landOffset * 120f - landVelocity * 16f) * step;
            landOffset += landVelocity * step;
            remaining -= step;
        }
        if (float.IsNaN(landOffset) || float.IsInfinity(landOffset))
            landOffset = landVelocity = 0f;

        cameraTransform.localPosition = cameraStartPosition + new Vector3(lateral, vertical + breath + landOffset, 0f);

        float rollTarget = -input.x * strafeRoll + side * stepRoll * bobWeight;
        rollCurrent = Mathf.Lerp(rollCurrent, rollTarget, 1f - Mathf.Exp(-8f * dt));
        float pitchDip = -landOffset * 20f;

        cameraTransform.localRotation = Quaternion.Euler(xRotation + pitchDip, 0f, rollCurrent);
    }

    // ---------- Coleta ----------

    private void OnTriggerEnter(Collider other)
    {
        ItemColetavel item = other.GetComponentInParent<ItemColetavel>();

        if (item == null)
            return;

        itemPerto = item;

        if (textoInteracao != null)
        {
            textoInteracao.text = "Aperte [E] para coletar " + item.itemNome;
            textoInteracao.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        ItemColetavel item = other.GetComponentInParent<ItemColetavel>();

        if (item == null || item != itemPerto)
            return;

        itemPerto = null;

        if (textoInteracao != null)
            textoInteracao.gameObject.SetActive(false);
    }
}
