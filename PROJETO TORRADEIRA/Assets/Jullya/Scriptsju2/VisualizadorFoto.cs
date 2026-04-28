using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class VisualizadorFoto : MonoBehaviour
{
    public GameObject painel;
    public Image imagemGrande;

    private List<Sprite> fotos = new List<Sprite>();
    private int indexAtual = 0;

    void Update()
    {
        if (!painel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Fechar();
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Proxima();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Anterior();
        }
    }

    public void Abrir(List<Sprite> lista, int index)
    {
        fotos = lista;
        indexAtual = index;

        painel.SetActive(true);
        Mostrar();
    }

    void Mostrar()
    {
        if (fotos.Count > 0)
        {
            imagemGrande.sprite = fotos[indexAtual];
        }
    }

    public void Proxima()
    {
        if (fotos.Count == 0) return;

        indexAtual++;
        if (indexAtual >= fotos.Count)
            indexAtual = 0;

        Mostrar();
    }

    public void Anterior()
    {
        if (fotos.Count == 0) return;

        indexAtual--;
        if (indexAtual < 0)
            indexAtual = fotos.Count - 1;

        Mostrar();
    }

    public void Fechar()
    {
        painel.SetActive(false);
    }
}