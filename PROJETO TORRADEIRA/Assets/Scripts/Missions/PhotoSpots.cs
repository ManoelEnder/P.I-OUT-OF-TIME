using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Lugares para fotografar (missao "Fotografar lugares marcados"):
// - todo lugar ganha um leve brilho esverdeado (some depois da foto) e aparece no radar em verde;
// - lugares extras em pontos marcantes da fase (elevador, geladeira, micro-ondas, sofa da recepcao);
// - easter egg (nao e missao): atras das janelas do "Ponto aq", no fundo da sala, uma imagem que so
//   aparece no visor da camera com zoom. Pixelada e levemente borrada.
public static class PhotoSpots
{
    // Posicao das janelas onde fica a figura (objeto "Ponto aq" da cena, se existir).
    private static readonly Vector3 WindowPoint = new Vector3(-23.935f, 14.0591f, -155.9081f);

    private static Texture2D markerTex;
    public const string RootName = "LugaresDeFoto";
    private static Transform root;

    // Refaz tudo (editor): apaga o grupo e os brilhos antigos e monta de novo.
    public static void Rebuild()
    {
        GameObject old = GameObject.Find(RootName);
        if (old != null) SceneBake.Kill(old);
        foreach (PhotoLocation loc in Object.FindObjectsByType<PhotoLocation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Transform glow = loc.transform.Find("BrilhoFoto");
            if (glow != null) SceneBake.Kill(glow.gameObject);
        }
        markerTex = null;
        Setup();
    }

