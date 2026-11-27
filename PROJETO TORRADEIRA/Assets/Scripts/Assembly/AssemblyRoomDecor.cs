using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Decoracao da sala de montagem (laboratorio do prototipo de 1975), montada em codigo:
// - piso de ladrilho de laboratorio e lambri de chapa metalica nas paredes (texturas do projeto);
// - bancada de engenharia com terminal CRT, gabinete, teclado, luminaria e itens de trabalho;
// - painel de controle da garra com botoes piscando, mostradores e tela de osciloscopio animada;
// - quadro de monitoramento na parede do fundo, estante com caixas, arquivo, cofre, aquecedor,
//   relogio, luminarias de parede/teto, cabos no chao e faixa de seguranca na area da garra.
// Cada peca so entra se o espaco estiver livre; as grandes tem colisor e obstaculo de NavMesh.
public class AssemblyRoomDecor : MonoBehaviour
{
    private const float S = 1.25f; // escala do mundo (a fase e maior que o real)

    private AssemblyRoomKit kit;
    [SerializeField, HideInInspector] private Vector3 c;            // centro do piso
    [SerializeField, HideInInspector] private float xMin, xMax, zMin, zMax, floorY, ceilY;
    [SerializeField, HideInInspector] private Material matFloor, matPanel, matDark, matMetal, matStripe, matScreenA, matScreenB, matBtnOn, matBtnOff, matBtnRed, matLight, matCable;
    private Texture2D scopeA, scopeB;
    private Color32[] scopePx;
    [SerializeField, HideInInspector] private List<Renderer> buttons = new List<Renderer>();
    [SerializeField, HideInInspector] private List<Light> fixtures = new List<Light>();
    private float nextScope, nextBlink;
    private int solidMask;

    public Vector3 Center => c;

    // Mede a sala a partir do centro (antes de qualquer peca ser colocada).
    public void Measure(Vector3 approxCenter, Vector2 approxSize)
    {
        solidMask = ~LayerMask.GetMask("Ignore Raycast", "UI", "Player", "Enemy", "Temporal", "TransparentFX");
        Vector3 o = approxCenter + Vector3.up * 1.2f;
        floorY = Physics.Raycast(o, Vector3.down, out RaycastHit f, 3f, solidMask, QueryTriggerInteraction.Ignore) ? f.point.y : approxCenter.y;
        ceilY = Physics.Raycast(o, Vector3.up, out RaycastHit u, 12f, solidMask, QueryTriggerInteraction.Ignore) ? u.point.y : floorY + 5f;
        xMin = Ray(o, Vector3.left, approxCenter.x - approxSize.x * 0.5f, -1);
        xMax = Ray(o, Vector3.right, approxCenter.x + approxSize.x * 0.5f, 1);
        zMin = Ray(o, Vector3.back, approxCenter.z - approxSize.y * 0.5f, -1);
        zMax = Ray(o, Vector3.forward, approxCenter.z + approxSize.y * 0.5f, 1);
        c = new Vector3((xMin + xMax) * 0.5f, floorY, (zMin + zMax) * 0.5f);
        Debug.Log("Sala: x " + xMin.ToString("F2") + ".." + xMax.ToString("F2") + " z " + zMin.ToString("F2") + ".." + zMax.ToString("F2") + " piso " + floorY.ToString("F2") + " teto " + ceilY.ToString("F2"));
    }

    private float Ray(Vector3 o, Vector3 dir, float fallback, int sign)
    {
        // Duas alturas: a parede de verdade bate nas duas.
        bool a = Physics.Raycast(o, dir, out RaycastHit h1, 8f, solidMask, QueryTriggerInteraction.Ignore);
        bool b = Physics.Raycast(o + Vector3.up * 0.8f, dir, out RaycastHit h2, 8f, solidMask, QueryTriggerInteraction.Ignore);
        if (!a && !b) return fallback;
        float d = Mathf.Max(a ? h1.distance : 0f, b ? h2.distance : 0f);
        return (dir.x != 0f ? o.x : o.z) + sign * d;
    }

