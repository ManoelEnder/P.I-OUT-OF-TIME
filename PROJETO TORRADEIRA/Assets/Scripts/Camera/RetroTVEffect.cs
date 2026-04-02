using UnityEngine;
using UnityEngine.UI;

public class RetroTVEffect : MonoBehaviour
{
    public RectTransform noise;
    public RectTransform scanlines;

    public float noiseSpeed = 50f;
    public float scanSpeed = 20f;

    public Image noiseImg;
    public Image scanImg;

    float baseNoiseAlpha;
    float baseScanAlpha;

    void Start()
    {
        baseNoiseAlpha = noiseImg.color.a;
        baseScanAlpha = scanImg.color.a;
    }

    void Update()
    {
    
        noise.anchoredPosition += new Vector2(30f, noiseSpeed) * Time.deltaTime;

        scanlines.anchoredPosition += Vector2.up * scanSpeed * Time.deltaTime;

        if (noise.anchoredPosition.y > Screen.height)
            noise.anchoredPosition = new Vector2(0, -Screen.height);

        if (scanlines.anchoredPosition.y > Screen.height)
            scanlines.anchoredPosition = new Vector2(0, 0);

        float flicker = 1 + Mathf.Sin(Time.time * 12f) * 0.05f;

        noiseImg.color = new Color(1, 1, 1, baseNoiseAlpha * flicker);
        scanImg.color = new Color(1, 1, 1, baseScanAlpha * flicker);
    }
}