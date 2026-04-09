using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MissionSystem : MonoBehaviour
{
    public TextMeshProUGUI textoFotos;
    public TextMeshProUGUI textoPeca;

    public int fotosNecessarias = 5;

    int fotos = 0;
    bool pecaDescoberta = false;

    public string nomeDaCena = "Final";

    public Image fadeImage;
    public float fadeTime = 1.5f;

    void Start()
    {
        textoFotos.text = "[ ] Tirar 5 fotos";
        textoPeca.text = "[ ] Descobrir peça";

        textoFotos.color = Color.gray;
        textoPeca.color = Color.gray;

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    public void AddFoto()
    {
        fotos++;

        if (fotos >= fotosNecessarias)
        {
            textoFotos.text = "[X] Tirar 5 fotos";
            textoFotos.color = Color.white;

            ChecarMissoes();
        }
    }

    public void DescobriuPeca()
    {
        if (pecaDescoberta) return;

        pecaDescoberta = true;

        textoPeca.text = "[X] Descobrir peça";
        textoPeca.color = Color.white;

        ChecarMissoes();
    }

    void ChecarMissoes()
    {
        if (fotos >= fotosNecessarias && pecaDescoberta)
        {
            StartCoroutine(FadeAndLoad());
        }
    }

    IEnumerator FadeAndLoad()
    {
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;

            float alpha = Mathf.Lerp(0f, 1f, t / fadeTime);

            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;

            yield return null;
        }

        SceneManager.LoadScene(nomeDaCena);
    }
}