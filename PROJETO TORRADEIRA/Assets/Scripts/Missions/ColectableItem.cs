
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
    public string nomeDaCena;
    public FadeController fade;
    public GameObject marcadorDetector;

    private bool coletado = false;

    void Update()
    {
        if (coletado || player == null || texto == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= distancia)
        {
            if (texto.text == "")
                texto.text = "Aperte E para coletar: " + itemNome;

            if (Input.GetKeyDown(KeyCode.E))
            {
                Coletar();
            }
        }
        else
        {
            if (!coletado)
                texto.text = "";
        }
    }

    public void Coletar()
    {
        if (coletado) return;

        coletado = true;
        texto.text = "Item coletado!";

        if (marcadorDetector != null)
        {
            marcadorDetector.SetActive(false);
        }

        MeshRenderer render = GetComponent<MeshRenderer>();
        if (render != null) render.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        StartCoroutine(MudarCena());
    }

    IEnumerator MudarCena()
    {
        if (fade != null)
        {
            fade.MostrarPainel();
        }

        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(nomeDaCena);
    }
}
