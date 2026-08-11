using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CRTController : MonoBehaviour
{
    [SerializeField] private UniversalRendererData rendererData;

    private FullScreenPassRendererFeature crtFeature;

    private void Awake()
    {
        FindCRTFeature();

        SetCRT(false);
    }

    private void FindCRTFeature()
    {
        if (rendererData == null)
        {
            Debug.LogError(
                "CRTController: Renderer Data não foi associado.",
                this
            );

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
            Debug.LogError(
                "CRTController: Full Screen Pass Renderer Feature não encontrada.",
                this
            );
        }
    }

    public void SetCRT(bool enabled)
    {
        if (crtFeature == null)
            return;

        crtFeature.SetActive(enabled);
    }
}