    public void Build()
    {
        kit = Resources.Load<AssemblyRoomKit>("AssemblyRoomKit");
        MakeMaterials();

        float w = xMax - xMin, d = zMax - zMin;
        Transform root = transform;

        // Piso de ladrilho e lambri metalico.
        Plate("Piso", root, new Vector3(c.x, floorY + 0.004f, c.z), new Vector3(w - 0.04f, 0.006f, d - 0.04f), matFloor, new Vector2(w / 1.2f, d / 1.2f));
        float doorX = c.x;
        Wainscot(root, new Vector3(xMin, 0f, zMin), new Vector3(xMin, 0f, zMax), Vector3.right, -1f);
        Wainscot(root, new Vector3(xMax, 0f, zMin), new Vector3(xMax, 0f, zMax), Vector3.left, -1f);
        Wainscot(root, new Vector3(xMin, 0f, zMax), new Vector3(xMax, 0f, zMax), Vector3.back, -1f);
        Wainscot(root, new Vector3(xMin, 0f, zMin), new Vector3(xMax, 0f, zMin), Vector3.forward, doorX);

        // Faixa de seguranca (vermelho e branco) em volta da area de trabalho da garra.
        float sx = w * 0.42f, sz0 = zMin + d * 0.38f, sz1 = zMax - 0.5f;
        Stripe(root, new Vector3(c.x, floorY + 0.008f, sz0), new Vector3(sx * 2f, 0.004f, 0.12f), sx * 2f);
        Stripe(root, new Vector3(c.x, floorY + 0.008f, sz1), new Vector3(sx * 2f, 0.004f, 0.12f), sx * 2f);
        Stripe(root, new Vector3(c.x - sx, floorY + 0.008f, (sz0 + sz1) * 0.5f), new Vector3(0.12f, 0.004f, sz1 - sz0), sz1 - sz0);
        Stripe(root, new Vector3(c.x + sx, floorY + 0.008f, (sz0 + sz1) * 0.5f), new Vector3(0.12f, 0.004f, sz1 - sz0), sz1 - sz0);

        BuildWorkbench(root);
        BuildControlPanel(root);
        BuildBackWall(root);
        BuildStorage(root);
        BuildLights(root);
        BuildCables(root);
    }

    // ---------- Areas ----------

    private void BuildWorkbench(Transform root)
    {
        // Bancada de engenharia encostada na parede esquerda, terco da frente.
        float z = zMin + (zMax - zMin) * 0.42f;
        GameObject bench = Place(kit != null ? kit.bench : null, root, 0.92f * S, true, new Vector3(xMin, floorY, z), Vector3.right, solid: true);
        if (bench == null) return;
        Bounds b = Bounds(bench);
        float top = b.max.y;
        Vector3 inward = Vector3.right;
        Vector3 along = Vector3.forward;
        Vector3 mid = new Vector3(b.center.x, top, b.center.z);

        GameObject crt = Place(kit?.crtScreen, root, 0.36f * S, true, mid - along * 0.25f - inward * 0.05f, inward, onTop: true);
        if (crt != null) AnimatedScreen(crt, matScreenA, inward);
        Place(kit?.pcTower, root, 0.42f * S, true, mid + along * 0.42f - inward * 0.05f, inward, onTop: true);
        Place(kit?.keyboard, root, 0.44f, false, mid - along * 0.25f + inward * 0.22f, inward, onTop: true);
        Place(kit?.deskLamp, root, 0.4f * S, true, mid - along * 0.7f, inward + along * 0.4f, onTop: true);
        Place(kit?.speaker, root, 0.2f * S, true, mid + along * 0.05f - inward * 0.12f, inward, onTop: true);
        if (kit != null && kit.smallItems != null)
            for (int i = 0; i < kit.smallItems.Length && i < 4; i++)
                Place(kit.smallItems[i], root, 0.2f, false, mid + along * (0.65f + i * 0.14f) + inward * ((i % 2) * 0.18f - 0.05f), inward + along * (i - 1.5f) * 0.3f, onTop: true);
        Place(kit?.chair, root, 0.95f * S, true, new Vector3(b.max.x + 0.55f, floorY, b.center.z - 0.2f), -inward, solid: true, yaw: 15f);
    }

