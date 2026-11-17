using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// Cria a cena IntroCutsceneVideo, que toca o vídeo renderizado no Remotion (pasta Cutscene-Remotion do repositório).
public static class IntroVideoSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/IntroCutsceneVideo.unity";
    const string ClipPath = "Assets/Cutscene/Video/intro_remotion.mp4";

    [MenuItem("Ferramentas/Cutscene de Abertura/Criar cena do vídeo (Remotion)")]
    static void Menu() { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Build(); }

    public static void Build()
    {
        var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(ClipPath);
        if (clip == null) throw new FileNotFoundException("Vídeo não encontrado: " + ClipPath);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/VCR_OSD_MONO SDF.asset");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cam = new GameObject("Camera").AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.cullingMask = 0;
        cam.gameObject.AddComponent<AudioListener>();

        var canvas = new GameObject("Tela", typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        RectTransform Full(string name, params System.Type[] types)
        {
            var go = new GameObject(name, types);
            var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rt.SetParent(canvas.transform, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        var screen = Full("Video 640x360", typeof(RawImage), typeof(AspectRatioFitter));
        var fit = screen.GetComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio = 16f / 9f;
        screen.GetComponent<RawImage>().raycastTarget = false;

        var fade = Full("Fade", typeof(Image)).GetComponent<Image>();
        fade.color = new Color(0, 0, 0, 0); fade.raycastTarget = false;

        var hintRt = Full("Aviso pular", typeof(TextMeshProUGUI));
        hintRt.anchorMin = hintRt.anchorMax = new Vector2(1, 1); hintRt.pivot = new Vector2(1, 1);
        hintRt.anchoredPosition = new Vector2(-40, -30); hintRt.sizeDelta = new Vector2(900, 60);
        var hint = hintRt.GetComponent<TextMeshProUGUI>();
        hint.text = "Pressione novamente para pular"; hint.font = font; hint.fontSize = 36;
        hint.alignment = TextAlignmentOptions.TopRight; hint.color = new Color(0.91f, 0.86f, 0.75f);

        var go = new GameObject("Cutscene");
        var player = go.AddComponent<VideoPlayer>();
        player.clip = clip;
        player.playOnAwake = false;
        player.isLooping = false;
        player.audioOutputMode = VideoAudioOutputMode.Direct;
        player.skipOnDrop = true;
        var ctrl = go.AddComponent<IntroVideoCutscene>();
        ctrl.player = player; ctrl.screen = screen.GetComponent<RawImage>(); ctrl.fade = fade; ctrl.skipHint = hint;

        EditorSceneManager.SaveScene(scene, ScenePath);

        var list = EditorBuildSettings.scenes.ToList();
        if (!list.Any(s => s.path == ScenePath))
        {
            int menu = list.FindIndex(s => s.path.EndsWith("/Menu.unity"));
            list.Insert(menu >= 0 ? menu + 1 : list.Count, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
        AssetDatabase.SaveAssets(); // grava o Build Settings em disco
        Debug.Log("[IntroCutscene] cena do vídeo salva: " + ScenePath);
    }
}
