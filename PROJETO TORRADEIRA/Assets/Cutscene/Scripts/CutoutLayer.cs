using TMPro;
using UnityEngine;

// Uma camada recortada da cutscene. O estado é função pura do tempo (Evaluate),
// então a mesma animação roda no jogo e na exportação da prévia pelo Editor.
public class CutoutLayer : MonoBehaviour
{
    public enum Entrance { Cut, Drop, Fade, Pop }

    [Header("Tempo (segundos após o início do plano 'anchor')")]
    public string anchor;
    public float enter;
    [Tooltip("< 0 = permanece até o grupo sair de cena")]
    public float exit = -1f;
    [Tooltip("Vazio = visível em todos os planos do grupo")]
    public string[] onlyIn;

    [Header("Entrada")]
    public Entrance entrance = Entrance.Drop;
    public float enterDuration = 0.55f;
    public float exitDuration = 0.2f;
    public float dropHeight = 7f;
    [Tooltip("Pequeno quique de acomodação na chegada (unidades)")]
    public float settle = 0.12f;

    [Header("Vida (após assentar)")]
    public float swayDegrees;
    public float swaySpeed = 0.5f;
    public float bob;
    public float bobSpeed = 0.7f;
    public float spin;
    [Range(0f, 1f)] public float flicker;
    public bool talk;
    public bool blink;
    [Tooltip("Gesto (graus) só enquanto há fala")]
    public float talkSway;

    [Header("Movimento (segundos após o anchor)")]
    public float moveStart;
    public float moveEnd;
    public Vector3 moveBy;
    public float rotateBy;
    public float scaleTo = 1f;
    public Vector2 fillX = Vector2.one;
    public float alphaTo = 1f;
    [Tooltip("0 = suave, 1 = linear")]
    [Range(0f, 1f)] public float linear;
    [Tooltip("Desacelera no fim (estilhaços, impactos)")]
    public bool easeOut;

    [Header("Quadros (granulação, estática, linhas de CRT)")]
    public Sprite[] frames;
    public float fps = 12f;

    [HideInInspector] public Vector3 basePos;
    [HideInInspector] public float baseRot;
    [HideInInspector] public Vector3 baseScale = Vector3.one;
    [HideInInspector] public float baseAlpha = 1f;
    [HideInInspector] public float phase;

    private SpriteRenderer sr;
    private TMP_Text text;

    public void Capture()
    {
        basePos = transform.localPosition;
        baseRot = transform.localEulerAngles.z;
        baseScale = transform.localScale;
        Cache();
        baseAlpha = sr != null ? sr.color.a : text != null ? text.alpha : 1f;
        phase = (Mathf.Abs(name.GetHashCode()) % 1000) / 1000f * 6.283f;
    }

    private void Cache()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (text == null) text = GetComponent<TMP_Text>();
    }

    // t = segundos desde o início do plano âncora; speaking = há fala ativa.
    public void Evaluate(float t, string shotId, bool speaking)
    {
        Cache();

        bool allowed = onlyIn == null || onlyIn.Length == 0 ||
                       System.Array.IndexOf(onlyIn, shotId) >= 0;

        float local = t - enter;
        float alpha = baseAlpha;
        Vector3 pos = basePos;
        float rot = baseRot;
        Vector3 scale = baseScale;

        if (!allowed || local < 0f || (exit >= 0f && t >= exit + exitDuration))
        {
            SetAlpha(0f);
            return;
        }

        float k = enterDuration <= 0f ? 1f : Mathf.Clamp01(local / enterDuration);
        switch (entrance)
        {
            case Entrance.Drop:
                // desacelera na chegada, passa um pouco do ponto e acomoda
                float drop = 1f - EaseOutCubic(k);
                pos.y += drop * dropHeight;
                rot += drop * 6f * Mathf.Sin(phase);
                float after = (local - enterDuration) / 0.4f;
                if (after > 0f && after < 1f)
                    pos.y -= settle * Mathf.Sin(after * Mathf.PI) * (1f - after);
                break;
            case Entrance.Fade:
                alpha *= k;
                break;
            case Entrance.Pop:
                scale *= Mathf.Lerp(0.6f, 1f, EaseOutBack(k));
                alpha *= Mathf.Clamp01(k * 3f);
                break;
        }

        if (exit >= 0f && t >= exit)
            alpha *= 1f - Mathf.Clamp01((t - exit) / Mathf.Max(0.0001f, exitDuration));

        // movimento roteirizado
        if (moveEnd > moveStart)
        {
            float m = Mathf.Clamp01((t - moveStart) / (moveEnd - moveStart));
            m = easeOut ? EaseOutCubic(m) : Mathf.Lerp(Mathf.SmoothStep(0f, 1f, m), m, linear);
            pos += moveBy * m;
            rot += rotateBy * m;
            scale *= Mathf.Lerp(1f, scaleTo, m);
            scale.x *= Mathf.Lerp(fillX.x, fillX.y, m);
            alpha *= Mathf.Lerp(1f, alphaTo, m);
        }
        else if (fillX != Vector2.one)
        {
            scale.x *= fillX.x;
        }

        // vida: só depois de assentar, para os pesados ficarem firmes
        float settled = Mathf.Clamp01((local - enterDuration) / 0.6f);
        float time = local + phase;
        rot += Mathf.Sin(time * swaySpeed * 6.283f) * swayDegrees * settled;
        pos.y += Mathf.Sin(time * bobSpeed * 6.283f) * bob * settled;
        rot += spin * local;

        if (flicker > 0f)
            alpha *= 1f - flicker * Step(Mathf.PerlinNoise(time * 9f, phase), 0.55f);

        if (talk)
            alpha *= speaking && Mathf.PerlinNoise(time * 11f, phase) > 0.42f ? 1f : 0f;
        if (blink)
            alpha *= Mathf.PerlinNoise(time * 1.7f, phase + 3f) > 0.78f ? 1f : 0f;
        if (speaking)
            rot += Mathf.Sin(time * 2.3f) * talkSway;

        if (frames != null && frames.Length > 0 && sr != null)
            sr.sprite = frames[Mathf.FloorToInt(Mathf.Max(0f, local) * fps) % frames.Length];

        transform.localPosition = pos;
        transform.localEulerAngles = new Vector3(0f, 0f, rot);
        transform.localScale = scale;
        SetAlpha(alpha);
    }

    private void SetAlpha(float a)
    {
        if (sr != null)
        {
            Color c = sr.color;
            c.a = a;
            sr.color = c;
        }
        else if (text != null)
        {
            text.alpha = a;
        }
    }

    private static float Step(float v, float edge) => v > edge ? 1f : 0f;
    private static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
