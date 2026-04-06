using UnityEngine;

public class RetroTVEffect : MonoBehaviour
{
    public RectTransform img1;
    public RectTransform img2;
    public float speed = 50f;

    float height;

    void Start()
    {
        height = img1.rect.height;

        img1.anchoredPosition = new Vector2(0, 0);
        img2.anchoredPosition = new Vector2(0, -height);
    }

    void Update()
    {
        float move = speed * Time.deltaTime;

        img1.anchoredPosition += new Vector2(0, move);
        img2.anchoredPosition += new Vector2(0, move);

        if (img1.anchoredPosition.y >= height)
        {
            img1.anchoredPosition = new Vector2(0, img2.anchoredPosition.y - height);
        }

        if (img2.anchoredPosition.y >= height)
        {
            img2.anchoredPosition = new Vector2(0, img1.anchoredPosition.y - height);
        }
    }
}