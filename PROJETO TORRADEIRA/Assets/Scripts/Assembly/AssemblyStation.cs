using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Estacao de montagem dentro da fase (sala real, o jogador entra andando):
// - bandeja de entrega perto da porta (E com todas as missoes prontas; aparece no radar);
// - pedestal com a carcaca do prototipo de 1975 no fundo da sala;
// - garra mecanica no teto que leva cada peca da bandeja ate o prototipo.
// Ao entregar: o jogador para, as pecas caem na bandeja uma a uma, a garra monta, o prototipo liga
// e o jogo segue para a cutscene final. Tudo montado em codigo a partir do centro/tamanho da sala.
public class AssemblyStation : MonoBehaviour
{
    [Header("Sala (mundo)")]
    public Vector3 floorCenter = new Vector3(-23.25f, 12f, -113.8f);
    public Vector2 size = new Vector2(5.4f, 8.2f);     // x, z
    public float ceilingHeight = 5f;
    [Tooltip("Lado da porta (o jogador entra por aqui): -1 = z menor.")]
    public int doorSide = -1;

    [Header("Referencias")]
    public GameObject pieceModel;
    public string nextScene = "OutroCutsceneVideo";

    public static AssemblyStation Instance { get; private set; }
    public bool Running { get; private set; }
    public bool Finished { get; private set; }

    private ClawMachine claw;
    private Transform tray, prototype;
    private Renderer protoLens, ringRenderer, trayRing;
    private readonly List<Vector3> sockets = new List<Vector3>();
    private Light beacon;
    private Transform beaconPivot;
    private ParticleSystem sparks;
    private AudioSource sfx, hum;
    private Material matMetal, matDark, matAccent, matStripe, matLampOn, matGlowOn, matGlowOff;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        MakeMaterials();
        Vector3 c = floorCenter;
        float back = -doorSide;  // fundo da sala (oposto a porta)

        // Pedestal + prototipo no fundo; bandeja perto da porta.
        Vector3 pedestal = c + new Vector3(0f, 0f, back * size.y * 0.22f);
        Vector3 trayPos = c + new Vector3(size.x * 0.22f, 0f, doorSide * size.y * 0.18f);
        BuildPedestal(pedestal);
        BuildTray(trayPos);

        GameObject g = new GameObject("GarraMecanica");
        g.transform.SetParent(transform, false);
        g.transform.position = new Vector3(c.x, c.y, c.z);
        claw = g.AddComponent<ClawMachine>();
        claw.ceilingY = ceilingHeight - 0.4f;
        claw.railX = new Vector2(-size.x * 0.4f, size.x * 0.4f);
        claw.railZ = new Vector2(-size.y * 0.4f, size.y * 0.4f);
        claw.Build(matMetal, matDark, matAccent, matLampOn);

        // Luz de trabalho fria sobre o pedestal e giroflex.
        Light work = new GameObject("LuzBancada", typeof(Light)).GetComponent<Light>();
        work.transform.SetParent(transform, false);
        work.transform.position = pedestal + Vector3.up * (ceilingHeight - 1.2f);
        work.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        work.type = LightType.Spot;
        work.spotAngle = 70f;
        work.range = ceilingHeight + 2f;
        work.intensity = 3f;
        work.color = new Color(0.9f, 0.94f, 1f);
        work.shadows = LightShadows.Soft;

        beaconPivot = new GameObject("Giroflex").transform;
        beaconPivot.SetParent(transform, false);
        beaconPivot.position = c + new Vector3(-size.x * 0.45f, ceilingHeight - 0.6f, back * size.y * 0.45f);
        Cyl("Cupula", beaconPivot, Vector3.zero, new Vector3(0.18f, 0.08f, 0.18f), Mat(new Color(0.6f, 0.05f, 0.05f), 0.6f, 0f, new Color(1.2f, 0.1f, 0.1f)));
        beacon = new GameObject("Luz", typeof(Light)).GetComponent<Light>();
        beacon.transform.SetParent(beaconPivot, false);
        beacon.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
        beacon.type = LightType.Spot;
        beacon.spotAngle = 60f;
        beacon.range = 12f;
        beacon.intensity = FlashSettings.Enabled ? 5f : 1.5f;
        beacon.color = new Color(1f, 0.15f, 0.1f);
        beacon.enabled = false;

