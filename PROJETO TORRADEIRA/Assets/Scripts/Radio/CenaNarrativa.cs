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

    [Header("Efeito de Pulinho")]
    [SerializeField] private float alturaPulo = 8f;
    [SerializeField] private float duracaoPulo = 0.12f;

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
        {
            textoLegenda.text = string.Empty;
            textoLegenda.ForceMeshUpdate();
        }
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
            audioRadio.volume = Mathf.Lerp(0f, 0.20f, progresso);

            yield return null;
        }

        audioRadio.volume = 0.20f;
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

        string legendaAtual =
            falaAtual < legendas.Length
                ? legendas[falaAtual]
                : string.Empty;

        textoCoroutine =
            StartCoroutine(
                EfeitoTexto(legendaAtual)
            );
    }

    private IEnumerator EfeitoTexto(string texto)
    {
        textoLegenda.text = texto;
        textoLegenda.ForceMeshUpdate();

        TMP_TextInfo textInfo =
            textoLegenda.textInfo;

        int totalCaracteres =
            textInfo.characterCount;

        textoLegenda.maxVisibleCharacters = 0;

        for (int i = 0; i < totalCaracteres; i++)
        {
            if (!textInfo.characterInfo[i].isVisible)
            {
                textoLegenda.maxVisibleCharacters = i + 1;

                yield return new WaitForSecondsRealtime(
                    velocidadeTexto
                );

                continue;
            }

            textoLegenda.maxVisibleCharacters = i + 1;

            textoLegenda.ForceMeshUpdate();

            textInfo = textoLegenda.textInfo;

            TMP_CharacterInfo characterInfo =
                textInfo.characterInfo[i];

            int materialIndex =
                characterInfo.materialReferenceIndex;

            int vertexIndex =
                characterInfo.vertexIndex;

            Vector3[] vertices =
                textInfo.meshInfo[materialIndex].vertices;

            Vector3[] originalVertices =
                new Vector3[4];

            for (int j = 0; j < 4; j++)
            {
                originalVertices[j] =
                    vertices[vertexIndex + j];
            }

            float tempo = 0f;

            while (tempo < duracaoPulo)
            {
                tempo += Time.unscaledDeltaTime;

                float progresso =
                    Mathf.Clamp01(
                        tempo / duracaoPulo
                    );

                float altura =
                    Mathf.Sin(
                        progresso * Mathf.PI
                    ) * alturaPulo;

                for (int j = 0; j < 4; j++)
                {
                    vertices[vertexIndex + j] =
                        originalVertices[j] +
                        Vector3.up * altura;
                }

                textoLegenda.UpdateVertexData(
                    TMP_VertexDataUpdateFlags.Vertices
                );

                yield return null;
            }

            for (int j = 0; j < 4; j++)
            {
                vertices[vertexIndex + j] =
                    originalVertices[j];
            }

            textoLegenda.UpdateVertexData(
                TMP_VertexDataUpdateFlags.Vertices
            );

            yield return new WaitForSecondsRealtime(
                velocidadeTexto
            );
        }

        textoLegenda.maxVisibleCharacters =
            totalCaracteres;

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

        textoLegenda.text =
            falaAtual < legendas.Length
                ? legendas[falaAtual]
                : string.Empty;

        textoLegenda.maxVisibleCharacters = -1;

        textoLegenda.ForceMeshUpdate();

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
        {
            textoLegenda.text = string.Empty;
            textoLegenda.maxVisibleCharacters = -1;
        }

        if (controleDoJogador != null)
            controleDoJogador.enabled = true;

        if (painelNarrativa != null)
            painelNarrativa.SetActive(false);

        SceneManager.LoadScene(cenaDoJogo);
    }
}