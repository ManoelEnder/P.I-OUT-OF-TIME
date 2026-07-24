using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class FadeController : MonoBehaviour
{
    public Image fadeImage;
    public float velocidade = 1f;
    public float esperaAntesDoFade = 3f;
    public float tempoTexto = 3f;

    public TextMeshProUGUI textoHistoria;

    [Header("Efeito de Digitação")]
    public float velocidadeDigitacao = 0.05f;

    private string textoCompleto;


    void Start()
    {
        if (textoHistoria != null)
        {
            // Guarda o texto original
            textoCompleto = textoHistoria.text;

            // Começa vazio
            textoHistoria.text = "";
        }

        StartCoroutine(IniciarCena());
    }


    IEnumerator IniciarCena()
    {
        // Espera antes de começar
        yield return new WaitForSeconds(esperaAntesDoFade);


        // Texto aparece enquanto a tela ainda está preta
        if (textoHistoria != null)
        {
            textoHistoria.gameObject.SetActive(true);

            yield return StartCoroutine(DigitarTexto());
        }


        // Espera depois que terminar de escrever
        yield return new WaitForSeconds(tempoTexto);


        // Abre o fade depois do texto
        yield return StartCoroutine(FadeIn());


        // Esconde o texto
        if (textoHistoria != null)
            textoHistoria.gameObject.SetActive(false);
    }



    IEnumerator DigitarTexto()
    {
        textoHistoria.text = "";

        foreach (char letra in textoCompleto)
        {
            textoHistoria.text += letra;

            yield return new WaitForSeconds(velocidadeDigitacao);
        }
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