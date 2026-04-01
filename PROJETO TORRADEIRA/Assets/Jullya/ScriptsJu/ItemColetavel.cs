using UnityEngine;
using TMPro;

public class ItemColetavel : MonoBehaviour
{
    public string itemNome;

    public float distancia = 3f; 
    public TMP_Text texto;
    public Transform player;

    private bool coletado = false;

    void Update()
    {
        if (coletado || player == null || texto == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        bool podeColetar = dist <= distancia;

        if (podeColetar)
        {
            texto.text = "Aperte E para coletar: " + itemNome;

            if (Input.GetKeyDown(KeyCode.E))
            {
                Coletar();
            }
        }
        else
        {
            texto.text = "";
        }
    }

    void Coletar()
    {
        coletado = true;
        texto.text = "";
        Destroy(gameObject);
    }
}