using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Easter egg: urinar no TemporalEraser.
// 1a e 2a vez: ele trava em curto-circuito, solta um "glitch de nojo" e FOGE de voce.
// 3a vez: a paciencia dele acaba -> fica FURIOSO (sabe onde voce esta e corre mais).
// O cientista comenta tudo pelo radio, e a 1a vez desbloqueia uma "conquista".
public static class PeeReaction
{
    private static int hits;
    private static float cooldownUntil;
    private static readonly ParticleSystem.Particle[] buffer = new ParticleSystem.Particle[400];

    // Ao renascer: zera a reacao para nada "vazar" da vida anterior.
    public static void ResetState()
    {
        hits = 0;
        cooldownUntil = Time.time + 3f;
    }

    public static void CheckHit(ParticleSystem stream)
    {
        if (stream == null || Time.time < cooldownUntil) return;
        TemporalEraserController enemy = Object.FindFirstObjectByType<TemporalEraserController>();
        if (enemy == null || !enemy.isActiveAndEnabled || enemy.IsEnraged || enemy.IsFleeing || enemy.IsStunned) return;
        if (DefeatController.IsDefeated) return;

        Renderer[] rs = enemy.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        b.Expand(0.15f);

        int n = stream.GetParticles(buffer);
        int inside = 0;
        for (int i = 0; i < n; i++)
            if (b.Contains(buffer[i].position) && ++inside >= 3)
                break;
        if (inside < 3) return;

        cooldownUntil = Time.time + 6f;
        hits++;
        enemy.StartCoroutine(React(enemy, hits));
    }

    private static IEnumerator React(TemporalEraserController enemy, int count)
    {
        // Curto-circuito: trava e treme (o Stun ja faz o glitch visual dele).
        enemy.Stun(1.3f);
        PlayClip(enemy.transform.position, ShortCircuit(), 0.7f);

        if (count == 1)
        {
            Achievement(Localization.T("CONQUISTA DESBLOQUEADA"), Localization.T("Marcando território temporal"));
            ScientistRadio.SayLine("Você... URINOU no apagador temporal?! E... ele está FUGINDO?! Vou anotar isso.", 0, 0.8f);
        }
        else if (count == 2)
        {
            ScientistRadio.SayLine("De novo?! A criatura está com nojo de você! Isso vai virar um artigo científico.", 1, 0.8f);
        }
        else
        {
            ScientistRadio.SayLine("Ok. Agora ele ficou FURIOSO. Eu avisaria antes... CORRE!!!", 3, 0.6f);
        }

        yield return new WaitForSeconds(1.3f);
        if (enemy == null || DefeatController.IsDefeated) yield break;

        if (count < 3)
        {
            PlayClip(enemy.transform.position, Disgust(), 0.6f);
            enemy.Flee(9f);
        }
        else
        {
            PlayClip(enemy.transform.position, Disgust(), 0.9f, 0.55f); // o mesmo grito, grave e bravo
            enemy.Enrage(12f);
            hits = 0;
        }
    }

    // ---------- Conquista ----------
    private static void Achievement(string title, string name)
    {
        GameObject go = new GameObject("Conquista", typeof(RectTransform));
        Canvas c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 31300;
        CanvasScaler cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        CanvasGroup g = go.AddComponent<CanvasGroup>();
        LoadingScreenArt art = Resources.Load<LoadingScreenArt>("LoadingScreenArt");
        TextMeshProUGUI t1 = Label(go.transform, art, title, 26f, new Color(1f, 0.85f, 0.1f), -60f);
        TextMeshProUGUI t2 = Label(go.transform, art, name, 40f, Color.white, -96f);
        go.AddComponent<Toast>().Init(g, t1, t2);
    }

    private static TextMeshProUGUI Label(Transform parent, LoadingScreenArt art, string text, float size, Color color, float y)
    {
        GameObject go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (art != null && art.font != null) t.font = art.font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.outlineWidth = 0.12f;
        t.outlineColor = new Color32(0, 0, 0, 230);
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        RectTransform r = t.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = new Vector2(1200f, 50f);
        r.anchoredPosition = new Vector2(0f, y);
        return t;
    }

    private class Toast : MonoBehaviour
    {
        private CanvasGroup group;
        private float t;
        public void Init(CanvasGroup g, TextMeshProUGUI a, TextMeshProUGUI b) { group = g; group.alpha = 0f; }

        private void Update()
        {
            t += Time.unscaledDeltaTime;
            group.alpha = t < 0.3f ? t / 0.3f : t < 4f ? 1f : 1f - (t - 4f) / 0.6f;
            if (t > 4.6f) Destroy(gameObject);
        }
    }

    // ---------- Sons (gerados em codigo) ----------
    private static AudioClip shortCircuit, disgust;

    private static void PlayClip(Vector3 pos, AudioClip clip, float volume, float pitch = 1f)
    {
        GameObject go = new GameObject("SomUrinaReacao");
        go.transform.position = pos;
        AudioSource s = go.AddComponent<AudioSource>();
        s.spatialBlend = 0.7f;
        s.maxDistance = 30f;
        s.pitch = pitch;
        s.PlayOneShot(clip, volume);
        Object.Destroy(go, clip.length / pitch + 0.2f);
    }

    // Chiado eletrico com estalos: "agua no circuito".
    private static AudioClip ShortCircuit()
    {
        if (shortCircuit != null) return shortCircuit;
        const int rate = 44100;
        int n = (int)(rate * 1.2f);
        float[] d = new float[n];
        var rng = new System.Random(21);
        float hold = 0f;
        for (int i = 0; i < n; i++)
        {
            float x = (float)i / rate;
            if (i % 40 == 0) hold = (float)(rng.NextDouble() * 2 - 1);            // ruido "bitcrush"
            float crackle = rng.NextDouble() < 0.004 ? (float)(rng.NextDouble() * 2 - 1) * 3f : 0f;
            float buzz = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 60f * x)) * 0.25f;  // zumbido de 60 Hz
            d[i] = Mathf.Clamp((hold * 0.35f + crackle + buzz) * Mathf.Exp(-x * 1.6f), -1f, 1f) * 0.7f;
        }
        shortCircuit = AudioClip.Create("CurtoCircuito", n, 1, rate, false);
        shortCircuit.SetData(d, 0);
        return shortCircuit;
    }

    // Grito "de nojo" digital: tom que desce tremendo, quebrado em pedacos.
    private static AudioClip Disgust()
    {
        if (disgust != null) return disgust;
        const int rate = 44100;
        int n = (int)(rate * 0.9f);
        float[] d = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float x = (float)i / rate;
            float k = x / 0.9f;
            float f = Mathf.Lerp(900f, 140f, k * k) * (1f + 0.08f * Mathf.Sin(x * 70f));
            phase += 2f * Mathf.PI * f / rate;
            float s = Mathf.Sin(phase) + 0.5f * Mathf.Sin(phase * 2.01f);
            float gate = Mathf.Repeat(x * 18f, 1f) < 0.75f ? 1f : 0.15f;     // "picotado"
            d[i] = Mathf.Round(s * 6f) / 6f * gate * Mathf.Sin(Mathf.PI * k) * 0.5f;
        }
        disgust = AudioClip.Create("Nojo", n, 1, rate, false);
        disgust.SetData(d, 0);
        return disgust;
    }
}
