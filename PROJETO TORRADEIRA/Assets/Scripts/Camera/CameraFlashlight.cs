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
    [SerializeField] private float intensity = 2.6f;
    [SerializeField] private float range = 16f;
    [SerializeField] private float spotAngle = 52f;

    public static CameraFlashlight Instance { get; private set; }
    public bool IsOn { get; private set; }

    private Light spot;
    private CameraBattery battery;
    private CameraOverheat overheat;
    private AudioSource clickSource;
    private static AudioClip click;
    private float drainTimer;
    private TextMeshProUGUI label;

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
        Camera cam = Camera.main;
        GameObject lightGo = new GameObject("Lanterna", typeof(Light));
        lightGo.transform.SetParent(cam != null ? cam.transform : transform, false);
        lightGo.transform.localPosition = new Vector3(0.08f, -0.06f, 0.1f);
        lightGo.transform.localRotation = Quaternion.Euler(1.5f, -1f, 0f);
        spot = lightGo.GetComponent<Light>();
        spot.type = LightType.Spot;
        spot.color = new Color(0.86f, 0.92f, 1f);
        spot.range = range;
        spot.spotAngle = spotAngle;
        spot.innerSpotAngle = spotAngle * 0.45f;
        spot.intensity = intensity;
        spot.shadows = LightShadows.Soft;
        spot.enabled = false;

        battery = FindFirstObjectByType<CameraBattery>();
        overheat = FindFirstObjectByType<CameraOverheat>();
        clickSource = gameObject.AddComponent<AudioSource>();
        clickSource.playOnAwake = false;
        clickSource.spatialBlend = 0f;
        if (click == null) click = BuildClick();
        BuildLabel();
    }

    private bool CameraAvailable => (overheat == null || overheat.LostCamera == null) && (battery == null || !battery.IsEmpty);

    private void Update()
    {
        if (PauseMenu.IsPaused || DefeatController.IsDefeated)
        {
            if (DefeatController.IsDefeated && IsOn) Set(false, false);
            return;
        }

        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
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
            label.alpha = Mathf.MoveTowards(label.alpha, IsOn ? 0.9f : 0.35f, Time.deltaTime * 4f);
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
