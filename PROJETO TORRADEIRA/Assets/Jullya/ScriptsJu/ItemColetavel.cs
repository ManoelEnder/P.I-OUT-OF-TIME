using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class ItemColetavel : MonoBehaviour
{
    public string itemNome;

    public float distancia = 3f;
    public TMP_Text texto;
    public Transform player;

    public float tempoParaMudarCena = 3f;
    public string nomeDaCena;

    private bool coletado = false;

    void Update()
    {
        if (coletado || player == null || texto == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= distancia)
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
        texto.text = "Item coletado!";

        // 🔥 esconder o objeto sem desativar ele
        if (GetComponent<MeshRenderer>() != null)
            GetComponent<MeshRenderer>().enabled = false;

        if (GetComponent<Collider>() != null)
            GetComponent<Collider>().enabled = false;

        // 🔥 AGORA funciona
        StartCoroutine(MudarCena());
    }

    IEnumerator MudarCena()
    {
        yield return new WaitForSeconds(tempoParaMudarCena);

        SceneManager.LoadScene(nomeDaCena);
    }
}