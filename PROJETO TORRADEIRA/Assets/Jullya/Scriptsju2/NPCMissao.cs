using UnityEngine;
using TMPro;
using System.Collections;

public class NPCMissao : MonoBehaviour
{
    public Transform player;
    public float distancia = 4f;
    public TMP_Text texto;

    public float velocidadeTexto = 0.07f;

    private bool digitando = false;
    private bool pularTexto = false;
    private bool mostrandoFala = false; 
    private Coroutine rotinaTexto;

    void Update()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= distancia)
        {
            
            if (!digitando && !mostrandoFala)
                texto.text = "Aperte E para falar";

            if (Input.GetKeyDown(KeyCode.E))
            {
             
                if (digitando)
                {
                    pularTexto = true;
                    return;
                }

                if (rotinaTexto != null)
                    StopCoroutine(rotinaTexto);

                mostrandoFala = true;

                if (!GameManager.instancia.missaoAceita)
                {
                    GameManager.instancia.missaoAceita = true;

                    rotinaTexto = StartCoroutine(DigitarTexto(
                        "Viajante, precisamos da sua ajuda! Vá até a mesa e monte a câmera com as peças que você encontrou. depois volte aqui novamente"
                    ));
                }
                else if (GameManager.instancia.missaoCompleta)
                {
                    rotinaTexto = StartCoroutine(DigitarTexto(
                        "Parabéns! Você conseguiu reconstruir a câmera! O futuro foi restaurado!"
                    ));
                }
                else
                {
                    rotinaTexto = StartCoroutine(DigitarTexto(
                        "Você ainda não terminou. Vá até a mesa!"
                    ));
                }
            }
        }
        else
        {
            
            texto.text = "";
            mostrandoFala = false;

            if (rotinaTexto != null)
            {
                StopCoroutine(rotinaTexto);
                rotinaTexto = null;
            }

            digitando = false;
        }
    }

    IEnumerator DigitarTexto(string frase)
    {
        digitando = true;
        pularTexto = false;
        texto.text = "";

        foreach (char letra in frase)
        {
            if (pularTexto)
            {
                texto.text = frase;
                break;
            }

            texto.text += letra;
            yield return new WaitForSeconds(velocidadeTexto);
        }

        digitando = false;

        
    }
}