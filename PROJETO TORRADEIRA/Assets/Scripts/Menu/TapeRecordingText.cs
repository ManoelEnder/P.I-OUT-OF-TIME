using TMPro;
using UnityEngine;

// Texto de gravacao em fita: conta os 23 segundos que o prototipo de 1975
// levava para gravar uma foto, mostra "FOTO SALVA" e recomeca.
[RequireComponent(typeof(TMP_Text))]
public class TapeRecordingText : MonoBehaviour
{
    [SerializeField] private float recordSeconds = 23f;
    [SerializeField] private float holdSeconds = 3f;
    [SerializeField] private string recordingFormat = "GRAVANDO NA FITA {0:00}s";
    [SerializeField] private string savedText = "FOTO 100x100 SALVA";

    private TMP_Text text;
    private float start;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
        start = Time.unscaledTime - Random.value * recordSeconds;
    }

    private void Update()
    {
        float t = Time.unscaledTime - start;

        if (t > recordSeconds + holdSeconds)
        {
            start = Time.unscaledTime;
            t = 0f;
        }

        string value = t < recordSeconds
            ? string.Format(recordingFormat, Mathf.FloorToInt(t))
            : savedText;

        if (text.text != value)
            text.SetText(value);
    }
}
