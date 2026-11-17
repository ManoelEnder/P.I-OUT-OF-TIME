using UnityEngine;

// Imagem de "camera de seguranca" em baixa resolucao numa tela do menu.
// A camera fica desligada e renderiza manualmente poucas vezes por segundo para pesar pouco.
[RequireComponent(typeof(Renderer))]
public class CrtCameraFeed : MonoBehaviour
{
    [SerializeField] private Camera feedCamera;
    [SerializeField] private int width = 160;
    [SerializeField] private int height = 120;
    [SerializeField] private float framesPerSecond = 8f;

    private RenderTexture target;
    private Material material;
    private float nextFrame;

    private void Awake()
    {
        if (feedCamera == null)
        {
            enabled = false;
            return;
        }

        target = new RenderTexture(width, height, 16)
        {
            filterMode = FilterMode.Point,
            name = "CrtFeed",
        };

        feedCamera.enabled = false;
        feedCamera.targetTexture = target;

        Renderer screen = GetComponent<Renderer>();
        material = screen.material;
        material.mainTexture = target;

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", target);
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime < nextFrame)
            return;

        nextFrame = Time.unscaledTime + 1f / Mathf.Max(1f, framesPerSecond);
        feedCamera.Render();
    }

    private void OnDestroy()
    {
        if (feedCamera != null)
            feedCamera.targetTexture = null;

        if (target != null)
        {
            target.Release();
            Destroy(target);
        }

        if (material != null)
            Destroy(material);
    }
}
