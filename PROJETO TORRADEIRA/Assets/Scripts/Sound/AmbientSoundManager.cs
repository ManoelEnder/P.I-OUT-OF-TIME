using UnityEngine;
using System.Collections;

public class AmbientSoundManager : MonoBehaviour
{
    [Header("Sons ambientes aleatórios")]
    public AudioClip[] ambientSounds;

    [Header("Tempo entre os sons")]
    public float minInterval = 5f;
    public float maxInterval = 15f;

    [Header("Volume")]
    [Range(0f, 1f)]
    public float volume = 0.6f;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        StartCoroutine(PlayRandomAmbientSound());
    }

    IEnumerator PlayRandomAmbientSound()
    {
        while (true)
        {
            float waitTime = Random.Range(minInterval, maxInterval);

            yield return new WaitForSeconds(waitTime);

            if (ambientSounds != null && ambientSounds.Length > 0)
            {
                int randomIndex = Random.Range(0, ambientSounds.Length);

                AudioClip selectedSound = ambientSounds[randomIndex];

                audioSource.PlayOneShot(selectedSound, volume);
            }
        }
    }
}