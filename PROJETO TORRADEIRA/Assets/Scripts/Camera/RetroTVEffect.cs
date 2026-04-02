using UnityEngine;
using UnityEngine.UI;

public class RetroTVEffect : MonoBehaviour
{
    public float speed = 50f;
    public float flickerSpeed = 10f;
    public float flickerAmount = 0.1f;

    RectTransform rect;
    Image img;
    Color baseColor;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        img = GetComponent<Image>();
        baseColor = img.color;
    }

    void Update()
    {
        rect.anchoredPosition += Vector2.up * speed * Time.deltaTime;

        if (rect.anchoredPosition.y > Screen.height)
        {
            rect.anchoredPosition = new Vector2(0, -Screen.height);
        }

        float flicker = 1 + Mathf.Sin(Time.time * flickerSpeed) * flickerAmount;
        img.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * flicker);
    }
}