    private void BuildControlPanel(Transform root)
    {
        // Painel de controle da garra na parede direita, entre a bandeja e o fundo.
        float z = zMin + (zMax - zMin) * 0.6f;
        Transform panel = new GameObject("PainelGarra").transform;
        panel.SetParent(root, false);
        panel.position = new Vector3(xMax - 0.36f, floorY, z);
        panel.rotation = Quaternion.LookRotation(Vector3.left);
        Collider[] block = Physics.OverlapBox(panel.position + Vector3.up * 0.75f, new Vector3(0.58f, 0.6f, 0.26f), panel.rotation, solidMask, QueryTriggerInteraction.Ignore);
        if (block.Length > 0)
        {
            Debug.Log("Sala: painel recusado, bate em " + block[0].name);
            SceneBake.Kill(panel.gameObject);
            return;
        }
        Box("Gabinete", panel, new Vector3(0f, 0.55f, 0f), new Vector3(1.2f, 1.1f, 0.6f), matPanel, true, new Vector2(1.2f, 1.1f));
        Transform desk = Box("Mesa", panel, new Vector3(0f, 1.16f, 0.12f), new Vector3(1.22f, 0.08f, 0.42f), matDark, false);
        desk.localRotation = Quaternion.Euler(-18f, 0f, 0f);
        // Botoes (grade) e alavancas.
        for (int i = 0; i < 12; i++)
        {
            float bx = -0.45f + (i % 6) * 0.18f, bz = 0.02f + (i / 6) * 0.13f;
            Quaternion tilt = Quaternion.Euler(-18f, 0f, 0f);
            Transform btn = Box("Botao", panel, new Vector3(0f, 1.16f, 0.12f) + tilt * new Vector3(bx, 0.05f, bz - 0.1f), new Vector3(0.06f, 0.03f, 0.06f), i % 5 == 0 ? matBtnRed : matBtnOff, false);
            btn.localRotation = tilt;
            buttons.Add(btn.GetComponent<Renderer>());
        }
        for (int i = 0; i < 2; i++)
        {
            Transform gauge = Cyl("Mostrador", panel, new Vector3(-0.35f + i * 0.7f, 1.5f, -0.28f), new Vector3(0.22f, 0.02f, 0.22f), matStripe);
            gauge.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Transform needle = Box("Ponteiro", panel, new Vector3(-0.35f + i * 0.7f, 1.5f, -0.3f), new Vector3(0.012f, 0.09f, 0.005f), matBtnRed, false);
            needle.localRotation = Quaternion.Euler(0f, 0f, 30f - i * 50f);
        }
        // Tela de osciloscopio.
        Box("Monitor", panel, new Vector3(0f, 1.62f, 0.05f), new Vector3(0.5f, 0.4f, 0.3f), matDark, false);
        Box("Tela", panel, new Vector3(0f, 1.62f, -0.105f), new Vector3(0.42f, 0.32f, 0.01f), matScreenB, false);
        Obstacle(panel.gameObject, new Vector3(0f, 0.8f, 0f), new Vector3(1.2f, 1.6f, 0.6f));
    }

