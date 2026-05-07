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
    private List<Sprite> sprites = new List<Sprite>();

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
                visualizador.Abrir(sprites, cores, 0);
            }
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            painel.SetActive(!painel.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (indexSelecionado >= 0 && visualizador != null)
            {
                visualizador.Abrir(sprites, cores, indexSelecionado);
            }
        }
    }

    public void AdicionarFoto(Texture2D texture)
    {
        Sprite novaSprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        GameObject novaFoto = Instantiate(fotoPrefab, container, false);

        Image img = novaFoto.GetComponent<Image>();

        img.sprite = novaSprite;
        img.color = Color.white;

        imagens.Add(img);
        sprites.Add(novaSprite);
        cores.Add(Color.white);

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