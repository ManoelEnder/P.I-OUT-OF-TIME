using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraOverheat : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PhotoCamera photoCamera;
    [SerializeField] private GameObject lostCameraPrefab;
    [SerializeField] private Transform[] teleportPoints;

    [Header("Temperatura")]
    [SerializeField] private float maxHeat = 100f;
    [SerializeField] private float heatPerSecond = 5f;
    [SerializeField] private float heatPerPhoto = 12f;
    [SerializeField] private float coolingPerSecond = 8f;

    [Header("Interface")]
    [SerializeField] private Image heatBar;
    [SerializeField] private GameObject heatWarning;
    [SerializeField, Range(0f, 1f)] private float warningThreshold = 0.75f;

    [Header("Distorcao Temporal")]
    [SerializeField] private Volume glitchVolume;

    private float currentHeat;
    private bool cameraLost;
    private bool rupturePending;

    private GameObject lostCameraInstance;
    private bool configurationErrorLogged;
    private float displayedHeat = -1f;

    [Header("Barra")]
    [Tooltip("A cor vem do sprite (vinho ao vermelho); aqui so o brilho.")]
    [SerializeField] private Color barraNormal = Color.white;
    [SerializeField] private Color barraCritica = new Color(1f, 0.55f, 0.55f, 1f);
    [SerializeField, Min(1)] private int segmentosBarra = 10;

    [Header("Som de aquecimento")]
    [SerializeField, Range(0f, 1f)] private float volumeZumbido = 0.12f;
    [SerializeField] private float pitchZumbidoFrio = 0.85f;
    [SerializeField] private float pitchZumbidoQuente = 1.2f;
    [SerializeField, Range(0f, 1f)] private float volumeRuptura = 0.35f;

    [Header("Efeito de calor (suave)")]
    [Tooltip("Aberracao cromatica maxima no calor maximo.")]
    [SerializeField, Range(0f, 1f)] private float calorCromatico = 0.3f;
    [Tooltip("Ondulacao de calor: amplitude da distorcao de lente.")]
    [SerializeField, Range(0f, 0.3f)] private float calorOndulacao = 0.05f;
    [Tooltip("Respiracoes por segundo das bordas vermelhas.")]
    [SerializeField] private float calorRespiracao = 0.45f;
    [SerializeField] private Color calorBorda = new Color(0.3f, 0.02f, 0.02f, 1f);
    [SerializeField, Range(0f, 1f)] private float calorBordaMaxima = 0.5f;

    private RawImage heatGlowRaw;
    private static Texture2D heatGlowTexture;

    private AudioSource heatAudio;
    private AudioClip heatLoop;
    private AudioClip ruptureClip;

    [Header("Avisos no radio")]
    [SerializeField] private RadioMessageUI radioMessageUI;
    [SerializeField] private string mensagemPerdida = "A câmera sumiu na ruptura. Procure-a pelo laboratório.";
    [SerializeField] private string mensagemRecuperada = "Câmera recuperada. Deixe esfriar antes de abrir o visor.";

    private ChromaticAberration chromatic;
    private LensDistortion distortion;

    private float originalChromatic;
    private float originalDistortion;

    public float HeatPercent =>
        currentHeat / Mathf.Max(1f, maxHeat);

    public bool IsCameraLost => cameraLost;

    // Camera caida no mapa (o radar mostra em azul).
    public Transform LostCamera => lostCameraInstance != null ? lostCameraInstance.transform : null;

    private void Awake()
    {
        if (photoCamera == null)
            photoCamera = GetComponent<PhotoCamera>();

        if (glitchVolume != null && glitchVolume.profile != null)
        {
            glitchVolume.profile.TryGet(out chromatic);
            glitchVolume.profile.TryGet(out distortion);

            if (chromatic != null)
                originalChromatic = chromatic.intensity.value;

            if (distortion != null)
                originalDistortion = distortion.intensity.value;
        }

        UpdateHeatUI();
        ResetGlitch();
        SetupHeatAudio();
    }

    private void Update()
    {
        UpdateHeatSound();

        if (cameraLost || photoCamera == null)
            return;

        if (!photoCamera.isActiveAndEnabled)
            return;

        if (!rupturePending)
        {
            float heatChange = photoCamera.IsCameraMode
                ? heatPerSecond
                : -coolingPerSecond;

            currentHeat = Mathf.Clamp(
                currentHeat + heatChange * Time.deltaTime,
                0f,
                Mathf.Max(1f, maxHeat)
            );

            if (currentHeat >= Mathf.Max(1f, maxHeat))
                rupturePending = true;
        }

        UpdateHeatUI();
        UpdateGlitch();

        if (rupturePending && photoCamera.CanLoseCamera)
            LoseCamera();
    }

    public void RegisterPhotoHeat()
    {
        if (cameraLost || !enabled || photoCamera == null)
            return;

        currentHeat = Mathf.Clamp(
            currentHeat + heatPerPhoto,
            0f,
            Mathf.Max(1f, maxHeat)
        );

        if (currentHeat >= Mathf.Max(1f, maxHeat))
            rupturePending = true;

        UpdateHeatUI();
        UpdateGlitch();
    }

    private void LoseCamera()
    {
        if (cameraLost || !photoCamera.CanLoseCamera)
            return;

        Transform destination = GetRandomPoint();

        if (lostCameraPrefab == null || destination == null)
        {
            if (!configurationErrorLogged)
            {
                Debug.LogError(
                    "CameraOverheat: configure o Lost Camera Prefab e pelo menos um Teleport Point.",
                    this
                );

                configurationErrorLogged = true;
            }

            return;
        }

        GameObject instance = Instantiate(
            lostCameraPrefab,
            destination.position,
            destination.rotation
        );

        RestOnFloor(instance);

        LostCameraPickup pickup =
            instance.GetComponent<LostCameraPickup>();

        if (pickup == null)
        {
            Debug.LogError(
                "O prefab precisa ter o script LostCameraPickup.",
                instance
            );

            Destroy(instance);
            return;
        }

        pickup.Initialize(this);

        lostCameraInstance = instance;

        cameraLost = true;
        rupturePending = false;

        photoCamera.OnCameraLost();

        if (heatAudio != null)
        {
            heatAudio.volume = 0f;

            if (ruptureClip != null)
                heatAudio.PlayOneShot(ruptureClip, volumeRuptura);
        }

        ShowRadioMessage(mensagemPerdida);

        ResetGlitch();
    }

    // O ponto de teleporte marca o chao; sem isso a camera nascia com o centro no piso (meio enterrada).
    private static void RestOnFloor(GameObject instance)
    {
        Collider body = instance.GetComponentInChildren<Collider>();

        if (body == null)
            return;

        Physics.SyncTransforms();

        Bounds bounds = body.bounds;
        Vector3 from = new Vector3(bounds.center.x, bounds.max.y + 0.5f, bounds.center.z);
        RaycastHit[] hits = Physics.RaycastAll(from, Vector3.down, bounds.size.y + 2f, ~0, QueryTriggerInteraction.Ignore);

        float floor = float.NegativeInfinity;
        float nearest = float.PositiveInfinity;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(instance.transform) || hit.distance >= nearest)
                continue;

            nearest = hit.distance;
            floor = hit.point.y;
        }

        if (float.IsNegativeInfinity(floor))
            return;

        instance.transform.position += Vector3.up * (floor - bounds.min.y);
    }

    private Transform GetRandomPoint()
    {
        if (teleportPoints == null || teleportPoints.Length == 0)
            return null;

        int validCount = 0;

        foreach (Transform point in teleportPoints)
        {
            if (point != null)
                validCount++;
        }

        if (validCount == 0)
            return null;

        int selected = Random.Range(0, validCount);

        foreach (Transform point in teleportPoints)
        {
            if (point == null)
                continue;

            if (selected == 0)
                return point;

            selected--;
        }

        return null;
    }

    public void RecoverCamera()
    {
        if (!cameraLost)
            return;

        cameraLost = false;
        rupturePending = false;
        currentHeat = 0f;

        photoCamera.OnCameraRecovered();

        ShowRadioMessage(mensagemRecuperada);

        if (lostCameraInstance != null)
            Destroy(lostCameraInstance);

        lostCameraInstance = null;

        UpdateHeatUI();
        ResetGlitch();
    }

    private void SetupHeatAudio()
    {
        heatAudio = gameObject.AddComponent<AudioSource>();
        heatAudio.playOnAwake = false;
        heatAudio.loop = true;
        heatAudio.spatialBlend = 0f;
        heatAudio.volume = 0f;

        heatLoop = CreateChargeLoop();
        ruptureClip = CreateRuptureClip();

        heatAudio.clip = heatLoop;
        heatAudio.Play();
    }

    private void UpdateHeatSound()
    {
        if (heatAudio == null)
            return;

        float heat = HeatPercent;

        bool audible =
            !cameraLost &&
            !PauseMenu.IsPaused &&
            Time.timeScale > 0f &&
            photoCamera != null &&
            photoCamera.IsCameraMode;

        float target = audible
            ? volumeZumbido * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.2f, 1f, heat))
            : 0f;

        heatAudio.volume = Mathf.MoveTowards(heatAudio.volume, target, Time.unscaledDeltaTime * 0.5f);
        heatAudio.pitch = Mathf.Lerp(pitchZumbidoFrio, pitchZumbidoQuente, Mathf.SmoothStep(0f, 1f, heat));
    }

    // Zumbido grave e morno de transformador: loop de 1 s com frequencias inteiras (sem estalo na emenda).
    private static AudioClip CreateChargeLoop()
    {
        int rate = UnityEngine.AudioSettings.outputSampleRate > 0 ? UnityEngine.AudioSettings.outputSampleRate : 44100;
        float[] data = new float[rate];

        for (int i = 0; i < rate; i++)
        {
            float t = (float)i / rate;
            float tone =
                Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.6f +
                Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.22f +
                Mathf.Sin(2f * Mathf.PI * 330f * t) * 0.06f;
            float swell = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 2f * t);
            data[i] = tone * swell * 0.5f;
        }

        AudioClip clip = AudioClip.Create("HeatChargeLoop", rate, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Estalo eletrico curto para a ruptura.
    private static AudioClip CreateRuptureClip()
    {
        int rate = UnityEngine.AudioSettings.outputSampleRate > 0 ? UnityEngine.AudioSettings.outputSampleRate : 44100;
        int length = Mathf.RoundToInt(rate * 0.6f);
        float[] data = new float[length];
        System.Random random = new System.Random(7);
        float held = 0f;

        for (int i = 0; i < length; i++)
        {
            float t = (float)i / length;

            if (i % 6 == 0)
                held = (float)(random.NextDouble() * 2.0 - 1.0);

            float sweep = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(900f, 120f, t) * i / rate);
            data[i] = (held * 0.6f + sweep * 0.4f) * Mathf.Pow(1f - t, 2f) * 0.8f;
        }

        AudioClip clip = AudioClip.Create("HeatRupture", length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private void ShowRadioMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        if (radioMessageUI == null)
            radioMessageUI = FindFirstObjectByType<RadioMessageUI>();

        if (radioMessageUI != null)
            radioMessageUI.ShowMessage(message, null);
    }

    private void UpdateHeatUI()
    {
        float heat = HeatPercent;

        if (heatWarning != null)
        {
            // Aviso fora do centro da tela, piscando devagar (sem estrobo).
            bool warn = heat >= warningThreshold && !cameraLost;
            bool visible = warn && Mathf.Repeat(Time.unscaledTime * 0.8f, 1f) < 0.75f;

            if (heatWarning.activeSelf != visible)
                heatWarning.SetActive(visible);
        }

        if (heatBar == null)
            return;

        // A barra so precisa ser redesenhada quando o valor muda visivelmente.
        if (Mathf.Abs(heat - displayedHeat) < 0.002f)
            return;

        displayedHeat = heat;

        // Barra em segmentos inteiros, como um medidor analogico.
        int segments = Mathf.Max(1, segmentosBarra);
        heatBar.fillAmount = Mathf.Ceil(heat * segments - 0.001f) / segments;

        heatBar.color = Color.Lerp(
            barraNormal,
            barraCritica,
            Mathf.InverseLerp(warningThreshold, 1f, heat)
        );
    }

    // Efeito calmo: as bordas esquentam em vermelho e "respiram" devagar, a imagem ondula
    // de leve como ar quente e as cores se separam pouco. Nada pisca nem treme rapido.
    private void UpdateGlitch()
    {
        float heat = HeatPercent;
        float strength = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, heat));
        bool visible = photoCamera != null && photoCamera.IsCameraMode && !cameraLost;

        // Respiracao lenta, mais marcada perto do limite.
        float breathe = 0.5f + 0.5f * Mathf.Sin(Time.time * calorRespiracao * Mathf.PI * 2f);
        float hot = Mathf.InverseLerp(0.6f, 1f, heat);

        if (chromatic != null)
            chromatic.intensity.Override(Mathf.Clamp01(originalChromatic + strength * calorCromatico));

        if (distortion != null)
        {
            float wave = Mathf.Sin(Time.time * 1.3f) * 0.6f + Mathf.Sin(Time.time * 0.7f) * 0.4f;
            distortion.intensity.Override(Mathf.Clamp(originalDistortion + wave * calorOndulacao * strength, -1f, 1f));
        }

        UpdateHeatGlow(visible ? strength * Mathf.Lerp(1f, 0.7f, breathe * hot) : 0f);
    }

    private void UpdateHeatGlow(float amount)
    {
        if (heatGlowRaw == null)
            CreateHeatGlow();

        if (heatGlowRaw == null)
            return;

        bool show = amount > 0.01f;
        if (heatGlowRaw.gameObject.activeSelf != show)
            heatGlowRaw.gameObject.SetActive(show);

        if (!show)
            return;

        Color color = calorBorda;
        color.a = amount * calorBordaMaxima;
        heatGlowRaw.color = color;
    }

    // Brilho vermelho nas bordas da tela, desenhado embaixo do HUD.
    private void CreateHeatGlow()
    {
        if (heatBar == null || heatBar.canvas == null)
            return;

        Transform canvas = heatBar.canvas.rootCanvas.transform;

        GameObject go = new GameObject("HeatGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.layer = canvas.gameObject.layer;
        go.transform.SetParent(canvas, false);
        go.transform.SetAsFirstSibling();

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        if (heatGlowTexture == null)
            heatGlowTexture = BuildEdgeTexture();

        heatGlowRaw = go.GetComponent<RawImage>();
        heatGlowRaw.texture = heatGlowTexture;
        heatGlowRaw.raycastTarget = false;
        go.SetActive(false);
    }

    private static Texture2D BuildEdgeTexture()
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "HeatGlow",
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = Mathf.Abs(x / (size - 1f) - 0.5f) * 2f;
                float v = Mathf.Abs(y / (size - 1f) - 0.5f) * 2f;
                float d = Mathf.Max(Mathf.Pow(u, 10f), Mathf.Pow(v, 7f));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0.05f, 1f, d)));
            }
        }

        texture.Apply(false, true);
        return texture;
    }

    private void ResetGlitch()
    {
        if (heatGlowRaw != null)
            heatGlowRaw.gameObject.SetActive(false);

        if (chromatic != null)
            chromatic.intensity.Override(originalChromatic);

        if (distortion != null)
            distortion.intensity.Override(originalDistortion);
    }

    private void OnDisable()
    {
        ResetGlitch();
    }

    private void OnDestroy()
    {
        ResetGlitch();

        if (heatLoop != null)
            Destroy(heatLoop);

        if (ruptureClip != null)
            Destroy(ruptureClip);
    }
}