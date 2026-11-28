using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TimeTravelIntro : MonoBehaviour
{
    [Header("UI da Intro")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text yearText;
    [SerializeField] private TMP_Text glitchText;
    [SerializeField] private GameObject crosshair;

    [Header("Canvas Principal")]
    [SerializeField] private GameObject mainCanvas;
    [SerializeField] private CanvasGroup mainCanvasGroup;
    [SerializeField] private float mainCanvasFadeDuration = 0.8f;

    [Header("Intro Volume")]
    [SerializeField] private UnityEngine.Rendering.Volume introVolume;

    [Header("Controles Bloqueados")]
    [SerializeField] private Behaviour[] playerControls;

    [Header("Áudio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip countdownSound;
    [SerializeField] private AudioClip glitchSound;
    [SerializeField] private AudioClip finalSound;

    [Header("Volume")]
    [SerializeField][Range(0f, 1f)] private float countdownVolume = 0.35f;
    [SerializeField][Range(0f, 1f)] private float glitchVolume = 0.7f;
    [SerializeField][Range(0f, 1f)] private float finalVolume = 0.8f;

    [Header("Anos")]
    [SerializeField] private int startYear = 2026;
    [SerializeField] private int targetYear = 1975;

    [Header("Contagem")]
    [SerializeField] private float countdownDuration = 2.2f;
    [SerializeField] private float countdownSoundInterval = 0.035f;

    [Header("Final")]
    [SerializeField] private float glitchDuration = 1.4f;
    [SerializeField] private float glitchInterval = 0.06f;
    [SerializeField] private float finalPause = 0.7f;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Glitch Visual")]
    [SerializeField] private float glitchPosition = 4f;
    [SerializeField][Range(0f, 1f)] private float flickerAmount = 0.65f;
    [SerializeField][Range(0f, 1f)] private float duplicateChance = 0.45f;

    private float previousTimeScale;

    private Vector3 yearOriginalPosition;
    private Vector3 glitchOriginalPosition;

    private Color yearOriginalColor;
    private Color glitchOriginalColor;

    private bool running;

    private readonly string[] glitchValues =
    {
        "1974",
        "1975",
        "197_",
        "19?5",
        "19/5",
        "1_75",
        "197",
        "197?",
        "19 75"
    };

    private LoadingScreenArt art;

    // ---------- Efeito "bug" digital no ano ----------
    // Sem nada elastico: tudo em degraus duros (como sinal digital corrompido).
    // - cada letra e fatiada/deslocada na horizontal e pula na vertical;
    // - fantasmas vermelho e ciano deslocados (separacao RGB);
    // - digitos trocados por lixo por um quadro;
    // - o efeito "trava" uns quadros e pula (stutter).
    private float bugAmount;
    private float nextBugStep;
    private TextMeshProUGUI ghostR, ghostC;
    private static readonly string Junk = "#%@&?X0123456789";

    private IEnumerator FinalBurst()
    {
        bugAmount = 1f;
        for (float t = 0f; t < 0.18f; t += Time.unscaledDeltaTime) yield return null;
        bugAmount = 0f;
        ApplyBug(true);
    }

    private void LateUpdate()
    {
        if (holdNarrow && introCam != null) introCam.fieldOfView = baseFov * NarrowFactor;
        if (!running || yearText == null) return;
        if (ghostR == null) BuildGhosts();
        ApplyBug(false);
    }

    private void BuildGhosts()
    {
        ghostR = Instantiate(yearText.gameObject, yearText.transform.parent).GetComponent<TextMeshProUGUI>();
        ghostC = Instantiate(yearText.gameObject, yearText.transform.parent).GetComponent<TextMeshProUGUI>();
        ghostR.name = "AnoFantasmaR";
        ghostC.name = "AnoFantasmaC";
        ghostR.transform.SetSiblingIndex(yearText.transform.GetSiblingIndex());
        ghostC.transform.SetSiblingIndex(yearText.transform.GetSiblingIndex());
    }

    private void ApplyBug(bool force)
    {
        if (!force && Time.unscaledTime < nextBugStep) return;
        // Passo irregular: as vezes congela mais tempo (lag).
        nextBugStep = Time.unscaledTime + (Random.value < 0.15f * bugAmount ? Random.Range(0.1f, 0.22f) : Random.Range(0.03f, 0.06f));

        string clean = yearText.text;
        float a = FlashSettings.Enabled ? bugAmount : bugAmount * 0.4f;

        // Ghosts RGB.
        foreach (TextMeshProUGUI g in new[] { ghostR, ghostC })
        {
            if (g == null) continue;
            bool red = g == ghostR;
            g.text = clean;
            g.color = red ? new Color(1f, 0.1f, 0.1f, 0.55f * a) : new Color(0.1f, 0.9f, 1f, 0.45f * a);
            float off = Mathf.Round(Random.Range(4f, 22f) * a) * (red ? -1f : 1f);
            g.rectTransform.localPosition = yearOriginalPosition + new Vector3(off, Mathf.Round(Random.Range(-3f, 3f) * a), 0f);
            g.rectTransform.localScale = Vector3.one;
        }

        // Fatias por letra (vertices do TMP).
        yearText.ForceMeshUpdate();
        if (a <= 0.01f) return;
        TMP_TextInfo info = yearText.textInfo;
        float sliceY = Random.Range(-0.4f, 0.6f);      // altura do "corte" dentro da letra
        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo ch = info.characterInfo[i];
            if (!ch.isVisible) continue;
            Vector3[] v = info.meshInfo[ch.materialReferenceIndex].vertices;
            int vi = ch.vertexIndex;
            float h = ch.topLeft.y - ch.bottomLeft.y;
            float cut = ch.bottomLeft.y + h * (0.5f + sliceY * 0.5f);
            float shiftTop = Random.value < 0.6f * a ? Mathf.Round(Random.Range(-28f, 28f) * a / 4f) * 4f : 0f;
            float shiftAll = Random.value < 0.25f * a ? Mathf.Round(Random.Range(-14f, 14f) * a / 4f) * 4f : 0f;
            float jumpY = Random.value < 0.2f * a ? Mathf.Round(Random.Range(-10f, 10f) * a / 4f) * 4f : 0f;
            for (int k = 0; k < 4; k++)
            {
                Vector3 p = v[vi + k];
                p.x += shiftAll + (p.y > cut ? shiftTop : 0f);
                p.y += jumpY;
                v[vi + k] = p;
            }
        }
        yearText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);

        // Lixo no lugar de um digito, so por um passo (texto real volta no proximo SetYear/passo).
        if (Random.value < 0.25f * a && clean.Length > 0)
        {
            char[] c = clean.ToCharArray();
            c[Random.Range(0, c.Length)] = Junk[Random.Range(0, Junk.Length)];
            if (ghostR != null) ghostR.text = new string(c);
        }
    }

    // ---------- FOV + volume da viagem ----------
    // Durante a intro a visao fica estreita (tunel) e corrompida; no fim ela "estoura"
    // para alem do normal com um whoosh e assenta oscilando ate o FOV do jogo.
    private Camera introCam;
    private float baseFov = 60f;
    private const float NarrowFactor = 0.55f;
    private Volume fxVolume;
    private LensDistortion fxLens;
    private ChromaticAberration fxChroma;
    private FilmGrain fxGrain;
    private ColorAdjustments fxColor;
    private bool holdNarrow;

    private void SetupLens()
    {
        introCam = Camera.main;
        if (introCam != null)
        {
            baseFov = introCam.fieldOfView;
            introCam.fieldOfView = baseFov * NarrowFactor;
            holdNarrow = true;
        }

        GameObject go = new GameObject("IntroViagemVolume");
        fxVolume = go.AddComponent<Volume>();
        fxVolume.isGlobal = true;
        fxVolume.priority = 150f;
        VolumeProfile p = ScriptableObject.CreateInstance<VolumeProfile>();
        fxVolume.profile = p;
        fxLens = p.Add<LensDistortion>(true);
        fxLens.intensity.Override(-0.45f);
        fxChroma = p.Add<ChromaticAberration>(true);
        fxChroma.intensity.Override(0.7f);
        fxGrain = p.Add<FilmGrain>(true);
        fxGrain.type.Override(FilmGrainLookup.Medium4);
        fxGrain.intensity.Override(0.7f);
        fxColor = p.Add<ColorAdjustments>(true);
        fxColor.saturation.Override(-40f);
        fxColor.contrast.Override(15f);
    }

    private IEnumerator Arrival()
    {
        holdNarrow = false;
        PlaySound(Whoosh(), 0.6f);
        float narrow = baseFov * NarrowFactor;
        const float burst = 0.45f, settle = 1.1f;
        for (float t = 0f; t < burst + settle; t += Time.unscaledDeltaTime)
        {
            float fov, lens, ca, grain, sat;
            if (t < burst)
            {
                float k = 1f - Mathf.Pow(2f, -10f * t / burst);           // ease-out expo
                fov = Mathf.Lerp(narrow, baseFov * 1.18f, k);
                lens = Mathf.Lerp(-0.45f, 0.35f, k);
                ca = Mathf.Lerp(0.7f, 1f, k);
                grain = 0.7f;
                sat = -40f;
            }
            else
            {
                float u = (t - burst) / settle;
                float wobble = Mathf.Exp(-5f * u) * Mathf.Cos(9f * u);  // mola amortecida
                fov = baseFov * (1f + 0.18f * wobble);
                lens = 0.35f * wobble;
                ca = 1f - u;
                grain = Mathf.Lerp(0.7f, 0f, u);
                sat = Mathf.Lerp(-40f, 0f, u);
            }
            if (introCam != null) introCam.fieldOfView = fov;
            fxLens.intensity.Override(lens);
            fxChroma.intensity.Override(ca);
            fxGrain.intensity.Override(grain);
            fxColor.saturation.Override(sat);
            yield return null;
        }
        if (introCam != null) introCam.fieldOfView = baseFov;
        if (fxVolume != null) Destroy(fxVolume.gameObject);
    }

    private static AudioClip whoosh;
    // Ar "puxado" subindo: ruido filtrado com varredura de frequencia.
    private static AudioClip Whoosh()
    {
        if (whoosh != null) return whoosh;
        const int rate = 44100;
        int n = (int)(rate * 0.9f);
        float[] d = new float[n];
        var rng = new System.Random(9);
        float lp = 0f, lp2 = 0f;
        for (int i = 0; i < n; i++)
        {
            float k = (float)i / n;
            float cutoff = Mathf.Lerp(0.02f, 0.35f, Mathf.Sin(k * Mathf.PI * 0.5f));
            float noise = (float)(rng.NextDouble() * 2 - 1);
            lp += (noise - lp) * cutoff;
            lp2 += (lp - lp2) * cutoff;
            float env = Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(k, 0.6f)), 2f);
            d[i] = lp2 * env * 1.4f;
        }
        whoosh = AudioClip.Create("Whoosh", n, 1, rate, false);
        whoosh.SetData(d, 0);
        return whoosh;
    }

    private void Awake()
    {
        if (yearText != null)
        {
            yearOriginalPosition =
                yearText.rectTransform.localPosition;

            yearOriginalColor =
                yearText.color;
        }

        if (glitchText != null)
        {
            glitchOriginalPosition =
                glitchText.rectTransform.localPosition;

            glitchOriginalColor =
                glitchText.color;
        }

        if (mainCanvasGroup != null)
            mainCanvasGroup.alpha = 0f;
    }

    private void Start()
    {
        previousTimeScale =
            Time.timeScale;

        Time.timeScale = 0f;

        DisableGameplay();

        if (mainCanvas != null)
            mainCanvas.SetActive(false);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (introVolume != null)
            introVolume.weight = 1f;

        ResetVisuals();

        SetYear(startYear);

        PlayerRespawn.ResetDeaths();
        art = Resources.Load<LoadingScreenArt>("LoadingScreenArt");
        SetupLens();

        running = true;

        StartCoroutine(
            PlayIntro()
        );
    }

    private void Update()
    {
        if (!running)
            return;

        if (crosshair != null &&
            crosshair.activeSelf)
        {
            crosshair.SetActive(false);
        }
    }

    private IEnumerator PlayIntro()
    {
        yield return StartCoroutine(
            FastCountdown()
        );

        yield return StartCoroutine(
            TemporalGlitch()
        );

        ResetVisuals();

        SetYear(targetYear);

        PlaySound(
            finalSound,
            finalVolume
        );
        if (art != null && art.tapeClick != null)
            PlaySound(art.tapeClick, 0.9f);
        StartCoroutine(FinalBurst());

        yield return new WaitForSecondsRealtime(
            finalPause
        );

        StartCoroutine(Arrival());

        yield return StartCoroutine(
            FadeIntro()
        );

        yield return StartCoroutine(
            FadeInMainCanvas()
        );

        FinishIntro();
    }

    private IEnumerator FastCountdown()
    {
        float elapsed = 0f;
        float soundTimer = 0f;

        int lastYear = startYear;

        SetYear(startYear);

        bool tape = art != null && art.tapeRewind != null;
        if (tape)
            PlaySound(art.tapeRewind, 0.5f);

        while (elapsed < countdownDuration)
        {
            float delta =
                Time.unscaledDeltaTime;

            elapsed += delta;
            soundTimer += delta;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    countdownDuration
                );

            float eased =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            int year =
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        startYear,
                        1976f,
                        eased
                    )
                );

            if (year != lastYear)
            {
                SetYear(year);
                lastYear = year;
            }

            // Corrupcao digital cresce conforme a viagem acelera.
            bugAmount = Mathf.Lerp(0.15f, 0.7f, progress);

            if (
                !tape &&
                countdownSound != null &&
                soundTimer >= countdownSoundInterval
            )
            {
                soundTimer = 0f;

                PlaySound(
                    countdownSound,
                    countdownVolume
                );
            }

            yield return null;
        }

        SetYear(1976);
    }

    private IEnumerator TemporalGlitch()
    {
        float elapsed = 0f;
        float timer = 0f;
        float soundTimer = 0f;

        bool playedGlitchSound = false;

        SetYear(1974);
        bugAmount = 1f;

        while (elapsed < glitchDuration)
        {
            float delta =
                Time.unscaledDeltaTime;

            elapsed += delta;
            timer += delta;
            soundTimer += delta;

            if (timer >= glitchInterval)
            {
                timer = 0f;

                ApplyGlitch();
            }

            if (
                !playedGlitchSound &&
                glitchSound != null
            )
            {
                PlaySound(
                    glitchSound,
                    glitchVolume
                );

                playedGlitchSound = true;
            }

            if (soundTimer >= 0.35f)
            {
                soundTimer = 0f;

                if (glitchSound != null)
                {
                    PlaySound(
                        glitchSound,
                        glitchVolume * 0.15f
                    );
                }
            }

            yield return null;
        }

        ResetVisuals();

        SetYear(1975);
    }

    private void ApplyGlitch()
    {
        if (yearText == null)
            return;

        yearText.text =
            glitchValues[
                Random.Range(
                    0,
                    glitchValues.Length
                )
            ];

        Color mainColor =
            yearOriginalColor;

        // Sem flashes: o ano troca de numero, mas nao pisca.
        mainColor.a =
            FlashSettings.Enabled
                ? Random.Range(flickerAmount, 1f)
                : yearOriginalColor.a;

        yearText.color =
            mainColor;

        Vector2 offset =
            Random.insideUnitCircle *
            glitchPosition;

        yearText.rectTransform.localPosition =
            yearOriginalPosition +
            new Vector3(
                offset.x,
                offset.y,
                0f
            );

        if (
            glitchText != null &&
            Random.value < duplicateChance
        )
        {
            glitchText.text =
                glitchValues[
                    Random.Range(
                        0,
                        glitchValues.Length
                    )
                ];

            Color duplicateColor =
                glitchOriginalColor;

            duplicateColor.a =
                Random.Range(
                    0.15f,
                    0.5f
                );

            glitchText.color =
                duplicateColor;

            glitchText.alpha =
                duplicateColor.a;

            Vector2 duplicateOffset =
                Random.insideUnitCircle *
                (glitchPosition * 1.5f);

            glitchText.rectTransform.localPosition =
                glitchOriginalPosition +
                new Vector3(
                    duplicateOffset.x,
                    duplicateOffset.y,
                    0f
                );
        }
        else if (glitchText != null)
        {
            glitchText.alpha = 0f;
        }
    }

    private void ResetVisuals()
    {
        if (yearText != null)
        {
            yearText.text =
                targetYear.ToString();

            yearText.color =
                yearOriginalColor;

            yearText.rectTransform.localScale = Vector3.one;
            yearText.rectTransform.localPosition =
                yearOriginalPosition;
        }

        if (glitchText != null)
        {
            glitchText.alpha = 0f;

            glitchText.rectTransform.localPosition =
                glitchOriginalPosition;

            glitchText.color =
                glitchOriginalColor;
        }
    }

    private void SetYear(int year)
    {
        if (yearText != null)
            yearText.text =
                year.ToString();
    }

    private IEnumerator FadeIntro()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    fadeDuration
                );

            float smooth =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            if (canvasGroup != null)
            {
                canvasGroup.alpha =
                    1f - smooth;
            }

            if (introVolume != null)
            {
                introVolume.weight =
                    1f - smooth;
            }

            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (introVolume != null)
            introVolume.weight = 0f;
    }

    private IEnumerator FadeInMainCanvas()
    {
        if (mainCanvas == null)
            yield break;

        mainCanvas.SetActive(true);

        if (mainCanvasGroup == null)
            yield break;

        mainCanvasGroup.alpha = 0f;

        float elapsed = 0f;

        while (
            elapsed <
            mainCanvasFadeDuration
        )
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    mainCanvasFadeDuration
                );

            float smooth =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            mainCanvasGroup.alpha =
                smooth;

            yield return null;
        }

        mainCanvasGroup.alpha = 1f;
    }

    private void PlaySound(
        AudioClip clip,
        float volume
    )
    {
        if (
            audioSource == null ||
            clip == null
        )
        {
            return;
        }

        audioSource.PlayOneShot(
            clip,
            volume
        );
    }

    private void FinishIntro()
    {
        if (ghostR != null) Destroy(ghostR.gameObject);
        if (ghostC != null) Destroy(ghostC.gameObject);

        if (introVolume != null)
            introVolume.weight = 0f;

        if (crosshair != null)
            crosshair.SetActive(true);

        SetPlayerControls(true);

        Time.timeScale =
            previousTimeScale > 0f ? previousTimeScale : 1f;

        running = false;

        enabled = false;
    }

    private void DisableGameplay()
    {
        if (crosshair != null)
            crosshair.SetActive(false);

        SetPlayerControls(false);
    }

    private void SetPlayerControls(
        bool enabledState
    )
    {
        if (playerControls == null)
            return;

        foreach (
            Behaviour control
            in playerControls
        )
        {
            if (control != null)
                control.enabled =
                    enabledState;
        }
    }

    private void OnDisable()
    {
        if (!running)
            return;

        Time.timeScale =
            previousTimeScale > 0f ? previousTimeScale : 1f;

        SetPlayerControls(true);

        if (crosshair != null)
            crosshair.SetActive(true);
    }
}