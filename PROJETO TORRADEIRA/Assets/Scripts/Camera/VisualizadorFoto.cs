
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class VisualizadorFoto : MonoBehaviour
{
    public GameObject painel;
    public Image imagemGrande;
    public GameObject textoApagado;
    public GameObject painelConfirmacao;
    public AlbumFotos albumFotos;

    private List<Sprite> fotos = new List<Sprite>();
    private List<Color> cores = new List<Color>();

    private int indexAtual = 0;

    private float zoom = 1f;
    public float velocidadeZoom = 5f;
    public float zoomMin = 0.5f;
    public float zoomMax = 3f;

    private bool confirmando = false;

    void Update()
    {
        if (!painel.activeSelf) return;

        if (confirmando)
        {
            if (Input.GetKeyDown(KeyCode.Y))
            {
                ConfirmarExclusao();
            }

            if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelarExclusao();
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            Proxima();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            Anterior();
        }

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            painelConfirmacao.SetActive(true);
            confirmando = true;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0f)
        {
            zoom += scroll * velocidadeZoom;
            zoom = Mathf.Clamp(zoom, zoomMin, zoomMax);
            imagemGrande.rectTransform.localScale = Vector3.one * zoom;
        }
    }

    public void Abrir(List<Sprite> listaSprites, List<Color> listaCores, int index)
    {
        if (listaSprites == null || listaSprites.Count == 0) return;

        fotos = listaSprites;
        cores = listaCores;

        indexAtual = Mathf.Clamp(index, 0, fotos.Count - 1);

        painel.SetActive(true);

        zoom = 1f;
        imagemGrande.rectTransform.localScale = Vector3.one;

        Mostrar();
    }

    void Mostrar()
    {
        if (fotos == null || fotos.Count == 0)
        {
            painel.SetActive(false);
            return;
        }

        imagemGrande.sprite = fotos[indexAtual];

        if (cores != null && cores.Count > indexAtual)
            imagemGrande.color = cores[indexAtual];
    }

    void Proxima()
    {
        if (fotos.Count == 0) return;

        indexAtual++;

        if (indexAtual >= fotos.Count)
            indexAtual = 0;

        zoom = 1f;
        imagemGrande.rectTransform.localScale = Vector3.one;

        Mostrar();
    }

    void Anterior()
    {
        if (fotos.Count == 0) return;

        indexAtual--;

        if (indexAtual < 0)
            indexAtual = fotos.Count - 1;

        zoom = 1f;
        imagemGrande.rectTransform.localScale = Vector3.one;

        Mostrar();
    }

    void ConfirmarExclusao()
    {
        if (fotos.Count == 0) return;

        int indiceExcluido = indexAtual;

        if (albumFotos != null)
        {
            albumFotos.RemoverFoto(indiceExcluido);

            fotos = albumFotos.ObterSprites();
            cores = albumFotos.ObterCores();
        }

        painelConfirmacao.SetActive(false);
        confirmando = false;

        StartCoroutine(MostrarMensagem());

        if (fotos.Count == 0)
        {
            painel.SetActive(false);
            return;
        }

        if (indexAtual >= fotos.Count)
            indexAtual = fotos.Count - 1;

        zoom = 1f;
        imagemGrande.rectTransform.localScale = Vector3.one;

        Mostrar();
    }

    void CancelarExclusao()
    {
        painelConfirmacao.SetActive(false);
        confirmando = false;
    }

    IEnumerator MostrarMensagem()
    {
        textoApagado.SetActive(true);

        yield return new WaitForSeconds(2f);

        textoApagado.SetActive(false);
    }
}
