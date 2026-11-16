using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Linha do tempo central da cutscene de abertura: planos, falas, eventos de áudio e saída.
// Edite os tempos aqui (Inspector do objeto "Diretor" na cena IntroCutscene).
public class IntroCutsceneDirector : MonoBehaviour
{
    [Serializable]
    public class Shot
    {
        public string id;
        public float start;
        public Transform group;
        public Vector2 camFrom;
        public float sizeFrom = 5.4f;
        public Vector2 camTo;
        public float sizeTo = 5.4f;
        [Tooltip("Fade a partir do preto no início do plano (s). 0 = corte seco")]
        public float fadeIn;
        public float shake;
    }

    [Serializable]
    public class Line
    {
        public float start;
        public AudioClip clip;
        [Tooltip("Usada quando não há áudio gravado")]
        public float duration = 3f;
        [TextArea(2, 4)] public string text;
        public float Length => clip != null ? clip.length : duration;
    }

    [Serializable]
    public class Cue
    {
        public string note;
        public float time;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Header("Linha do tempo")]
    public float duration = 112f;
    public string nextScene = "Principal";
    public Shot[] shots;
    public Line[] lines;
    public Cue[] cues;
    [Tooltip("Volume da trilha ao longo do tempo (abaixa no impacto e nas instruções)")]
    public AnimationCurve musicVolume = AnimationCurve.Constant(0f, 112f, 0.5f);
    public AnimationCurve ambienceVolume = AnimationCurve.Constant(0f, 112f, 0.4f);

    [Header("Cena")]
    public Camera cam;
    public Transform overlay;
    public SpriteRenderer fade;
    public TMP_Text subtitle;
    public SpriteRenderer subtitleBack;
    public TMP_Text skipHint;

    [Header("Áudio")]
    public AudioSource voice;
    public AudioSource music;
    public AudioSource ambience;
    public AudioSource sfx;

    [Header("Saída")]
    public float exitFade = 0.4f;
    public float skipConfirmWindow = 3f;

    private readonly Dictionary<Transform, CutoutLayer[]> layers = new Dictionary<Transform, CutoutLayer[]>();
    private readonly Dictionary<string, float> shotStart = new Dictionary<string, float>();
    private float time;
    private float skipHintUntil = -1f;
    private bool exiting;
    private CursorLockMode savedLock;
    private bool savedVisible;

    public float Time01 => time / duration;

    public void Init()
    {
        layers.Clear();
        shotStart.Clear();
        foreach (Shot s in shots)
        {
            shotStart[s.id] = s.start;
            if (s.group != null && !layers.ContainsKey(s.group))
                layers[s.group] = s.group.GetComponentsInChildren<CutoutLayer>(true);
        }
        if (overlay != null)
            layers[overlay] = overlay.GetComponentsInChildren<CutoutLayer>(true);
    }

    private void Start()
    {
        Init();
        UnityEngine.Time.timeScale = 1f;
        savedLock = Cursor.lockState;
        savedVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (MusicManager.instance != null)
            MusicManager.instance.FadeOut(1.5f);

        if (music != null) { music.loop = true; music.volume = 0f; music.Play(); }
        if (ambience != null) { ambience.loop = true; ambience.volume = 0f; ambience.Play(); }
        if (skipHint != null) skipHint.alpha = 0f;

        Evaluate(0f);
    }

    private void Update()
    {
        if (exiting)
            return;

        float prev = time;
        time += UnityEngine.Time.unscaledDeltaTime;

        foreach (Cue c in cues)
            if (c.clip != null && c.time > prev && c.time <= time)
                sfx.PlayOneShot(c.clip, c.volume);

        foreach (Line l in lines)
            if (l.clip != null && l.start > prev && l.start <= time)
            {
                voice.clip = l.clip;
                voice.Play();
            }

        if (music != null) music.volume = musicVolume.Evaluate(time);
        if (ambience != null) ambience.volume = ambienceVolume.Evaluate(time);

        Evaluate(time);
        HandleSkip();

        if (time >= duration - exitFade)
            Finish();
    }

