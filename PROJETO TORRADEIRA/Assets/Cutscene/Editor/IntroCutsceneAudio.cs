using System;
using System.IO;
using UnityEngine;

// Efeitos sintetizados para o que o projeto não tem gravado. Arquivos existentes não são sobrescritos.
public static class IntroCutsceneAudio
{
    const int Rate = 44100;
    static System.Random rng;

    static float N() => (float)(rng.NextDouble() * 2.0 - 1.0);

    static void Save(string dir, string name, float seconds, Func<float, float> f)
    {
        string path = Path.Combine(dir, name + ".wav");
        if (File.Exists(path)) return;
        rng = new System.Random(name.GetHashCode());
        int n = (int)(seconds * Rate);
        var s = new float[n];
        for (int i = 0; i < n; i++) s[i] = f(i / (float)Rate);
        float peak = 0.0001f;
        foreach (float v in s) peak = Mathf.Max(peak, Mathf.Abs(v));
        for (int i = 0; i < n; i++) s[i] = s[i] / peak * 0.85f;
        WriteWav(path, s);
    }

    public static void WriteWav(string path, float[] s)
    {
        using var w = new BinaryWriter(File.Create(path));
        w.Write("RIFF".ToCharArray()); w.Write(36 + s.Length * 2); w.Write("WAVE".ToCharArray());
        w.Write("fmt ".ToCharArray()); w.Write(16); w.Write((short)1); w.Write((short)1);
        w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
        w.Write("data".ToCharArray()); w.Write(s.Length * 2);
        foreach (float v in s) w.Write((short)(Mathf.Clamp(v, -1f, 1f) * 32767));
    }

    static float Env(float t, float attack, float decay) =>
        t < attack ? t / attack : Mathf.Exp(-(t - attack) / decay);

    public static void Generate(string dir)
    {
        Directory.CreateDirectory(dir);

        // ambiente: ventiladores (ruído grave filtrado), zumbido de 60 Hz, chiado de fita e apito de CRT
        float lp = 0f, lp2 = 0f;
        Save(dir, "amb_lab", 12f, t =>
        {
            lp += (N() - lp) * 0.02f;
            lp2 += (N() - lp2) * 0.3f;
            float fan = lp * (0.8f + 0.2f * Mathf.Sin(t * 2f * Mathf.PI * 0.5f)); // 12 s fecham ciclos inteiros: loop sem emenda
            float hum = Mathf.Sin(t * 2f * Mathf.PI * 60f) * 0.12f + Mathf.Sin(t * 2f * Mathf.PI * 120f) * 0.05f;
            float hiss = lp2 * 0.05f;
            float whine = Mathf.Sin(t * 2f * Mathf.PI * 7830f) * 0.008f;
            return fan * 1.2f + hum + hiss + whine;
        });

        Save(dir, "sfx_tape_click", 0.35f, t =>
            N() * Env(t, 0.001f, 0.008f) + Mathf.Sin(t * 2f * Mathf.PI * 420f) * Env(t, 0.002f, 0.04f) * 0.6f +
            (t > 0.06f ? N() * Env(t - 0.06f, 0.001f, 0.01f) * 0.5f : 0f));

        Save(dir, "sfx_tool_click", 0.2f, t =>
            Mathf.Sin(t * 2f * Mathf.PI * 3100f) * Env(t, 0.0005f, 0.02f) * 0.6f + N() * Env(t, 0.0005f, 0.004f));

        Save(dir, "sfx_whoosh", 0.45f, t =>
        {
            lp += (N() - lp) * (0.05f + 0.3f * t);
            return lp * Mathf.Sin(Mathf.Clamp01(t / 0.45f) * Mathf.PI);
        });

        Save(dir, "sfx_discovery", 2.2f, t =>
        {
            float e = Env(t, 0.03f, 0.7f);
            return (Mathf.Sin(t * 2f * Mathf.PI * 659.25f) + 0.5f * Mathf.Sin(t * 2f * Mathf.PI * 987.77f) +
                    0.25f * Mathf.Sin(t * 2f * Mathf.PI * 1318.5f)) * e;
        });

        Save(dir, "sfx_impact", 1.6f, t =>
        {
            float thud = Mathf.Sin(t * 2f * Mathf.PI * (55f + 60f * Mathf.Exp(-t * 20f))) * Env(t, 0.002f, 0.25f);
            lp += (N() - lp) * 0.25f;
            float crash = lp * Env(t, 0.001f, 0.12f) * 1.4f;
            float rattle = 0f;
            for (int k = 1; k < 9; k++)
            {
                float tk = t - k * 0.09f - (k % 3) * 0.03f;
                if (tk > 0f) rattle += Mathf.Sin(tk * 2f * Mathf.PI * (1800f + k * 310f)) * Env(tk, 0.0005f, 0.015f) * 0.4f / k * 3f;
            }
            return thud * 1.3f + crash + rattle;
        });

        Save(dir, "sfx_reveal", 1.4f, t =>
        {
            float f = 380f + 900f * (t / 1.4f);
            return Mathf.Sin(t * 2f * Mathf.PI * f) * Mathf.Sin(Mathf.Clamp01(t / 1.4f) * Mathf.PI) *
                   (0.6f + 0.4f * Mathf.Sin(t * 2f * Mathf.PI * 14f)) + N() * 0.08f * Env(t, 0.2f, 0.4f);
        });

        Save(dir, "sfx_collect", 0.9f, t =>
        {
            float a = Mathf.Sin(t * 2f * Mathf.PI * 784f) * Env(t, 0.005f, 0.12f);
            float b = t > 0.12f ? Mathf.Sin(t * 2f * Mathf.PI * 1175f) * Env(t - 0.12f, 0.005f, 0.25f) : 0f;
            return a + b;
        });

        Save(dir, "sfx_heat_warning", 0.6f, t =>
        {
            float sq = Mathf.Sign(Mathf.Sin(t * 2f * Mathf.PI * 880f)) * 0.4f + Mathf.Sin(t * 2f * Mathf.PI * 880f) * 0.6f;
            float gate = (t < 0.18f || (t > 0.3f && t < 0.48f)) ? 1f : 0f;
            return sq * gate * 0.7f;
        });

        Save(dir, "sfx_rupture", 1.8f, t =>
        {
            lp += (N() - lp) * 0.5f;
            float swell = t < 0.25f ? t / 0.25f : Mathf.Exp(-(t - 0.25f) / 0.35f);
            float drop = Mathf.Sin(t * 2f * Mathf.PI * (900f * Mathf.Exp(-t * 3f) + 40f));
            return lp * swell * 1.1f + drop * swell * 0.8f;
        });

        Save(dir, "sfx_crt_on", 0.8f, t =>
            Mathf.Sin(t * 2f * Mathf.PI * 70f) * Env(t, 0.003f, 0.08f) + Mathf.Sin(t * 2f * Mathf.PI * 9800f) * Env(t, 0.05f, 0.3f) * 0.2f +
            N() * Env(t, 0.001f, 0.05f) * 0.6f);

        Save(dir, "sfx_crt_off", 0.9f, t =>
            Mathf.Sin(t * 2f * Mathf.PI * (8000f * Mathf.Exp(-t * 4f) + 200f)) * Env(t, 0.002f, 0.35f) * 0.5f +
            Mathf.Sin(t * 2f * Mathf.PI * 60f) * Env(t, 0.001f, 0.06f));

        Save(dir, "sfx_denied", 0.4f, t =>
            Mathf.Sin(t * 2f * Mathf.PI * 180f) * (t < 0.12f || (t > 0.18f && t < 0.3f) ? 1f : 0f) * 0.8f);
    }
}
