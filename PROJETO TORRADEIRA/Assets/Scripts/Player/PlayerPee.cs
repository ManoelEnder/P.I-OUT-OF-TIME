using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Mecanica escondida (easter egg): segurar U para urinar.
// Nao aparece em nenhuma tela de controles de proposito.
// Jato de particulas com gravidade, pocas que somem devagar e som de agua gerado em codigo.
// A "bexiga" esvazia enquanto segura e enche de novo com o tempo.
public class PlayerPee : MonoBehaviour
{
    private const Key PeeKey = Key.U;
    private const float Capacity = 6f;     // segundos de jato
    private const float RefillPerSecond = 0.08f;

    private ParticleSystem stream;
    private AudioSource audioSource;
    private Transform cam;
    private float bladder = Capacity;
    private float nextPuddle;
    private float nextEnemyCheck;
    private static Material liquidMat;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += (s, m) => Install();
        Install();
    }

    private static void Install()
    {
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null && pm.GetComponent<PlayerPee>() == null)
            pm.gameObject.AddComponent<PlayerPee>();
    }

    private void Start()
    {
        cam = Camera.main != null ? Camera.main.transform : transform;
        BuildStream();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = BuildStreamSound();
        audioSource.loop = true;
        audioSource.volume = 0f;
        audioSource.spatialBlend = 0f;
        audioSource.Play();
    }

    private void Update()
    {
        bool blocked = PauseMenu.IsPaused || RadioMessageUI.CameraOpen || Time.timeScale <= 0f;
        bool held = !blocked && Keyboard.current != null && Keyboard.current[PeeKey].isPressed;
        bool peeing = held && bladder > 0f;

        if (peeing) bladder = Mathf.Max(0f, bladder - Time.deltaTime);
        else bladder = Mathf.Min(Capacity, bladder + RefillPerSecond * Time.deltaTime);

        // Origem: na altura da cintura, um pouco a frente, apontando para onde o jogador olha.
        Vector3 fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        if (fwd.sqrMagnitude < 0.01f) fwd = transform.forward;
        stream.transform.position = transform.position + Vector3.up * 0.85f + fwd * 0.25f;
        float pitch = Mathf.Clamp(cam.forward.y, -0.6f, 0.4f);
        stream.transform.rotation = Quaternion.LookRotation(fwd + Vector3.up * (0.25f + pitch * 0.5f));

        var emission = stream.emission;
        // Fim do jato fica fraco.
        emission.rateOverTime = peeing ? Mathf.Lerp(25f, 90f, Mathf.Clamp01(bladder / 1.5f)) : 0f;
        var main = stream.main;
        main.startSpeed = Mathf.Lerp(1.6f, 3.4f, Mathf.Clamp01(bladder / 1.5f));

        audioSource.volume = Mathf.MoveTowards(audioSource.volume, peeing ? 0.22f : 0f, Time.deltaTime * 2f);
        if (peeing && Time.time > nextPuddle)
        {
            nextPuddle = Time.time + 0.35f;
            SpawnPuddle();
        }
        if (peeing && Time.time > nextEnemyCheck)
        {
            nextEnemyCheck = Time.time + 0.1f;
            PeeReaction.CheckHit(stream);
        }
    }

    private void BuildStream()
    {
        GameObject go = new GameObject("PeeStream");
        stream = go.AddComponent<ParticleSystem>();
        stream.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = stream.main;
        main.startLifetime = 1.2f;
        main.startSize = 0.035f;
        main.startColor = PeeColor;
        main.gravityModifier = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 400;
        var shape = stream.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 2f;
        shape.radius = 0.005f;
        var col = stream.collision;
        col.enabled = true;
        col.type = ParticleSystemCollisionType.World;
        col.bounce = 0.05f;
        col.lifetimeLoss = 0.6f;
        col.quality = ParticleSystemCollisionQuality.Low;
        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.material = LiquidMaterial();
        stream.Play();
    }

    private void SpawnPuddle()
    {
        Vector3 origin = stream.transform.position;
        Vector3 dir = stream.transform.forward;
        // Ponto aproximado onde o jato cai.
        Vector3 guess = origin + Vector3.ProjectOnPlane(dir, Vector3.up).normalized * 0.7f;
        if (!Physics.Raycast(guess + Vector3.up * 0.3f, Vector3.down, out RaycastHit hit, 3f, ~0, QueryTriggerInteraction.Ignore))
            return;
        if (hit.collider.transform.IsChildOf(transform)) return;
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(p.GetComponent<Collider>());
        p.name = "Poca";
        p.transform.position = hit.point + hit.normal * 0.005f;
        p.transform.rotation = Quaternion.LookRotation(-hit.normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        p.transform.localScale = Vector3.one * Random.Range(0.18f, 0.35f);
        Renderer pr = p.GetComponent<Renderer>();
        pr.sharedMaterial = LiquidMaterial();
        pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        p.AddComponent<PeePuddle>();
    }

    // Sprites/Default sempre vai no build e multiplica a cor da particula: amarelo garantido.
    private static Material LiquidMaterial()
    {
        if (liquidMat != null) return liquidMat;
        Shader s = Shader.Find("Sprites/Default");
        if (s == null) s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        liquidMat = new Material(s);
        liquidMat.color = PeeColor;
        if (liquidMat.HasProperty("_BaseColor")) liquidMat.SetColor("_BaseColor", PeeColor);
        return liquidMat;
    }

    private static readonly Color PeeColor = new Color(1f, 0.82f, 0f, 0.9f);

    private static AudioClip BuildStreamSound()
    {
        const int rate = 44100;
        int n = rate * 2;
        float[] d = new float[n];
        var rng = new System.Random(7);
        float lp = 0f, bp = 0f;
        for (int i = 0; i < n; i++)
        {
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += (noise - lp) * 0.35f;
            bp += (lp - bp) * 0.05f;
            float gurgle = 0.7f + 0.3f * Mathf.Sin(i * 0.0009f) * Mathf.Sin(i * 0.00031f);
            d[i] = (lp - bp) * gurgle * 0.8f;
        }
        AudioClip clip = AudioClip.Create("Jato", n, 1, rate, false);
        clip.SetData(d, 0);
        return clip;
    }
}

// Poca que encolhe e some com o tempo.
public class PeePuddle : MonoBehaviour
{
    private float life = 25f;
    private Vector3 start;

    private void Start() { start = transform.localScale; }

    private void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f) { Destroy(gameObject); return; }
        if (life < 5f) transform.localScale = start * (life / 5f);
    }
}
