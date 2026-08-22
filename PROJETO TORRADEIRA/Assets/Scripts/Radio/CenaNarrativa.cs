using UnityEngine;
using UnityEngine.SceneManagement;

public class CenaNarrativa : MonoBehaviour
{
    public AudioSource audioNarracao;
    public MonoBehaviour controleDoJogador;
    public GameObject painelNarrativa;
    public GameObject botaoPular;
    public string cenaDoJogo;

    private bool narrativaTerminou = false;

    void Start()
    {
        if (controleDoJogador != null)
            controleDoJogador.enabled = false;

        if (painelNarrativa != null)
            painelNarrativa.SetActive(true);

        if (botaoPular != null)
            botaoPular.SetActive(false);

        Invoke("IniciarNarracao", 5f);
    }

    void Update()
    {
        if (!narrativaTerminou &&
            audioNarracao != null &&
            audioNarracao.clip != null &&
            !audioNarracao.isPlaying &&
            audioNarracao.time > 0f)
        {
            IniciarJogo();
        }
    }

    void IniciarNarracao()
    {
        if (audioNarracao != null)
            audioNarracao.Play();

        if (botaoPular != null)
            botaoPular.SetActive(true);
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

        SceneManager.LoadScene(cenaDoJogo);
    }
}