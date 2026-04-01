using UnityEngine;
using TMPro;
using System.Collections;

public class ControleJogador : MonoBehaviour
{
    public CharacterController controller;
    public float velocidade = 6f;
    public float gravidade = -20f;
    public float pulo = 2f;

    private Vector3 velocidadeQueda;

    [Header("NPC")]
    public Transform npc;
    public float distancia = 3f;
    public TMP_Text texto;

    private bool digitando = false;
    private bool falou = false;

    private Coroutine rotinaTexto; 

    void Update()
    {
        
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 mover = transform.right * x + transform.forward * z;
        controller.Move(mover * velocidade * Time.deltaTime);

        
        if (controller.isGrounded && velocidadeQueda.y < 0)
        {
            velocidadeQueda.y = -2f;
        }

        
        if (Input.GetButtonDown("Jump") && controller.isGrounded)
        {
            velocidadeQueda.y = Mathf.Sqrt(pulo * -2f * gravidade);
        }

        velocidadeQueda.y += gravidade * Time.deltaTime;
        controller.Move(velocidadeQueda * Time.deltaTime);

      
        if (npc != null && texto != null)
        {
            float dist = Vector3.Distance(transform.position, npc.position);
            bool podeConversar = dist <= distancia;

            if (podeConversar)
            {
                if (!digitando && !falou)
                    texto.text = "Aperte E para conversar";

                if (Input.GetKeyDown(KeyCode.E) && !digitando && !falou)
                {
                    falou = true;
                    rotinaTexto = StartCoroutine(DigitarTexto("Viajante, precisamos da sua ajuda... A linha do tempo foi alterada e a primeira câmera digital criada com objetos recicláveis na empresa kodak em 1975 por Steve Sasson  deixou de existir. Sem ela, o futuro das fotos está instável. Você deve voltar ao passado e reconstruí-la. Nessa sala tem  uma câmera especial para tirar fotos, ela revelará objetos essenciais para montar a câmera novamente. Encontre todas as peças e restaure a invenção.Apos coletar a camera voce viajara para o passado.. boa sorte o futuro depende de você."));
                }
            }
            else
            {
                
                texto.text = "";
                falou = false;

                if (rotinaTexto != null)
                {
                    StopCoroutine(rotinaTexto);
                    rotinaTexto = null;
                }

                digitando = false;
            }
        }
    }

    IEnumerator DigitarTexto(string frase)
    {
        digitando = true;
        texto.text = "";

        foreach (char letra in frase.ToCharArray())
        {
            texto.text += letra;
            yield return new WaitForSeconds(0.05f);
        }

        digitando = false;
    }
}