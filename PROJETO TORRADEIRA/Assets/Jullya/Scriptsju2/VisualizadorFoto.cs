using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class VisualizadorFoto : MonoBehaviour
{
    public GameObject painel;
    public Image imagemGrande;

    private List<Sprite> fotos = new List<Sprite>();
    private List<Color> cores = new List<Color>();
    private int indexAtual = 0;

    private float zoom = 1f;
    public float velocidadeZoom = 5f;
    public float zoomMin = 0.5f;
    public float zoomMax = 3f;

    void Update()
    {
        if (!painel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            painel.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            Proxima();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            Anterior();
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
        if (fotos == null || fotos.Count == 0) return;

        imagemGrande.sprite = fotos[indexAtual];

        if (cores != null && cores.Count > indexAtual)
            imagemGrande.color = cores[indexAtual];
    }

    void Proxima()
    {
        if (fotos == null || fotos.Count == 0) return;

        indexAtual++;
        if (indexAtual >= fotos.Count)
            indexAtual = 0;

        zoom = 1f;
        imagemGrande.rectTransform.localScale = Vector3.one;

        Mostrar();
    }

    void Anterior()
    {
        if (fotos == null || fotos.Count == 0) return;

        indexAtual--;
        if (indexAtual < 0)
            indexAtual = fotos.Count - 1;

        zoom = 1f;
        imagemGrande.rectTransform.localScale = Vector3.one;

        Mostrar();
    }
}