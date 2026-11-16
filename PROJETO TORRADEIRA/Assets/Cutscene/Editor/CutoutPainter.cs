using System.Collections.Generic;
using UnityEngine;

// Rasterizador mínimo para as ilustrações recortadas: formas com contorno irregular,
// textura de papel, granulação, contorno escuro e sombra dura.
public class CutoutPainter
{
    public readonly int W, H;
    public readonly Color[] Px;
    private readonly int seed;

    public CutoutPainter(int w, int h, int seed)
    {
        W = w; H = h; this.seed = seed;
        Px = new Color[w * h];
    }

    public static float Hash(int x, int y, int s)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + s * 982451653;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0xFFFFFF) / 16777215f;
        }
    }

    public static float Noise(float x, float y, int s)
    {
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
        float fx = x - xi, fy = y - yi;
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        float a = Hash(xi, yi, s), b = Hash(xi + 1, yi, s), c = Hash(xi, yi + 1, s), d = Hash(xi + 1, yi + 1, s);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }

    private int shapeCount;

    // Contorno irregular: subdivide as arestas e desloca na perpendicular.
    private List<Vector2> Roughen(IList<Vector2> pts, float jitter)
    {
        var outPts = new List<Vector2>();
        int s = seed * 31 + shapeCount++;
        for (int i = 0; i < pts.Count; i++)
        {
            Vector2 a = pts[i], b = pts[(i + 1) % pts.Count];
            float len = Vector2.Distance(a, b);
            int n = Mathf.Max(1, Mathf.CeilToInt(len / 7f));
            Vector2 nrm = new Vector2(-(b - a).y, (b - a).x).normalized;
            for (int k = 0; k < n; k++)
            {
                Vector2 p = Vector2.Lerp(a, b, k / (float)n);
                float j = jitter <= 0f ? 0f : (Noise(p.x * 0.09f, p.y * 0.09f, s) - 0.5f) * 2f * jitter;
                outPts.Add(p + nrm * j);
            }
        }
        return outPts;
    }

    public void Poly(IList<Vector2> pts, Color c, float jitter = 1.6f)
    {
        List<Vector2> p = Roughen(pts, jitter);
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (Vector2 v in p) { minY = Mathf.Min(minY, v.y); maxY = Mathf.Max(maxY, v.y); }
        int y0 = Mathf.Max(0, Mathf.FloorToInt(minY)), y1 = Mathf.Min(H - 1, Mathf.CeilToInt(maxY));
        var xs = new List<float>();
        for (int y = y0; y <= y1; y++)
        {
            float yc = y + 0.5f;
            xs.Clear();
            for (int i = 0; i < p.Count; i++)
            {
                Vector2 a = p[i], b = p[(i + 1) % p.Count];
                if ((a.y <= yc && b.y > yc) || (b.y <= yc && a.y > yc))
                    xs.Add(a.x + (yc - a.y) / (b.y - a.y) * (b.x - a.x));
            }
            xs.Sort();
            for (int i = 0; i + 1 < xs.Count; i += 2)
            {
                int xa = Mathf.Max(0, Mathf.CeilToInt(xs[i] - 0.5f));
                int xb = Mathf.Min(W - 1, Mathf.FloorToInt(xs[i + 1] - 0.5f));
                for (int x = xa; x <= xb; x++)
                    Blend(x, y, c);
            }
        }
    }

    public void Rect(float x, float y, float w, float h, Color c, float jitter = 1.6f) =>
        Poly(new[] { new Vector2(x, y), new Vector2(x + w, y), new Vector2(x + w, y + h), new Vector2(x, y + h) }, c, jitter);

    public void Ellipse(float cx, float cy, float rx, float ry, Color c, float jitter = 1.4f, float rotDeg = 0f)
    {
        int n = Mathf.Clamp(Mathf.CeilToInt((rx + ry) * 0.6f), 16, 160);
        var pts = new Vector2[n];
        float r = rotDeg * Mathf.Deg2Rad;
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            Vector2 v = new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
            pts[i] = new Vector2(cx + v.x * Mathf.Cos(r) - v.y * Mathf.Sin(r), cy + v.x * Mathf.Sin(r) + v.y * Mathf.Cos(r));
        }
        Poly(pts, c, jitter);
    }

    public void Circle(float cx, float cy, float r, Color c, float jitter = 1.4f) => Ellipse(cx, cy, r, r, c, jitter);

    public void Line(Vector2 a, Vector2 b, float width, Color c, float jitter = 0.8f)
    {
        Vector2 n = new Vector2(-(b - a).y, (b - a).x).normalized * width * 0.5f;
        Poly(new[] { a - n, b - n, b + n, a + n }, c, jitter);
    }

    public void Curve(Vector2 a, Vector2 ctrl, Vector2 b, float width, Color c)
    {
        Vector2 prev = a;
        for (int i = 1; i <= 16; i++)
        {
            float t = i / 16f;
            Vector2 p = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * ctrl + t * t * b;
            Line(prev, p, width, c, 0.4f);
            Circle(p.x, p.y, width * 0.5f, c, 0f);
            prev = p;
        }
    }

    private void Blend(int x, int y, Color c)
    {
        int i = y * W + x;
        Color d = Px[i];
        float a = c.a + d.a * (1f - c.a);
        if (a <= 0f) return;
        Color o = (c * c.a + d * d.a * (1f - c.a)) / a;
        o.a = a;
        Px[i] = o;
    }

    // Luz: clareia/tinge pixels já pintados (poça de luz âmbar, brilho de CRT).
    public void Light(float cx, float cy, float radius, Color tint, float strength)
    {
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int i = y * W + x;
                if (Px[i].a <= 0f) continue;
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) / radius;
                float k = Mathf.Clamp01(1f - d);
                k = k * k * strength;
                Color p = Px[i];
                Px[i] = new Color(Mathf.Lerp(p.r, p.r * tint.r * 1.6f, k), Mathf.Lerp(p.g, p.g * tint.g * 1.6f, k), Mathf.Lerp(p.b, p.b * tint.b * 1.6f, k), p.a);
            }
    }

    // Escurece de um lado (sombra definida, em degraus).
    public void Shade(Vector2 dir, float amount, int steps = 3)
    {
        dir.Normalize();
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int i = y * W + x;
                if (Px[i].a <= 0f) continue;
                float t = Vector2.Dot(new Vector2(x / (float)W - 0.5f, y / (float)H - 0.5f), dir) + 0.5f;
                t = Mathf.Floor(Mathf.Clamp01(t) * steps) / steps;
                float k = 1f - t * amount;
                Color p = Px[i];
                Px[i] = new Color(p.r * k, p.g * k, p.b * k, p.a);
            }
    }

    public void Paper(float amount = 0.12f, float grain = 0.06f)
    {
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int i = y * W + x;
                if (Px[i].a <= 0f) continue;
                float fiber = Noise(x * 0.05f, y * 0.012f, seed + 7) * 0.6f + Noise(x * 0.2f, y * 0.2f, seed + 9) * 0.4f;
                float g = Hash(x, y, seed + 3) - 0.5f;
                float k = 1f - amount * (fiber - 0.5f) * 2f + grain * g;
                Color p = Px[i];
                Px[i] = new Color(p.r * k, p.g * k, p.b * k, p.a);
            }
    }

    // Contorno escuro irregular + sombra dura deslocada.
    public void Finish(Color outline, int outlineW, Vector2Int shadow, float shadowAlpha)
    {
        var src = (Color[])Px.Clone();
        var res = new Color[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int i = y * W + x;
                Color c = src[i];
                if (c.a >= 0.99f) { res[i] = c; continue; }

                float edge = 0f;
                int r = outlineW + (Hash(x / 3, y / 3, seed) > 0.7f ? 1 : 0);
                for (int dy = -r; dy <= r && edge < 1f; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        int sx = x + dx, sy = y + dy;
                        if (sx < 0 || sy < 0 || sx >= W || sy >= H) continue;
                        if (dx * dx + dy * dy > r * r) continue;
                        if (src[sy * W + sx].a > 0.5f) { edge = 1f; break; }
                    }

                Color under = Color.clear;
                int ux = x - shadow.x, uy = y - shadow.y;
                if (shadowAlpha > 0f && ux >= 0 && uy >= 0 && ux < W && uy < H && src[uy * W + ux].a > 0.5f)
                    under = new Color(0.04f, 0.03f, 0.03f, shadowAlpha);
                if (edge > 0f)
                    under = outline;

                float a = c.a + under.a * (1f - c.a);
                res[i] = a <= 0f ? Color.clear : new Color(
                    (c.r * c.a + under.r * under.a * (1f - c.a)) / a,
                    (c.g * c.a + under.g * under.a * (1f - c.a)) / a,
                    (c.b * c.a + under.b * under.a * (1f - c.a)) / a, a);
            }
        System.Array.Copy(res, Px, Px.Length);
    }

    public void Fill(Color c)
    {
        for (int i = 0; i < Px.Length; i++) Px[i] = c;
    }

    public Texture2D ToTexture()
    {
        var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
        t.SetPixels(Px);
        t.Apply();
        return t;
    }
}
