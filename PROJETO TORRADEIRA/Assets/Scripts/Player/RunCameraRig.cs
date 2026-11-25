using Unity.Cinemachine;
using UnityEngine;

// Camera do jogador pelo Cinemachine:
// - o PlayerMovement passa a mover um "CameraAnchor" (balanco de passo, pouso, olhar);
// - uma CinemachineCamera fica travada nesse anchor (posicao e rotacao);
// - correndo, o Recomposer inclina para a frente (tilt) e para os lados (dutch: curva com o mouse,
//   passo e andar de lado), o ruido Perlin aumenta e o FOV abre um pouco.
// - FovBridge: os scripts que ja mudavam camera.fieldOfView (zoom do visor, efeitos) continuam
//   funcionando: o valor e copiado para a lente da CinemachineCamera antes do Brain aplicar.
// - Durante o jumpscare o Brain e desligado (o jumpscare controla a camera direto).
public class RunCameraRig : MonoBehaviour
{
    [Header("Inclinacao correndo")]
    [SerializeField] private float forwardLean = 5f;      // graus para a frente
    [SerializeField] private float strafeLean = 4f;       // graus andando de lado
    [SerializeField] private float turnLean = 5f;         // graus na curva (mouse)
    [SerializeField] private float stepLean = 1.6f;       // graus alternando a cada passo
    [SerializeField] private float leanSmooth = 6f;
    [SerializeField] private float runFovBoost = 1.08f;

    [Header("Ruido (Perlin)")]
    [SerializeField] private float noiseWalk = 0.25f;
    [SerializeField] private float noiseRun = 1.1f;

    public static RunCameraRig Instance { get; private set; }

    private PlayerMovement movement;
    private Camera cam;
    private CinemachineBrain brain;
    private CinemachineCamera vcam;
    private CinemachineRecomposer recomposer;
    private CinemachineBasicMultiChannelPerlin noise;
    private float tilt, dutch, run;

    public void SetBrainEnabled(bool on)
    {
        if (brain != null) brain.enabled = on;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        movement = GetComponent<PlayerMovement>();
        cam = Camera.main;
        if (movement == null || cam == null || movement.cameraTransform == null)
        {
            enabled = false;
            return;
        }

        // Anchor no lugar exato da camera real (mundo); o PlayerMovement passa a mexer nele.
        // Se a camera e filha do objeto que o PlayerMovement move, o offset entre os dois e mantido.
        Transform moved = movement.cameraTransform;
        GameObject anchor = new GameObject("CameraAnchor");
        anchor.transform.SetParent(moved.parent, false);
        anchor.transform.localPosition = moved.localPosition;
        anchor.transform.localRotation = moved.localRotation;
        Transform eye = new GameObject("CameraEye").transform;
        eye.SetParent(anchor.transform, false);
        eye.position = cam.transform.position;
        eye.rotation = cam.transform.rotation;
        movement.cameraTransform = anchor.transform;
        float startFov = cam.fieldOfView > 1f ? cam.fieldOfView : 60f;

        brain = cam.GetComponent<CinemachineBrain>();
        if (brain == null) brain = cam.gameObject.AddComponent<CinemachineBrain>();
        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
        brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;

        // Criada inativa: o Cinemachine monta o pipeline com todos os componentes ao ativar.
        GameObject vgo = new GameObject("CM PlayerCamera");
        vgo.SetActive(false);
        vcam = vgo.AddComponent<CinemachineCamera>();
        vcam.Follow = eye;
        vcam.LookAt = null;
        vcam.Priority = 10;
        // Lente copiada da camera real (inclui sensor/fisica: sem isso o FOV pode virar 0).
        LensSettings lens = LensSettings.FromCamera(cam);
        lens.FieldOfView = startFov;
        vcam.Lens = lens;
        vgo.AddComponent<CinemachineHardLockToTarget>();
        vgo.AddComponent<CinemachineRotateWithFollowTarget>();
        recomposer = vgo.AddComponent<CinemachineRecomposer>();
        // AddComponent em tempo de jogo nao roda o Reset(): sem isso o zoom fica 0 (FOV minusculo).
        recomposer.ZoomScale = 1f;
        recomposer.FollowAttachment = 1f;
        recomposer.LookAtAttachment = 1f;
        recomposer.ApplyAfter = CinemachineCore.Stage.Aim;
        noise = vgo.AddComponent<CinemachineBasicMultiChannelPerlin>();
        noise.NoiseProfile = BuildNoise();
        noise.AmplitudeGain = noiseWalk;
        noise.FrequencyGain = 1f;

        vgo.SetActive(true);

        FovBridge bridge = cam.gameObject.AddComponent<FovBridge>();
        bridge.Init(cam, vcam, this, startFov);
    }

