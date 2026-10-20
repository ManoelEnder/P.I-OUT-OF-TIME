
using UnityEngine;

public class DetectorPecas : MonoBehaviour
{
    public int distanciaMaxima = 10;
    public float intervaloLonge = 1.5f;
    public float intervaloPerto = 0.12f;

    public float volumeMinimo = 0.03f;
    public float volumeMaximo = 0.2f;

    public AudioClip somDetector;

    private Transform jogador;
    private AudioSource audioSource;
    private GameObject[] marcadores;

    private float tempoProximoBip;

    void Start()
    {
        if (Camera.main != null)
            jogador = Camera.main.transform;

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;

        marcadores = GameObject.FindGameObjectsWithTag("PecaDetector");
    }

    void Update()
    {
        if (jogador == null || somDetector == null)
            return;

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
            return;
        }

        float proximidade = 1f - (
            menorDistancia / distanciaMaxima
        );

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
            audioSource.PlayOneShot(somDetector, volume);
            tempoProximoBip = intervalo;
        }
    }

    public void DesativarMarcador(GameObject marcador)
    {
        if (marcador != null)
            marcador.SetActive(false);
    }
}
