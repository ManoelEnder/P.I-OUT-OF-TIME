using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Visao do jogador como uma tela CRT.
// - A distorcao (abaulado de tubo), aberracao cromatica e vinheta ficam no GameplayVolume
//   (Assets/Settings/Volumes) para continuar funcionando com o calor da camera e o pause.
// - Aqui: motion blur (Volume proprio) + moldura com cantos curvos do tubo + linhas de
//   varredura PARADAS e bem leves. Nada se move na tela.
public class CrtOverlay : MonoBehaviour
{
    private static readonly string[] GameplayScenes = { "Principal" };
    private static Texture2D scanTex, bezelTex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (s, m) => TryInstall(s);
        TryInstall(SceneManager.GetActiveScene());
    }

    private static void TryInstall(Scene s)
    {
        if (System.Array.IndexOf(GameplayScenes, s.name) < 0) return;
        if (FindFirstObjectByType<CrtOverlay>() != null) return;
        new GameObject("CrtOverlay").AddComponent<CrtOverlay>();
    }

    private void Start()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -50; // abaixo do HUD: o texto continua legivel

        RawImage scan = NewLayer("Scanlines", ScanTexture());
        scan.uvRect = new Rect(0f, 0f, 1f, Screen.height / 3f);
        scan.color = new Color(1f, 1f, 1f, 0.05f);


        Volume volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = -10f;
        VolumeProfile p = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.profile = p;
        MotionBlur mb = p.Add<MotionBlur>(true);
        mb.mode.Override(MotionBlurMode.CameraOnly);
        mb.quality.Override(MotionBlurQuality.Medium);
        mb.intensity.Override(0.45f);

        Camera cam = Camera.main;
        if (cam != null)
        {
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null) data.renderPostProcessing = true;
        }
    }

    private RawImage NewLayer(string name, Texture2D tex)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(transform, false);
        RectTransform r = (RectTransform)go.transform;
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        RawImage img = go.GetComponent<RawImage>();
        img.texture = tex;
        img.raycastTarget = false;
        return img;
    }

    private static Texture2D ScanTexture()
    {
        if (scanTex != null) return scanTex;
        scanTex = new Texture2D(1, 3, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        scanTex.SetPixels(new[] { new Color(0, 0, 0, 1f), new Color(0, 0, 0, 0f), new Color(0, 0, 0, 0f) });
        scanTex.Apply();
        return scanTex;
    }

    // Cantos pretos arredondados + sombra interna: formato do vidro de um tubo CRT.
    public static Texture2D BezelTexture()
    {
        if (bezelTex != null) return bezelTex;
        const int w = 512, h = 288;
        bezelTex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            // Coordenadas -1..1 com leve curvatura de tubo.
            float u = x / (w - 1f) * 2f - 1f, v = y / (h - 1f) * 2f - 1f;
            float cu = u * (1f + v * v * 0.02f), cv = v * (1f + u * u * 0.03f);
            // Retangulo arredondado (superelipse).
            float d = Mathf.Pow(Mathf.Pow(Mathf.Abs(cu), 8f) + Mathf.Pow(Mathf.Abs(cv), 8f), 1f / 8f);
            float edge = Mathf.InverseLerp(0.99f, 1.0f, d);   // fora do vidro: preto
            float shade = Mathf.InverseLerp(0.9f, 1.0f, d);   // sombra interna
            float a = Mathf.Clamp01(edge + shade * shade * 0f);
            px[y * w + x] = new Color(0f, 0f, 0f, a);
        }
        bezelTex.SetPixels(px);
        bezelTex.Apply();
        return bezelTex;
    }
}