    public static void Setup()
    {
        // Ja montado na cena (editor): nada a fazer.
        if (GameObject.Find(RootName) != null)
            return;
        root = new GameObject(RootName).transform;

        foreach (PhotoLocation loc in Object.FindObjectsByType<PhotoLocation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            AttachMarker(loc, loc.TargetPoint + Vector3.up * 0.5f, 1.0f, null);

        AddAtObject("elevador", "Elevador", "Elevator_4");
        AddAtObject("geladeira", "Geladeira da copa", "geladeira_no_estilo_de_ps1");
        AddAtObject("microondas", "Micro-ondas", "psx_old_microwave");
        AddAtObject("recepcao", "Sofá da recepção", "3Seat2");
        AddEasterEgg();
    }

    // ---------- Lugares em objetos da cena ----------

    private static void AddAtObject(string id, string name, string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        if (target == null)
            return;
        Bounds b = Bounds(target);
        GameObject go = new GameObject("FotoLugar_" + id);
        go.transform.SetParent(root, true);
        go.transform.position = b.center;
        PhotoLocation loc = go.AddComponent<PhotoLocation>();
        AttachMarker(loc, new Vector3(b.center.x, b.max.y + 0.6f, b.center.z), 1f, target.transform);
        loc.Configure(id, name, loc.transform.GetChild(0).gameObject, 18f, false, target.transform);
    }

    // ---------- Foto escondida atras das janelas ----------

    private static void AddEasterEgg()
    {
        GameObject anchor = GameObject.Find("Ponto aq");
        Vector3 window = anchor != null ? anchor.transform.position : WindowPoint;

        // Lado da sala (onde o jogador anda) e lado do fundo: testa as 4 direcoes horizontais.
        Vector3 spawn = GameObject.Find("PlayerSpawn") != null ? GameObject.Find("PlayerSpawn").transform.position : window;
        Vector3 back = Vector3.zero;
        float bestDepth = -1f;
        foreach (Vector3 dir in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
        {
            // Lado do jogador: tem chao andavel e caminho a partir do inicio.
            Vector3 probe = new Vector3(window.x, window.y - 2f, window.z) + dir * 1.4f;
            bool playerSide = NavMesh.SamplePosition(probe, out NavMeshHit nh, 0.8f, NavMesh.AllAreas) &&
                              NavMesh.SamplePosition(spawn, out NavMeshHit sh, 3f, NavMesh.AllAreas) &&
                              Reachable(sh.position, nh.position);
            if (playerSide) continue;
            // O lado com vidro e o fundo; atras do vidro a sala nao tem colisao (usa 4 m).
            bool glass = HitsGlass(window, dir);
            float depth = Depth(window, dir);
            if (glass && depth < 0.8f) depth = 4f;
            if (!glass && depth < 0.8f) continue;
            if (glass) depth += 100f;   // prioridade para o lado da janela
            if (depth > bestDepth)
            {
                bestDepth = depth;
                back = dir;
            }
        }
        if (back == Vector3.zero)
            return;
        if (bestDepth > 100f) bestDepth -= 100f;

        // No fundo da sala, de pe no chao, de frente para as janelas.
        float dist = Mathf.Clamp(bestDepth - 0.06f, 0.8f, 8f);
        Vector3 spot = window + back * dist;
        float floor = Physics.Raycast(spot + Vector3.up * 0.5f, Vector3.down, out RaycastHit fh, 4f, ~0, QueryTriggerInteraction.Ignore) ? fh.point.y : window.y - 2.06f;

        Texture2D photo = LoadFigure();
        if (photo == null)
            return;
        float height = 1.9f;
        float width = height * photo.width / photo.height;

        GameObject fig = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fig.transform.SetParent(root, true);
        fig.name = "EasterEgg";
        SceneBake.Kill(fig.GetComponent<Collider>());
        fig.transform.position = new Vector3(spot.x, floor + 0.55f + height * 0.5f, spot.z);
        fig.transform.rotation = Quaternion.LookRotation(back);   // a frente do quad aponta para as janelas
        fig.transform.localScale = new Vector3(width, height, 1f);
        Material m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetTexture("_BaseMap", photo);
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_AlphaClip", 1f);
        m.SetFloat("_Cutoff", 0.5f);
        m.EnableKeyword("_ALPHATEST_ON");
        m.SetFloat("_Cull", 0f);
        fig.GetComponent<Renderer>().sharedMaterial = m;
        fig.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        fig.AddComponent<HiddenFigure>();
    }

    private static bool HitsGlass(Vector3 from, Vector3 dir)
    {
        foreach (RaycastHit h in Physics.RaycastAll(from - Vector3.up * 0.6f, dir, 1.2f, ~0, QueryTriggerInteraction.Ignore))
        {
            string n = h.transform.name.ToLowerInvariant();
            if (n.Contains("glass") || n.Contains("vidro") || n.Contains("window") || n.Contains("janela"))
                return true;
        }
        return false;
    }

    private static bool Reachable(Vector3 a, Vector3 b)
    {
        var path = new NavMeshPath();
        return NavMesh.CalculatePath(a, b, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
    }

    // Distancia ate a parede do fundo, ignorando vidro e o que estiver colado na janela.
    private static float Depth(Vector3 from, Vector3 dir)
    {
        RaycastHit[] hits = Physics.RaycastAll(from - Vector3.up * 0.6f, dir, 15f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
        foreach (RaycastHit h in hits)
        {
            string n = h.transform.name.ToLowerInvariant();
            if (h.distance < 0.35f || n.Contains("glass") || n.Contains("vidro") || n.Contains("window") || n.Contains("janela"))
                continue;
            return h.distance;
        }
        return 0f;
    }

    // Imagem pixelada (reduzida), fundo claro removido e leve borrao.
    private static Texture2D LoadFigure()
    {
        TextAsset bytes = Resources.Load<TextAsset>("FotoEscondida");
        if (bytes == null)
            return null;
        var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!src.LoadImage(bytes.bytes))
            return null;

        const int h = 96;
        int w = Mathf.Max(8, Mathf.RoundToInt(h * (float)src.width / src.height));
        var small = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // Media de uma area da imagem original (reduz sem serrilhar).
                Color sum = Color.clear;
                int n = 0;
                int x0 = x * src.width / w, x1 = Mathf.Max(x0 + 1, (x + 1) * src.width / w);
                int y0 = y * src.height / h, y1 = Mathf.Max(y0 + 1, (y + 1) * src.height / h);
                for (int sy = y0; sy < y1; sy += 3)
                    for (int sx = x0; sx < x1; sx += 3)
                    {
                        sum += src.GetPixel(sx, sy);
                        n++;
                    }
                small[y * w + x] = n > 0 ? sum / n : Color.clear;
            }
        SceneBake.Kill(src);

        // Imagem inteira (opaca).
        var alpha = new float[w * h];
        for (int i = 0; i < small.Length; i++)
        {
            Color c = small[i];
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            alpha[i] = 1f;
        }

        // Leve borrao (media 3x3 so da cor) e tons em degraus (aspecto de fita/PSX).
        var outPx = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color sum = Color.clear;
                int n = 0;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int xx = Mathf.Clamp(x + dx, 0, w - 1), yy = Mathf.Clamp(y + dy, 0, h - 1);
                        if (alpha[yy * w + xx] < 0.5f) continue;
                        sum += small[yy * w + xx] * (dx == 0 && dy == 0 ? 2f : 1f);
                        n += dx == 0 && dy == 0 ? 2 : 1;
                    }
                Color c = n > 0 ? sum / n : Color.black;
                c.r = Mathf.Round(c.r * 12f) / 12f;
                c.g = Mathf.Round(c.g * 12f) / 12f;
                c.b = Mathf.Round(c.b * 12f) / 12f;
                c.a = alpha[y * w + x];
                outPx[y * w + x] = c;
            }

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "FiguraEscondida" };
        tex.SetPixels(outPx);
        tex.Apply(false);
        return tex;
    }

    // ---------- Marcador ----------

    private static void AttachMarker(PhotoLocation loc, Vector3 worldPos, float size, Transform owner)
    {
        if (markerTex == null)
            markerTex = BuildGlowTexture();
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "BrilhoFoto";
        SceneBake.Kill(quad.GetComponent<Collider>());
        quad.transform.SetParent(loc.transform, false);
        quad.transform.position = worldPos;
        quad.transform.localScale = Vector3.one * 1.6f * size;
        Material m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetTexture("_BaseMap", markerTex);
        m.SetColor("_BaseColor", new Color(0.55f, 1f, 0.65f, 0.85f));
        // Transparente aditivo (brilho suave).
        m.SetFloat("_Surface", 1f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_Cull", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
        Renderer r = quad.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;

        Light light = new GameObject("Luz", typeof(Light)).GetComponent<Light>();
        light.transform.SetParent(quad.transform, false);
        light.type = LightType.Point;
        light.color = new Color(0.6f, 1f, 0.7f);
        light.range = 3.5f;
        light.intensity = 1.4f;
        light.shadows = LightShadows.None;
        quad.AddComponent<PhotoMarkerPulse>();

        // Lugar da cena sem marcador: Configure so troca o marcador e mantem o resto.
        if (owner == null && loc.gameObject.name.StartsWith("FotoLugar_") == false)
            loc.Configure(loc.Id, loc.DisplayName, quad, 25f, false, loc.transform);
    }

    // Brilho radial suave (centro claro, borda transparente).
    private static Texture2D BuildGlowTexture()
    {
        const int n = 32;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x - (n - 1) * 0.5f) / (n * 0.5f), dy = (y - (n - 1) * 0.5f) / (n * 0.5f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        tex.Apply(false);
        return tex;
    }

    private static Bounds Bounds(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}
