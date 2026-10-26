
using UnityEngine;

public class LinhaRadar : MonoBehaviour
{
    public float velocidade = 120f;
    public float comprimento = 80f;

    private RectTransform linha;

    void Start()
    {
        linha = GetComponent<RectTransform>();

        linha.anchorMin = new Vector2(0.5f, 0.5f);
        linha.anchorMax = new Vector2(0.5f, 0.5f);
        linha.pivot = new Vector2(0.5f, 0f);

        linha.anchoredPosition = Vector2.zero;

        linha.sizeDelta = new Vector2(
            linha.sizeDelta.x,
            comprimento
        );
    }

    void Update()
    {
        transform.Rotate(
            0f,
            0f,
            -velocidade * Time.deltaTime
        );
    }
}
