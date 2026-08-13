using UnityEngine;
using System.Collections;

public class RadioSystem : MonoBehaviour
{
    [System.Serializable]
    public class RadioTransmission
    {
        public AudioClip audio;
        [TextArea(2, 5)]
        public string message;
    }

    public RadioMessageUI radioMessageUI;

    public RadioTransmission[] transmissions;

    public float minInterval = 20f;
    public float maxInterval = 45f;

    public bool playOnStart = true;

    private Coroutine radioCoroutine;

    void Start()
    {
        if (playOnStart)
        {
            radioCoroutine = StartCoroutine(
                RadioRoutine()
            );
        }
    }

    IEnumerator RadioRoutine()
    {
        yield return new WaitForSeconds(
            Random.Range(minInterval, maxInterval)
        );

        while (true)
        {
            PlayRandomTransmission();

            yield return new WaitForSeconds(
                Random.Range(minInterval, maxInterval)
            );
        }
    }

    void PlayRandomTransmission()
    {
        if (transmissions == null ||
            transmissions.Length == 0)
        {
            return;
        }

        RadioTransmission transmission =
            transmissions[
                Random.Range(
                    0,
                    transmissions.Length
                )
            ];

        if (radioMessageUI != null)
        {
            radioMessageUI.ShowMessage(
                transmission.message,
                transmission.audio
            );
        }
    }

    public void PlayTransmission(int index)
    {
        if (transmissions == null ||
            index < 0 ||
            index >= transmissions.Length)
        {
            return;
        }

        RadioTransmission transmission =
            transmissions[index];

        if (radioMessageUI != null)
        {
            radioMessageUI.ShowMessage(
                transmission.message,
                transmission.audio
            );
        }
    }

    public void StopRadio()
    {
        if (radioCoroutine != null)
        {
            StopCoroutine(radioCoroutine);
            radioCoroutine = null;
        }
    }

    public void StartRadio()
    {
        if (radioCoroutine == null)
        {
            radioCoroutine =
                StartCoroutine(
                    RadioRoutine()
                );
        }
    }
}