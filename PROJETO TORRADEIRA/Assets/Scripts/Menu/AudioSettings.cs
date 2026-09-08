using UnityEngine;

public class AudioSettings : MonoBehaviour
{
    private const string VolumeKey = "GameVolume";
    private const float DefaultVolume = 1f;

    public float Volume { get; private set; }

    private void Awake()
    {
        Load();
    }

    public void SetVolume(float value)
    {
        Volume = Mathf.Clamp01(value);
        AudioListener.volume = Volume;

        PlayerPrefs.SetFloat(VolumeKey, Volume);
        PlayerPrefs.Save();
    }

    public void Load()
    {
        Volume = PlayerPrefs.GetFloat(VolumeKey, DefaultVolume);
        AudioListener.volume = Volume;
    }

    public void ResetVolume()
    {
        SetVolume(DefaultVolume);
    }
}