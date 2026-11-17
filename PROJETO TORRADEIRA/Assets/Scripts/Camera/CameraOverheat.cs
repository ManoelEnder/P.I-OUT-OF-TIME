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
    [SerializeField, Range(0f, 1f)] private float maxChromatic = 0.8f;
    [SerializeField, Range(0f, 1f)] private float maxDistortion = 0.25f;

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
    [SerializeField, Range(0f, 1f)] private float volumeAquecimento = 0.22f;
    [SerializeField] private float pitchFrio = 0.6f;
    [SerializeField] private float pitchQuente = 1.9f;
    [SerializeField, Range(0f, 1f)] private float volumeRuptura = 0.35f;

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
            ? volumeAquecimento * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.1f, 1f, heat))
            : 0f;

        heatAudio.volume = Mathf.MoveTowards(heatAudio.volume, target, Time.unscaledDeltaTime * 0.8f);
        heatAudio.pitch = Mathf.Lerp(pitchFrio, pitchQuente, heat);
    }

    // Zumbido de capacitor carregando: loop de 1 s com frequencias inteiras (sem estalo na emenda).
    private static AudioClip CreateChargeLoop()
    {
        int rate = UnityEngine.AudioSettings.outputSampleRate > 0 ? UnityEngine.AudioSettings.outputSampleRate : 44100;
        float[] data = new float[rate];

        for (int i = 0; i < rate; i++)
        {
            float t = (float)i / rate;
            float tone =
                Mathf.Sin(2f * Mathf.PI * 330f * t) * 0.55f +
                Mathf.Sin(2f * Mathf.PI * 660f * t) * 0.25f +
                Mathf.Sin(2f * Mathf.PI * 990f * t) * 0.1f;
            float flutter = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 12f * t);
            data[i] = tone * flutter * 0.5f;
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
            // Pisca o aviso fora do centro da tela, sem cobrir a mira.
            bool warn = heat >= warningThreshold && !cameraLost;
            bool visible = warn && Mathf.Repeat(Time.unscaledTime * 3f, 1f) < 0.6f;

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

    private void UpdateGlitch()
    {
        float strength = Mathf.InverseLerp(
            0.25f,
            1f,
            HeatPercent
        );

        float pulse = 0f;

        if (HeatPercent >= 0.65f && FlashSettings.Enabled)
        {
            pulse = Mathf.Pow(
                Mathf.Max(
                    0f,
                    Mathf.Sin(Time.time * 22f)
                ),
                12f
            ) * strength;
        }

        if (chromatic != null)
        {
            float value = Mathf.Clamp01(
                originalChromatic +
                strength * maxChromatic +
                pulse * 0.15f
            );

            chromatic.intensity.Override(value);
        }

        if (distortion != null)
        {
            float value =
                originalDistortion +
                Mathf.Sin(Time.time * 13f) *
                maxDistortion *
                strength +
                pulse * 0.1f;

            distortion.intensity.Override(
                Mathf.Clamp(value, -1f, 1f)
            );
        }
    }

    private void ResetGlitch()
    {
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