    private void HandleSkip()
    {
        bool pressed =
            (Keyboard.current != null &&
             (Keyboard.current.escapeKey.wasPressedThisFrame ||
              Keyboard.current.spaceKey.wasPressedThisFrame ||
              Keyboard.current.enterKey.wasPressedThisFrame)) ||
            (Gamepad.current != null &&
             (Gamepad.current.startButton.wasPressedThisFrame ||
              Gamepad.current.buttonSouth.wasPressedThisFrame));

        if (pressed)
        {
            if (time < skipHintUntil)
            {
                Finish();
                return;
            }
            skipHintUntil = time + skipConfirmWindow;
        }

        if (skipHint != null)
            skipHint.alpha = time < skipHintUntil ? Mathf.Clamp01((skipHintUntil - time) * 2f) : 0f;
    }

    // Saída única para fim natural e pulo: inicia a jogabilidade uma só vez.
    public void Finish()
    {
        if (exiting)
            return;
        exiting = true;
        StartCoroutine(Exit());
    }

    private IEnumerator Exit()
    {
        float startAlpha = fade != null ? fade.color.a : 0f;
        float v0 = voice != null ? voice.volume : 0f;
        float m0 = music != null ? music.volume : 0f;
        float a0 = ambience != null ? ambience.volume : 0f;

        for (float e = 0f; e < exitFade; e += UnityEngine.Time.unscaledDeltaTime)
        {
            float k = e / exitFade;
            SetFade(Mathf.Lerp(startAlpha, 1f, k));
            if (voice != null) voice.volume = v0 * (1f - k);
            if (music != null) music.volume = m0 * (1f - k);
            if (ambience != null) ambience.volume = a0 * (1f - k);
            yield return null;
        }

        SetFade(1f);
        Cursor.lockState = savedLock;
        Cursor.visible = savedVisible;
        UnityEngine.Time.timeScale = 1f;
        SceneManager.LoadScene(nextScene);
    }

    public int ShotIndexAt(float t)
    {
        int index = 0;
        for (int i = 0; i < shots.Length; i++)
            if (shots[i].start <= t)
                index = i;
        return index;
    }

    public bool IsSpeaking(float t)
    {
        foreach (Line l in lines)
            if (t >= l.start && t < l.start + l.Length)
                return true;
        return false;
    }

    // Determinístico: usado pelo jogo e pela exportação de prévia no Editor.
    public void Evaluate(float t)
    {
        if (layers.Count == 0)
            Init();

        int index = ShotIndexAt(t);
        Shot shot = shots[index];
        float end = index + 1 < shots.Length ? shots[index + 1].start : duration;
        bool speaking = IsSpeaking(t);

        foreach (var pair in layers)
        {
            bool active = pair.Key == shot.group || pair.Key == overlay;
            if (pair.Key.gameObject.activeSelf != active)
                pair.Key.gameObject.SetActive(active);
            if (!active)
                continue;

            foreach (CutoutLayer layer in pair.Value)
            {
                float anchor = !string.IsNullOrEmpty(layer.anchor) && shotStart.TryGetValue(layer.anchor, out float a)
                    ? a
                    : shot.start;
                layer.Evaluate(t - anchor, shot.id, speaking);
            }
        }

        if (cam != null)
        {
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - shot.start) / Mathf.Max(0.01f, end - shot.start)));
            Vector2 p = Vector2.Lerp(shot.camFrom, shot.camTo, k);
            if (shot.shake > 0f)
                p += new Vector2(Mathf.PerlinNoise(t * 25f, 0f) - 0.5f, Mathf.PerlinNoise(0f, t * 25f) - 0.5f) * shot.shake;
            cam.transform.localPosition = new Vector3(p.x, p.y, -10f);
            cam.orthographicSize = Mathf.Lerp(shot.sizeFrom, shot.sizeTo, k);
            if (overlay != null)
                overlay.localScale = Vector3.one * (cam.orthographicSize / 5.4f);
        }

        float black = shot.fadeIn > 0f ? 1f - Mathf.Clamp01((t - shot.start) / shot.fadeIn) : 0f;
        SetFade(black);

        if (subtitle != null)
        {
            string text = string.Empty;
            foreach (Line l in lines)
                if (t >= l.start && t < l.start + l.Length + 0.3f)
                    text = l.text;
            subtitle.text = text;
            if (subtitleBack != null)
            {
                Color c = subtitleBack.color;
                c.a = text.Length > 0 ? 0.5f : 0f;
                subtitleBack.color = c;
            }
        }
    }

    private void SetFade(float a)
    {
        if (fade == null)
            return;
        Color c = fade.color;
        c.a = a;
        fade.color = c;
    }
}