    private void BuildBackWall(Transform root)
    {
        // Quadro de monitoramento: tres telas CRT embutidas numa chapa, com luzes de status.
        Transform board = new GameObject("QuadroMonitoramento").transform;
        board.SetParent(root, false);
        board.position = new Vector3(c.x, floorY + 2.1f, zMax - 0.06f);
        board.rotation = Quaternion.LookRotation(Vector3.back);
        Box("Chapa", board, Vector3.zero, new Vector3(2.6f, 1.1f, 0.08f), matPanel, false, new Vector2(2.6f, 1.1f));
        for (int i = 0; i < 3; i++)
        {
            Box("Moldura", board, new Vector3(-0.85f + i * 0.85f, 0.08f, -0.05f), new Vector3(0.7f, 0.55f, 0.04f), matDark, false);
            Box("Tela", board, new Vector3(-0.85f + i * 0.85f, 0.08f, -0.075f), new Vector3(0.6f, 0.45f, 0.01f), i == 1 ? matScreenA : matScreenB, false);
        }
        for (int i = 0; i < 8; i++)
        {
            Transform led = Box("Led", board, new Vector3(-1.05f + i * 0.3f, -0.42f, -0.06f), new Vector3(0.05f, 0.05f, 0.02f), matBtnOff, false);
            buttons.Add(led.GetComponent<Renderer>());
        }

        // Relogio acima da porta (parede da frente, olhando para dentro).
        if (kit != null && kit.clock != null)
        {
            GameObject clock = Instantiate(kit.clock, root);
            Strip(clock);
            if (kit.clockMaterial != null)
                foreach (Renderer r in clock.GetComponentsInChildren<Renderer>()) r.sharedMaterial = kit.clockMaterial;
            Fit(clock, 0.42f, false);
            Bounds cb = Bounds(clock);
            Vector3 thinAxis = cb.size.x <= cb.size.z ? Vector3.right : Vector3.forward;
            clock.transform.rotation = Quaternion.FromToRotation(-thinAxis, Vector3.forward) * clock.transform.rotation;
            cb = Bounds(clock);
            clock.transform.position += new Vector3(c.x + 1.3f, floorY + 2.6f, zMin + 0.04f + cb.extents.z) - cb.center;
        }
    }

    private void BuildStorage(Transform root)
    {
        // Estante no canto do fundo esquerdo, com caixas; arquivo e lixeira perto da porta;
        // cofre e aquecedor na parede direita do fundo; bobina de cabo no canto.
        Place(kit?.shelf, root, 1.9f * S, true, new Vector3(xMin, floorY, zMax - 0.8f), Vector3.right, solid: true);
        Place(kit?.drawer, root, 1.2f, true, new Vector3(xMax - 2.4f, floorY, zMax), Vector3.back, solid: true);
        Place(kit?.bin, root, 0.4f * S, true, new Vector3(xMin + 0.35f, floorY, zMin + 1.25f), Vector3.right, solid: false);
        Place(kit?.safe, root, 1.1f * S, true, new Vector3(xMax, floorY, zMax - 0.55f), Vector3.left, solid: true);
        Place(kit?.radiator, root, 0.65f * S, true, new Vector3(xMin, floorY, zMax - 2.7f), Vector3.right, solid: true);
        if (kit != null && kit.crates != null && kit.crates.Length > 0)
        {
            GameObject a = Place(kit.crates[0], root, 0.6f * S, true, new Vector3(xMin + 1.5f, floorY, zMax), Vector3.back, solid: true, yaw: 8f);
            if (a != null)
            {
                Bounds ab = Bounds(a);
                Place(kit.crates[Mathf.Min(3, kit.crates.Length - 1)], root, 0.35f * S, true, new Vector3(ab.center.x, ab.max.y, ab.center.z), Vector3.back, onTop: true, yaw: -12f);
            }
            Place(kit.crates[Mathf.Min(2, kit.crates.Length - 1)], root, 0.45f * S, true, new Vector3(xMin + 2.3f, floorY, zMax), Vector3.back, solid: true, yaw: -5f);
        }
        Place(kit?.coil, root, 0.5f * S, true, new Vector3(xMax - 0.6f, floorY, zMin + 1.7f), Vector3.left, solid: true);
    }

