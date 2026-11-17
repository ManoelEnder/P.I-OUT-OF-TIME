using TMPro;
using UnityEngine;

// Faz LEDs, lampadas, luzes e textos do menu piscarem.
// Usa MaterialPropertyBlock, entao varios LEDs podem dividir o mesmo material.
public class MenuBlinker : MonoBehaviour
{
    public enum Mode { Blink, Pulse, Flicker, Random }

    [SerializeField] private Mode mode = Mode.Blink;
    [SerializeField] private float period = 1f;
    [SerializeField, Range(0f, 1f)] private float duty = 0.5f;
    [SerializeField] private bool randomOffset = true;

    [Header("Alvos (opcionais)")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color onColor = Color.red;
    [SerializeField] private Color offColor = new Color(0.08f, 0.01f, 0.01f, 1f);
    [SerializeField] private Light targetLight;
    [SerializeField] private float lightMin = 0f;
    [SerializeField] private TMP_Text targetText;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private MaterialPropertyBlock block;
    private float lightMax;
    private float offset;
    private float nextRandom;
    private bool randomState;

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        offset = randomOffset ? Random.value * 10f : 0f;

        if (targetLight != null)
            lightMax = targetLight.intensity;
    }

    private void Update()
    {
        float level = Evaluate(Time.unscaledTime + offset);

        if (targetRenderer != null)
        {
            targetRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, Color.Lerp(offColor, onColor, level));
            targetRenderer.SetPropertyBlock(block);
        }

        if (targetLight != null)
            targetLight.intensity = Mathf.Lerp(lightMin, lightMax, level);

        if (targetText != null)
            targetText.alpha = Mathf.Lerp(0.05f, 1f, level);
    }

    private float Evaluate(float t)
    {
        float p = Mathf.Max(0.01f, period);

        switch (mode)
        {
            case Mode.Pulse:
                return 0.5f + 0.5f * Mathf.Sin(t / p * Mathf.PI * 2f);

            case Mode.Flicker:
                // Lampada fluorescente cansada: quase sempre acesa, com quedas curtas.
                float n = Mathf.PerlinNoise(t * 6f / p, offset);
                return n > 0.22f ? 1f : (Mathf.PerlinNoise(t * 40f, offset) > 0.5f ? 0.6f : 0.05f);

            case Mode.Random:
                if (Time.unscaledTime >= nextRandom)
                {
                    randomState = Random.value < duty;
                    nextRandom = Time.unscaledTime + p * Random.Range(0.4f, 1.6f);
                }
                return randomState ? 1f : 0f;

            default:
                return Mathf.Repeat(t / p, 1f) < duty ? 1f : 0f;
        }
    }
}
