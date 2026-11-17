using UnityEngine;

// Cintilacao sutil de tubo CRT: varia a luz da tela e o alfa da interface.
public class CrtFlicker : MonoBehaviour
{
    [SerializeField] private Light screenLight;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField, Range(0f, 0.5f)] private float amount = 0.08f;
    [SerializeField] private float speed = 9f;

    private float baseIntensity;
    private float seed;

    private void Awake()
    {
        seed = Random.value * 100f;

        if (screenLight != null)
            baseIntensity = screenLight.intensity;
    }

    private void Update()
    {
        float t = Time.unscaledTime * speed + seed;
        float noise = Mathf.PerlinNoise(t, seed) * 2f - 1f;

        // Quedas rapidas e raras de sinal.
        if (Mathf.PerlinNoise(seed, t * 0.3f) > 0.86f)
            noise -= 0.6f;

        float k = 1f + noise * amount;

        if (screenLight != null)
            screenLight.intensity = baseIntensity * k;

        if (canvasGroup != null)
            canvasGroup.alpha = Mathf.Clamp01(0.92f + noise * amount);
    }
}
