using UnityEngine;
using UnityEngine.InputSystem;

// Interacao = botao esquerdo do mouse (com o visor da camera fechado; aberto, o clique tira foto).
public static class InteractInput
{
    public static bool Pressed =>
        Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame &&
        !RadioMessageUI.CameraOpen && !PauseMenu.IsPaused && Time.timeScale > 0f;
}

// Objeto que escolhe onde o icone aparece (ex.: maçaneta da porta).
public interface IInteractionAnchor
{
    Vector3 IconPosition(Vector3 viewer);
}

// Iconezinho de mouse (clique esquerdo) em pixel art, flutuando no proprio objeto.
// Fica no mundo 3D (nao no HUD), entao passa pela paleta retro, pontilhado e pos-processamento
// do jogo. O botao esquerdo pisca em vermelho como um "clique".
public class InteractionIcon : MonoBehaviour
{
    private static InteractionIcon instance;
    private const float Size = 0.16f;

    private MeshRenderer quad;
    private Material[] frames;
    private Transform target;
    private Vector3 offsetPoint;
    private bool hasPoint;
    private float shownAt;
    private bool visible;

    public static void Show(Component target, Vector3 hitPoint)
    {
        Ensure();
        if (instance.target != target.transform || !instance.visible)
        {
            instance.shownAt = Time.time;
            instance.target = target.transform;
        }
        instance.offsetPoint = hitPoint;
        instance.hasPoint = true;
        instance.visible = true;
    }

    public static void Hide()
    {
        if (instance == null) return;
        instance.visible = false;
        instance.target = null;
    }

    private static void Ensure()
    {
        if (instance != null) return;
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "IconeInteracao";
        Object.Destroy(go.GetComponent<Collider>());
        instance = go.AddComponent<InteractionIcon>();
        instance.quad = go.GetComponent<MeshRenderer>();
        instance.quad.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        instance.quad.receiveShadows = false;
        instance.frames = new Material[2];
        for (int i = 0; i < 2; i++)
        {
            Texture2D tex = Resources.Load<Texture2D>("Icons/mouse_left_" + i);
            if (tex != null) tex.filterMode = FilterMode.Point;
            Material m = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex };
            m.renderQueue = 3500; // por cima de outros transparentes
            instance.frames[i] = m;
        }
        instance.quad.sharedMaterial = instance.frames[0];
        go.SetActive(true);
    }

    private void LateUpdate()
    {
        Camera cam = Camera.main;
        bool show = visible && target != null && cam != null && !RadioMessageUI.CameraOpen;
        quad.enabled = show;
        if (!show) return;

        // Posicao: ancora propria (maçaneta) ou topo do objeto; puxado um pouco para o jogador.
        Vector3 pos;
        IInteractionAnchor anchor = target.GetComponentInParent<IInteractionAnchor>();
        if (anchor != null) pos = anchor.IconPosition(cam.transform.position);
        else pos = TopOf(target, hasPoint ? offsetPoint : target.position);
        Vector3 toCam = (cam.transform.position - pos).normalized;
        pos += toCam * 0.12f;

        // Entra com um "pulo" curto e depois flutua de leve.
        float age = Time.time - shownAt;
        float pop = age < 0.18f ? Mathf.Lerp(0.4f, 1.15f, age / 0.18f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((age - 0.18f) / 0.1f));
        pos += Vector3.up * Mathf.Round(Mathf.Sin(Time.time * 3f) * 2f) * 0.006f; // flutua em "degraus" (pixel)

        transform.position = pos;
        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, cam.transform.up);
        // Tamanho constante na tela (perto ou longe).
        float dist = Vector3.Distance(cam.transform.position, pos);
        transform.localScale = Vector3.one * Size * Mathf.Clamp(dist / 1.6f, 0.6f, 1.6f) * pop;

        // Clique: botao esquerdo pisca vermelho.
        quad.sharedMaterial = frames[Mathf.Repeat(Time.time * 1.6f, 1f) < 0.3f ? 1 : 0];
    }

    private static Vector3 TopOf(Transform t, Vector3 fallback)
    {
        Renderer[] rs = t.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return fallback + Vector3.up * 0.15f;
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs)
            if (r.enabled && !(r is ParticleSystemRenderer)) b.Encapsulate(r.bounds);
        // Objeto grande (mesa de montagem etc.): usa o ponto mirado.
        if (b.size.magnitude > 2.5f) return fallback + Vector3.up * 0.1f;
        return new Vector3(b.center.x, b.max.y + 0.12f, b.center.z);
    }
}