    // Chamado pelo FovBridge antes do Brain: a corrida abre um pouco o FOV pedido pelos outros scripts.
    // O FOV nao muda na corrida: os scripts do visor leem o FOV da camera e recalculam a partir dele
    // a cada quadro (zoom), entao qualquer aumento aqui virava uma bola de neve com o visor aberto.
    public float RunFov(float baseFov) => baseFov;

    private void Update()
    {
        if (movement == null || vcam == null)
            return;

        float dt = Time.deltaTime;
        bool sprinting = movement.IsSprinting && movement.MoveBlend > 0.2f && !PauseMenu.IsPaused;
        run = Mathf.MoveTowards(run, sprinting ? 1f : 0f, dt * (sprinting ? 2.5f : 3.5f));

        Vector2 input = movement.MoveInput;
        float turn = Mathf.Clamp(movement.LookDelta.x * 0.35f, -1f, 1f);
        float step = Mathf.Sin(movement.StepPhase * Mathf.PI);

        float targetTilt = forwardLean * run * Mathf.Max(0f, input.y);
        float targetDutch = (-input.x * strafeLean - turn * turnLean) * run + step * stepLean * run;
        // Andando: so um resto bem leve da inclinacao lateral.
        targetDutch += -input.x * 0.8f * movement.MoveBlend * (1f - run);

        float k = 1f - Mathf.Exp(-leanSmooth * dt);
        tilt = Mathf.Lerp(tilt, targetTilt, k);
        dutch = Mathf.Lerp(dutch, targetDutch, k);
        recomposer.Tilt = tilt;
        recomposer.Dutch = dutch;

        noise.AmplitudeGain = Mathf.Lerp(noiseWalk * movement.MoveBlend, noiseRun, run);
        noise.FrequencyGain = Mathf.Lerp(1f, 1.8f, run);
    }

    // Ruido de "camera na mao": posicao quase parada, rotacao balancando devagar.
    private static NoiseSettings BuildNoise()
    {
        NoiseSettings n = ScriptableObject.CreateInstance<NoiseSettings>();
        n.name = "RunHandheld";
        n.PositionNoise = new[]
        {
            new NoiseSettings.TransformNoiseParams
            {
                X = new NoiseSettings.NoiseParams { Frequency = 0.9f, Amplitude = 0.004f },
                Y = new NoiseSettings.NoiseParams { Frequency = 1.7f, Amplitude = 0.006f },
                Z = new NoiseSettings.NoiseParams { Frequency = 0.6f, Amplitude = 0.002f },
            },
        };
        n.OrientationNoise = new[]
        {
            new NoiseSettings.TransformNoiseParams
            {
                X = new NoiseSettings.NoiseParams { Frequency = 1.1f, Amplitude = 0.35f },
                Y = new NoiseSettings.NoiseParams { Frequency = 0.8f, Amplitude = 0.3f },
                Z = new NoiseSettings.NoiseParams { Frequency = 0.6f, Amplitude = 0.25f },
            },
            new NoiseSettings.TransformNoiseParams
            {
                X = new NoiseSettings.NoiseParams { Frequency = 3.1f, Amplitude = 0.08f },
                Y = new NoiseSettings.NoiseParams { Frequency = 2.7f, Amplitude = 0.08f },
                Z = new NoiseSettings.NoiseParams { Frequency = 2.3f, Amplitude = 0.05f },
            },
        };
        return n;
    }
}

// Copia o FOV que os scripts do jogo escrevem na Camera para a lente da CinemachineCamera,
// antes do Brain aplicar (ordem de execucao bem cedo no LateUpdate).
[DefaultExecutionOrder(-10000)]
public class FovBridge : MonoBehaviour
{
    private Camera cam;
    private CinemachineCamera vcam;
    private RunCameraRig rig;
    private float lastApplied = -1f;
    private float requested;

    public void Init(Camera c, CinemachineCamera v, RunCameraRig r, float startFov)
    {
        cam = c;
        vcam = v;
        rig = r;
        requested = startFov;
        lastApplied = startFov;
    }

    private void LateUpdate()
    {
        if (cam == null || vcam == null)
            return;
        // Se alguem mudou o FOV da camera desde o ultimo quadro, esse e o novo pedido.
        if (Mathf.Abs(cam.fieldOfView - lastApplied) > 0.001f && cam.fieldOfView > 1f)
            requested = cam.fieldOfView;
        float fov = rig != null ? rig.RunFov(requested) : requested;
        LensSettings lens = vcam.Lens;
        lens.FieldOfView = fov;
        lens.NearClipPlane = cam.nearClipPlane;
        vcam.Lens = lens;
        lastApplied = fov;
    }
}
