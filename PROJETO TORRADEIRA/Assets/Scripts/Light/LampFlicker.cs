using UnityEngine;

// Lampada com defeito: quase sempre acesa, com crises curtas de pisca-pisca de tempos em tempos
// e (algumas) um zumbido leve de intensidade. Com flashes desligados so esmaece, sem piscar.
// Adicionada pelo RandomEvents em parte das luzes da fase.
public class LampFlicker : MonoBehaviour
{
    public Vector2 burstEvery = new Vector2(4f, 14f);
    public Vector2 burstLength = new Vector2(0.25f, 1.1f);
    public bool buzz;

    private Light lamp;
    private float baseIntensity;
    private float nextBurst;
    private float burstEnd;
    private float nextToggle;
    private bool off;
    private float seed;

    private void Start()
    {
        lamp = GetComponent<Light>();
        if (lamp == null)
        {
            enabled = false;
            return;
        }

        baseIntensity = lamp.intensity;
        seed = Random.value * 100f;
        nextBurst = Time.time + Random.Range(1f, burstEvery.y);
    }

    private void Update()
    {
        if (lamp == null)
            return;

        float level = 1f;
        if (buzz)
            level *= 0.93f + 0.07f * Mathf.PerlinNoise(seed, Time.time * 9f);

        if (Time.time >= nextBurst)
        {
            burstEnd = Time.time + Random.Range(burstLength.x, burstLength.y);
            nextBurst = burstEnd + Random.Range(burstEvery.x, burstEvery.y);
        }

        if (Time.time < burstEnd)
        {
            if (FlashSettings.Enabled)
            {
                if (Time.time >= nextToggle)
                {
                    off = !off && Random.value < 0.6f;
                    nextToggle = Time.time + Random.Range(0.03f, 0.11f);
                }
                level *= off ? Random.Range(0f, 0.12f) : 1f;
            }
            else
            {
                level *= 0.5f;
            }
        }
        else
        {
            off = false;
        }

        lamp.intensity = baseIntensity * level;
    }

    private void OnDisable()
    {
        if (lamp != null)
            lamp.intensity = baseIntensity;
    }
}
