using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class AmbientSoundManager : MonoBehaviour
{
    [Header("Sons ambientes")]
    [SerializeField] private AudioClip[] ambientSounds;

    [Header("Intervalo")]
    [SerializeField] private float minInterval = 8f;
    [SerializeField] private float maxInterval = 25f;

    [Header("Repetição")]
    [SerializeField] private int recentSoundsMemory = 3;

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
    private readonly List<int> recentSounds = new List<int>();

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
        yield return new WaitForSeconds(
            Random.Range(minInterval, maxInterval)
        );

        while (true)
        {
            if (HasValidSounds())
            {
                PlayRandomSound();
            }

            float waitTime = Random.Range(
                minInterval,
                maxInterval
            );

            yield return new WaitForSeconds(waitTime);
        }
    }

    private void PlayRandomSound()
    {
        int soundIndex = GetRandomSoundIndex();

        if (soundIndex == -1)
            return;

        AudioClip selectedSound = ambientSounds[soundIndex];

        if (selectedSound == null)
            return;

        AddToRecentSounds(soundIndex);

        audioSource.volume = GetRandomVolume();
        audioSource.pitch = Random.Range(minPitch, maxPitch);

        audioSource.PlayOneShot(
            selectedSound,
            audioSource.volume
        );
    }

    private int GetRandomSoundIndex()
    {
        List<int> availableSounds = new List<int>();

        for (int i = 0; i < ambientSounds.Length; i++)
        {
            if (ambientSounds[i] == null)
                continue;

            if (recentSounds.Contains(i))
                continue;

            availableSounds.Add(i);
        }

        if (availableSounds.Count == 0)
        {
            recentSounds.Clear();

            for (int i = 0; i < ambientSounds.Length; i++)
            {
                if (ambientSounds[i] != null)
                    availableSounds.Add(i);
            }
        }

        if (availableSounds.Count == 0)
            return -1;

        return availableSounds[
            Random.Range(0, availableSounds.Count)
        ];
    }

    private void AddToRecentSounds(int soundIndex)
    {
        recentSounds.Add(soundIndex);

        while (recentSounds.Count > recentSoundsMemory)
        {
            recentSounds.RemoveAt(0);
        }
    }

    private float GetRandomVolume()
    {
        float minimumVolume = Mathf.Max(
            0f,
            volume - volumeVariation
        );

        float maximumVolume = Mathf.Min(
            1f,
            volume + volumeVariation
        );

        return Random.Range(
            minimumVolume,
            maximumVolume
        );
    }

    private bool HasValidSounds()
    {
        if (ambientSounds == null || ambientSounds.Length == 0)
            return false;

        foreach (AudioClip sound in ambientSounds)
        {
            if (sound != null)
                return true;
        }

        return false;
    }
}