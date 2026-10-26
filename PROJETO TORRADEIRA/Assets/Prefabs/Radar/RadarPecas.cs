
using UnityEngine;

public class RadarPecas : MonoBehaviour
{
    public Camera cameraJogador;
    public float raioRadar = 90f;
    public string tagMarcador = "PecaDetector";

    private RectTransform ponto;
    private GameObject marcadorMaisProximo;

    void Start()
    {
        ponto = GetComponent<RectTransform>();

        if (cameraJogador == null)
            cameraJogador = Camera.main;

        ponto.anchorMin = new Vector2(0.5f, 0.5f);
        ponto.anchorMax = new Vector2(0.5f, 0.5f);
        ponto.pivot = new Vector2(0.5f, 0.5f);

        ponto.anchoredPosition = Vector2.zero;
    }

    void Update()
    {
        if (cameraJogador == null || ponto == null)
            return;

        marcadorMaisProximo = null;
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
            ponto.gameObject.SetActive(false);
            return;
        }

        ponto.gameObject.SetActive(true);

        Vector3 direcao =
            marcadorMaisProximo.transform.position -
            cameraJogador.transform.position;

        Vector3 frente = cameraJogador.transform.forward;
        Vector3 direita = cameraJogador.transform.right;

        frente.y = 0f;
        direita.y = 0f;
        direcao.y = 0f;

        if (direcao.sqrMagnitude < 0.001f)
            return;

        frente.Normalize();
        direita.Normalize();
        direcao.Normalize();

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

        limite = Mathf.Max(0f, limite);

        float raio = Mathf.Min(raioRadar, limite);

        ponto.anchoredPosition =
            direcaoRadar.normalized * raio;
    }
}
