using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class FadeController : MonoBehaviour
{
    [Header("Painel de Transição")]
    public GameObject painelPreto;

    public float esperaAntesDoFade = 3f;
    public float tempoTexto = 3f;

    public TextMeshProUGUI textoHistoria;

    [Header("Efeito de Digitação")]
    public float velocidadeDigitacao = 0.05f;
    private string textoCompleto;

    void Start()
    {
        
        if (painelPreto != null)
            painelPreto.SetActive(true);

        if (textoHistoria != null)
        {
          
            textoCompleto = textoHistoria.text;
           
            textoHistoria.text = "";
        }

        StartCoroutine(IniciarCena());
    }

    IEnumerator IniciarCena()
    {
       
        yield return new WaitForSeconds(esperaAntesDoFade);

        
        if (textoHistoria != null)
        {
            textoHistoria.gameObject.SetActive(true);
            yield return StartCoroutine(DigitarTexto());
        }

        
        yield return new WaitForSeconds(tempoTexto);

       
        EsconderPainel();

      
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

    public void MostrarPainel()
    {
        if (painelPreto != null)
            painelPreto.SetActive(true);
    }

    public void EsconderPainel()
    {
        if (painelPreto != null)
            painelPreto.SetActive(false);
    }
}