using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SistemaDialogo : MonoBehaviour
{
    [System.Serializable]
    public class Fala
    {
        public AudioClip audio;

        [TextArea(2, 5)]
        public string legenda;
    }

    public AudioSource audioSource;
    public TextMeshProUGUI textoLegenda;
    public Button botaoProximo;

    public Fala[] falas;

    private int falaAtual = 0;

    void Start()
    {
        botaoProximo.onClick.AddListener(ProximaFala);
        MostrarFala();
    }

    void Update()
    {
        
        botaoProximo.interactable = !audioSource.isPlaying;
    }

    void MostrarFala()
    {
        if (falaAtual >= falas.Length)
        {
            textoLegenda.text = "";
            botaoProximo.gameObject.SetActive(false);
            return;
        }

        textoLegenda.text = falas[falaAtual].legenda;

        audioSource.clip = falas[falaAtual].audio;
        audioSource.Play();

        botaoProximo.interactable = false;
    }

    public void ProximaFala()
    {
        if (audioSource.isPlaying)
            return;

        falaAtual++;
        MostrarFala();
    }
}