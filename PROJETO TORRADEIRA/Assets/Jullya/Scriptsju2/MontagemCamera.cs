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

    public TMP_Text texto;
    public TMP_Text textoMissao;

    private int etapa = 0;

    void Start()
    {
        AtualizarMissao();
    }

    void Update()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= distancia)
        {
            if (etapa < 4)
                texto.text = "Aperte E para montar";

            if (Input.GetKeyDown(KeyCode.E))
            {
                Montar();
            }
        }
        else
        {
            texto.text = "";
        }
    }

    void Montar()
    {
        if (etapa == 0 && GameManager.instancia.temLente)
        {
            lente.SetActive(true);
            texto.text = "Lente instalada!";
            etapa++;
            AtualizarMissao();
            return;
        }

        if (etapa == 1 && GameManager.instancia.temBateria)
        {
            bateria.SetActive(true);
            texto.text = "Bateria instalada!";
            etapa++;
            AtualizarMissao();
            return;
        }

        if (etapa == 2 && GameManager.instancia.temSensor)
        {
            sensor.SetActive(true);
            texto.text = "Sensor instalado!";
            etapa++;
            AtualizarMissao();
            return;
        }

        if (etapa == 3 && GameManager.instancia.temCircuito)
        {
            circuito.SetActive(true);
            texto.text = "Circuito instalado!";
            etapa++;
            AtualizarMissao();
            return;
        }

  
        if (etapa >= 4)
        {
            texto.text = "Câmera completa! O futuro foi restaurado!";
            GameManager.instancia.missaoCompleta = true;
        }
    }

    void AtualizarMissao()
    {
        if (etapa < 4)
        {
            textoMissao.text = "Objetivo: Monte a câmera\nPeças: " + etapa + "/4";
        }
        else
        {
            textoMissao.text = "Objetivo concluído!";
        }
    }
}