    private void BuildLights(Transform root)
    {
        // Duas luminarias de teto com luz fria (tremulando de leve) e arandelas nas laterais.
        for (int i = 0; i < 2; i++)
        {
            float z = Mathf.Lerp(zMin, zMax, i == 0 ? 0.3f : 0.72f);
            Vector3 p = new Vector3(c.x, ceilY - 0.06f, z);
            Box("Luminaria", root, p, new Vector3(1.3f, 0.08f, 0.32f), matDark, false, null, true);
            Box("Tubo", root, p - Vector3.up * 0.05f, new Vector3(1.2f, 0.03f, 0.12f), matLight, false, null, true);
            Light l = new GameObject("LuzTeto", typeof(Light)).GetComponent<Light>();
            l.transform.SetParent(root, false);
            l.transform.position = p - Vector3.up * 0.4f;
            l.type = LightType.Point;
            l.range = 6.5f;
            l.intensity = 1.4f;
            l.color = new Color(0.86f, 0.92f, 1f);
            l.shadows = LightShadows.None;
            fixtures.Add(l);
        }
        if (kit != null && kit.wallLight != null)
        {
            Place(kit.wallLight, root, 0.35f, false, new Vector3(xMin, floorY + 2.3f, zMax - 2.4f), Vector3.right, wall: true);
            Place(kit.wallLight, root, 0.35f, false, new Vector3(xMax, floorY + 2.3f, zMin + 2.2f), Vector3.left, wall: true);
        }
    }

    private void BuildCables(Transform root)
    {
        // Cabos grossos no chao: do painel e da bancada ate o pedestal (passam por cima do piso).
        Vector3 ped = new Vector3(c.x, floorY + 0.03f, zMin + (zMax - zMin) * 0.72f);
        Cable(root, new Vector3(xMax - 0.65f, floorY + 0.03f, zMin + (zMax - zMin) * 0.6f), ped + Vector3.right * 0.7f);
        Cable(root, new Vector3(xMin + 0.9f, floorY + 0.03f, zMin + (zMax - zMin) * 0.42f), ped - Vector3.right * 0.7f);
        // Calha de cabos subindo do pedestal ao teto (alimenta a garra).
        Box("Calha", root, new Vector3(xMax - 0.08f, (floorY + ceilY) * 0.5f, zMax - 1.6f), new Vector3(0.12f, ceilY - floorY, 0.18f), matDark, false, null, true);
    }

    private void Cable(Transform root, Vector3 a, Vector3 b)
    {
        Vector3 mid = (a + b) * 0.5f;
        Transform t = Cyl("Cabo", root, mid, new Vector3(0.05f, Vector3.Distance(a, b) * 0.5f, 0.05f), matCable, true);
        t.rotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
    }

    // ---------- Animacao (telas e botoes) ----------

