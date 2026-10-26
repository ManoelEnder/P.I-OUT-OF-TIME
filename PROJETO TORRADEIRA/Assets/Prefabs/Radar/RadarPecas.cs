
using UnityEngine;
using UnityEngine.UI;

public class RadarPecas : MonoBehaviour
{
    public Camera cameraJogador;
    public float raioRadar = 90f;
    public string tagMarcador = "PecaDetector";

    [Header("Piscar do LED")]
    public float intervaloPiscar = 0.25f;

    private RectTransform ponto;
    private Image imagemPonto;
    private float tempoPiscar;
    private bool ledLigado = true;

    void Start()
    {
        ponto = GetComponent<RectTransform>();
        imagemPonto = GetComponent<Image>();

        if (cameraJogador == null)
            cameraJogador = Camera.main;

        ponto.anchorMin = new Vector2(0.5f, 0.5f);
        ponto.anchorMax = new Vector2(0.5f, 0.5f);
        ponto.pivot = new Vector2(0.5f, 0.5f);

        ponto.anchoredPosition = Vector2.zero;
    }

    void Update()
    {
        if (cameraJogador == null || ponto == null || imagemPonto == null)
            return;

        GameObject marcadorMaisProximo = null;
        float menorDistancia = Mathf.Infinity;

        GameObject[] marcadores =
            GameObject.FindGameObjectsWithTag(tagMarcador);

        foreach (GameObject marcador in marcadores)
        {
            if (marcador == null || !marcador.activeInHierarchy)
                continue;

            float distancia = Vector3.Distance(
                cameraJogador.transform.position,
                marcador.transform.position
            );

            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                marcadorMaisProximo = marcador;
            }
        }

        if (marcadorMaisProximo == null)
        {
            imagemPonto.enabled = false;
            return;
        }

        Vector3 direcao =
            marcadorMaisProximo.transform.position -
            cameraJogador.transform.position;

        direcao.y = 0f;

        Vector3 frente = cameraJogador.transform.forward;
        Vector3 direita = cameraJogador.transform.right;

        frente.y = 0f;
        direita.y = 0f;

        if (direcao.sqrMagnitude < 0.001f)
            return;

        direcao.Normalize();
        frente.Normalize();
        direita.Normalize();

        Vector2 direcaoRadar = new Vector2(
            Vector3.Dot(direcao, direita),
            Vector3.Dot(direcao, frente)
        );

        RectTransform circulo =
            ponto.parent.GetComponent<RectTransform>();

        if (circulo == null)
            return;

        float limite = Mathf.Min(
            circulo.rect.width,
            circulo.rect.height
        ) / 2f - ponto.rect.width / 2f;

        float raio = Mathf.Min(
            raioRadar,
            Mathf.Max(0f, limite)
        );

        ponto.anchoredPosition =
            direcaoRadar.normalized * raio;

        tempoPiscar -= Time.deltaTime;

        if (tempoPiscar <= 0f)
        {
            ledLigado = !ledLigado;
            imagemPonto.enabled = ledLigado;
            tempoPiscar = Mathf.Max(0.05f, intervaloPiscar);
        }
    }
}
