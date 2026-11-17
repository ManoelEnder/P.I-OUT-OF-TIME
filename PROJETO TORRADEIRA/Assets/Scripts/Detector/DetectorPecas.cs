
using UnityEngine;

public class DetectorPecas : MonoBehaviour
{
    public int distanciaMaxima = 10;
    public float intervaloLonge = 1.5f;
    public float intervaloPerto = 0.12f;

    public float volumeMinimo = 0.03f;
    public float volumeMaximo = 0.2f;

    public AudioClip somDetector;

    [Header("Bipe")]
    [Tooltip("Gera um bipe curto. Desligue para usar o Som Detector (precisa ser um clipe curto).")]
    public bool usarBipSintetizado = true;
    public float frequenciaBip = 1400f;
    public float duracaoBip = 0.07f;
    [Tooltip("Pitch quando a peca esta encostada (1 = longe).")]
    public float pitchPerto = 1.5f;

    private AudioClip bip;

    private Transform jogador;
    private AudioSource audioSource;
    private float tempoProximoBip;

    // Os marcadores so somem (SetActive false); basta atualizar a lista de vez em quando.
    private const float IntervaloBuscaMarcadores = 1f;
    private GameObject[] marcadores = new GameObject[0];
    private float proximaBuscaMarcadores;

    void Start()
    {
        if (Camera.main != null)
            jogador = Camera.main.transform;

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        if (usarBipSintetizado || somDetector == null)
            bip = CriarBip();
        audioSource.volume = 1f;
    }

    void Update()
    {
        AudioClip som = bip != null ? bip : somDetector;

        if (jogador == null || som == null)
            return;

        if (Time.unscaledTime >= proximaBuscaMarcadores)
        {
            marcadores = GameObject.FindGameObjectsWithTag("PecaDetector");
            proximaBuscaMarcadores = Time.unscaledTime + IntervaloBuscaMarcadores;
        }

        Transform marcadorMaisProximo = null;
        float menorDistancia = distanciaMaxima;

        foreach (GameObject marcador in marcadores)
        {
            if (marcador == null || !marcador.activeInHierarchy)
                continue;

            float distancia = Vector3.Distance(
                jogador.position,
                marcador.transform.position
            );

            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                marcadorMaisProximo = marcador.transform;
            }
        }

        if (marcadorMaisProximo == null)
        {
            tempoProximoBip = 0f;

            if (audioSource.isPlaying)
                audioSource.Stop();

            return;
        }

        float proximidade = 1f -
            (menorDistancia / distanciaMaxima);

        float intervalo = Mathf.Lerp(
            intervaloLonge,
            intervaloPerto,
            proximidade
        );

        float volume = Mathf.Lerp(
            volumeMinimo,
            volumeMaximo,
            proximidade
        );

        tempoProximoBip -= Time.deltaTime;

        if (tempoProximoBip <= 0f)
        {
            // Um bipe por vez: Play reinicia a fonte em vez de empilhar copias do som.
            audioSource.clip = som;
            audioSource.volume = volume;
            audioSource.pitch = Mathf.Lerp(1f, pitchPerto, proximidade);
            audioSource.Play();
            tempoProximoBip = intervalo;
        }
    }

    private AudioClip CriarBip()
    {
        int taxa = UnityEngine.AudioSettings.outputSampleRate > 0 ? UnityEngine.AudioSettings.outputSampleRate : 44100;
        int amostras = Mathf.Max(1, Mathf.RoundToInt(taxa * duracaoBip));
        float[] dados = new float[amostras];

        for (int i = 0; i < amostras; i++)
        {
            float t = (float)i / taxa;
            float envelope = Mathf.Min(1f, i / (taxa * 0.004f)) * (1f - (float)i / amostras);
            float onda = Mathf.Sin(2f * Mathf.PI * frequenciaBip * t) >= 0f ? 1f : -1f;
            dados[i] = onda * envelope * 0.5f;
        }

        AudioClip clip = AudioClip.Create("BipDetector", amostras, 1, taxa, false);
        clip.SetData(dados, 0);
        return clip;
    }

    private void OnDestroy()
    {
        if (bip != null)
            Destroy(bip);
    }

    public void DesativarMarcador(GameObject marcador)
    {
        if (marcador != null)
            marcador.SetActive(false);
    }
}
