using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Aparelho portatil de 1975 com tela CRT: mapa do andar (gerado do NavMesh, revelado conforme
// o jogador explora) + varredura de radar. Pecas pulsam em branco, o inimigo em vermelho.
// Fica abaixado no canto; segurar Q levanta o aparelho. O LED vermelho pisca mais rapido
// quanto mais perto o inimigo estiver. Tudo desenhado em pixel art no codigo.
public class RadarDevice : MonoBehaviour
{
    [Header("Tela")]
    [SerializeField] private float rangeMeters = 22f;
    [SerializeField] private float sweepSeconds = 2.4f;
    [SerializeField] private float revealRadius = 7f;
    [SerializeField] private float screenFps = 24f;

    [Header("Mao")]
    [SerializeField] private Key raiseKey = Key.Q;

    // Tamanhos em pixels do desenho (exibido em escala 4x).
    private const int DeviceW = 80, DeviceH = 96, Scale = 5;
    private const int ScreenX = 13, ScreenY = 37, ScreenW = 54, ScreenH = 40;
    private const int TexW = 108, TexH = 80; // tela em meia-resolucao do aparelho
    private const float CellSize = 0.5f;

    private static readonly Color32 Black = new Color32(6, 7, 8, 255);
    private static readonly Color32 Floor = new Color32(48, 52, 54, 255);
    private static readonly Color32 FloorUnknown = new Color32(18, 20, 21, 255);
    private static readonly Color32 Wall = new Color32(190, 196, 196, 255);
    private static readonly Color32 WallUnknown = new Color32(52, 56, 58, 255);
    private static readonly Color32 Sweep = new Color32(120, 128, 130, 255);
    private static readonly Color32 PieceColor = new Color32(255, 255, 255, 255);
    private static readonly Color32 EnemyColor = new Color32(235, 40, 36, 255);
    private static readonly Color32 LostCameraColor = new Color32(70, 140, 255, 255);

    private Transform player;
    private PlayerMovement movement;
    private Vector2 lag, lagVelocity;
    private Transform enemy;
    private CameraOverheat overheat;
    private readonly List<Transform> pieces = new List<Transform>();
    private float nextRefresh;

    // Bipe de peca proxima: mais rapido e agudo conforme chega perto (mais alto com o radar levantado).
    [SerializeField] private float beepRange = 12f;
    private AudioSource beepSource;
    private AudioClip beepClip;
    private float nextBeep;

    private RectTransform root;
    private RawImage screen;
    private Texture2D screenTex;
    private Color32[] pixels;
    private Image led;
    private Image ledGlow;
    private float raise;
    private float nextFrame;

    // Mapa do andar atual.
    private bool[] walk;
    private bool[] edge;
    private bool[] explored;
    private int gridW, gridH;
    private Vector2 gridMin;
    private float floorY = float.NaN;
    private float nextReveal;

    // Pulsos: hora em que a varredura passou por cada alvo.
    private readonly Dictionary<Transform, float> pulses = new Dictionary<Transform, float>();
    private float lastSweepAngle;

    public static RadarDevice Create(Transform parent)
    {
        GameObject go = new GameObject("RadarDevice", typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return go.AddComponent<RadarDevice>();
    }

    private void Awake()
    {
        root = (RectTransform)transform;
        root.anchorMin = root.anchorMax = new Vector2(1f, 0f);
        root.pivot = new Vector2(1f, 0f);
        root.sizeDelta = new Vector2(DeviceW * Scale, DeviceH * Scale);

        BuildBody();
        BuildScreen();
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        player = p != null ? p.transform : null;
        movement = FindFirstObjectByType<PlayerMovement>();
        RefreshTargets();

        // O detector antigo ficava no braco escondido; o radar assume o bipe.
        foreach (DetectorPecas old in FindObjectsByType<DetectorPecas>(FindObjectsSortMode.None))
            old.enabled = false;

        beepSource = gameObject.AddComponent<AudioSource>();
        beepSource.playOnAwake = false;
        beepSource.spatialBlend = 0f;
        beepClip = BuildBeep();
    }

    private void UpdateBeep()
    {
        float nearest = beepRange;
        foreach (Transform piece in pieces)
        {
            if (piece == null || !piece.gameObject.activeInHierarchy)
                continue;
            float d = Vector3.Distance(piece.position, player.position);
            if (d < nearest)
                nearest = d;
        }

        if (nearest >= beepRange || DefeatController.IsDefeated || Time.timeScale == 0f)
        {
            nextBeep = 0f;
            return;
        }

        float near = 1f - nearest / beepRange;
        if (Time.time < nextBeep)
            return;

        nextBeep = Time.time + Mathf.Lerp(1.3f, 0.12f, near);
        beepSource.pitch = Mathf.Lerp(1f, 1.6f, near);
        beepSource.PlayOneShot(beepClip, Mathf.Lerp(0.06f, 0.3f, near) * Mathf.Lerp(0.5f, 1f, raise));
    }

    private static AudioClip BuildBeep()
    {
        const int rate = 44100;
        int n = (int)(rate * 0.07f);
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            float env = Mathf.Min(1f, i / (rate * 0.004f)) * (1f - (float)i / n);
            data[i] = (Mathf.Sin(2f * Mathf.PI * 1400f * t) >= 0f ? 1f : -1f) * env * 0.5f;
        }
        AudioClip clip = AudioClip.Create("RadarBeep", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ---------- Desenho do aparelho ----------

    private void BuildBody()
    {
        var tex = new Texture2D(DeviceW, DeviceH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "RadarDeviceBody" };
        var px = new Color32[DeviceW * DeviceH];

        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 outline = new Color32(22, 23, 24, 255);
        Color32 body = new Color32(150, 152, 148, 255);
        Color32 bodyLight = new Color32(186, 188, 184, 255);
        Color32 bodyDark = new Color32(104, 106, 104, 255);
        Color32 bezel = new Color32(34, 35, 36, 255);
        Color32 metal = new Color32(120, 122, 124, 255);

        for (int i = 0; i < px.Length; i++)
            px[i] = clear;

        // Antenas.
        foreach (int ax in new[] { 16, 63 })
        {
            for (int y = 82; y < 94; y++)
                Set(px, ax, y, metal);
            Rect(px, ax - 1, 93, ax + 1, 95, bodyLight);
        }

        // Corpo arredondado com contorno, luz em cima e sombra embaixo.
        for (int y = 2; y < 84; y++)
        {
            for (int x = 3; x < 77; x++)
            {
                bool corner = (x < 6 || x > 73) && (y < 5 || y > 80) && Mathf.Abs(x < 40 ? x - 6 : x - 73) + Mathf.Abs(y < 40 ? y - 5 : y - 80) > 3;
                if (corner)
                    continue;

                bool border = x == 3 || x == 76 || y == 2 || y == 83;
                Color32 c = border ? outline : (y > 79 ? bodyLight : y < 6 ? bodyDark : body);
                // Textura leve de metal escovado.
                if (!border && (x * 7 + y * 13) % 11 == 0)
                    c = new Color32((byte)(c.r - 8), (byte)(c.g - 8), (byte)(c.b - 8), 255);
                Set(px, x, y, c);
            }
        }

        // Moldura da tela (com cantos chanfrados).
        Rect(px, ScreenX - 4, ScreenY - 4, ScreenX + ScreenW + 3, ScreenY + ScreenH + 3, bezel);
        Rect(px, ScreenX - 4, ScreenY + ScreenH + 3, ScreenX + ScreenW + 3, ScreenY + ScreenH + 3, outline);
        Rect(px, ScreenX - 1, ScreenY - 1, ScreenX + ScreenW, ScreenY + ScreenH, outline);

        // Botao giratorio, LEDs e botao de ligar.
        Circle(px, 21, 18, 7, outline);
        Circle(px, 21, 18, 6, bodyDark);
        Circle(px, 21, 18, 4, bodyLight);
        Rect(px, 21, 18, 21, 23, outline);
        Rect(px, 54, 11, 68, 25, outline);
        Rect(px, 55, 12, 67, 24, new Color32(92, 30, 28, 255));
        Rect(px, 57, 14, 65, 22, new Color32(120, 40, 36, 255));
        Rect(px, 60, 16, 62, 21, new Color32(40, 14, 12, 255));

        // Grade de alto-falante.
        for (int y = 9; y < 28; y += 3)
            Rect(px, 34, y, 47, y, bodyDark);

        // Etiqueta.
        Rect(px, 30, 30, 50, 31, bodyDark);

        tex.SetPixels32(px);
        tex.Apply(false, true);

        Image img = NewImage("Body", root);
        img.sprite = Sprite.Create(tex, new Rect(0, 0, DeviceW, DeviceH), new Vector2(0.5f, 0.5f), 100f);
        Stretch(img.rectTransform);

        led = NewImage("Led", root);
        PlacePx(led.rectTransform, 39, 18, 3, 3);
        ledGlow = NewImage("LedGlow", root);
        PlacePx(ledGlow.rectTransform, 37, 16, 7, 7);
        ledGlow.transform.SetSiblingIndex(led.transform.GetSiblingIndex());
    }

    private void BuildScreen()
    {
        screenTex = new Texture2D(TexW, TexH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "RadarScreen" };
        pixels = new Color32[TexW * TexH];

        screen = new GameObject("Screen", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
        screen.gameObject.layer = gameObject.layer;
        screen.transform.SetParent(root, false);
        screen.texture = screenTex;
        screen.raycastTarget = false;
        PlacePx(screen.rectTransform, ScreenX, ScreenY, ScreenW, ScreenH);
    }

    // ---------- Atualizacao ----------

    private void Update()
    {
        if (player == null)
            return;

        UpdateHand();
        UpdateLed();
        UpdateBeep();

        if (Time.unscaledTime >= nextRefresh)
            RefreshTargets();

        if (float.IsNaN(floorY) || Mathf.Abs(player.position.y - floorY) > 2f)
            BuildMap(player.position.y);

        if (Time.time >= nextReveal)
        {
            nextReveal = Time.time + 0.2f;
            Reveal(player.position);
        }

        if (Time.unscaledTime >= nextFrame)
        {
            nextFrame = Time.unscaledTime + 1f / screenFps;
            DrawScreen();
        }
    }

    private void UpdateHand()
    {
        bool up = Keyboard.current != null && Keyboard.current[raiseKey].isPressed && Time.timeScale > 0f;
        raise = Mathf.MoveTowards(raise, up ? 1f : 0f, Time.unscaledDeltaTime * 5f);
        float e = raise * raise * (3f - 2f * raise);

        // Abaixado: so a metade de cima aparece no canto. Levantado: sobe e chega mais perto do centro.
        float lowered = -DeviceH * Scale * 0.2f;
        Vector2 pos = new Vector2(Mathf.Lerp(-70f, -150f, e), Mathf.Lerp(lowered, 60f, e));

        // Balanca com os passos e atrasa um pouco quando a camera gira (aparelho na mao).
        if (movement != null && Time.timeScale > 0f)
        {
            float phase = movement.StepPhase - Mathf.Floor(movement.StepPhase);
            float move = movement.MoveBlend * (1f - e * 0.6f);
            pos.y += -10f * (0.5f + 0.5f * Mathf.Cos(phase * Mathf.PI * 2f)) * move + movement.LandingOffset * 250f;
            pos.x += Mathf.Sin(movement.StepPhase * Mathf.PI) * 8f * move;

            Vector2 target = Vector2.ClampMagnitude(-movement.LookDelta * 1.6f, 30f);
            float remaining = Mathf.Min(Time.deltaTime, 0.1f);
            while (remaining > 0f)
            {
                float step = Mathf.Min(remaining, 1f / 120f);
                lagVelocity += ((target - lag) * 90f - lagVelocity * 14f) * step;
                lag += lagVelocity * step;
                remaining -= step;
            }
            pos += lag;
        }

        root.anchoredPosition = pos;
        root.localRotation = Quaternion.Euler(0f, 0f, -lag.x * 0.08f + Mathf.Lerp(4f, 0f, e));
        root.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.05f, e);
    }

    private void UpdateLed()
    {
        float distance = enemy != null && enemy.gameObject.activeInHierarchy ? Vector3.Distance(enemy.position, player.position) : 999f;
        float rate = Mathf.Lerp(8f, 0.8f, Mathf.InverseLerp(4f, 30f, distance));
        bool on = distance < 30f && Mathf.Repeat(Time.time * rate, 1f) < 0.5f;

        led.color = on ? new Color(1f, 0.2f, 0.15f, 1f) : new Color(0.25f, 0.06f, 0.05f, 1f);
        ledGlow.color = on ? new Color(1f, 0.15f, 0.1f, 0.35f) : new Color(0f, 0f, 0f, 0f);
    }

    private void RefreshTargets()
    {
        nextRefresh = Time.unscaledTime + 1f;
        pieces.Clear();

        foreach (TemporalObjectPickup piece in FindObjectsByType<TemporalObjectPickup>(FindObjectsSortMode.None))
        {
            if (piece.isActiveAndEnabled)
                pieces.Add(piece.transform);
        }

        if (overheat == null)
            overheat = FindFirstObjectByType<CameraOverheat>();

        TemporalEraserController e = FindFirstObjectByType<TemporalEraserController>();
        enemy = e != null ? e.transform : null;
    }

    // ---------- Mapa do andar (NavMesh) ----------

    private void BuildMap(float y)
    {
        floorY = y;
        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector3 v in tri.vertices)
        {
            if (Mathf.Abs(v.y - y) > 1.6f)
                continue;
            min = Vector2.Min(min, new Vector2(v.x, v.z));
            max = Vector2.Max(max, new Vector2(v.x, v.z));
        }

        if (min.x > max.x)
        {
            walk = null;
            return;
        }

        gridMin = min - Vector2.one * 2f;
        gridW = Mathf.CeilToInt((max.x - min.x + 4f) / CellSize);
        gridH = Mathf.CeilToInt((max.y - min.y + 4f) / CellSize);
        walk = new bool[gridW * gridH];
        edge = new bool[gridW * gridH];
        explored = new bool[gridW * gridH];

        for (int t = 0; t < tri.indices.Length; t += 3)
        {
            Vector3 a = tri.vertices[tri.indices[t]], b = tri.vertices[tri.indices[t + 1]], c = tri.vertices[tri.indices[t + 2]];
            if (Mathf.Abs((a.y + b.y + c.y) / 3f - y) > 1.6f)
                continue;
            FillTriangle(ToCell(a), ToCell(b), ToCell(c));
        }

        // Borda = chao encostado em algo nao andavel (vira a linha clara das paredes).
        for (int gy = 0; gy < gridH; gy++)
            for (int gx = 0; gx < gridW; gx++)
            {
                int i = gy * gridW + gx;
                if (!walk[i])
                    continue;
                edge[i] = !Walk(gx + 1, gy) || !Walk(gx - 1, gy) || !Walk(gx, gy + 1) || !Walk(gx, gy - 1);
            }
    }

    private Vector2 ToCell(Vector3 world)
    {
        return new Vector2((world.x - gridMin.x) / CellSize, (world.z - gridMin.y) / CellSize);
    }

    private bool Walk(int gx, int gy)
    {
        return gx >= 0 && gy >= 0 && gx < gridW && gy < gridH && walk[gy * gridW + gx];
    }

    private void FillTriangle(Vector2 a, Vector2 b, Vector2 c)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
        int maxX = Mathf.Min(gridW - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
        int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
        int maxY = Mathf.Min(gridH - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
        float area = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        if (Mathf.Abs(area) < 1e-6f)
            return;

        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float w0 = ((b.x - p.x) * (c.y - p.y) - (b.y - p.y) * (c.x - p.x)) / area;
                float w1 = ((c.x - p.x) * (a.y - p.y) - (c.y - p.y) * (a.x - p.x)) / area;
                if (w0 >= -0.05f && w1 >= -0.05f && 1f - w0 - w1 >= -0.05f)
                    walk[y * gridW + x] = true;
            }
    }

    private void Reveal(Vector3 position)
    {
        if (walk == null)
            return;

        Vector2 c = ToCell(position);
        int r = Mathf.CeilToInt(revealRadius / CellSize);
        for (int y = -r; y <= r; y++)
            for (int x = -r; x <= r; x++)
            {
                if (x * x + y * y > r * r)
                    continue;
                int gx = (int)c.x + x, gy = (int)c.y + y;
                if (gx >= 0 && gy >= 0 && gx < gridW && gy < gridH)
                    explored[gy * gridW + gx] = true;
            }
    }

    // ---------- Tela ----------

    private void DrawScreen()
    {
        float ppm = TexW / (rangeMeters * 2f);
        Vector3 fwd = player.forward;
        float yaw = Mathf.Atan2(fwd.x, fwd.z);
        float cos = Mathf.Cos(yaw), sin = Mathf.Sin(yaw);
        Vector2 center = new Vector2(TexW * 0.5f, TexH * 0.42f);

        for (int y = 0; y < TexH; y++)
        {
            bool dimRow = (y & 1) == 1;
            for (int x = 0; x < TexW; x++)
            {
                // Pixel da tela -> metros relativos ao jogador (frente = cima), girado pela direcao.
                float sx = (x - center.x) / ppm, sy = (y - center.y) / ppm;
                float wx = player.position.x + sx * cos + sy * sin;
                float wz = player.position.z - sx * sin + sy * cos;

                Color32 c = Black;
                if (walk != null)
                {
                    int gx = (int)((wx - gridMin.x) / CellSize), gy = (int)((wz - gridMin.y) / CellSize);
                    if (gx >= 0 && gy >= 0 && gx < gridW && gy < gridH)
                    {
                        int i = gy * gridW + gx;
                        if (walk[i])
                        {
                            bool known = explored[i];
                            c = edge[i] ? (known ? Wall : WallUnknown) : (known ? Floor : (((x + y) & 1) == 0 ? FloorUnknown : Black));
                        }
                    }
                }

                if (dimRow)
                    c = new Color32((byte)(c.r * 0.72f), (byte)(c.g * 0.72f), (byte)(c.b * 0.72f), 255);

                pixels[y * TexW + x] = c;
            }
        }

        DrawSweepAndBlips(center, ppm, cos, sin);
        DrawPlayer(center);

        screenTex.SetPixels32(pixels);
        screenTex.Apply(false);
    }

    private void DrawSweepAndBlips(Vector2 center, float ppm, float cos, float sin)
    {
        float angle = Mathf.Repeat(Time.time / sweepSeconds, 1f) * Mathf.PI * 2f;
        float radius = Mathf.Max(TexW, TexH);

        // Rastro da varredura (algumas linhas em leque).
        for (int k = 0; k < 6; k++)
        {
            float a = angle - k * 0.05f;
            Color32 col = k == 0 ? Sweep : new Color32((byte)(Sweep.r / (k + 1)), (byte)(Sweep.g / (k + 1)), (byte)(Sweep.b / (k + 1)), 255);
            for (float r = 2f; r < radius; r += 1f)
                Blend(Mathf.RoundToInt(center.x + Mathf.Sin(a) * r), Mathf.RoundToInt(center.y + Mathf.Cos(a) * r), col, 0.35f);
        }

        // Quem a varredura cruzou neste quadro ganha um pulso novo.
        foreach (Transform target in Targets())
        {
            Vector3 d = target.position - player.position;
            float lx = d.x * cos - d.z * sin;
            float ly = d.x * sin + d.z * cos;
            float bearing = Mathf.Atan2(lx, ly);
            if (bearing < 0f)
                bearing += Mathf.PI * 2f;

            if (Crossed(lastSweepAngle, angle, bearing))
                pulses[target] = Time.time;

            if (!pulses.TryGetValue(target, out float start))
                continue;

            float age = Time.time - start;
            if (age > 1.6f)
                continue;

            bool isEnemy = target == enemy;
            bool isLost = overheat != null && target == overheat.LostCamera;
            bool isGoal = DeliveryPoint.Instance != null && target == DeliveryPoint.Instance.transform;
            Vector2 p = center + new Vector2(lx, ly) * ppm;

            // Fora do alcance: fica na borda da tela.
            p.x = Mathf.Clamp(p.x, 2f, TexW - 3f);
            p.y = Mathf.Clamp(p.y, 2f, TexH - 3f);

            Color32 color = isEnemy ? EnemyColor : isLost ? LostCameraColor : PieceColor;
            float fade = 1f - age / 1.6f;
            int dot = isEnemy || isLost || isGoal ? 2 : 1;
            for (int y = -dot; y <= dot; y++)
                for (int x = -dot; x <= dot; x++)
                    Blend((int)p.x + x, (int)p.y + y, color, fade);

            // Anel do pulso se abrindo.
            float ring = (isEnemy || isGoal ? 9f : 6f) * Mathf.Clamp01(age / 0.7f);
            if (age < 0.7f)
            {
                for (int s = 0; s < 32; s++)
                {
                    float a = s / 32f * Mathf.PI * 2f;
                    Blend(Mathf.RoundToInt(p.x + Mathf.Cos(a) * ring), Mathf.RoundToInt(p.y + Mathf.Sin(a) * ring), color, (1f - age / 0.7f) * 0.9f);
                }
            }
        }

        lastSweepAngle = angle;
    }

    private IEnumerable<Transform> Targets()
    {
        foreach (Transform piece in pieces)
            if (piece != null && piece.gameObject.activeInHierarchy && Mathf.Abs(piece.position.y - player.position.y) < 3f)
                yield return piece;

        if (enemy != null && enemy.gameObject.activeInHierarchy && Mathf.Abs(enemy.position.y - player.position.y) < 3f)
            yield return enemy;

        // Camera perdida no superaquecimento: aparece em qualquer andar.
        Transform lost = overheat != null ? overheat.LostCamera : null;
        if (lost != null)
            yield return lost;

        // Missao final: o ponto de montagem aparece quando da para entregar.
        if (DeliveryPoint.Instance != null && DeliveryPoint.Instance.Active)
            yield return DeliveryPoint.Instance.transform;
    }

    private static bool Crossed(float from, float to, float bearing)
    {
        if (to >= from)
            return bearing > from && bearing <= to;
        return bearing > from || bearing <= to; // deu a volta
    }

    private void DrawPlayer(Vector2 center)
    {
        int cx = (int)center.x, cy = (int)center.y;
        for (int y = 0; y < 5; y++)
            for (int x = -y / 2; x <= y / 2; x++)
                Blend(cx + x, cy + 2 - y, PieceColor, 1f);
    }

    private void Blend(int x, int y, Color32 color, float alpha)
    {
        if (x < 0 || y < 0 || x >= TexW || y >= TexH)
            return;
        int i = y * TexW + x;
        pixels[i] = Color32.Lerp(pixels[i], color, Mathf.Clamp01(alpha));
    }

    // ---------- Utilitarios ----------

    private static void Set(Color32[] px, int x, int y, Color32 c)
    {
        if (x >= 0 && y >= 0 && x < DeviceW && y < DeviceH)
            px[y * DeviceW + x] = c;
    }

    private static void Rect(Color32[] px, int x0, int y0, int x1, int y1, Color32 c)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                Set(px, x, y, c);
    }

    private static void Circle(Color32[] px, int cx, int cy, int r, Color32 c)
    {
        for (int y = -r; y <= r; y++)
            for (int x = -r; x <= r; x++)
                if (x * x + y * y <= r * r)
                    Set(px, cx + x, cy + y, c);
    }

    private Image NewImage(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = gameObject.layer;
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    // Posiciona em pixels do desenho (origem embaixo a esquerda).
    private static void PlacePx(RectTransform rect, int x, int y, int w, int h)
    {
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(x * Scale, y * Scale);
        rect.sizeDelta = new Vector2(w * Scale, h * Scale);
    }

    private void OnDestroy()
    {
        if (screenTex != null)
            Destroy(screenTex);
    }
}
