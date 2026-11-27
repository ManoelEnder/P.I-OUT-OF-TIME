using UnityEditor;
using UnityEditor.SceneManagement;
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

    public const string PaletteShaderPath = "Assets/Art/Shaders/RetroPalette.shader";
    public const string PaletteMaterialPath = "Assets/Art/Materials/RetroPalette.mat";
    public const string PaletteFeatureName = "RetroPalette";

    // Passe de tela cheia com a paleta do jogo (cinzas frios + vermelho) e pontilhado.
    [MenuItem("Ferramentas/Visual/Aplicar paleta retro")]
    public static void ApplyPalette()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(PaletteShaderPath);
        if (shader == null)
        {
            Debug.LogError("RetroRenderSetup: shader nao encontrado em " + PaletteShaderPath);
            return;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(PaletteMaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, PaletteMaterialPath);
        }
        material.shader = shader;
        EditorUtility.SetDirty(material);

        ScriptableRendererData data = ActiveRendererData();
        if (data == null)
        {
            Debug.LogError("RetroRenderSetup: renderer do URP nao encontrado.");
            return;
        }

        var feature = data.rendererFeatures.Find(f => f != null && f.name == PaletteFeatureName) as FullScreenPassRendererFeature;
        if (feature == null)
        {
            feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            feature.name = PaletteFeatureName;
            AssetDatabase.AddObjectToAsset(feature, data);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);

            var so = new SerializedObject(data);
            SerializedProperty list = so.FindProperty("m_RendererFeatures");
            SerializedProperty map = so.FindProperty("m_RendererFeatureMap");
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            so.ApplyModifiedProperties();
        }

        feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
        feature.fetchColorBuffer = true;
        feature.passMaterial = material;
        feature.passIndex = 0;
        feature.SetActive(true);

        EditorUtility.SetDirty(feature);
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        Debug.Log("RETRO paleta: feature " + feature.name + " no renderer " + data.name);
    }

    public static void ApplyPaletteFromCommandLine()
    {
        ApplyPalette();
        EditorApplication.Exit(0);
    }

    public const string GameplayProfilePath = "Assets/Settings/Volumes/GameplayVolume.asset";
    public const string GameplayScene = "Assets/Scenes/Principal.unity";
    public const string GameplayVolumeName = "GlobalVolume";

    // Pos-processamento da fase para pixel art: sem motion blur, Panini e desfoque de profundidade
    // (borram o pixel). Saturacao 0: a paleta (passe RetroPalette) faz os cinzas e preserva o vermelho.
    // Aberracao cromatica e distorcao ficam (o superaquecimento e a pausa animam as duas).
    [MenuItem("Ferramentas/Visual/Aplicar volume da fase")]
    public static void ApplyGameplayVolume()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Settings/Volumes"))
            AssetDatabase.CreateFolder("Assets/Settings", "Volumes");

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(GameplayProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, GameplayProfilePath);
        }

        Get<Tonemapping>(profile).mode.Override(TonemappingMode.Neutral);

        ColorAdjustments color = Get<ColorAdjustments>(profile);
        color.postExposure.Override(0.2f);
        color.contrast.Override(22f);
        color.saturation.Override(-20f);
        // Filtro levemente frio de filmadora antiga.
        color.colorFilter.Override(new Color(0.94f, 0.97f, 1f));

        Bloom bloom = Get<Bloom>(profile);
        bloom.threshold.Override(0.9f);
        bloom.intensity.Override(1f);
        bloom.scatter.Override(0.6f);
        bloom.tint.Override(Color.white);

        Vignette vignette = Get<Vignette>(profile);
        vignette.color.Override(Color.black);
        vignette.intensity.Override(0.5f);
        vignette.smoothness.Override(0.4f);

        FilmGrain grain = Get<FilmGrain>(profile);
        grain.type.Override(FilmGrainLookup.Medium1);
        grain.intensity.Override(0.45f);
        grain.response.Override(0.8f);

        Get<ChromaticAberration>(profile).intensity.Override(0.25f);

        // Lente de filmadora: borda curvada e zoom leve para esconder o canto esticado.
        LensDistortion lens = Get<LensDistortion>(profile);
        lens.intensity.Override(-0.2f);
        lens.scale.Override(1.05f);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.OpenScene(GameplayScene);
        bool assigned = false;
        foreach (Volume volume in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!volume.isGlobal || volume.name != GameplayVolumeName)
                continue;

            Undo.RecordObject(volume, "Volume da fase");
            volume.sharedProfile = profile;
            assigned = true;
        }

        if (assigned)
            EditorSceneManager.SaveScene(scene);
        Debug.Log("RETRO volume: " + (assigned ? "aplicado em " : "NAO achou o volume em ") + GameplayScene);
    }

    public static void ApplyGameplayVolumeFromCommandLine()
    {
        ApplyGameplayVolume();
        EditorApplication.Exit(0);
    }

    private static T Get<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (!profile.TryGet(out T component))
        {
            component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(component, profile);
        }

        component.active = true;
        return component;
    }

    public static ScriptableRendererData ActiveRendererData()
    {
        UniversalRenderPipelineAsset asset = ActiveAsset();
        if (asset == null)
            return null;

        var so = new SerializedObject(asset);
        SerializedProperty list = so.FindProperty("m_RendererDataList");
        int index = so.FindProperty("m_DefaultRendererIndex").intValue;
        return list != null && index < list.arraySize ? list.GetArrayElementAtIndex(index).objectReferenceValue as ScriptableRendererData : null;
    }

    public static UniversalRenderPipelineAsset ActiveAsset()
    {
        return (QualitySettings.renderPipeline ?? GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;
    }
}
