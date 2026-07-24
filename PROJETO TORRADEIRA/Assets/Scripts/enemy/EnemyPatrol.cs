using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    public Transform[] pontos;
    public Transform player;

    public float velocidade = 2f;
    public float velocidadePerseguicao = 4f;

    public float distanciaParar = 0.2f;
    public float distanciaPerseguir = 8f;

    private int pontoAtual = 0;
    private bool perseguindo = false;

    void Update()
    {
        float distanciaPlayer = Vector3.Distance(transform.position, player.position);

        // Detecta jogador
        if (distanciaPlayer <= distanciaPerseguir)
        {
            perseguindo = true;
        }
        else
        {
            perseguindo = false;
        }


        // Perseguição
        if (perseguindo)
        {
            SeguirPlayer();
        }
        else
        {
            Patrulhar();
        }
    }


    void Patrulhar()
    {
        Transform destino = pontos[pontoAtual];

        Vector3 direcao = destino.position - transform.position;

        transform.position += direcao.normalized * velocidade * Time.deltaTime;

        transform.LookAt(destino);


        if (Vector3.Distance(transform.position, destino.position) < distanciaParar)
        {
            pontoAtual++;

            if (pontoAtual >= pontos.Length)
            {
                pontoAtual = 0;
            }
        }
    }


    void SeguirPlayer()
    {
        Vector3 direcao = player.position - transform.position;

        transform.position += direcao.normalized * velocidadePerseguicao * Time.deltaTime;

        transform.LookAt(player);
    }
}