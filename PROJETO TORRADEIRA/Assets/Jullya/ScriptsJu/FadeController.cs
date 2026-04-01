using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeController : MonoBehaviour
{
    public Image fadeImage;
    public float velocidade = 2f;

    void Start()
    {
        StartCoroutine(FadeIn());
    }

    public IEnumerator FadeIn()
    {
        Color cor = fadeImage.color;

        while (cor.a > 0)
        {
            cor.a -= Time.deltaTime * velocidade;
            fadeImage.color = cor;
            yield return null;
        }

        cor.a = 0;
        fadeImage.color = cor;
    }

    public IEnumerator FadeOut()
    {
        Color cor = fadeImage.color;

        while (cor.a < 1)
        {
            cor.a += Time.deltaTime * velocidade;
            fadeImage.color = cor;
            yield return null;
        }

        cor.a = 1;
        fadeImage.color = cor;
    }
}