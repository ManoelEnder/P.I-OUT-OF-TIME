using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CenaNarrativa : MonoBehaviour
{
    [Header("Áudio da Narrativa")]
    [SerializeField] private AudioSource audioNarracao;
    [SerializeField] private AudioClip[] audios;

    [Header("Rádio")]
    [SerializeField] private AudioSource audioRadio;
    [SerializeField] private float tempoFadeRadio = 3f;

    [Header("Legendas")]
    [TextArea(2, 5)]
    [SerializeField] private string[] legendas;

    [SerializeField] private TextMeshProUGUI textoLegenda;

    [Header("UI")]
    [SerializeField] private GameObject painelNarrativa;

    [Header("Jogador")]
    [SerializeField] private MonoBehaviour controleDoJogador;

    [Header("Cena")]
    [SerializeField] private string cenaDoJogo;

    [Header("Texto")]
    [SerializeField] private float velocidadeTexto = 0.035f;

    [Header("Transição")]
    [SerializeField] private float intervaloEntreFalas = 0.25f;

    [Header("Música")]
    [SerializeField] private float tempoFadeOutMusica = 2.5f;

    private int falaAtual;
    private bool narrativaTerminou;
    private bool textoTerminou;
    private bool aguardandoProximaFala;

    private Coroutine textoCoroutine;
    private Coroutine narrativaCoroutine;

    private void Start()
    {
        PrepararCena();

        if (MusicManager.instance != null)
            MusicManager.instance.FadeOut(tempoFadeOutMusica);

        ConfigurarRadio();

        narrativaCoroutine = StartCoroutine(IniciarNarrativa());
    }

    private void Update()
    {
        if (narrativaTerminou)
            return;

        HandleMouseInput();
        VerificarFimDoAudio();
    }

    private void PrepararCena()
    {
        if (controleDoJogador != null)
            controleDoJogador.enabled = false;

        if (painelNarrativa != null)
            painelNarrativa.SetActive(true);

        if (textoLegenda != null)
            textoLegenda.text = string.Empty;
    }

    private void ConfigurarRadio()
    {
        if (audioRadio == null)
            return;

        audioRadio.loop = true;
        audioRadio.volume = 0f;
        audioRadio.Play();

        StartCoroutine(FadeInRadio());
    }

    private IEnumerator FadeInRadio()
    {
        float tempo = 0f;

        while (tempo < tempoFadeRadio)
        {
            tempo += Time.unscaledDeltaTime;

            float progresso = tempo / tempoFadeRadio;
            audioRadio.volume = Mathf.Lerp(0f, 1f, progresso);

            yield return null;
        }

        audioRadio.volume = 1f;
    }

    private IEnumerator IniciarNarrativa()
    {
        yield return new WaitForSecondsRealtime(4f);

        falaAtual = 0;
        TocarFala();
    }

    private void TocarFala()
    {
        if (narrativaTerminou)
            return;

        if (falaAtual >= audios.Length)
        {
            IniciarJogo();
            return;
        }

        aguardandoProximaFala = false;
        textoTerminou = false;

        TocarAudio();
        IniciarTexto();
    }

    private void TocarAudio()
    {
        if (audioNarracao == null)
            return;

        if (falaAtual >= audios.Length)
            return;

        AudioClip audioAtual = audios[falaAtual];

        if (audioAtual == null)
            return;

        audioNarracao.clip = audioAtual;
        audioNarracao.Play();
    }

    private void IniciarTexto()
    {
        if (textoLegenda == null)
            return;

        if (textoCoroutine != null)
            StopCoroutine(textoCoroutine);

        string legendaAtual = falaAtual < legendas.Length
            ? legendas[falaAtual]
            : string.Empty;

        textoCoroutine = StartCoroutine(EfeitoTexto(legendaAtual));
    }

    private IEnumerator EfeitoTexto(string texto)
    {
        textoLegenda.text = string.Empty;

        foreach (char caractere in texto)
        {
            textoLegenda.text += caractere;

            yield return new WaitForSecondsRealtime(
                velocidadeTexto
            );
        }

        textoCoroutine = null;
        textoTerminou = true;

        VerificarProximaFala();
    }

    private void VerificarFimDoAudio()
    {
        if (audioNarracao == null)
            return;

        if (audioNarracao.isPlaying)
            return;

        if (audioNarracao.clip == null)
            return;

        if (!textoTerminou)
            return;

        if (aguardandoProximaFala)
            return;

        aguardandoProximaFala = true;

        StartCoroutine(IniciarProximaFala());
    }

    private void VerificarProximaFala()
    {
        if (audioNarracao == null)
            return;

        if (audioNarracao.isPlaying)
            return;

        if (aguardandoProximaFala)
            return;

        aguardandoProximaFala = true;

        StartCoroutine(IniciarProximaFala());
    }

    private IEnumerator IniciarProximaFala()
    {
        yield return new WaitForSecondsRealtime(
            intervaloEntreFalas
        );

        if (narrativaTerminou)
            yield break;

        falaAtual++;

        if (falaAtual >= audios.Length)
        {
            IniciarJogo();
            yield break;
        }

        TocarFala();
    }

    private void HandleMouseInput()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        AvancarNarrativa();
    }

    private void AvancarNarrativa()
    {
        if (narrativaTerminou)
            return;

        if (!textoTerminou)
        {
            TerminarTextoInstantaneamente();
            return;
        }

        if (aguardandoProximaFala)
            return;

        if (textoCoroutine != null)
        {
            StopCoroutine(textoCoroutine);
            textoCoroutine = null;
        }

        if (audioNarracao != null)
            audioNarracao.Stop();

        aguardandoProximaFala = true;

        falaAtual++;

        if (falaAtual >= audios.Length)
        {
            IniciarJogo();
            return;
        }

        TocarFala();
    }

    private void TerminarTextoInstantaneamente()
    {
        if (textoCoroutine != null)
        {
            StopCoroutine(textoCoroutine);
            textoCoroutine = null;
        }

        if (textoLegenda == null)
            return;

        textoLegenda.text = falaAtual < legendas.Length
            ? legendas[falaAtual]
            : string.Empty;

        textoTerminou = true;
    }

    private void IniciarJogo()
    {
        if (narrativaTerminou)
            return;

        narrativaTerminou = true;

        if (textoCoroutine != null)
        {
            StopCoroutine(textoCoroutine);
            textoCoroutine = null;
        }

        if (narrativaCoroutine != null)
        {
            StopCoroutine(narrativaCoroutine);
            narrativaCoroutine = null;
        }

        if (audioNarracao != null)
            audioNarracao.Stop();

        if (audioRadio != null)
            audioRadio.Stop();

        if (textoLegenda != null)
            textoLegenda.text = string.Empty;

        if (controleDoJogador != null)
            controleDoJogador.enabled = true;

        if (painelNarrativa != null)
            painelNarrativa.SetActive(false);

        SceneManager.LoadScene(cenaDoJogo);
    }
}