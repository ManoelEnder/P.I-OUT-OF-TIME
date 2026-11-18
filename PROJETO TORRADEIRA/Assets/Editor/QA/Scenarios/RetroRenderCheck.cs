using System.Collections;
using UnityEngine.Rendering.Universal;

// O jogo renderiza em resolucao reduzida com ampliacao Point (imagem pixelada, HUD nitido).
public class RetroRenderCheck : QAScenario
{
    public override string Name => "RetroRenderCheck";
    public override bool NeedsPlayMode => false;

    public override IEnumerator Run()
    {
        UniversalRenderPipelineAsset asset = RetroRenderSetup.ActiveAsset();
        Check(asset != null, "ha URP asset ativo");
        if (asset == null)
            yield break;

        Check(asset.renderScale <= 0.5f, "render em resolucao reduzida (escala " + asset.renderScale + ")");
        Check(asset.upscalingFilter == UpscalingFilterSelection.Point, "ampliacao sem suavizar (" + asset.upscalingFilter + ")");

        ScriptableRendererData data = RetroRenderSetup.ActiveRendererData();
        var palette = data != null ? data.rendererFeatures.Find(f => f != null && f.name == RetroRenderSetup.PaletteFeatureName) as FullScreenPassRendererFeature : null;
        Check(palette != null && palette.isActive, "passe de paleta retro ativo no renderer");
        Check(palette != null && palette.passMaterial != null && palette.passMaterial.shader.name == "Retro/Palette", "passe usa o shader Retro/Palette");
        yield break;
    }
}
