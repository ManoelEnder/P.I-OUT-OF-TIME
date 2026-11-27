using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Lanterna embutida na camera: L liga/desliga. Gasta a mesma bateria da camera (1 barra a cada
// drainInterval segundos ligada), somando ao gasto do visor. Sem bateria ou com a camera perdida
// no superaquecimento, desliga sozinha. Bateria baixa: a luz treme.
public class CameraFlashlight : MonoBehaviour
{
    [SerializeField] private Key toggleKey = Key.L;
    [SerializeField] private float drainInterval = 6f;
    [SerializeField] private float intensity = 14f;
    [SerializeField] private float range = 26f;
    [SerializeField] private float spotAngle = 62f;

    public static CameraFlashlight Instance { get; private set; }
    public bool IsOn { get; private set; }

    [SerializeField, HideInInspector] private Light spot;
    private CameraBattery battery;
    private CameraOverheat overheat;
    private PhotoCamera photo;
    // A lanterna so funciona com o visor aberto; ao fechar o visor ela apaga.
    private bool CameraOpen => photo != null && photo.IsCameraMode;
    private AudioSource clickSource;
    private static AudioClip click;
    private float drainTimer;
    [SerializeField, HideInInspector] private TextMeshProUGUI label;

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
        if (spot == null)
            BuildLight();
        spot.enabled = false;

        battery = FindFirstObjectByType<CameraBattery>();
        photo = FindFirstObjectByType<PhotoCamera>();
        overheat = FindFirstObjectByType<CameraOverheat>();
        clickSource = gameObject.AddComponent<AudioSource>();
        clickSource.playOnAwake = false;
        clickSource.spatialBlend = 0f;
        if (click == null) click = BuildClick();
        if (label == null)
            BuildLabel();
    }

    // Monta a luz (filha da camera do jogador) e o texto do HUD.
    public void BuildAll()
    {
        if (spot != null) SceneBake.Kill(spot.gameObject);
        if (label != null) SceneBake.Kill(label.gameObject);
        BuildLight();
        BuildLabel();
    }

    private void BuildLight()
    {
        // Camera do jogador (a do PhotoCamera tambem tem a tag MainCamera, entao nao usa Camera.main).
        PhotoCamera pc = FindFirstObjectByType<PhotoCamera>(FindObjectsInactive.Include);
        Camera cam = pc != null && pc.playerCam != null ? pc.playerCam : Camera.main;
        GameObject lightGo = new GameObject("Lanterna", typeof(Light));
        lightGo.transform.SetParent(cam != null ? cam.transform : transform, false);
        lightGo.transform.localPosition = new Vector3(0.08f, -0.06f, 0.1f);
        lightGo.transform.localRotation = Quaternion.Euler(1.5f, -1f, 0f);
        spot = lightGo.GetComponent<Light>();
        spot.type = LightType.Spot;
        spot.color = new Color(0.86f, 0.92f, 1f);
        spot.range = range;
        spot.spotAngle = spotAngle;
        spot.innerSpotAngle = spotAngle * 0.6f;
        spot.intensity = intensity;
        spot.shadows = LightShadows.Soft;
        spot.enabled = false;
    }

    private bool CameraAvailable => (overheat == null || overheat.LostCamera == null) && (battery == null || !battery.IsEmpty);

    private void Update()
    {
        if (PauseMenu.IsPaused || DefeatController.IsDefeated)
        {
            if (DefeatController.IsDefeated && IsOn) Set(false, false);
            return;
        }

        if (IsOn && !CameraOpen)
            Set(false, true);

        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame && CameraOpen)
        {
            if (!IsOn && battery != null && battery.IsEmpty)
                BatteryWarning.Show(true);
            Set(!IsOn && CameraAvailable, true);
        }

        if (IsOn && !CameraAvailable)
            Set(false, true);

        if (IsOn && battery != null)
        {
            drainTimer += Time.deltaTime;
            if (drainTimer >= drainInterval)
            {
                drainTimer = 0f;
                battery.Use(1);
                if (battery.IsEmpty)
                    BatteryWarning.Show(false);
            }

            // Bateria baixa: tremula.
            float flicker = battery.IsLow && Random.value < 0.08f ? Random.Range(0.15f, 0.6f) : 1f;
            spot.intensity = intensity * (battery.IsLow ? 0.7f : 1f) * flicker;
        }

        if (label != null)
            label.alpha = Mathf.MoveTowards(label.alpha, !CameraOpen ? 0f : IsOn ? 0.9f : 0.45f, Time.deltaTime * 4f);
    }

    public void Set(bool on, bool sound)
    {
        if (on == IsOn) return;
        IsOn = on;
        if (spot != null) spot.enabled = on;
        drainTimer = 0f;
        if (sound && clickSource != null) clickSource.PlayOneShot(click, 0.6f);
        if (label != null) label.text = on ? "[L] LANTERNA  ON" : "[L] LANTERNA";
    }

    private void BuildLabel()
    {
        GameObject hud = GameObject.Find("HUDCanvas");
        if (hud == null) return;
        TMP_Text any = hud.GetComponentInChildren<TMP_Text>(true);
        GameObject go = new GameObject("FlashlightLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.layer = hud.layer;
        go.transform.SetParent(hud.transform, false);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0f, 0f);
        r.pivot = new Vector2(0f, 0f);
        r.anchoredPosition = new Vector2(24f, 20f);
        r.sizeDelta = new Vector2(300f, 28f);
        label = go.GetComponent<TextMeshProUGUI>();
        if (any != null) label.font = any.font;
        label.fontSize = 18f;
        label.color = new Color(0.88f, 0.88f, 0.86f, 1f);
        label.alpha = 0.35f;
        label.raycastTarget = false;
        label.text = "[L] LANTERNA";
    }

    private static AudioClip BuildClick()
    {
        const int rate = 44100;
        int n = (int)(rate * 0.06f);
        float[] data = new float[n];
        var rng = new System.Random(3);
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            data[i] = ((float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 180f) + Mathf.Sin(t * 2f * Mathf.PI * 2400f) * Mathf.Exp(-t * 90f) * 0.5f) * 0.8f;
        }
        AudioClip c = AudioClip.Create("FlashlightClick", n, 1, rate, false);
        c.SetData(data, 0);
        return c;
    }
}
