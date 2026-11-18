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
        yield break;
    }
}
