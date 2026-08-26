using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class AmbientSoundManager : MonoBehaviour
{
    [Header("Sons ambientes")]
    [SerializeField] private AudioClip[] ambientSounds;

    [Header("Intervalo")]
    [SerializeField] private float minInterval = 5f;
    [SerializeField] private float maxInterval = 15f;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float volume = 0.6f;
    [SerializeField, Range(0f, 1f)] private float volumeVariation = 0.15f;

    [Header("Distância")]
    [SerializeField] private float minDistance = 5f;
    [SerializeField] private float maxDistance = 35f;

    [Header("Eco")]
    [SerializeField, Range(0f, 1f)] private float reverbZoneMix = 1f;

    [Header("Pitch")]
    [SerializeField, Range(0.8f, 1.2f)] private float minPitch = 0.95f;
    [SerializeField, Range(0.8f, 1.2f)] private float maxPitch = 1.05f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.reverbZoneMix = reverbZoneMix;
    }

    private void Start()
    {
        StartCoroutine(PlayRandomAmbientSound());
    }

    private IEnumerator PlayRandomAmbientSound()
    {
        while (true)
        {
            float waitTime = Random.Range(
                minInterval,
                maxInterval
            );

            yield return new WaitForSeconds(waitTime);

            if (ambientSounds == null || ambientSounds.Length == 0)
                continue;

            int randomIndex = Random.Range(
                0,
                ambientSounds.Length
            );

            AudioClip selectedSound = ambientSounds[randomIndex];

            if (selectedSound == null)
                continue;

            float randomVolume = Random.Range(
                volume - volumeVariation,
                volume + volumeVariation
            );

            audioSource.volume = Mathf.Clamp01(randomVolume);

            audioSource.pitch = Random.Range(
                minPitch,
                maxPitch
            );

            audioSource.PlayOneShot(selectedSound);
        }
    }
}