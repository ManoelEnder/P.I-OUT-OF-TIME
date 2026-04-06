using UnityEngine;
using TMPro;

public class MontagemCamera : MonoBehaviour
{
    public Transform player;
    public float distancia = 3f;

    public GameObject lente;
    public GameObject bateria;
    public GameObject sensor;
    public GameObject circuito;
    public GameObject flash;
    public GameObject visor;
    public GameObject carcaca;

    public TMP_Text texto;
    public TMP_Text textoMissao;

    private int etapa = 0;
    private const int TOTAL_PECAS = 7;

    void Start()
    {
        // garante que tudo começa desligado
        lente.SetActive(false);
        bateria.SetActive(false);
        sensor.SetActive(false);
        circuito.SetActive(false);
        flash.SetActive(false);
        visor.SetActive(false);
        carcaca.SetActive(false);

        AtualizarMissao();
    }

    void Update()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= distancia && etapa < TOTAL_PECAS)
        {
            texto.text = "Aperte E para montar";

            if (Input.GetKeyDown(KeyCode.E))
                Montar();
        }
        else
        {
            texto.text = "";
        }
    }

    void Montar()
    {
        switch (etapa)
        {
            case 0:
                lente.SetActive(true);
                texto.text = "Lente instalada!";
                break;

            case 1:
                bateria.SetActive(true);
                texto.text = "Bateria instalada!";
                break;

            case 2:
                sensor.SetActive(true);
                texto.text = "Sensor instalado!";
                break;

            case 3:
                circuito.SetActive(true);
                texto.text = "Circuito instalado!";
                break;

            case 4:
                flash.SetActive(true);
                texto.text = "Flash instalado!";
                break;

            case 5:
                visor.SetActive(true);
                texto.text = "Visor instalado!";
                break;

            case 6:
                carcaca.SetActive(true);
                texto.text = "Carcaça instalada!";
                break;
        }

        etapa++;
        AtualizarMissao();

        if (etapa >= TOTAL_PECAS)
        {
            texto.text = "Câmera completa! O futuro foi restaurado!";
            GameManager.instancia.missaoCompleta = true;
        }
    }

    void AtualizarMissao()
    {
        if (etapa < TOTAL_PECAS)
            textoMissao.text = "Objetivo: Monte a câmera\nPeças: " + etapa + "/" + TOTAL_PECAS;
        else
            textoMissao.text = "Objetivo concluído!";
    }
}