using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Ilustrações próprias da cutscene, desenhadas em camadas. Cada PNG pode ser substituído
// por arte final com o mesmo nome, tamanho em unidades e pivô.
public static class IntroCutsceneArt
{
    public struct Spec { public float ppu; public Vector2 pivot; }
    public static readonly Dictionary<string, Spec> Specs = new Dictionary<string, Spec>();

    static Color C(string hex, float a = 1f) { ColorUtility.TryParseHtmlString(hex, out Color c); c.a = a; return c; }
    static Vector2 V(float x, float y) => new Vector2(x, y);

    static readonly Color Ink = C("#17120f");
    static readonly Color Amber = C("#f0b356");
    static readonly Color Wall = C("#7d6c52");
    static readonly Color Metal = C("#7a7e82"), MetalDark = C("#474b4f"), MetalLight = C("#a9adb0");
    static readonly Color Wood = C("#6a4a2d"), WoodDark = C("#3b2817");
    static readonly Color Beige = C("#cdb892"), BeigeDark = C("#9c8a68"), PaperC = C("#e6dcc3");
    static readonly Color Skin = C("#d6a07a"), SkinShadow = C("#a8765a"), Hair = C("#2b211b");
    static readonly Color Shirt = C("#b39d74");
    static readonly Color Pcb = C("#3f6b4a"), Copper = C("#b8864b");
    static readonly Color Cold = C("#1b262b"), ColdMid = C("#2e3d44"), Coat = C("#c9d1cd");
    static readonly Color Phos = C("#7cffa0"), PhosDim = C("#2e7a4a");
    static readonly Color Alert = C("#a8483c");

    static string dir;

    static void Make(string name, int w, int h, float ppu, Vector2 pivot, Action<CutoutPainter> draw,
                     bool cutout = true, int outline = 3, int shadow = 7, float paper = 0.12f)
    {
        Specs[name] = new Spec { ppu = ppu, pivot = pivot };
        string path = Path.Combine(dir, name + ".png");
        if (File.Exists(path)) return; // arte existente (ou substituída pelo artista) é preservada
        var p = new CutoutPainter(w, h, name.GetHashCode());
        draw(p);
        if (paper > 0f) p.Paper(paper);
        if (cutout) p.Finish(Ink, outline, new Vector2Int(shadow, -shadow), 0.45f);
        var tex = p.ToTexture();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }

    static Vector2 Mid => new Vector2(0.5f, 0.5f);

    public static void Generate(string outDir)
    {
        dir = outDir;
        Directory.CreateDirectory(dir);

        // ---------- utilitários ----------
        Make("white", 8, 8, 8, Mid, p => p.Fill(Color.white), false, paper: 0f);
        Make("white_left", 8, 8, 8, V(0, 0.5f), p => p.Fill(Color.white), false, paper: 0f);
        for (int f = 0; f < 4; f++)
        {
            int ff = f;
            // grão grosso: cada ponto vira 4x4 pixels na tela de 640x360
            Make("grain_" + f, 160, 90, 160f / 19.2f, Mid, p =>
            {
                for (int y = 0; y < p.H; y++)
                    for (int x = 0; x < p.W; x++)
                    {
                        float n = CutoutPainter.Hash(x, y, 91 + ff);
                        p.Px[y * p.W + x] = n > 0.5f ? new Color(1, 1, 1, (n - 0.5f) * 0.2f) : new Color(0, 0, 0, (0.5f - n) * 0.26f);
                    }
            }, false, paper: 0f);
        }
        Make("vignette", 640, 360, 33.3333f, Mid, p =>
        {
            for (int y = 0; y < p.H; y++)
                for (int x = 0; x < p.W; x++)
                {
                    float dx = (x - 320f) / 320f, dy = (y - 180f) / 180f;
                    float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy);
                    p.Px[y * p.W + x] = new Color(0, 0, 0, Mathf.Clamp01((d - 0.55f) * 1.3f) * 0.85f);
                }
        }, false, paper: 0f);
        Make("paper_overlay", 640, 360, 33.3333f, Mid, p =>
        {
            for (int y = 0; y < p.H; y++)
                for (int x = 0; x < p.W; x++)
                {
                    float n = CutoutPainter.Noise(x * 0.08f, y * 0.02f, 5) * 0.7f + CutoutPainter.Noise(x * 0.4f, y * 0.4f, 6) * 0.3f;
                    p.Px[y * p.W + x] = new Color(0.95f, 0.88f, 0.72f, Mathf.Abs(n - 0.5f) * 0.18f);
                }
        }, false, paper: 0f);

        // ---------- LABORATÓRIO 1975 ----------
        Make("lab_wall", 2300, 1300, 100, Mid, p =>
        {
            p.Fill(Wall);
            for (int x = 0; x < 2300; x += 115) p.Rect(x, 330, 5, 970, C("#5f5240"), 1f);
            p.Rect(0, 300, 2300, 34, C("#5b4b37"), 1f);
            p.Rect(0, 0, 2300, 300, C("#4c4033"), 1f);
            p.Rect(1460, 700, 300, 380, C("#c9bc9c"), 2f);          // esquema pregado na parede
            for (int i = 0; i < 6; i++) p.Rect(1490, 740 + i * 52, 230, 4, C("#7c7360"), 0.5f);
            p.Circle(1610, 960, 50, C("#00000000"), 0f);
            p.Light(1100, 560, 950, Amber, 1.1f);
        }, false, paper: 0.16f);

        Make("lab_shelf", 520, 240, 100, Mid, p =>
        {
            p.Rect(0, 0, 520, 26, Wood);
            p.Rect(20, 26, 90, 150, C("#8b7b58"));
            p.Rect(115, 26, 70, 120, C("#6f6a5f"));
            p.Rect(190, 26, 40, 170, C("#5a3f2a"));
            p.Rect(232, 26, 40, 160, C("#6b5130"));
            p.Rect(274, 26, 36, 175, C("#4d4535"));
            p.Rect(330, 26, 170, 90, C("#9a917e"));
            p.Rect(350, 116, 120, 70, C("#878070"));
            p.Shade(V(1, -0.3f), 0.35f);
        });

        Make("lab_pegboard", 420, 280, 100, Mid, p =>
        {
            p.Rect(0, 0, 420, 280, C("#9a8662"));
            for (int y = 20; y < 280; y += 30) for (int x = 20; x < 420; x += 30) p.Circle(x, y, 3, C("#5e523d"), 0f);
            p.Line(V(70, 230), V(70, 90), 14, MetalDark);
            p.Line(V(150, 240), V(140, 110), 10, C("#8f2f22"));
            p.Circle(260, 160, 55, C("#00000000"), 0f);
            p.Ellipse(260, 170, 50, 50, MetalDark, 1f);
            p.Ellipse(260, 170, 34, 34, C("#9a8662"), 1f);
            p.Rect(330, 60, 50, 170, Wood);
            p.Shade(V(1, -0.5f), 0.3f);
        });

