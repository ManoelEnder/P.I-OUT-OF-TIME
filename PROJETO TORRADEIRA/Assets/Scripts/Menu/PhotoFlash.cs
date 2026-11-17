using System;
using System.Collections;
using UnityEngine;

// Camera fotografica dos anos 70 do cenario: dispara o flash de tempos em tempos.
// Respeita FlashSettings: com flashes desligados toca so o obturador e acende um LED pequeno.
public class PhotoFlash : MonoBehaviour
{
    [Header("Disparo automatico")]
    [SerializeField] private bool autoFire = true;
    [SerializeField] private Vector2 interval = new Vector2(6f, 12f);

    [Header("Flash")]
    [SerializeField] private Light flashLight;
    [SerializeField] private float lightIntensity = 60f;
    [SerializeField] private Renderer flashRenderer;
    [SerializeField] private Color flashOn = new Color(1f, 1f, 1f) * 12f;
    [SerializeField] private Color flashOff = new Color(0.12f, 0.12f, 0.12f);
    [SerializeField] private float flashDuration = 0.08f;
    [SerializeField] private float fadeDuration = 0.12f;

    [Header("LED de pronto (modo sem flash)")]
    [SerializeField] private Renderer readyLed;
    [SerializeField] private Color ledOn = new Color(1f, 0.08f, 0.05f) * 4f;
    [SerializeField] private Color ledOff = new Color(0.35f, 0.02f, 0.02f);

    [Header("Som")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Range(0f, 1f)] private float volume = 0.6f;

    public event Action Fired;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static AudioClip shutterClip;

    private MaterialPropertyBlock block;
    private float nextShot;
    private Coroutine running;

    private void Awake()
    {
        block = new MaterialPropertyBlock();

        if (flashLight != null)
        {
            flashLight.intensity = 0f;
            flashLight.enabled = false;
        }

        SetColor(flashRenderer, flashOff);
        SetColor(readyLed, ledOff);
        ScheduleNext();
    }

    private void Update()
    {
        if (autoFire && Time.unscaledTime >= nextShot)
        {
            Fire();
            ScheduleNext();
        }
    }

    public void Fire()
    {
        PlayShutter();

        if (running != null)
            StopCoroutine(running);

        running = StartCoroutine(FlashSettings.Enabled ? Flash() : LedOnly());
        Fired?.Invoke();
    }

    private void ScheduleNext()
    {
        nextShot = Time.unscaledTime + UnityEngine.Random.Range(interval.x, interval.y);
    }

    private IEnumerator Flash()
    {
        if (flashLight != null)
        {
            flashLight.enabled = true;
            flashLight.intensity = lightIntensity;
        }

        SetColor(flashRenderer, flashOn);
        SetColor(readyLed, ledOff);

        yield return new WaitForSecondsRealtime(flashDuration);

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(t / fadeDuration);
            k *= k;

            if (flashLight != null)
                flashLight.intensity = lightIntensity * k;

            SetColor(flashRenderer, Color.Lerp(flashOff, flashOn, k));
            yield return null;
        }

        if (flashLight != null)
        {
            flashLight.intensity = 0f;
            flashLight.enabled = false;
        }

        SetColor(flashRenderer, flashOff);

        // LED de "carregando" enquanto o capacitor recarrega.
        yield return LedOnly();
    }

    private IEnumerator LedOnly()
    {
        SetColor(readyLed, ledOn);
        yield return new WaitForSecondsRealtime(0.6f);
        SetColor(readyLed, ledOff);
        running = null;
    }

    private void SetColor(Renderer target, Color color)
    {
        if (target == null)
            return;

        target.GetPropertyBlock(block);
        block.SetColor(BaseColorId, color);
        target.SetPropertyBlock(block);
    }

    private void PlayShutter()
    {
        if (audioSource == null)
            return;

        if (shutterClip == null)
            shutterClip = BuildShutterClip();

        audioSource.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
        audioSource.PlayOneShot(shutterClip, volume);
    }

    // Obturador sintetizado: dois estalos mecanicos (abre/fecha), o "pop" do flash
    // e o apito baixinho do capacitor recarregando.
    private static AudioClip BuildShutterClip()
    {
        const int rate = 44100;
        int length = (int)(rate * 0.9f);
        float[] data = new float[length];
        var random = new System.Random(1975);

        for (int i = 0; i < length; i++)
        {
            float t = i / (float)rate;
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            float s = 0f;

            s += Click(t, 0.000f, noise, 0.9f);
            s += Click(t, 0.055f, noise, 0.7f);

            // Pop grave do tubo de flash.
            if (t < 0.04f)
                s += Mathf.Sin(2f * Mathf.PI * 140f * t) * Mathf.Exp(-t * 90f) * 0.5f;

            // Recarga: apito subindo, bem baixo.
            if (t > 0.12f)
            {
                float u = (t - 0.12f) / 0.78f;
                float freq = Mathf.Lerp(2200f, 6200f, u * u);
                s += Mathf.Sin(2f * Mathf.PI * freq * t) * 0.035f * Mathf.Sin(Mathf.PI * u);
            }

            data[i] = Mathf.Clamp(s, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("PhotoShutter", length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static float Click(float t, float start, float noise, float gain)
    {
        float d = t - start;
        if (d < 0f || d > 0.03f)
            return 0f;

        return noise * Mathf.Exp(-d * 260f) * gain;
    }
}
