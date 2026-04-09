using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class NPCMissao : MonoBehaviour
{
    public Transform player;
    public float distancia = 4f;
    public TMP_Text texto;

    public GameObject painelDialogo;
    public GameObject canvasFinal;

    public float velocidadeTexto = 0.07f;

    public string nomeCenaMenu = "Menu";

    private bool digitando = false;
    private bool pularTexto = false;
    private bool mostrandoFala = false;
    private Coroutine rotinaTexto;

    void Start()
    {
        
        if (player == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("Player");
            if (obj != null)
                player = obj.transform;
            else
                Debug.LogError("Player não encontrado! Coloque a tag 'Player'.");
        }

        if (canvasFinal != null)
            canvasFinal.SetActive(false);
    }

    void Update()
    {
        if (player == null) return;

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

                if (mostrandoFala)
                {
                    texto.text = "Aperte E para falar";
                    painelDialogo.SetActive(false);
                    mostrandoFala = false;
                    return;
                }

                if (rotinaTexto != null)
                    StopCoroutine(rotinaTexto);

                mostrandoFala = true;
                painelDialogo.SetActive(true);

                if (!GameManager.instancia.missaoAceita)
                {
                    GameManager.instancia.missaoAceita = true;

                    rotinaTexto = StartCoroutine(DigitarTexto(
                        "Viajante, precisamos da sua ajuda! Vá até a mesa e monte a câmera com as peças que você encontrou, depois volte aqui novamente."
                    ));
                }
                else if (GameManager.instancia.missaoCompleta)
                {
                    rotinaTexto = StartCoroutine(FinalDoJogo(
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
            painelDialogo.SetActive(false);

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

 
    IEnumerator FinalDoJogo(string frase)
    {
        yield return StartCoroutine(DigitarTexto(frase));

        yield return new WaitForSeconds(1.5f);

        painelDialogo.SetActive(false);

        if (canvasFinal != null)
            canvasFinal.SetActive(true);
        else
            Debug.LogError("Canvas Final não foi atribuído!");

   
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;
    }

  
    public void VoltarMenu()
    {
        Debug.Log("BOTÃO CLICADO");

        Time.timeScale = 1f;


        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (string.IsNullOrEmpty(nomeCenaMenu))
        {
            Debug.LogError("Nome da cena não definido!");
            return;
        }

        SceneManager.LoadScene(nomeCenaMenu);
    }
}