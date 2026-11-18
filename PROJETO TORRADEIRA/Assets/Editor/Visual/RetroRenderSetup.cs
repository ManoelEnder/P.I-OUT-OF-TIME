using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Visual retro do jogo inteiro (revisao, Parte 2). Reexecutavel.
// - Pixelizacao: a camera renderiza em resolucao reduzida e o URP amplia sem suavizar (Point);
//   o HUD em Canvas overlay continua em resolucao cheia. Tambem reduz o custo de GPU.
// - Sombras duras e mais curtas: combinam com pixel art e custam menos num laboratorio fechado.
public static class RetroRenderSetup
{
    // 0.5 em 1920x1080 = imagem de 960x540 (pixel de 2x2 na tela).
    public const float RenderScale = 0.5f;

    [MenuItem("Ferramentas/Visual/Aplicar render retro")]
    public static void Apply()
    {
        UniversalRenderPipelineAsset asset = ActiveAsset();
        if (asset == null)
        {
            Debug.LogError("RetroRenderSetup: nenhum URP asset ativo.");
            return;
        }

        Undo.RecordObject(asset, "Render retro");
        asset.renderScale = RenderScale;
        asset.upscalingFilter = UpscalingFilterSelection.Point;
        asset.msaaSampleCount = 1;
        asset.shadowCascadeCount = 2;
        asset.shadowDistance = 30f;

        var so = new SerializedObject(asset);
        so.FindProperty("m_MainLightShadowmapResolution").intValue = 1024;
        so.FindProperty("m_AdditionalLightsShadowmapResolution").intValue = 1024;
        so.FindProperty("m_SoftShadowsSupported").boolValue = false;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        Debug.Log("RETRO render: " + asset.name + " escala " + asset.renderScale + " filtro " + asset.upscalingFilter);
    }

    public static void ApplyFromCommandLine()
    {
        Apply();
        EditorApplication.Exit(0);
    }

    public static UniversalRenderPipelineAsset ActiveAsset()
    {
        return (QualitySettings.renderPipeline ?? GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;
    }
}
