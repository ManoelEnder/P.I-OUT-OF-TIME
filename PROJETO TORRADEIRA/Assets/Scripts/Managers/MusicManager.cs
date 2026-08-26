using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip musica;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        audioSource.clip = musica;
        audioSource.loop = true;
        audioSource.Play();
    }

    public void FadeOut(float duration)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeOutCoroutine(duration));
    }

    private IEnumerator FadeOutCoroutine(float duration)
    {
        float startVolume = audioSource.volume;
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;

            float progress = time / duration;

            audioSource.volume = Mathf.Lerp(
                startVolume,
                0f,
                progress
            );

            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();

        fadeCoroutine = null;
    }
}