        Make("lab_clock", 150, 150, 100, Mid, p =>
        {
            p.Circle(75, 75, 70, MetalDark);
            p.Circle(75, 75, 60, C("#e1d6bb"));
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f;
                p.Line(V(75 + Mathf.Cos(a) * 48, 75 + Mathf.Sin(a) * 48), V(75 + Mathf.Cos(a) * 56, 75 + Mathf.Sin(a) * 56), 4, Ink, 0f);
            }
            p.Circle(75, 75, 6, Ink, 0f);
        });
        Make("clock_hand", 16, 64, 100, V(0.5f, 0.1f), p => p.Poly(new[] { V(4, 0), V(12, 0), V(9, 64), V(7, 64) }, Ink, 0.3f), false, paper: 0f);

        Make("lamp", 260, 300, 100, V(0.5f, 0f), p =>
        {
            p.Ellipse(90, 18, 80, 18, MetalDark);
            p.Line(V(90, 20), V(110, 170), 14, Metal);
            p.Line(V(110, 170), V(190, 230), 12, Metal);
            p.Poly(new[] { V(150, 270), V(250, 250), V(230, 160), V(170, 190) }, C("#3f4a3f"));
            p.Circle(110, 170, 12, MetalDark);
            p.Shade(V(1, 0.4f), 0.3f);
        });

        Make("lamp_glow", 900, 800, 100, V(0.5f, 1f), p =>
        {
            for (int y = 0; y < p.H; y++)
                for (int x = 0; x < p.W; x++)
                {
                    float t = 1f - y / (float)p.H;                     // 0 no topo
                    float half = 60f + t * 420f;
                    float dx = Mathf.Abs(x - 450f) / half;
                    float a = Mathf.Clamp01(1f - dx) * (1f - t * 0.55f) * 0.33f;
                    a *= 0.85f + 0.15f * CutoutPainter.Noise(x * 0.02f, y * 0.02f, 4);
                    p.Px[y * p.W + x] = new Color(1f, 0.78f, 0.42f, a);
                }
        }, false, paper: 0f);

        Make("bench", 1700, 420, 100, Mid, p =>
        {
            p.Rect(0, 330, 1700, 90, C("#86643f"));
            p.Rect(0, 300, 1700, 34, WoodDark);
            p.Rect(30, 0, 1640, 300, Wood);
            for (int i = 0; i < 4; i++)
            {
                p.Rect(80 + i * 400, 150, 330, 120, C("#5a3e24"));
                p.Rect(210 + i * 400, 200, 70, 14, MetalLight, 0.5f);
            }
            p.Light(800, 420, 900, Amber, 0.7f);
            p.Shade(V(0, -1), 0.4f);
        });

        Make("steve_body", 330, 460, 100, V(0.5f, 0f), p =>
        {
            p.Poly(new[] { V(70, 0), V(270, 0), V(305, 380), V(250, 430), V(90, 430), V(30, 370) }, Shirt);
            p.Poly(new[] { V(130, 430), V(210, 430), V(190, 395), V(150, 395) }, C("#d8c7a0"));
            p.Line(V(165, 380), V(170, 20), 3, C("#8a7650"), 0.5f);
            p.Shade(V(-1, 0.2f), 0.45f);
        });
        Make("steve_head", 170, 200, 100, V(0.5f, 0f), p =>
        {
            p.Rect(62, 0, 46, 50, SkinShadow);
            p.Ellipse(85, 112, 60, 72, Skin);
            p.Ellipse(80, 130, 66, 64, Hair, 2.2f);
            p.Ellipse(124, 104, 12, 18, SkinShadow);
            p.Line(V(126, 120), V(168, 126), 4, Ink, 0.2f);
            p.Shade(V(-1, 0), 0.35f);
        });
        Make("steve_arm", 330, 90, 100, V(0f, 0.5f), p =>
        {
            p.Poly(new[] { V(0, 15), V(220, 28), V(220, 68), V(0, 78) }, Shirt);
            p.Rect(205, 24, 26, 48, C("#d8c7a0"));
            p.Ellipse(268, 46, 46, 28, Skin);
            for (int i = 0; i < 3; i++) p.Line(V(280, 34 + i * 12), V(318, 30 + i * 13), 9, Skin, 0.4f);
            p.Shade(V(0, -1), 0.3f);
        });

        Make("notes", 170, 210, 100, Mid, p =>
        {
            p.Rect(0, 0, 170, 210, PaperC);
            for (int i = 0; i < 8; i++) p.Rect(14, 30 + i * 20, 130 - (i % 3) * 25, 3, C("#5d6b8a"), 0.6f);
            p.Rect(20, 150, 60, 40, C("#00000000"));
            p.Line(V(30, 160), V(90, 190), 3, Ink);
            p.Circle(110, 170, 16, C("#00000000"), 0f);
        }, outline: 2, shadow: 4);
        Make("hanging_paper", 140, 210, 100, V(0.5f, 1f), p =>
        {
            p.Line(V(70, 210), V(70, 175), 3, Ink, 0.2f);
            p.Rect(10, 0, 120, 170, PaperC);
            p.Rect(55, 160, 30, 18, MetalDark);
            for (int i = 0; i < 6; i++) p.Rect(22, 20 + i * 22, 90, 3, C("#6c6455"), 0.5f);
        }, outline: 2, shadow: 4);
        Make("screwdriver", 170, 40, 100, Mid, p =>
        {
            p.Rect(0, 8, 70, 24, C("#a63b2c"));
            p.Rect(70, 16, 95, 8, MetalLight, 0.3f);
        }, outline: 2, shadow: 4);
        Make("pliers", 150, 70, 100, Mid, p =>
        {
            p.Line(V(0, 10), V(90, 35), 14, C("#2f4f7a"));
            p.Line(V(0, 60), V(90, 35), 14, C("#2f4f7a"));
            p.Poly(new[] { V(85, 25), V(150, 33), V(150, 37), V(85, 46) }, Metal);
        }, outline: 2, shadow: 4);
        Make("solder_iron", 230, 50, 100, Mid, p =>
        {
            p.Rect(0, 12, 120, 26, C("#3d3a35"));
            p.Rect(120, 18, 80, 14, MetalLight);
            p.Poly(new[] { V(200, 19), V(230, 25), V(200, 31) }, Copper);
            p.Curve(V(0, 25), V(-20, 10), V(-40, 0), 6, Ink);
        }, outline: 2, shadow: 4);
        Make("components", 230, 70, 100, Mid, p =>
        {
            for (int i = 0; i < 5; i++)
            {
                float x = 15 + i * 44;
                p.Line(V(x - 12, 18), V(x + 12, 18), 2, MetalLight, 0f);
                p.Rect(x - 7, 11, 14, 14, i % 2 == 0 ? C("#c9a36b") : C("#5c7fa6"), 0.4f);
            }
            p.Rect(20, 36, 50, 24, Ink, 0.5f);
            for (int i = 0; i < 6; i++) p.Rect(22 + i * 8, 30, 3, 6, MetalLight, 0f);
            p.Circle(130, 48, 14, C("#3d5c8a"));
            p.Circle(175, 46, 11, C("#8a3d3d"));
        }, outline: 2, shadow: 3);
        Make("cable_hang", 70, 440, 100, V(0.5f, 1f), p =>
        {
            p.Curve(V(35, 440), V(10, 220), V(40, 40), 8, C("#26221e"));
            p.Rect(26, 0, 22, 44, MetalDark);
        }, outline: 2, shadow: 5);
        Make("dust", 700, 600, 100, Mid, p =>
        {
            for (int i = 0; i < 70; i++)
            {
                float x = CutoutPainter.Hash(i, 1, 3) * 700, y = CutoutPainter.Hash(i, 2, 3) * 600;
                p.Circle(x, y, 1.5f + CutoutPainter.Hash(i, 3, 3) * 2.5f, new Color(1f, 0.9f, 0.7f, 0.5f), 0f);
            }
        }, false, paper: 0f);

        // protótipo de 1975: corpo robusto, lente circular, placas expostas, gravador de fita
        Make("proto_body", 310, 210, 100, Mid, p =>
        {
            p.Rect(0, 0, 310, 170, Metal);
            p.Rect(0, 150, 310, 22, MetalLight);
            p.Poly(new[] { V(90, 170), V(110, 205), V(200, 205), V(220, 170), V(200, 170), V(190, 190), V(120, 190), V(110, 170) }, MetalDark);
            p.Rect(20, 20, 150, 120, C("#202322"));
            for (int i = 0; i < 6; i++) p.Rect(195, 30 + i * 18, 90, 6, MetalDark, 0.5f);
            foreach (var v in new[] { V(10, 10), V(300, 10), V(10, 160), V(300, 160) }) p.Circle(v.x, v.y, 5, MetalDark, 0f);
            p.Shade(V(0.6f, -1), 0.35f);
        });
        Make("proto_board", 190, 130, 100, Mid, p =>
        {
            p.Rect(0, 0, 190, 130, Pcb);
            for (int i = 0; i < 7; i++) p.Rect(8, 10 + i * 17, 170, 3, Copper, 0.5f);
            p.Rect(20, 60, 60, 40, Ink, 0.6f);
            p.Rect(100, 30, 45, 30, Ink, 0.6f);
            p.Rect(110, 80, 60, 30, C("#26302a"), 0.6f);
            for (int i = 0; i < 8; i++) p.Rect(22 + i * 7, 56, 3, 4, MetalLight, 0f);
            p.Circle(160, 25, 10, C("#c9a36b"));
            p.Circle(40, 25, 8, C("#5c7fa6"));
            p.Curve(V(80, 80), V(120, 140), V(180, 100), 4, C("#a33a2a"));
        }, outline: 2, shadow: 5);
        Make("proto_lens", 140, 140, 100, Mid, p =>
        {
            p.Circle(70, 70, 66, MetalDark);
            p.Circle(70, 70, 54, Ink);
            p.Circle(70, 70, 46, MetalLight, 1f);
            p.Circle(70, 70, 36, C("#22303a"), 0.8f);
            p.Ellipse(58, 84, 14, 8, C("#9fc4d6"), 0.4f, 30f);
            p.Circle(84, 56, 5, C("#6f8fa0"), 0f);
        });
        Make("tape_deck", 1200, 780, 600, Mid, p =>
        {
            p.Rect(0, 0, 1200, 780, C("#9a9282"), 4f);
            p.Rect(0, 0, 1200, 160, C("#7c7466"), 3f);
            for (int i = 0; i < 5; i++) p.Rect(80 + i * 210, 30, 180, 100, C("#d8d0bc"), 3f);
            p.Rect(150, 230, 900, 450, C("#1c1a18"), 3f);                      // janela
            p.Rect(190, 270, 820, 370, C("#cfc3a2"), 3f);                      // cassete
            p.Rect(240, 520, 720, 90, C("#e8e0cc"), 2f);                       // etiqueta
            for (int i = 0; i < 3; i++) p.Rect(270, 540 + i * 22, 640 - i * 120, 6, C("#7d6f55"), 1f);
            p.Rect(330, 350, 540, 160, C("#2a2622"), 2f);
            p.Circle(420, 430, 112, C("#3c3127"), 2f);                          // fita enrolada
            p.Circle(780, 430, 80, C("#3c3127"), 2f);
            p.Light(600, 520, 900, Amber, 0.6f);
            p.Shade(V(0.4f, -1), 0.3f);
        }, outline: 10, shadow: 26);
        Make("tape_reel", 260, 260, 600, Mid, p =>
        {
            p.Circle(130, 130, 70, C("#efe6d0"), 1f);
            p.Circle(130, 130, 40, C("#2a2622"), 0.5f);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                p.Rect(0, 0, 0, 0, Color.clear);
                p.Line(V(130, 130), V(130 + Mathf.Cos(a) * 66, 130 + Mathf.Sin(a) * 66), 14, C("#efe6d0"), 0.5f);
            }
        }, outline: 4, shadow: 0, paper: 0.05f);
        Make("wire_bundle", 280, 130, 100, Mid, p =>
        {
            p.Curve(V(0, 60), V(140, -10), V(280, 70), 9, C("#a33a2a"));
            p.Curve(V(0, 70), V(140, 10), V(280, 80), 9, C("#2c3f66"));
            p.Curve(V(0, 50), V(140, -20), V(280, 60), 7, C("#c8b25a"));
        }, outline: 2, shadow: 4);
        Make("wire_plug", 90, 50, 100, Mid, p =>
        {
            p.Rect(0, 12, 60, 26, MetalDark);
            p.Rect(60, 18, 30, 14, Copper);
        }, outline: 2, shadow: 3);

        // terminal estilo anos 70: carcaça bege, tela curva, teclado integrado
        Make("crt1975", 900, 840, 300, Mid, p =>
        {
            p.Poly(new[] { V(40, 250), V(860, 250), V(830, 840), V(70, 840) }, Beige, 3f);
            p.Rect(140, 290, 620, 480, BeigeDark, 3f);
            p.Rect(150, 300, 600, 460, C("#101411"), 2f);
            p.Poly(new[] { V(0, 0), V(900, 0), V(860, 250), V(40, 250) }, C("#b9a47d"), 3f);
            for (int r = 0; r < 4; r++) for (int k = 0; k < 13; k++) p.Rect(80 + k * 57 + r * 10, 30 + r * 52, 46, 40, C("#e5dccb"), 1.5f);
            p.Light(450, 300, 900, Amber, 0.5f);
            p.Shade(V(0.5f, -1), 0.3f);
        }, outline: 6, shadow: 18);

        // conteúdo das telas: 600x460 px a 300 ppu nas duas épocas (o corte por semelhança alinha)
        for (int f = 0; f < 4; f++)
        {
            int ff = f;
            Make("crt_lines_" + f, 600, 460, 300, Mid, p =>
            {
                for (int y = 0; y < p.H; y++)
                {
                    float band = Mathf.Repeat(y + ff * 115, 230) < 26 ? 0.85f : 0.08f;
                    for (int x = 0; x < p.W; x++)
                    {
                        float n = CutoutPainter.Hash(x, y, ff) * 0.15f;
                        float v = (y % 4 < 2 ? 1f : 0.6f) * (band + n);
                        p.Px[y * p.W + x] = new Color(v * 0.85f, v, v * 0.9f, 1f);
                    }
                }
            }, false, paper: 0f);
        }
        Make("crt_portrait", 600, 460, 300, Mid, p =>
        {
            p.Fill(C("#2a2c2a"));
            p.Ellipse(300, 470, 210, 120, C("#1a1b1a"), 6f);                    // cabelo
            p.Rect(240, 0, 120, 120, C("#7c7f7b"), 3f);                         // pescoço
            p.Poly(new[] { V(80, 0), V(520, 0), V(470, 90), V(130, 90) }, C("#3a3c3a"), 4f);
            p.Ellipse(300, 250, 135, 175, C("#a7aaa5"), 4f);                   // rosto
            p.Ellipse(300, 395, 160, 70, C("#1f201f"), 5f);
            p.Ellipse(250, 270, 26, 14, C("#2a2b2a"), 1.5f);
            p.Ellipse(352, 270, 26, 14, C("#2a2b2a"), 1.5f);
            p.Rect(220, 292, 60, 6, C("#4d4f4c"), 1f);
            p.Rect(322, 292, 60, 6, C("#4d4f4c"), 1f);
            p.Poly(new[] { V(300, 260), V(282, 190), V(318, 190) }, C("#878a85"), 1f);
            p.Ellipse(300, 145, 46, 12, C("#4a4c49"), 1.5f);
            p.Shade(V(1, 0.2f), 0.5f, 4);
            for (int y = 0; y < p.H; y++)
                for (int x = 0; x < p.W; x++)
                {
                    int i = y * p.W + x;
                    float g = (CutoutPainter.Hash(x / 2, y / 2, 17) - 0.5f) * 0.35f;
                    float s = y % 4 < 2 ? 1f : 0.72f;
                    float v = Mathf.Clamp01(p.Px[i].r + g) * s;
                    v = Mathf.Round(v * 6f) / 6f;                                    // poucos tons: imagem de 0,01 MP
                    p.Px[i] = new Color(v, v, v, 1f);
                }
        }, false, paper: 0f);

        Make("hand_close", 460, 280, 200, V(1f, 0.5f), p =>
        {
            p.Rect(330, 60, 130, 170, Shirt, 3f);
            p.Rect(300, 70, 40, 150, C("#d8c7a0"), 2f);
            p.Ellipse(230, 140, 95, 70, Skin, 3f);
            for (int i = 0; i < 4; i++) p.Line(V(170, 110 + i * 26), V(50 + i * 6, 100 + i * 32), 26, Skin, 1.5f);
            p.Line(V(220, 80), V(150, 40), 30, Skin, 1.5f);
            p.Shade(V(0.2f, -1), 0.3f);
        }, outline: 4, shadow: 10);
        Make("hand_tool", 520, 280, 200, V(1f, 0.5f), p =>
        {
            p.Rect(80, 125, 230, 20, MetalLight, 1f);
            p.Rect(300, 105, 90, 60, C("#a63b2c"), 2f);
            p.Rect(400, 60, 120, 170, Shirt, 3f);
            p.Ellipse(330, 140, 85, 66, Skin, 3f);
            for (int i = 0; i < 3; i++) p.Line(V(320, 100 + i * 28), V(300, 172 + i * 10), 24, SkinShadow, 1f);
            p.Shade(V(0.2f, -1), 0.3f);
        }, outline: 4, shadow: 10);

        // sabotador: rosto sempre oculto, só recorte e luz fria de contorno
        Make("silhouette", 380, 720, 100, V(0.5f, 0f), p =>
        {
            Color rim = C("#4d6c78"), dark = C("#0d1013");
            Vector2[] body = { V(70, 0), V(310, 0), V(300, 470), V(250, 560), V(130, 560), V(80, 470) };
            Vector2 o = V(-9, 0);
            p.Poly(body, rim, 2f);
            p.Ellipse(190, 620, 62, 76, rim, 2f);
            p.Poly(Array.ConvertAll(body, v => v + o), dark, 2f);
            p.Ellipse(181, 620, 62, 76, dark, 2f);
            p.Poly(new[] { V(110, 640), V(270, 640), V(250, 700), V(130, 700) }, dark, 2f);
        }, outline: 2, shadow: 0, paper: 0.08f);
        Make("silhouette_arm", 330, 90, 100, V(0f, 0.5f), p =>
        {
            p.Poly(new[] { V(0, 15), V(250, 30), V(250, 64), V(0, 78) }, C("#0d1013"));
            p.Ellipse(285, 46, 46, 30, C("#0d1013"));
            p.Line(V(0, 70), V(250, 60), 4, C("#4d6c78"), 0.5f);
        }, outline: 2, shadow: 0);

        Make("floor", 2300, 1300, 100, Mid, p =>
        {
            p.Fill(C("#2b2620"));
            for (int y = 0; y < 1300; y += 95) p.Rect(0, y, 2300, 6, C("#1a1612"), 1.5f);
            for (int i = 0; i < 40; i++) p.Rect(CutoutPainter.Hash(i, 0, 2) * 2300, (i % 14) * 95, 5, 95, C("#1a1612"), 0.5f);
            p.Light(1150, 650, 1100, C("#9fb4c4"), 0.8f);
        }, false, paper: 0.18f);
        Make("dust_puff", 520, 300, 100, Mid, p =>
        {
            for (int i = 0; i < 14; i++)
                p.Circle(80 + CutoutPainter.Hash(i, 0, 8) * 360, 60 + CutoutPainter.Hash(i, 1, 8) * 180, 40 + CutoutPainter.Hash(i, 2, 8) * 50, new Color(0.75f, 0.72f, 0.66f, 0.35f), 3f);
        }, false, paper: 0.05f);
        Make("screw", 40, 40, 100, Mid, p => { p.Circle(20, 20, 14, MetalLight); p.Line(V(10, 20), V(30, 20), 4, MetalDark, 0f); }, outline: 2, shadow: 3);

        // ---------- MOTION COMIC ----------
        Make("comic_bg", 2300, 1300, 100, Mid, p =>
        {
            p.Fill(C("#2a231c"));
            p.Light(1150, 650, 1300, C("#8a7354"), 0.6f);
        }, false, paper: 0.25f);
        Make("panel", 560, 700, 100, Mid, p =>
        {
            p.Rect(0, 0, 560, 700, C("#e3d6b8"), 3f);
            p.Rect(22, 22, 516, 656, C("#3b3127"), 2f);
            for (int y = 30; y < 680; y += 14) for (int x = 30 + (y / 14 % 2) * 7; x < 530; x += 14) p.Circle(x, y, 1.6f, C("#4a3e31"), 0f);
        }, outline: 3, shadow: 9, paper: 0.18f);
        Make("cabinet", 400, 340, 100, Mid, p =>
        {
            p.Rect(20, 0, 360, 230, C("#6c6f68"));
            p.Rect(40, 140, 320, 70, C("#5a5d57"));
            p.Poly(new[] { V(30, 200), V(370, 200), V(400, 260), V(0, 260) }, C("#7d817a"));
            p.Rect(170, 160, 60, 12, MetalLight, 0.5f);
            p.Shade(V(1, -0.5f), 0.3f);
        });
        for (int i = 0; i < 3; i++)
        {
            int ii = i;
            Make("photo_" + i, 140, 110, 100, Mid, p =>
            {
                p.Rect(0, 0, 140, 110, C("#efe8d8"));
                p.Rect(10, 10, 120, 90, C("#6f7c86"));
                p.Poly(new[] { V(10, 10), V(50 + ii * 10, 60), V(80, 35), V(130, 80 - ii * 10), V(130, 10) }, C("#3c464d"));
                p.Circle(100 - ii * 20, 80, 10, C("#d8d2c4"), 0.5f);
            }, outline: 2, shadow: 4);
        }
        Make("digital_cam", 230, 150, 100, Mid, p =>
        {
            p.Rect(0, 0, 230, 130, C("#8e9296"));
            p.Rect(20, 130, 50, 18, C("#6d7175"));
            p.Circle(150, 65, 46, MetalDark);
            p.Circle(150, 65, 30, C("#22303a"));
            p.Rect(20, 30, 70, 60, C("#3a5566"));
        });
        Make("film_cam", 240, 170, 100, Mid, p =>
        {
            p.Rect(0, 0, 240, 120, C("#2a2826"));
            p.Rect(0, 90, 240, 30, C("#b8b2a6"));
            p.Rect(170, 120, 50, 30, C("#b8b2a6"));
            p.Rect(20, 120, 40, 22, C("#2a2826"));
            p.Circle(120, 55, 50, MetalDark);
            p.Circle(120, 55, 34, C("#1e2830"));
        });
        Make("timeline", 480, 120, 100, Mid, p =>
        {
            p.Rect(0, 55, 480, 10, C("#e3d6b8"), 1f);
            for (int i = 0; i < 6; i++) p.Rect(20 + i * 88, 35, 8, 50, C("#e3d6b8"), 1f);
        }, outline: 2, shadow: 4);
        Make("marker", 150, 80, 100, Mid, p =>
        {
            p.Poly(new[] { V(0, 20), V(150, 20), V(150, 80), V(0, 80) }, C("#d9c48f"));
            p.Poly(new[] { V(60, 20), V(90, 20), V(75, 0) }, C("#d9c48f"));
        }, outline: 2, shadow: 4);
        Make("room_half", 960, 1080, 100, Mid, p =>
        {
            p.Fill(C("#b9b0a0"));
            p.Rect(0, 0, 960, 330, C("#6d655a"), 2f);
            p.Rect(0, 330, 960, 40, C("#8a8070"), 2f);
            p.Rect(380, 370, 240, 150, C("#4a4743"), 3f);
            p.Circle(640, 445, 50, C("#2f2d2a"), 2f);
            p.Line(V(220, 370), V(250, 620), 12, C("#4a4743"));
            p.Poly(new[] { V(220, 640), V(320, 640), V(300, 580), V(240, 580) }, C("#4a4743"));
            p.Light(480, 500, 700, Color.white, 0.6f);
        }, false, paper: 0.2f);
        Make("temporal_piece", 180, 140, 100, Mid, p =>
        {
            p.Rect(0, 0, 180, 140, C("#30443a"));
            p.Rect(10, 10, 160, 120, Pcb);
            p.Rect(50, 40, 80, 60, Ink);
            for (int i = 0; i < 6; i++) { p.Rect(56 + i * 12, 30, 5, 10, MetalLight, 0f); p.Rect(56 + i * 12, 100, 5, 10, MetalLight, 0f); }
            for (int i = 0; i < 4; i++) p.Rect(15, 18 + i * 32, 30, 3, Copper, 0.3f);
            p.Rect(0, 0, 180, 6, Phos, 1f);
            p.Rect(0, 134, 180, 6, Phos, 1f);
        }, outline: 3, shadow: 6);
        Make("piece_glow", 360, 300, 100, Mid, p =>
        {
            for (int y = 0; y < p.H; y++)
                for (int x = 0; x < p.W; x++)
                {
                    float d = Mathf.Sqrt(Mathf.Pow((x - 180f) / 180f, 2) + Mathf.Pow((y - 150f) / 150f, 2));
                    p.Px[y * p.W + x] = new Color(0.5f, 1f, 0.7f, Mathf.Clamp01(1f - d) * 0.55f);
                }
        }, false, paper: 0f);
        Make("tear", 90, 1120, 100, Mid, p =>
        {
            var pts = new List<Vector2>();
            for (int y = 0; y <= 1120; y += 40) pts.Add(V(30 + CutoutPainter.Hash(y, 0, 4) * 30, y));
            for (int y = 1120; y >= 0; y -= 40) pts.Add(V(55 + CutoutPainter.Hash(y, 1, 4) * 30, y));
            p.Poly(pts, C("#e6dcc3"), 2f);
        }, outline: 2, shadow: 5);

        // ---------- GRAVAÇÃO DO PESQUISADOR (verde-fósforo, sombras frias) ----------
        Make("rec_bg", 2300, 1300, 100, Mid, p =>
        {
            p.Fill(Cold);
            for (int s = 0; s < 3; s++) p.Rect(0, 520 + s * 240, 2300, 22, ColdMid, 1.5f);
            for (int i = 0; i < 26; i++)
            {
                float x = 60 + i * 86, y = 542 + (i % 3) * 240;
                p.Rect(x, y, 34 + (i % 4) * 8, 110 + (i % 5) * 12, i % 4 == 0 ? C("#34454c") : C("#26333a"), 1.5f);
            }
            p.Curve(V(300, 1300), V(500, 700), V(760, 1300), 10, C("#11181c"));
            p.Curve(V(1600, 1300), V(1800, 600), V(2000, 1300), 8, C("#11181c"));
            p.Light(1150, 650, 1000, C("#5fae86"), 0.45f);
        }, false, paper: 0.16f);
        Make("crt_cold", 780, 680, 300, Mid, p =>
        {
            p.Rect(0, 60, 780, 620, C("#3a4440"), 3f);
            p.Rect(70, 140, 640, 500, C("#28302d"), 3f);
            p.Rect(90, 160, 600, 460, C("#071009"), 2f);
            p.Rect(80, 0, 620, 70, C("#2c3431"), 2f);
            p.Circle(660, 100, 14, PhosDim, 0f);
            p.Shade(V(-0.6f, -1), 0.35f);
        }, outline: 6, shadow: 16);
        Make("mon_schematic", 600, 460, 300, Mid, p =>
        {
            p.Fill(C("#04120a"));
            Color l = Phos;
            void Box(float x, float y, float w, float h) { p.Rect(x, y, w, 4, l, 0.6f); p.Rect(x, y + h, w, 4, l, 0.6f); p.Rect(x, y, 4, h, l, 0.6f); p.Rect(x + w, y, 4, h + 4, l, 0.6f); }
            Box(200, 150, 200, 140);
            Box(60, 300, 120, 90);
            Box(420, 300, 120, 80);
            Box(70, 60, 140, 70);
            for (int a = 0; a < 40; a++) { float t = a / 40f * Mathf.PI * 2f; p.Circle(480 + Mathf.Cos(t) * 50, 120 + Mathf.Sin(t) * 50, 2.5f, l, 0f); }
            for (int i = 0; i < 12; i++) { p.Rect(180 + i * 4, 290 + i * 1, 3, 3, l, 0f); p.Rect(400 + i * 2, 290 + i * 1, 3, 3, l, 0f); }
            for (int i = 0; i < 10; i++) p.Rect(210 + i * 6, 130 - i * 6, 3, 3, l, 0f);
            for (int i = 0; i < 14; i++) p.Rect(400 + i * 4, 210 - i * 6, 3, 3, l, 0f);
            Scan(p);
        }, false, paper: 0f);
        for (int f = 0; f < 4; f++)
        {
            int ff = f;
            Make("mon_wave_" + f, 600, 460, 300, Mid, p =>
            {
                p.Fill(C("#04120a"));
                for (int x = 0; x < 600; x += 60) p.Rect(x, 0, 2, 460, PhosDim, 0f);
                for (int y = 0; y < 460; y += 58) p.Rect(0, y, 600, 2, PhosDim, 0f);
                Vector2 prev = V(0, 230);
                for (int x = 4; x < 600; x += 4)
                {
                    float spike = CutoutPainter.Hash(x / 24, ff, 12) > 0.85f ? (CutoutPainter.Hash(x, ff, 13) - 0.5f) * 260f : 0f;
                    Vector2 pt = V(x, 230 + Mathf.Sin(x * 0.045f + ff * 1.3f) * 90 + spike);
                    p.Line(prev, pt, 4, Phos, 0f);
                    prev = pt;
                }
                Scan(p);
            }, false, paper: 0f);
            Make("mon_static_" + f, 600, 460, 300, Mid, p =>
            {
                for (int y = 0; y < p.H; y++)
                    for (int x = 0; x < p.W; x++)
                    {
                        float v = CutoutPainter.Hash(x / 3, y / 2, 40 + ff) * (y % 4 < 2 ? 0.75f : 0.5f);
                        p.Px[y * p.W + x] = new Color(v * 0.8f, v, v * 0.85f, 1f);
                    }
            }, false, paper: 0f);
        }
        Make("mon_saboteur", 600, 460, 300, Mid, p =>
        {
            p.Poly(new[] { V(200, 0), V(400, 0), V(390, 260), V(350, 300), V(250, 300), V(210, 260) }, new Color(0.02f, 0.03f, 0.03f, 0.92f), 6f);
            p.Ellipse(300, 350, 55, 70, new Color(0.02f, 0.03f, 0.03f, 0.92f), 5f);
        }, false, paper: 0f);
        Make("mon_gauges", 600, 460, 300, Mid, p =>
        {
            p.Fill(C("#04120a"));
            p.Rect(60, 290, 330, 90, Phos, 1f); p.Rect(70, 300, 310, 70, C("#04120a"), 0.5f);
            p.Rect(390, 315, 18, 40, Phos, 0.5f);
            for (int i = 0; i < 6; i++) p.Rect(82 + i * 48, 310, 38, 50, Phos, 0.5f);
            p.Rect(60, 90, 440, 70, Phos, 1f); p.Rect(70, 100, 420, 50, C("#04120a"), 0.5f);
            p.Rect(80, 108, 300, 34, Phos, 0.5f);
            p.Rect(380, 108, 100, 34, Alert, 0.5f);
            p.Circle(530, 125, 32, Alert, 1f);
            p.Rect(522, 125, 16, 110, Alert, 0.5f);
            Scan(p);
        }, false, paper: 0f);

        Make("researcher_body", 560, 540, 100, V(0.5f, 0f), p =>
        {
            p.Poly(new[] { V(30, 0), V(530, 0), V(500, 400), V(380, 470), V(180, 470), V(60, 400) }, Coat);
            p.Poly(new[] { V(220, 470), V(340, 470), V(300, 250), V(280, 240), V(260, 250) }, C("#5d6b72"));
            p.Poly(new[] { V(270, 440), V(290, 440), V(296, 260), V(280, 245), V(264, 260) }, C("#2b3236"));
            p.Poly(new[] { V(180, 470), V(220, 470), V(270, 230), V(250, 200) }, C("#aeb8b4"));
            p.Poly(new[] { V(380, 470), V(340, 470), V(290, 230), V(310, 200) }, C("#aeb8b4"));
            p.Rect(380, 250, 80, 90, C("#b5bfbb"));
            p.Rect(395, 310, 8, 50, C("#2f4f7a"), 0.4f); p.Rect(412, 310, 8, 44, Alert, 0.4f);
            p.Shade(V(1, -0.2f), 0.5f);
            p.Light(280, 300, 500, C("#7fd9a5"), 0.25f);
        });
        Make("researcher_head", 240, 300, 100, V(0.5f, 0f), p => DrawResearcherHead(p, 0), outline: 3, shadow: 6);
        Make("r_mouth", 240, 300, 100, V(0.5f, 0f), p => p.Ellipse(120, 92, 20, 11, C("#3a1f1c"), 1f), false, paper: 0f);
        Make("r_lids", 240, 300, 100, V(0.5f, 0f), p => { p.Ellipse(88, 160, 22, 12, C("#a98872"), 0.8f); p.Ellipse(152, 160, 22, 12, C("#a98872"), 0.8f); }, false, paper: 0f);
        // braços: manga do jaleco afinando até o punho, punho da camisa e mão; o pivô fica no ombro
        Color rSkin = C("#c49a80"), cuff = C("#5d6b72"), fold = C("#9aa5a1");
        Make("r_arm_rest", 220, 320, 100, V(0.5f, 1f), p =>
        {
            p.Poly(new[] { V(25, 300), V(70, 320), V(150, 320), V(195, 300), V(170, 170), V(150, 92), V(72, 92), V(55, 170) }, Coat);
            p.Line(V(70, 210), V(110, 190), 4, fold, 0.5f);
            p.Line(V(120, 150), V(150, 140), 4, fold, 0.5f);
            p.Rect(70, 70, 82, 26, cuff, 1f);
            p.Ellipse(111, 44, 44, 32, rSkin);
            p.Ellipse(146, 56, 14, 22, rSkin, 0.8f, -30f);
            for (int i = 0; i < 3; i++) p.Line(V(85 + i * 18, 30), V(83 + i * 19, 6), 12, rSkin, 0.4f);
            p.Shade(V(1, 0), 0.35f);
        });
        Make("r_arm_present", 380, 170, 100, V(1f, 0.5f), p =>
        {
            p.Poly(new[] { V(380, 38), V(380, 132), V(250, 120), V(130, 108), V(130, 64), V(250, 52) }, Coat);
            p.Line(V(260, 60), V(240, 110), 4, fold, 0.5f);
            p.Rect(110, 56, 26, 58, cuff, 1f);
            p.Ellipse(78, 86, 42, 34, rSkin);
            for (int i = 0; i < 4; i++) p.Line(V(55, 70 + i * 12), V(8, 62 + i * 16), 12, rSkin, 0.5f);
            p.Line(V(70, 112), V(48, 140), 12, rSkin, 0.5f);
            p.Shade(V(0, -1), 0.35f);
        });
        Make("r_arm_point", 400, 130, 100, V(1f, 0.5f), p =>
        {
            p.Poly(new[] { V(400, 22), V(400, 108), V(270, 96), V(145, 86), V(145, 46), V(270, 36) }, Coat);
            p.Line(V(280, 40), V(260, 92), 4, fold, 0.5f);
            p.Rect(125, 40, 24, 52, cuff, 1f);
            p.Ellipse(98, 64, 34, 28, rSkin);
            p.Line(V(76, 72), V(14, 80), 13, rSkin, 0.5f);
            p.Line(V(90, 44), V(70, 30), 12, rSkin, 0.4f);
            p.Shade(V(0, -1), 0.35f);
        });
        Make("rec_bench", 2000, 400, 100, Mid, p =>
        {
            p.Rect(0, 320, 2000, 80, C("#3d4b4f"));
            p.Rect(0, 300, 2000, 24, C("#1d2629"));
            p.Rect(20, 0, 1960, 300, C("#2a3539"));
            p.Light(1000, 400, 900, C("#7fd9a5"), 0.35f);
        });
        Make("keyboard", 380, 120, 100, Mid, p =>
        {
            p.Poly(new[] { V(0, 0), V(380, 0), V(360, 100), V(20, 100) }, C("#57615f"));
            for (int r = 0; r < 3; r++) for (int k = 0; k < 12; k++) p.Rect(30 + k * 27 + r * 6, 12 + r * 28, 22, 20, C("#8b9592"), 0.8f);
        });
        Make("oscilloscope", 320, 240, 100, Mid, p =>
        {
            p.Rect(0, 0, 320, 240, C("#4d5652"));
            p.Rect(20, 60, 180, 160, C("#061209"));
            p.Curve(V(25, 140), V(110, 230), V(195, 120), 4, Phos);
            for (int i = 0; i < 6; i++) p.Circle(240 + (i % 2) * 40, 80 + (i / 2) * 50, 14, C("#262c2a"));
            p.Shade(V(1, -1), 0.3f);
        });
        Make("tapes", 300, 140, 100, Mid, p =>
        {
            for (int i = 0; i < 3; i++)
            {
                p.Circle(55 + i * 95, 60 + (i % 2) * 18, 52, C("#2b2a28"));
                p.Circle(55 + i * 95, 60 + (i % 2) * 18, 16, C("#9a9080"));
            }
        });
        Make("rec_wires", 440, 180, 100, Mid, p =>
        {
            p.Curve(V(0, 160), V(200, -40), V(440, 120), 8, C("#151b1d"));
            p.Curve(V(0, 120), V(240, 40), V(440, 170), 7, C("#6d2c25"));
            p.Curve(V(30, 180), V(260, 10), V(400, 40), 6, C("#25425c"));
        }, outline: 2, shadow: 4);
        Make("schematic_paper", 320, 220, 100, Mid, p =>
        {
            p.Rect(0, 0, 320, 220, C("#c4d0d3"));
            p.Rect(110, 70, 100, 70, C("#c4d0d3"));
            void Box(float x, float y, float w, float h) { p.Rect(x, y, w, 3, C("#2c4f7a"), 0.4f); p.Rect(x, y + h, w, 3, C("#2c4f7a"), 0.4f); p.Rect(x, y, 3, h, C("#2c4f7a"), 0.4f); p.Rect(x + w, y, 3, h + 3, C("#2c4f7a"), 0.4f); }
            Box(110, 70, 100, 70); Box(30, 150, 60, 40); Box(230, 150, 60, 40); Box(40, 30, 70, 30);
            p.Circle(260, 60, 30, C("#00000000"), 0f);
        }, outline: 2, shadow: 4);
        // câmera temporal do jogador: compacta, grafite, anéis de latão, faixa verde-fósforo
        Make("temporal_cam", 320, 210, 100, Mid, p =>
        {
            p.Poly(new[] { V(20, 0), V(240, 0), V(270, 40), V(270, 170), V(240, 200), V(40, 200), V(10, 160), V(10, 30) }, C("#2b2f33"));
            p.Rect(30, 150, 200, 14, Phos, 0.8f);
            p.Circle(230, 95, 88, C("#9c7a3c"));
            p.Circle(230, 95, 70, C("#5d4a26"));
            p.Circle(230, 95, 56, C("#9c7a3c"));
            p.Circle(230, 95, 42, C("#0f2a2a"));
            p.Ellipse(214, 112, 14, 8, C("#9ff5d0"), 0.4f, 30f);
            p.Line(V(60, 200), V(90, 240), 6, C("#9c7a3c"));
            p.Circle(60, 60, 10, Phos, 0.5f);
        });

        // ---------- HUD (reprodução do visor do jogo) ----------
        Make("hud_frame", 1920, 1080, 100, Mid, p =>
        {
            Color w = new Color(1, 1, 1, 0.85f);
            foreach (var c in new[] { V(120, 120), V(1800, 120), V(120, 960), V(1800, 960) })
            {
                float sx = c.x < 960 ? 1 : -1, sy = c.y < 540 ? 1 : -1;
                p.Rect(c.x - (sx < 0 ? 140 : 0), c.y - (sy < 0 ? 8 : 0), 140, 8, w, 0.5f);
                p.Rect(c.x - (sx < 0 ? 8 : 0), c.y - (sy < 0 ? 140 : 0), 8, 140, w, 0.5f);
            }
        }, false, paper: 0f);
        Make("crosshair", 120, 120, 100, Mid, p =>
        {
            Color w = new Color(1, 1, 1, 0.9f);
            for (int a = 0; a < 48; a++) { float t = a / 48f * Mathf.PI * 2f; p.Circle(60 + Mathf.Cos(t) * 40, 60 + Mathf.Sin(t) * 40, 2.4f, w, 0f); }
            p.Circle(60, 60, 5, w, 0f);
        }, false, paper: 0f);
        Make("battery_frame", 240, 80, 100, V(0f, 0.5f), p =>
        {
            Color w = Color.white;
            p.Rect(0, 0, 220, 6, w, 0.3f); p.Rect(0, 74, 220, 6, w, 0.3f); p.Rect(0, 0, 6, 80, w, 0.3f); p.Rect(214, 0, 6, 80, w, 0.3f);
            p.Rect(220, 24, 20, 32, w, 0.3f);
        }, false, paper: 0f);
        Make("battery_bars", 200, 56, 100, V(0f, 0.5f), p =>
        {
            for (int i = 0; i < 10; i++) p.Rect(i * 20 + 2, 0, 15, 56, Color.white, 0f);
        }, false, paper: 0f);
        Make("heat_frame", 320, 44, 100, V(0f, 0.5f), p =>
        {
            Color w = Color.white;
            p.Rect(0, 0, 320, 4, w, 0.3f); p.Rect(0, 40, 320, 4, w, 0.3f); p.Rect(0, 0, 4, 44, w, 0.3f); p.Rect(316, 0, 4, 44, w, 0.3f);
        }, false, paper: 0f);
        Make("keycap", 130, 130, 100, Mid, p =>
        {
            p.Rect(0, 0, 130, 130, C("#d9d2c0"), 1.5f);
            p.Rect(12, 18, 106, 102, C("#efe9da"), 1.5f);
        }, outline: 3, shadow: 6, paper: 0.08f);
        Make("mouse_icon", 110, 160, 100, Mid, p =>
        {
            p.Ellipse(55, 70, 52, 72, C("#efe9da"), 1.5f);
            p.Poly(new[] { V(8, 90), V(55, 90), V(55, 150), V(20, 135) }, Amber, 1f);
            p.Rect(53, 90, 4, 60, Ink, 0.3f);
        }, outline: 3, shadow: 6, paper: 0.08f);
        Make("bracket", 300, 300, 100, Mid, p =>
        {
            foreach (var c in new[] { V(0, 0), V(260, 0), V(0, 260), V(260, 260) })
            {
                p.Rect(c.x, c.y + (c.y > 0 ? 34 : 0), 40, 6, Phos, 0.8f);
                p.Rect(c.x + (c.x > 0 ? 34 : 0), c.y, 6, 40, Phos, 0.8f);
            }
        }, false, paper: 0f);
        Make("timer_ring", 140, 140, 100, Mid, p =>
        {
            for (int a = 0; a < 60; a++)
            {
                if (a % 5 == 4) continue;
                float t = a / 60f * Mathf.PI * 2f;
                p.Circle(70 + Mathf.Cos(t) * 56, 70 + Mathf.Sin(t) * 56, 4f, Phos, 0f);
            }
        }, false, paper: 0f);
        Make("cross_x", 120, 120, 100, Mid, p =>
        {
            p.Line(V(15, 15), V(105, 105), 16, Alert, 1f);
            p.Line(V(15, 105), V(105, 15), 16, Alert, 1f);
        }, outline: 2, shadow: 4);
        // ---------- visão do jogador (ilustrada, sem imagens do jogo) ----------
        Color nWall = C("#232e33"), nWall2 = C("#1b2428"), nWood = C("#4a3626"), nWoodTop = C("#6b5038");
        Make("pov_bench", 1920, 1080, 100, Mid, p =>
        {
            p.Fill(nWall);
            for (int x = 0; x < 1920; x += 120) p.Rect(x, 420, 6, 660, nWall2, 1f);
            p.Rect(1340, 600, 420, 360, C("#5d7a87"), 2f);                         // janela com persiana
            for (int y = 610; y < 950; y += 34) p.Rect(1340, y, 420, 14, C("#1d282c"), 1f);
            p.Rect(1330, 590, 440, 14, C("#151d20"), 1f); p.Rect(1330, 956, 440, 14, C("#151d20"), 1f);
            for (int s = 0; s < 2; s++)                                             // prateleiras
            {
                float y = 640 + s * 220;
                p.Rect(80, y, 560, 22, nWood, 1.5f);
                for (int i = 0; i < 7; i++) p.Rect(100 + i * 74, y + 22, 50 + (i % 3) * 10, 90 + (i % 4) * 22, i % 3 == 0 ? C("#4d5a52") : C("#3a3f3c"), 1.5f);
            }
            p.Rect(720, 560, 480, 300, C("#5b5040"), 2f);                           // quadro de ferramentas
            for (int y = 580; y < 850; y += 30) for (int x = 740; x < 1190; x += 30) p.Circle(x, y, 3, C("#3a3328"), 0f);
            p.Line(V(800, 820), V(800, 640), 14, C("#2b2f33")); p.Line(V(880, 830), V(870, 660), 10, C("#7a3b2c"));
            p.Poly(new[] { V(0, 0), V(1920, 0), V(1920, 300), V(0, 300) }, nWood, 2f);   // frente da bancada
            p.Poly(new[] { V(0, 300), V(1920, 300), V(1800, 460), V(120, 460) }, nWoodTop, 2f); // tampo em perspectiva
            p.Rect(0, 290, 1920, 18, C("#2c2017"), 1f);
            p.Rect(200, 460, 330, 230, C("#3c4440"), 2f); p.Rect(225, 500, 190, 150, C("#0b140e"), 1f);  // osciloscópio desligado
            for (int i = 0; i < 4; i++) p.Circle(460, 520 + i * 40, 14, C("#232826"), 1f);
            p.Poly(new[] { V(1380, 340), V(1600, 330), V(1620, 380), V(1400, 395) }, PaperC, 2f);        // papéis
            p.Poly(new[] { V(1420, 360), V(1640, 370), V(1630, 420), V(1410, 410) }, C("#cfc5ad"), 2f);
            p.Line(V(1650, 460), V(1700, 640), 12, C("#3b3f3a")); p.Poly(new[] { V(1640, 650), V(1780, 640), V(1760, 590), V(1680, 595) }, C("#3b4a3f")); // luminária apagada
            p.Rect(560, 330, 160, 80, C("#5a4a36"), 1.5f); p.Rect(1100, 320, 220, 50, C("#3a3a3e"), 1.5f);
            p.Light(960, 420, 900, C("#9fb4c4"), 0.55f);
            p.Light(1550, 780, 700, C("#8fb0c0"), 0.4f);
        }, false, paper: 0.16f);
        Make("pov_corner", 1920, 1080, 100, Mid, p =>
        {
            p.Fill(nWall);
            p.Poly(new[] { V(0, 0), V(1920, 0), V(1920, 480), V(0, 480) }, C("#2e2a25"), 1f);             // piso
            for (int i = -8; i <= 8; i++) p.Line(V(960 + i * 300, 0), V(960 + i * 70, 480), 4, C("#1e1b18"), 0.3f);
            for (int y = 60; y < 480; y += 60 + y / 6) p.Rect(0, y, 1920, 4, C("#1e1b18"), 0.3f);
            p.Rect(0, 470, 1920, 20, C("#15181a"), 1f);
            p.Rect(160, 480, 520, 470, C("#4a5250"), 2f);                                                 // armário de metal
            for (int i = 0; i < 4; i++) { p.Rect(180, 500 + i * 112, 480, 96, C("#3f4645"), 1.5f); p.Rect(390, 540 + i * 112, 60, 12, C("#8b9592"), 0.5f); }
            p.Rect(1260, 480, 380, 560, C("#3c3228"), 2f); p.Rect(1290, 510, 320, 500, C("#4a3e31"), 2f);   // porta
            p.Circle(1570, 760, 14, C("#9c7a3c"), 0.5f);
            p.Line(V(1760, 480), V(1760, 1080), 26, C("#2f3537"));                                        // cano
            p.Rect(760, 480, 260, 170, C("#5a4a36"), 2f); p.Rect(800, 650, 180, 120, C("#6b5840"), 2f);    // caixas
            p.Light(960, 220, 800, C("#a9bdc8"), 0.65f);
        }, false, paper: 0.16f);
        Make("battery_pickup", 130, 220, 100, Mid, p =>
        {
            p.Rect(15, 0, 100, 190, C("#2b2f33"));
            p.Rect(15, 60, 100, 70, Phos, 1f);
            p.Rect(45, 190, 40, 26, Copper);
            p.Rect(30, 80, 70, 8, C("#1e5a38"), 0.3f);
            p.Shade(V(1, 0), 0.4f);
        }, outline: 3, shadow: 6);
        Make("prompt_bg", 64, 64, 100, Mid, p => p.Fill(new Color(0.05f, 0.05f, 0.05f, 0.75f)), false, paper: 0f);
    }

    static void Scan(CutoutPainter p)
    {
        for (int y = 0; y < p.H; y++)
            if (y % 4 >= 2)
                for (int x = 0; x < p.W; x++)
                {
                    int i = y * p.W + x;
                    Color c = p.Px[i];
                    p.Px[i] = new Color(c.r * 0.6f, c.g * 0.6f, c.b * 0.6f, c.a);
                }
    }

    static void DrawResearcherHead(CutoutPainter p, int _)
    {
        Color skin = C("#c49a80"), dark = C("#8d6a57");
        p.Rect(92, 0, 56, 70, dark);
        p.Ellipse(120, 160, 82, 102, skin);
        p.Ellipse(38, 160, 12, 22, dark);
        p.Ellipse(202, 160, 12, 22, dark);
        var hair = new List<Vector2>();
        for (int i = 0; i <= 12; i++)
        {
            float a = Mathf.PI * i / 12f;
            float r = 80 + (i % 2) * 14;
            hair.Add(V(120 + Mathf.Cos(a) * r, 212 + Mathf.Sin(a) * r * 0.8f));
        }
        hair.Add(V(40, 205)); hair.Add(V(200, 205));
        p.Poly(hair, C("#3f3a34"), 3f);
        p.Poly(new[] { V(150, 270), V(185, 240), V(170, 205), V(150, 230) }, C("#7d776e"), 2f);
        p.Ellipse(88, 160, 20, 10, C("#dfe6e2"), 0.6f);
        p.Ellipse(152, 160, 20, 10, C("#dfe6e2"), 0.6f);
        p.Circle(90, 159, 6, C("#1d2422"), 0f);
        p.Circle(150, 159, 6, C("#1d2422"), 0f);
        p.Line(V(68, 146), V(108, 146), 3, C("#8d6a57"), 0.4f);       // olheiras
        p.Line(V(132, 146), V(172, 146), 3, C("#8d6a57"), 0.4f);
        p.Line(V(66, 182), V(108, 188), 7, C("#3f3a34"), 0.5f);       // sobrancelhas cansadas
        p.Line(V(132, 188), V(174, 182), 7, C("#3f3a34"), 0.5f);
        foreach (float cx in new[] { 88f, 152f })
        {
            p.Rect(cx - 30, 140, 60, 4, Ink, 0.3f); p.Rect(cx - 30, 178, 60, 4, Ink, 0.3f);
            p.Rect(cx - 30, 140, 4, 42, Ink, 0.3f); p.Rect(cx + 26, 140, 4, 42, Ink, 0.3f);
        }
        p.Rect(116, 166, 8, 4, Ink, 0.2f);
        p.Poly(new[] { V(120, 150), V(106, 112), V(130, 112) }, dark, 1f);
        p.Line(V(100, 92), V(140, 90), 4, C("#5a3b33"), 0.4f);
        for (int i = 0; i < 160; i++)
        {
            float a = CutoutPainter.Hash(i, 0, 21) * Mathf.PI, r = 30 + CutoutPainter.Hash(i, 1, 21) * 55;
            p.Circle(120 + Mathf.Cos(a + Mathf.PI) * r, 130 + Mathf.Sin(a + Mathf.PI) * r * 0.9f, 1.2f, C("#6e584b"), 0f);
        }
        p.Shade(V(1, -0.2f), 0.45f);
        p.Light(60, 160, 260, C("#7fd9a5"), 0.35f);
    }
}
