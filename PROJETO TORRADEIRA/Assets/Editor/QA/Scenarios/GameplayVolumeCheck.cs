using System.Collections;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// O volume global da fase nao usa efeitos que borram pixel art e tem tonemapping;
// os efeitos que o superaquecimento e a pausa animam continuam la.
public class GameplayVolumeCheck : QAScenario
{
    public override string Name => "GameplayVolumeCheck";
    public override bool NeedsPlayMode => false;

    public override IEnumerator Run()
    {
        EditorSceneManager.OpenScene(RetroRenderSetup.GameplayScene);
        Volume main = null;
        foreach (Volume v in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (v.isGlobal && v.name == RetroRenderSetup.GameplayVolumeName)
                main = v;

        Check(main != null && main.sharedProfile != null, "fase tem o volume global com perfil");
        if (main == null || main.sharedProfile == null)
            yield break;

        VolumeProfile p = main.sharedProfile;
        Check(!(p.TryGet(out MotionBlur mb) && mb.active), "sem motion blur");
        Check(!(p.TryGet(out PaniniProjection pp) && pp.active), "sem projecao Panini");
        Check(!(p.TryGet(out DepthOfField dof) && dof.active), "sem desfoque de profundidade");
        Check(p.TryGet(out Tonemapping tm) && tm.active, "com tonemapping");
        Check(p.TryGet(out ChromaticAberration ca) && ca.active, "aberracao cromatica (superaquecimento usa)");
        Check(p.TryGet(out LensDistortion ld) && ld.active, "distorcao de lente (pausa e superaquecimento usam)");
        Check(p.TryGet(out ColorAdjustments color) && color.saturation.value > -50f, "saturacao preserva o vermelho para a paleta (" + (color != null ? color.saturation.value : 0) + ")");
        yield break;
    }
}
