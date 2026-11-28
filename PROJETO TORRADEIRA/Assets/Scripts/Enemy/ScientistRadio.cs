using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// O cientista fala pelo radio quando o jogador renasce. Quanto mais mortes, mais bravo:
// a fala fica mais curta e grossa, o texto vai do branco ao vermelho e treme,
// e a "voz" (bipes filtrados de radio) fica mais grave e rapida.
public class ScientistRadio : MonoBehaviour
{
    private static readonly string[][] Lines =
    {
        new[] // 1a morte: calmo
        {
            "Consegui colocar você de volta no passado. Tome cuidado.",
            "Peguei seu sinal! Você está de volta em 1975. Fique longe daquela coisa.",
            "Te puxei de volta bem a tempo. Respira... e continua a missão.",
        },
        new[] // 2-3: preocupado
        {
            "De novo? A máquina não aguenta muitas viagens assim. Presta atenção!",
            "Te coloquei de volta no passado. Mais uma vez. Evite aquela criatura!",
            "Isso gasta muita energia, sabia? Tome mais cuidado dessa vez.",
        },
        new[] // 4-5: irritado
        {
            "Você quer quebrar a máquina?! Eu mal consegui te trazer de volta!",
            "Quantas vezes eu vou ter que fazer isso?! FIQUE LONGE DELE!",
            "O protótipo está superaquecendo por sua causa. FOCO!",
        },
        new[] // 6+: furioso
        {
            "CHEGA! Já é a {0}ª vez! Voltou. De novo. NÃO MORRA!",
            "Eu devia ter mandado o estagiário! {0} viagens! VOLTA LÁ E SOBREVIVE!",
            "Sabe quanto custa uma viagem no tempo?! {0} vezes! PARA DE MORRER!",
        },
    };

    private int deaths;
    private float delay;
    private string custom;
    private int customTier = -1;

    // Fala especifica (ex.: reacao a urina). tier 0..3 define o humor.
    public static void SayLine(string line, int tier, float delay)
    {
        StopAll();
        ScientistRadio r = new GameObject("RadioCientista").AddComponent<ScientistRadio>();
        r.custom = line;
        r.customTier = tier;
        r.delay = delay;
    }

    // So um radio por vez: uma fala nova corta a anterior (antes podiam sobrepor).
    public static void StopAll()
    {
        foreach (ScientistRadio old in FindObjectsByType<ScientistRadio>(FindObjectsSortMode.None))
            Destroy(old.gameObject);
    }

    public static void Say(int deaths, float delay)
    {
        StopAll();
        ScientistRadio r = new GameObject("RadioCientista").AddComponent<ScientistRadio>();
        r.deaths = deaths;
        r.delay = delay;
    }

    // Menu de pausa aberto: o radio congela (tempo escalado para) e some/silencia.
    private bool paused;
    private void Update()
    {
        bool p = PauseMenu.IsPaused || Time.timeScale <= 0f;
        if (p == paused) return;
        paused = p;
        foreach (AudioSource a in GetComponents<AudioSource>())
        {
            if (p) a.Pause();
            else a.UnPause();
        }
        Canvas cv = GetComponentInChildren<Canvas>();
        if (cv != null) cv.enabled = !p;
    }

    private static int Tier(int d) => d <= 1 ? 0 : d <= 3 ? 1 : d <= 5 ? 2 : 3;

