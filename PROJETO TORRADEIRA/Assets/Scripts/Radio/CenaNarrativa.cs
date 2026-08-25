using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class CenaNarrativa : MonoBehaviour
{
    public AudioSource audioNarracao;
    public AudioSource audioRadio;

    [Header("Áudios da narrativa")]
    public AudioClip[] audios;

    [Header("Legendas")]
    [TextArea(2, 5)]
    public string[] legendas;

    public TextMeshProUGUI textoLegenda;
    public Button botaoProximo;

    public MonoBehaviour controleDoJogador;
    public GameObject painelNarrativa;
    public GameObject botaoPular;
    public string cenaDoJogo;

    private int falaAtual = 0;
    private bool narrativaTerminou = false;
    private bool audioTerminou = false;

    void Start()
    {
        if (controleDoJogador != null)
            controleDoJogador.enabled = false;

        if (painelNarrativa != null)
            painelNarrativa.SetActive(true);

        if (botaoPular != null)
            botaoPular.SetActive(false);

        if (audioRadio != null)
        {
            audioRadio.loop = true;
            audioRadio.Play();
        }

        if (botaoProximo != null)
        {
            botaoProximo.onClick.RemoveAllListeners();
            botaoProximo.onClick.AddListener(ProximaFala);

            
            botaoProximo.gameObject.SetActive(false);
            botaoProximo.interactable = false;
        }

        if (textoLegenda != null)
            textoLegenda.text = "";

        Invoke("IniciarNarracao", 4f);
    }

    void Update()
    {
        if (narrativaTerminou)
            return;

       
        if (!audioTerminou &&
            audioNarracao != null &&
            audioNarracao.clip != null &&
            !audioNarracao.isPlaying)
        {
            audioTerminou = true;

            
            if (botaoProximo != null)
                botaoProximo.interactable = true;
        }
    }

    void IniciarNarracao()
    {
        falaAtual = 0;
        TocarFala();
    }

    void TocarFala()
    {
        if (falaAtual >= audios.Length)
        {
            IniciarJogo();
            return;
        }

        audioTerminou = false;

       
        audioNarracao.clip = audios[falaAtual];

       
        audioNarracao.Play();

        
        if (textoLegenda != null)
        {
            if (falaAtual < legendas.Length)
                textoLegenda.text = legendas[falaAtual];
            else
                textoLegenda.text = "";
        }

      
        if (botaoProximo != null)
        {
            botaoProximo.gameObject.SetActive(true);
            botaoProximo.interactable = false;
        }

        if (botaoPular != null)
            botaoPular.SetActive(true);
    }

    public void ProximaFala()
    {
       
        if (!audioTerminou)
            return;

        falaAtual++;

        if (falaAtual >= audios.Length)
        {
            IniciarJogo();
        }
        else
        {
            TocarFala();
        }
    }

    public void PularNarrativa()
    {
        IniciarJogo();
    }

    void IniciarJogo()
    {
        if (narrativaTerminou)
            return;

        narrativaTerminou = true;

        if (audioNarracao != null)
            audioNarracao.Stop();

        if (audioRadio != null)
            audioRadio.Stop();

        if (textoLegenda != null)
            textoLegenda.text = "";

        if (botaoProximo != null)
            botaoProximo.gameObject.SetActive(false);

        SceneManager.LoadScene(cenaDoJogo);
    }
}