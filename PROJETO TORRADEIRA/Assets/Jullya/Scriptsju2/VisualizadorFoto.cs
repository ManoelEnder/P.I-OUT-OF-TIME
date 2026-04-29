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
        if (lista == null || lista.Count == 0) return;

        fotos = lista;
        indexAtual = Mathf.Clamp(index, 0, fotos.Count - 1);

        painel.SetActive(true);
        Mostrar();
    }

    void Mostrar()
    {
        if (fotos == null || fotos.Count == 0) return;

        imagemGrande.sprite = fotos[indexAtual];
        imagemGrande.color = new Color(Random.value, Random.value, Random.value);
    }

    public void Proxima()
    {
        if (fotos == null || fotos.Count == 0) return;

        indexAtual++;

        if (indexAtual >= fotos.Count)
            indexAtual = 0;

        Mostrar();
    }

    public void Anterior()
    {
        if (fotos == null || fotos.Count == 0) return;

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