        BuildSparks();
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.spatialBlend = 0.4f;
        hum = gameObject.AddComponent<AudioSource>();
        hum.spatialBlend = 1f;
        hum.minDistance = 2f;
        hum.maxDistance = 14f;
        hum.loop = true;
        hum.volume = 0.25f;
        hum.clip = Hum();
        hum.Play();
    }

    private void Update()
    {
        if (beaconPivot != null)
            beaconPivot.Rotate(0f, (Running ? 300f : 40f) * Time.deltaTime, 0f);
        // Bandeja pulsa quando da para entregar.
        DeliveryPoint dp = tray != null ? tray.GetComponent<DeliveryPoint>() : null;
        bool ready = dp != null && dp.Active && !Running;
        if (trayRing != null)
            trayRing.sharedMaterial = ready && Mathf.Repeat(Time.time * 2f, 1f) < 0.6f ? matGlowOn : matGlowOff;
    }

    // Chamado pela missao ao entregar as pecas na bandeja.
    public void Begin(int pieces, MissionSystem missions)
    {
        if (Running) return;
        StartCoroutine(Sequence(Mathf.Clamp(pieces, 1, 6), missions));
    }

    private IEnumerator Sequence(int pieces, MissionSystem missions)
    {
        Running = true;

        // Para o jogador e o inimigo durante a montagem.
        PhotoCamera photo = FindFirstObjectByType<PhotoCamera>();
        if (photo != null) photo.TryExitCameraMode();
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            Vector3 look = prototype.position - player.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f) player.transform.rotation = Quaternion.LookRotation(look);
            player.enabled = false;
        }
        foreach (TemporalEraserController e in FindObjectsByType<TemporalEraserController>(FindObjectsSortMode.None))
        {
            var agent = e.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            e.enabled = false;
        }

        beacon.enabled = true;
        hum.volume = 0.5f;

        // Pecas caem na bandeja.
        var items = new List<Transform>();
        for (int i = 0; i < pieces; i++)
        {
            Transform p = MakePiece(i);
            Vector3 rest = tray.position + new Vector3((i % 3 - 1) * 0.16f, 0.1f, (i / 3) * 0.16f - 0.08f);
            for (float t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                p.position = Vector3.Lerp(rest + Vector3.up * 0.5f, rest, t / 0.25f);
                yield return null;
            }
            p.position = rest;
            sfx.PlayOneShot(Beep(660f + i * 110f), 0.5f);
            items.Add(p);
            yield return new WaitForSeconds(0.15f);
        }

        // A garra leva uma a uma para o prototipo.
        for (int i = 0; i < items.Count; i++)
        {
            Vector3 socket = prototype.TransformPoint(sockets[i % sockets.Count]);
            yield return claw.Transfer(items[i], socket, prototype.rotation, prototype);
            sparks.transform.position = socket;
            sparks.Play();
            sfx.PlayOneShot(Beep(440f + i * 140f), 0.7f);
            protoLens.sharedMaterial.SetColor("_EmissionColor", Color.white * (0.4f + 0.9f * (i + 1) / items.Count));
        }

        yield return claw.Park();
        ringRenderer.sharedMaterial = matGlowOn;
        yield return claw.Flourish();

        sfx.PlayOneShot(Beep(220f, 1.2f), 0.9f);
        for (float t = 0f; t < 1.6f; t += Time.deltaTime)
        {
            protoLens.sharedMaterial.SetColor("_EmissionColor", Color.white * (2f + Mathf.Sin(t * 30f) * (FlashSettings.Enabled ? 1.5f : 0.2f)));
            yield return null;
        }
        Finished = true;
        if (missions != null)
            missions.LoadFinalScene(nextScene);
    }

    // ---------- Construcao ----------

    private void BuildPedestal(Vector3 pos)
    {
        Transform ped = new GameObject("Pedestal").transform;
        ped.SetParent(transform, false);
        ped.position = pos;
        for (int i = 0; i < 16; i++)
        {
            float a = i * 22.5f;
            Transform s = Box("FaixaPiso", ped, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.006f, 1.35f), new Vector3(0.38f, 0.01f, 0.16f), i % 2 == 0 ? matAccent : matStripe, false);
            s.localRotation = Quaternion.Euler(0f, a, 0f);
        }
        Cyl("Base", ped, new Vector3(0f, 0.25f, 0f), new Vector3(1.5f, 0.25f, 1.5f), matDark, true);
        Cyl("Coluna", ped, new Vector3(0f, 0.65f, 0f), new Vector3(0.85f, 0.2f, 0.85f), matMetal, true);
        ringRenderer = Cyl("Anel", ped, new Vector3(0f, 0.86f, 0f), new Vector3(0.95f, 0.015f, 0.95f), matGlowOff).GetComponent<Renderer>();
        Cyl("Mesa", ped, new Vector3(0f, 0.9f, 0f), new Vector3(0.78f, 0.03f, 0.78f), matMetal, true);

        prototype = new GameObject("Prototipo").transform;
        prototype.SetParent(ped, false);
        prototype.localPosition = new Vector3(0f, 0.93f, 0f);
        prototype.localRotation = Quaternion.Euler(0f, doorSide < 0 ? 0f : 180f, 0f);
        Box("Corpo", prototype, new Vector3(0f, 0.14f, 0f), new Vector3(0.5f, 0.28f, 0.32f), matMetal, false);
        Box("Gravador", prototype, new Vector3(-0.38f, 0.1f, 0f), new Vector3(0.26f, 0.2f, 0.3f), matStripe, false);
        Box("Alca", prototype, new Vector3(0f, 0.32f, 0f), new Vector3(0.3f, 0.04f, 0.06f), matDark, false);
        Transform lens = Cyl("Lente", prototype, new Vector3(0.12f, 0.14f, -0.2f), new Vector3(0.16f, 0.05f, 0.16f), Mat(new Color(0.15f, 0.2f, 0.25f), 0.9f, 0f, Color.black));
        lens.localRotation = Quaternion.Euler(90f, 0f, 0f);
        protoLens = lens.GetComponent<Renderer>();
        sockets.Add(new Vector3(-0.12f, 0.3f, 0.05f));
        sockets.Add(new Vector3(0.14f, 0.3f, 0.06f));
        sockets.Add(new Vector3(-0.38f, 0.22f, 0f));
        sockets.Add(new Vector3(0.05f, 0.16f, 0.17f));
        sockets.Add(new Vector3(-0.2f, 0.16f, 0.17f));
        sockets.Add(new Vector3(0.2f, 0.16f, 0.17f));
    }

    private void BuildTray(Vector3 pos)
    {
        // Bandeja de entrega: coluna com cuba metalica e anel de luz (a garra pega daqui).
        tray = new GameObject("BandejaEntrega").transform;
        tray.SetParent(transform, false);
        tray.position = pos + Vector3.up * 1.0f;
        Transform stand = Cyl("Coluna", transform, pos + Vector3.up * 0.5f, new Vector3(0.22f, 0.5f, 0.22f), matDark, true);
        Box("Cuba", tray, new Vector3(0f, -0.02f, 0f), new Vector3(0.6f, 0.04f, 0.45f), matMetal, false);
        Box("BordaA", tray, new Vector3(0f, 0.04f, 0.22f), new Vector3(0.6f, 0.1f, 0.02f), matMetal, false);
        Box("BordaB", tray, new Vector3(0f, 0.04f, -0.22f), new Vector3(0.6f, 0.1f, 0.02f), matMetal, false);
        Box("BordaC", tray, new Vector3(0.3f, 0.04f, 0f), new Vector3(0.02f, 0.1f, 0.45f), matMetal, false);
        Box("BordaD", tray, new Vector3(-0.3f, 0.04f, 0f), new Vector3(0.02f, 0.1f, 0.45f), matMetal, false);
        trayRing = Box("Luz", tray, new Vector3(0f, -0.045f, 0f), new Vector3(0.66f, 0.012f, 0.5f), matGlowOff, false).GetComponent<Renderer>();

        BoxCollider col = tray.gameObject.AddComponent<BoxCollider>();
        col.size = new Vector3(0.7f, 0.35f, 0.55f);
        col.center = new Vector3(0f, 0.05f, 0f);
        tray.gameObject.AddComponent<DeliveryPoint>();
    }

    private void BuildSparks()
    {
        GameObject sp = new GameObject("Faiscas", typeof(ParticleSystem));
        sp.transform.SetParent(transform, false);
        sparks = sp.GetComponent<ParticleSystem>();
        sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = sparks.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.3f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.035f);
        main.startColor = new Color(1f, 0.9f, 0.75f);
        main.gravityModifier = 1.4f;
        var emission = sparks.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });
        var shape = sparks.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.04f;
        sp.GetComponent<ParticleSystemRenderer>().sharedMaterial = Mat(Color.white, 0f, 0f, new Color(3f, 2.6f, 2f));
    }

    private Transform MakePiece(int index)
    {
        GameObject piece;
        if (pieceModel != null)
        {
            piece = Instantiate(pieceModel);
            foreach (Collider col in piece.GetComponentsInChildren<Collider>()) Destroy(col);
            foreach (MonoBehaviour mb in piece.GetComponentsInChildren<MonoBehaviour>()) Destroy(mb);
            Renderer[] rs = piece.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                foreach (Renderer r in rs) b.Encapsulate(r.bounds);
                float max = Mathf.Max(b.size.x, b.size.y, b.size.z);
                if (max > 0.0001f) piece.transform.localScale *= 0.16f / max;
            }
            foreach (Renderer r in rs) r.enabled = true;
        }
        else
        {
            piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(piece.GetComponent<Collider>());
            piece.transform.localScale = new Vector3(0.14f, 0.07f, 0.1f);
            piece.GetComponent<Renderer>().sharedMaterial = matAccent;
        }
        piece.tag = "Untagged";
        foreach (Transform t in piece.GetComponentsInChildren<Transform>()) t.gameObject.tag = "Untagged";
        piece.name = "PecaMontagem" + (index + 1);
        piece.transform.SetParent(transform, true);
        piece.transform.rotation = Quaternion.Euler(0f, index * 37f, 0f);
        return piece.transform;
    }

    private Material Mat(Color c, float smooth = 0.2f, float metallic = 0f, Color? emission = null)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metallic);
        if (emission.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission.Value);
        }
        return m;
    }

    private void MakeMaterials()
    {
        matMetal = Mat(new Color(0.55f, 0.56f, 0.57f), 0.55f, 0.6f);
        matDark = Mat(new Color(0.1f, 0.1f, 0.11f), 0.4f, 0.3f);
        matAccent = Mat(new Color(0.55f, 0.12f, 0.1f), 0.35f, 0.2f);
        matStripe = Mat(new Color(0.85f, 0.84f, 0.8f), 0.2f);
        matLampOn = Mat(Color.white, 0.5f, 0f, new Color(2.2f, 2.2f, 2.1f));
        matGlowOn = Mat(new Color(0.8f, 0.85f, 1f), 0.5f, 0f, new Color(0.9f, 1.1f, 1.6f));
        matGlowOff = Mat(new Color(0.12f, 0.13f, 0.15f), 0.5f);
    }

    private Transform Box(string name, Transform parent, Vector3 pos, Vector3 size, Material m, bool collider)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        if (!collider) Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = m;
        return go.transform;
    }

    private Transform Cyl(string name, Transform parent, Vector3 pos, Vector3 size, Material m, bool collider = false)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        Destroy(go.GetComponent<Collider>());
        if (collider) go.AddComponent<BoxCollider>();
        go.transform.SetParent(parent, false);
        if (parent == transform) go.transform.position = pos; else go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = m;
        return go.transform;
    }

    private static AudioClip Hum()
    {
        const int rate = 44100;
        int n = rate * 2;
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            d[i] = (Mathf.Sin(t * 2f * Mathf.PI * 60f) * 0.5f + Mathf.Sin(t * 2f * Mathf.PI * 120f) * 0.25f) * 0.3f;
        }
        AudioClip c = AudioClip.Create("MontagemHum", n, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    private static AudioClip Beep(float freq, float seconds = 0.18f)
    {
        const int rate = 44100;
        int n = (int)(rate * seconds);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            d[i] = (Mathf.Sin(t * 2f * Mathf.PI * freq) >= 0f ? 1f : -1f) * Mathf.Min(1f, t / 0.005f) * (1f - t / seconds) * 0.35f;
        }
        AudioClip c = AudioClip.Create("Beep", n, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }
}
