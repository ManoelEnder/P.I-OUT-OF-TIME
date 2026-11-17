using UnityEngine;

// Estatica de TV em baixa resolucao para as telas do menu. Atualiza poucas vezes por segundo.
[RequireComponent(typeof(Renderer))]
public class CrtNoiseScreen : MonoBehaviour
{
    [SerializeField] private int width = 64;
    [SerializeField] private int height = 48;
    [SerializeField] private float framesPerSecond = 15f;
    [SerializeField] private Color tint = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField, Range(0f, 1f)] private float brightness = 0.38f;

    private Texture2D texture;
    private Color32[] pixels;
    private Material material;
    private float nextFrame;
    private int rollingBar;
    private System.Random random;

    private void Awake()
    {
        texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "CrtNoise",
        };

        pixels = new Color32[width * height];
        random = new System.Random(GetInstanceID());

        Renderer screen = GetComponent<Renderer>();
        material = screen.material;
        material.mainTexture = texture;

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);

        Draw();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextFrame)
            return;

        nextFrame = Time.unscaledTime + 1f / Mathf.Max(1f, framesPerSecond);
        Draw();
    }

    private void Draw()
    {
        rollingBar = (rollingBar + 1) % height;

        for (int y = 0; y < height; y++)
        {
            // Faixa mais clara que desce pela tela, como em uma TV sem sinal.
            float bar = Mathf.Abs(y - rollingBar) < 4 ? 1.25f : 1f;
            float scanline = y % 2 == 0 ? 1f : 0.7f;

            for (int x = 0; x < width; x++)
            {
                float v = (float)random.NextDouble() * brightness * bar * scanline;
                pixels[y * width + x] = new Color32(
                    (byte)(Mathf.Clamp01(v * tint.r) * 255),
                    (byte)(Mathf.Clamp01(v * tint.g) * 255),
                    (byte)(Mathf.Clamp01(v * tint.b) * 255),
                    255);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false);
    }

    private void OnDestroy()
    {
        if (texture != null)
            Destroy(texture);

        if (material != null)
            Destroy(material);
    }
}