    private IEnumerator Start()
    {
        int tier = customTier >= 0 ? customTier : Tier(deaths);
        string[] pool = Lines[tier];
        string line = custom != null
            ? Localization.T(custom)
            : string.Format(Localization.T(pool[Random.Range(0, pool.Length)]), deaths);

        LoadingScreenArt art = Resources.Load<LoadingScreenArt>("LoadingScreenArt");
        AudioSource src = gameObject.AddComponent<AudioSource>();
        src.spatialBlend = 0f;
        src.bypassListenerEffects = true; // nao e abafado pelo atordoamento
        AudioSource staticSrc = gameObject.AddComponent<AudioSource>();
        staticSrc.spatialBlend = 0f;
        staticSrc.bypassListenerEffects = true;
        staticSrc.loop = true;
        staticSrc.clip = art != null ? art.radioStatic : null;

        // UI: caixa de radio embaixo, fonte VCR.
        GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        Canvas c = canvasGo.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 31200;
        CanvasScaler cs = canvasGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        CanvasGroup group = canvasGo.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        RectTransform box = new GameObject("Caixa", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        box.SetParent(canvasGo.transform, false);
        box.anchorMin = box.anchorMax = new Vector2(0.5f, 0f);
        box.pivot = new Vector2(0.5f, 0f);
        box.anchoredPosition = new Vector2(0f, 120f);
        box.sizeDelta = new Vector2(1100f, 150f);
        box.GetComponent<Image>().enabled = false; // sem painel de fundo

        Color anger = Color.Lerp(new Color(0.93f, 0.95f, 0.9f), new Color(0.95f, 0.18f, 0.15f), tier / 3f);
        TextMeshProUGUI who = Label(box, art, "● RADIO  //  CIENTISTA", 24f, new Color(0.85f, 0.12f, 0.12f));
        who.rectTransform.anchoredPosition = new Vector2(30f, -16f);
        TextMeshProUGUI text = Label(box, art, "", 34f, anger);
        text.textWrappingMode = TextWrappingModes.Normal;
        text.rectTransform.anchoredPosition = new Vector2(30f, -52f);
        text.rectTransform.sizeDelta = new Vector2(1040f, 90f);

        float t0 = Time.time;
        while (Time.time - t0 < delay) yield return null;

        // Liga o radio: estalo + chiado.
        src.PlayOneShot(Click(), 0.6f);
        if (staticSrc.clip != null)
        {
            staticSrc.time = Random.Range(0f, Mathf.Max(0f, staticSrc.clip.length - 4f));
            staticSrc.volume = 0.35f;
            staticSrc.Play();
        }
        for (float t = 0f; t < 0.25f; t += Time.deltaTime) { group.alpha = t / 0.25f; yield return null; }
        group.alpha = 1f;
        // Chiado baixa quando ele comeca a falar.
        if (staticSrc.isPlaying) staticSrc.volume = 0.12f + tier * 0.03f;

        // Fala: maquina de escrever com "voz" de radio.
        float charTime = Mathf.Lerp(0.045f, 0.03f, tier / 3f);
        float pitch = Mathf.Lerp(1.15f, 0.8f, tier / 3f);
        AudioClip blip = Voice();
        Vector2 basePos = text.rectTransform.anchoredPosition;
        // Texto inteiro de uma vez (as palavras nao pulam de linha enquanto digita) e
        // as letras sao reveladas com maxVisibleCharacters. Os glifos (acentos, ! ?) sao
        // gerados no atlas antes, para nao "piscar" a fonte no meio da fala.
        if (text.font != null) text.font.TryAddCharacters(line + "●/", out _);
        text.text = line;
        text.maxVisibleCharacters = 0;
        text.ForceMeshUpdate();
        for (int i = 1; i <= line.Length; i++)
        {
            text.maxVisibleCharacters = i;
            char ch = line[i - 1];
            if (char.IsLetter(ch) && i % 2 == 0)
            {
                src.pitch = pitch * Random.Range(0.9f, 1.15f);
                src.PlayOneShot(blip, 0.35f + tier * 0.08f);
            }
            float wait = charTime * (ch == '.' || ch == '!' || ch == '?' ? 6f : ch == ',' ? 3f : 1f);
            for (float t = 0f; t < wait; t += Time.deltaTime)
            {
                if (tier >= 2) text.rectTransform.anchoredPosition = basePos + Random.insideUnitCircle * (tier - 1) * 1.5f;
                yield return null;
            }
        }
        text.rectTransform.anchoredPosition = basePos;
        src.pitch = 1f;

        for (float t = 0f; t < 2.6f; t += Time.deltaTime) yield return null;

        // Desliga: estalo, chiado some.
        src.PlayOneShot(Click(), 0.5f);
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            group.alpha = 1f - t / 0.4f;
            staticSrc.volume *= 0.9f;
            yield return null;
        }
        Destroy(gameObject);
    }

    private static TextMeshProUGUI Label(RectTransform parent, LoadingScreenArt art, string value, float size, Color color)
    {
        GameObject go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (art != null && art.font != null) t.font = art.font;
        t.text = value;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.TopLeft;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        // Sem fundo: contorno escuro para ler em cima do jogo.
        t.outlineWidth = 0.12f;
        t.outlineColor = new Color32(0, 0, 0, 230);
        RectTransform r = t.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
        r.pivot = new Vector2(0f, 1f);
        r.sizeDelta = new Vector2(1040f, 30f);
        return t;
    }

    private static AudioClip click, voice;

    // Estalo do botao do radio + bip curto de "cambio".
    private static AudioClip Click()
    {
        if (click != null) return click;
        const int rate = 44100;
        int n = (int)(rate * 0.12f);
        float[] d = new float[n];
        var rng = new System.Random(4);
        for (int i = 0; i < n; i++)
        {
            float x = (float)i / rate;
            float pop = (float)(rng.NextDouble() * 2 - 1) * Mathf.Exp(-x * 120f);
            float beep = x > 0.03f && x < 0.1f ? Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 1400f * x)) * 0.12f : 0f;
            d[i] = pop * 0.8f + beep;
        }
        click = AudioClip.Create("RadioClick", n, 1, rate, false);
        click.SetData(d, 0);
        return click;
    }

    // Silaba de voz "de radio": onda com formantes, filtrada em faixa estreita.
    private static AudioClip Voice()
    {
        if (voice != null) return voice;
        const int rate = 44100;
        int n = (int)(rate * 0.07f);
        float[] d = new float[n];
        float lp = 0f, hp = 0f;
        for (int i = 0; i < n; i++)
        {
            float x = (float)i / rate;
            float env = Mathf.Sin(Mathf.PI * i / n);
            float s = Mathf.Sin(2f * Mathf.PI * 160f * x) + 0.6f * Mathf.Sin(2f * Mathf.PI * 720f * x) + 0.35f * Mathf.Sin(2f * Mathf.PI * 1250f * x);
            s = Mathf.Clamp(s * 1.6f, -1f, 1f);
            lp += (s - lp) * 0.35f;      // corta agudos
            hp += (lp - hp) * 0.08f;      // corta graves
            d[i] = (lp - hp) * env * 0.7f;
        }
        voice = AudioClip.Create("RadioVoz", n, 1, rate, false);
        voice.SetData(d, 0);
        return voice;
    }
}
