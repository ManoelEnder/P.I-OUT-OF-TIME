using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class AlbumFotos : MonoBehaviour
{
    public GameObject painel;
    public Transform container;
    public GameObject fotoPrefab;
    public VisualizadorFoto visualizador;

    private List<Image> imagens = new List<Image>();
    private List<Color> cores = new List<Color>();
    private int indexSelecionado = -1;

    void Start()
    {
        painel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (visualizador != null && imagens.Count > 0)
            {
                List<Sprite> listaSprites = new List<Sprite>();

                foreach (var img in imagens)
                {
                    listaSprites.Add(img.sprite);
                }

                visualizador.Abrir(listaSprites, cores, 0);
            }
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            painel.SetActive(!painel.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            AdicionarFoto();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (indexSelecionado >= 0 && visualizador != null)
            {
                List<Sprite> listaSprites = new List<Sprite>();

                foreach (var img in imagens)
                {
                    listaSprites.Add(img.sprite);
                }

                visualizador.Abrir(listaSprites, cores, indexSelecionado);
            }
        }
    }

    public void AdicionarFoto()
    {
        GameObject novaFoto = Instantiate(fotoPrefab, container, false);

        Image img = novaFoto.GetComponent<Image>();

        Color corAleatoria = new Color(Random.value, Random.value, Random.value, 1f);
        img.color = corAleatoria;

        imagens.Add(img);
        cores.Add(corAleatoria);

        int index = imagens.Count - 1;

        Button botao = novaFoto.GetComponent<Button>();
        if (botao == null)
            botao = novaFoto.AddComponent<Button>();

        botao.onClick.RemoveAllListeners();
        botao.onClick.AddListener(() => Selecionar(index));
    }

    void Selecionar(int index)
    {
        indexSelecionado = index;
        Debug.Log("Selecionou: " + index);
    }
}