    // Texturas das telas (osciloscopio) sao recriadas no jogo e ligadas aos materiais salvos.
    private void Start()
    {
        if (matScreenA == null || matScreenB == null) return;
        scopeA = new Texture2D(64, 48, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        scopeB = new Texture2D(64, 48, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        scopePx = new Color32[64 * 48];
        matScreenA.SetTexture("_BaseMap", scopeA);
        matScreenA.SetTexture("_EmissionMap", scopeA);
        matScreenB.SetTexture("_BaseMap", scopeB);
        matScreenB.SetTexture("_EmissionMap", scopeB);
    }

    private void Update()
    {
        if (Time.time >= nextScope && scopeA != null)
        {
            nextScope = Time.time + 0.08f;
            DrawScope(scopeA, Time.time, 0);
            DrawScope(scopeB, Time.time * 1.7f, 1);
        }
        if (Time.time >= nextBlink)
        {
            nextBlink = Time.time + 0.35f;
            foreach (Renderer r in buttons)
                if (r != null && r.sharedMaterial != matBtnRed && Random.value < 0.45f)
                    r.sharedMaterial = r.sharedMaterial == matBtnOn ? matBtnOff : matBtnOn;
        }
        foreach (Light l in fixtures)
            l.intensity = 1.4f * (FlashSettings.Enabled && Random.value < 0.01f ? 0.4f : 1f);
    }

    private void DrawScope(Texture2D tex, float t, int mode)
    {
        int w = tex.width, h = tex.height;
        Color32 bg = new Color32(8, 14, 12, 255), grid = new Color32(22, 40, 34, 255), line = new Color32(190, 235, 220, 255);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                scopePx[y * w + x] = (x % 8 == 0 || y % 8 == 0) ? grid : bg;
        for (int x = 0; x < w; x++)
        {
            float u = x / (float)w;
            float v = mode == 0
                ? 0.5f + 0.32f * Mathf.Sin(u * 18f + t * 6f) * Mathf.Sin(u * 3f + t)
                : 0.5f + 0.3f * (Mathf.Repeat(u * 4f + t, 1f) < 0.5f ? 1f : -1f) * (0.6f + 0.4f * Mathf.Sin(t * 2f));
            int y = Mathf.Clamp(Mathf.RoundToInt(v * (h - 1)), 0, h - 1);
            scopePx[y * w + x] = line;
            if (y + 1 < h) scopePx[(y + 1) * w + x] = line;
        }
        // Linha de varredura.
        int scan = Mathf.FloorToInt(Mathf.Repeat(t * 0.7f, 1f) * h);
        for (int x = 0; x < w; x++) scopePx[scan * w + x] = new Color32(60, 90, 80, 255);
        tex.SetPixels32(scopePx);
        tex.Apply(false);
    }

    private void AnimatedScreen(GameObject crt, Material screen, Vector3 facing)
    {
        // Quad emissivo na frente da tela do CRT.
        Bounds b = Bounds(crt);
        float sz = Mathf.Min(b.size.y, Mathf.Max(b.size.x, b.size.z)) * 0.6f;
        Transform q = Box("TelaCRT", crt.transform.parent, Vector3.zero, new Vector3(sz * 1.2f, sz * 0.9f, 0.005f), screen, false, null, true);
        q.rotation = Quaternion.LookRotation(-facing);
        float depth = Vector3.Dot(b.extents, new Vector3(Mathf.Abs(facing.x), Mathf.Abs(facing.y), Mathf.Abs(facing.z)));
        q.position = b.center + facing * (depth + 0.006f) + Vector3.up * b.size.y * 0.06f;
    }

    // ---------- Instancia, escala e checagem ----------

    private GameObject Place(GameObject prefab, Transform parent, float size, bool byHeight, Vector3 anchor, Vector3 front,
                             bool solid = false, bool onTop = false, bool wall = false, float yaw = 0f)
    {
        if (prefab == null) return null;
        GameObject go = Instantiate(prefab, parent);
        Strip(go);
        Fit(go, size, byHeight);
        Vector3 f = new Vector3(front.x, 0f, front.z).normalized;
        go.transform.rotation = Quaternion.LookRotation(f, Vector3.up) * Quaternion.Euler(0f, yaw, 0f) * go.transform.rotation;
        Bounds b = Bounds(go);

        Vector3 target;
        if (onTop)
            target = new Vector3(anchor.x, anchor.y + 0.002f + b.extents.y, anchor.z);
        else if (wall)
        {
            float depth = Vector3.Dot(b.extents, new Vector3(Mathf.Abs(f.x), 0f, Mathf.Abs(f.z)));
            target = new Vector3(anchor.x, anchor.y, anchor.z) + f * (depth + 0.01f);
        }
        else
        {
            // Encostado na parede (anchor na parede) ou num ponto do piso.
            float depth = Vector3.Dot(b.extents, new Vector3(Mathf.Abs(f.x), 0f, Mathf.Abs(f.z)));
            bool atWall = Mathf.Abs(anchor.x - xMin) < 0.05f || Mathf.Abs(anchor.x - xMax) < 0.05f || Mathf.Abs(anchor.z - zMin) < 0.05f || Mathf.Abs(anchor.z - zMax) < 0.05f;
            Vector3 p = atWall ? anchor + f * (depth + 0.06f) : anchor;
            target = new Vector3(p.x, floorY + 0.003f + b.extents.y, p.z);
        }
        go.transform.position += target - b.center;
        b = Bounds(go);

        // Espaco livre (nao entra em parede, porta, outro movel ou peca do jogo).
        Vector3 half = b.extents - new Vector3(0.03f, 0.03f, 0.03f);
        if (onTop) half.y = Mathf.Max(0.01f, half.y - 0.01f);
        if (!onTop) { half.y = Mathf.Max(0.01f, half.y - 0.05f); }
        Collider[] hits = Physics.OverlapBox(b.center + Vector3.up * (onTop ? 0.02f : 0.07f), Vector3.Max(half, Vector3.one * 0.01f), Quaternion.identity, solidMask, QueryTriggerInteraction.Ignore);
        foreach (Collider h in hits)
        {
            if (h.transform.IsChildOf(go.transform)) continue;
            if (h.name == "Piso") continue;
            Debug.Log("Sala: " + prefab.name + " recusado, bate em " + h.name);
            SceneBake.Kill(go);
            return null;
        }
        if (solid)
        {
            BoxCollider bc = go.AddComponent<BoxCollider>();
            bc.center = go.transform.InverseTransformPoint(b.center);
            Vector3 ls = go.transform.InverseTransformVector(b.size);
            bc.size = new Vector3(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
            Obstacle(go, bc.center, bc.size);
        }
        Physics.SyncTransforms();
        return go;
    }

    private static void Strip(GameObject go)
    {
        foreach (Collider col in go.GetComponentsInChildren<Collider>(true)) SceneBake.Kill(col);
        foreach (LODGroup g in go.GetComponentsInChildren<LODGroup>(true)) SceneBake.Kill(g);
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.tag = "Untagged";
    }

    private static void Fit(GameObject go, float size, bool byHeight)
    {
        Bounds b = Bounds(go);
        float cur = byHeight ? b.size.y : Mathf.Max(b.size.x, b.size.z);
        if (cur > 0.0001f) go.transform.localScale *= size / cur;
    }

    private static Bounds Bounds(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.1f);
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    private static void Obstacle(GameObject go, Vector3 center, Vector3 size)
    {
        NavMeshObstacle o = go.AddComponent<NavMeshObstacle>();
        o.shape = NavMeshObstacleShape.Box;
        o.center = center;
        o.size = size;
        o.carving = true;
        o.carveOnlyStationary = true;
    }

    private void Wainscot(Transform root, Vector3 a, Vector3 b, Vector3 inward, float doorX)
    {
        // Chapas de 1,2 m de altura ao longo da parede; deixa o vao da porta livre.
        Vector3 dir = (b - a);
        float len = dir.magnitude;
        dir /= len;
        for (float s = 0f; s < len - 0.05f; s += 1.2f)
        {
            float seg = Mathf.Min(1.2f, len - s);
            Vector3 mid = a + dir * (s + seg * 0.5f);
            if (doorX > -0.5f && Mathf.Abs(mid.x - doorX) < 1.05f) continue;
            Vector3 p = new Vector3(mid.x, floorY + 0.6f, mid.z) + inward * 0.025f;
            Vector3 size = Mathf.Abs(dir.x) > 0.5f ? new Vector3(seg - 0.02f, 1.2f, 0.04f) : new Vector3(0.04f, 1.2f, seg - 0.02f);
            Box("Chapa", root, p, size, matPanel, false, new Vector2(seg, 1.2f), true);
            Box("Friso", root, p + Vector3.up * 0.62f + inward * 0.01f, Mathf.Abs(dir.x) > 0.5f ? new Vector3(seg, 0.05f, 0.06f) : new Vector3(0.06f, 0.05f, seg), matDark, false, null, true);
        }
    }

    private void Stripe(Transform root, Vector3 pos, Vector3 size, float length)
    {
        Transform t = Box("FaixaSeguranca", root, pos, size, new Material(matStripe), false, null, true);
        Renderer r = t.GetComponent<Renderer>();
        r.sharedMaterial.mainTextureScale = new Vector2(Mathf.Max(1f, length / 0.6f), 1f);
        if (size.z > size.x) r.sharedMaterial.mainTextureScale = new Vector2(1f, Mathf.Max(1f, length / 0.6f));
    }

    private void Plate(string name, Transform root, Vector3 pos, Vector3 size, Material m, Vector2 tiling)
    {
        Material inst = new Material(m);
        inst.mainTextureScale = tiling;
        if (inst.HasProperty("_BumpMap")) inst.SetTextureScale("_BumpMap", tiling);
        Box(name, root, pos, size, inst, false, null, true);
    }

    private Transform Box(string name, Transform parent, Vector3 pos, Vector3 size, Material m, bool collider, Vector2? tiling = null, bool world = false)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        if (!collider) SceneBake.Kill(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        if (world) go.transform.position = pos; else go.transform.localPosition = pos;
        go.transform.localScale = size;
        Material mat = m;
        if (tiling.HasValue && m.mainTexture != null)
        {
            mat = new Material(m);
            mat.mainTextureScale = tiling.Value;
        }
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go.transform;
    }

    private Transform Cyl(string name, Transform parent, Vector3 pos, Vector3 size, Material m, bool world = false)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        SceneBake.Kill(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        if (world) go.transform.position = pos; else go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = m;
        return go.transform;
    }

    // ---------- Materiais e texturas ----------

    private Material Lit(Color color, float smooth, float metal, Texture2D tex = null, Texture2D normal = null, Color? emission = null, Texture emitTex = null)
    {
        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metal);
        if (tex != null) m.SetTexture("_BaseMap", tex);
        if (normal != null)
        {
            m.SetTexture("_BumpMap", normal);
            m.EnableKeyword("_NORMALMAP");
            m.SetFloat("_BumpScale", 0.8f);
        }
        if (emission.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission.Value);
            if (emitTex != null) m.SetTexture("_EmissionMap", emitTex);
        }
        return m;
    }

    private void MakeMaterials()
    {
        matFloor = Lit(new Color(0.72f, 0.74f, 0.74f), 0.45f, 0f, kit?.floorTiles, kit?.floorTilesNormal);
        matPanel = Lit(new Color(0.62f, 0.64f, 0.66f), 0.5f, 0.55f, kit?.metalPanel, kit?.metalPanelNormal);
        matDark = Lit(new Color(0.11f, 0.11f, 0.12f), 0.35f, 0.3f);
        matMetal = Lit(new Color(0.55f, 0.56f, 0.57f), 0.55f, 0.6f);
        matCable = Lit(new Color(0.06f, 0.06f, 0.07f), 0.3f, 0f);
        matLight = Lit(Color.white, 0.5f, 0f, null, null, new Color(2.2f, 2.3f, 2.4f));
        matBtnOn = Lit(new Color(0.8f, 0.9f, 1f), 0.6f, 0f, null, null, new Color(1.1f, 1.4f, 1.8f));
        matBtnOff = Lit(new Color(0.12f, 0.13f, 0.15f), 0.6f, 0f);
        matBtnRed = Lit(new Color(0.6f, 0.06f, 0.05f), 0.6f, 0f, null, null, new Color(1.3f, 0.1f, 0.08f));

        // Faixa vermelha e branca em diagonal (pixelada).
        var stripe = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                stripe.SetPixel(x, y, ((x + y) / 4) % 2 == 0 ? new Color(0.62f, 0.08f, 0.07f) : new Color(0.86f, 0.85f, 0.82f));
        stripe.Apply();
        matStripe = Lit(Color.white, 0.25f, 0f, stripe);

        scopeA = new Texture2D(64, 48, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        scopeB = new Texture2D(64, 48, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        scopePx = new Color32[64 * 48];
        DrawScope(scopeA, 0f, 0);
        DrawScope(scopeB, 0f, 1);
        matScreenA = Lit(Color.black, 0.85f, 0f, scopeA, null, Color.white * 1.6f, scopeA);
        matScreenB = Lit(Color.black, 0.85f, 0f, scopeB, null, Color.white * 1.4f, scopeB);
    }

    private void OnDestroy()
    {
        if (scopeA != null) Destroy(scopeA);
        if (scopeB != null) Destroy(scopeB);
    }
}
