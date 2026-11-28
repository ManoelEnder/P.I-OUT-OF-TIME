using TMPro;
using UnityEngine;

// Referencias de fonte, arte e sons do jogo usadas por telas montadas em codigo
// (carregamento, intro da viagem no tempo, radio do cientista). Fica em Resources/LoadingScreenArt.
public class LoadingScreenArt : ScriptableObject
{
    public TMP_FontAsset font;
    public Texture2D tapeDeck;
    public Texture2D tapeReel;

    [Header("Sons")]
    public AudioClip tapeRewind;
    public AudioClip tapeClick;
    public AudioClip radioStatic;
    public AudioClip doorCreak;
    public AudioClip cameraOn;
    public AudioClip cameraOff;
}
