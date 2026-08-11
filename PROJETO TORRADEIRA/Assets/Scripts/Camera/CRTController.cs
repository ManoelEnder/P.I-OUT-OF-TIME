using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CRTController : MonoBehaviour
{
    [SerializeField] private UniversalRendererData rendererData;

    private FullScreenPassRendererFeature crtFeature;

    private void Awake()
    {
        if (rendererData == null)
        {
            Debug.LogError("CRT: Renderer Data não está associado.", this);
            return;
        }

        foreach (ScriptableRendererFeature feature in rendererData.rendererFeatures)
        {
            if (feature is FullScreenPassRendererFeature fullScreenFeature)
            {
                crtFeature = fullScreenFeature;
                break;
            }
        }

        if (crtFeature == null)
        {
            Debug.LogError("CRT: Full Screen Pass Renderer Feature não encontrada.", this);
            return;
        }

        Debug.Log("CRT: Full Screen Pass encontrado.", this);

        SetCRT(false);
    }

    public void SetCRT(bool enabled)
    {
        if (crtFeature == null)
        {
            Debug.LogError("CRT: Feature nula.", this);
            return;
        }

        crtFeature.SetActive(enabled);

        Debug.Log("CRT: " + (enabled ? "ATIVADO" : "DESATIVADO"), this);
    }
}