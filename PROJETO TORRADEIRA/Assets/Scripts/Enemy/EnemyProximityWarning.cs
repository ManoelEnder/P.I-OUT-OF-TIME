using UnityEngine;
using UnityEngine.UI;

// Aviso de que o TemporalEraser esta chegando: batida de coracao que acelera e bordas da tela
// escurecendo no ritmo dela. Comeca a ~12 m; perseguindo fica mais forte. Criado sozinho
// pelo TemporalEraserPresence.
public class EnemyProximityWarning : MonoBehaviour
{
    [SerializeField] private float startDistance = 12f;
    [SerializeField] private float maxVolume = 0.55f;

    private TemporalEraserController enemy;
    private Transform player;
    private AudioSource audioSource;
    private RawImage edge;
    private float nextBeat;
    private float beatGlow;
    private static AudioClip beatClip;
    private static Texture2D edgeTexture;

    public void Init(TemporalEraserController target)
    {
        enemy = target;
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        player = p != null ? p.transform : null;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        if (beatClip == null)
            beatClip = BuildHeartbeat();

        CreateEdge();
    }

    private void Update()
    {
        if (enemy == null || player == null)
            return;

        bool active = enemy.isActiveAndEnabled && enemy.gameObject.activeInHierarchy && Time.timeScale > 0f && !DefeatController.IsDefeated;
        float distance = Vector3.Distance(enemy.transform.position, player.position);
        float near = active ? Mathf.Clamp01(Mathf.InverseLerp(startDistance, 2.5f, distance)) : 0f;
        if (enemy.IsChasing)
            near = Mathf.Max(near, 0.35f);
        if (enemy.IsStunned)
            near *= 0.3f;

        // Batida: de ~1 a ~3 por segundo.
        if (near > 0.02f && Time.time >= nextBeat)
        {
            nextBeat = Time.time + Mathf.Lerp(1.05f, 0.33f, near);
            audioSource.PlayOneShot(beatClip, maxVolume * 0.6f * Mathf.Lerp(0.25f, 1f, near));
            beatGlow = 1f;
        }

        beatGlow = Mathf.MoveTowards(beatGlow, 0f, Time.deltaTime * 3.5f);

        if (edge != null)
        {
            float a = near * (0.55f + 0.45f * beatGlow) * 0.7f;
            edge.color = new Color(0.03f, 0f, 0f, a);
            if (edge.gameObject.activeSelf != a > 0.01f)
                edge.gameObject.SetActive(a > 0.01f);
        }
    }

    private void CreateEdge()
    {
        GameObject hud = GameObject.Find("HUDCanvas");
        if (hud == null)
            return;

        GameObject go = new GameObject("EnemyNearEdge", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.layer = hud.layer;
        go.transform.SetParent(hud.transform, false);
        go.transform.SetAsFirstSibling();
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        if (edgeTexture == null)
        {
            const int size = 96;
            edgeTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = Mathf.Abs(x / (size - 1f) - 0.5f) * 2f, v = Mathf.Abs(y / (size - 1f) - 0.5f) * 2f;
                    float d = Mathf.Clamp01(Mathf.Sqrt(u * u + v * v) - 0.35f) / 0.65f;
                    edgeTexture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, d)));
                }
            edgeTexture.Apply(false, true);
        }

        edge = go.GetComponent<RawImage>();
        edge.texture = edgeTexture;
        edge.raycastTarget = false;
        go.SetActive(false);
    }

    // "Tum-tum": duas batidas graves curtas.
    private static AudioClip BuildHeartbeat()
    {
        const int rate = 44100;
        int length = (int)(rate * 0.45f);
        float[] data = new float[length];
        for (int i = 0; i < length; i++)
        {
            float t = i / (float)rate;
            float a = Thump(t, 0f, 60f) + Thump(t, 0.17f, 52f) * 0.75f;
            data[i] = Mathf.Clamp(a, -1f, 1f);
        }
        AudioClip clip = AudioClip.Create("Heartbeat", length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static float Thump(float t, float start, float freq)
    {
        float x = t - start;
        if (x < 0f)
            return 0f;
        return Mathf.Sin(2f * Mathf.PI * freq * x) * Mathf.Exp(-x * 22f) * 0.9f;
    }
}
