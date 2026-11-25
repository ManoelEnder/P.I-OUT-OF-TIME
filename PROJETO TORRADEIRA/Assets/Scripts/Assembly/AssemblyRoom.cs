using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Sala de montagem (missao final): o jogador coloca cada peca num encaixe do console; com todas
// colocadas, a garra mecanica do teto leva uma a uma ate o prototipo no pedestal, o prototipo liga
// e o jogo segue para a cutscene final. A sala inteira e montada em codigo ao abrir a cena.
public class AssemblyRoom : MonoBehaviour
{
    [Header("Referencias (preenchidas pelo construtor da cena)")]
    public GameObject pieceModel;
    public TMP_FontAsset font;
    public string nextScene = "OutroCutsceneVideo";

    private const float RoomW = 11f, RoomD = 10f, RoomH = 4.6f;

    private AssemblyPlayer player;
    private ClawMachine claw;
    private readonly List<Transform> slots = new List<Transform>();
    private readonly List<Transform> slotPieces = new List<Transform>();
    private readonly List<Renderer> slotLights = new List<Renderer>();
    private readonly List<Vector3> sockets = new List<Vector3>();
    private Transform prototype;
    private Renderer protoLens, pedestalRing;
    private Light beacon;
    private Transform beaconPivot;
    private TextMeshPro monitorText;
    private TextMeshProUGUI prompt, hint;
    private Image fade;
    private AudioSource ambience, sfx;
    private ParticleSystem sparks;
    private int pieceCount;
    private bool assembling;

    private Material matFloor, matWall, matWallDark, matMetal, matDark, matAccent, matLampOn, matLampOff, matSlotOn, matSlotOff, matStripe, matScreen;

    public int Placed => slotPieces.Count;
    public int PieceCount => pieceCount;
    public bool Finished { get; private set; }

    private void Awake()
    {
        pieceCount = Mathf.Clamp(MissionSystem.DeliveredPieces, 1, 6);
        Time.timeScale = 1f;
        MakeMaterials();
        BuildRoom();
        BuildConsole();
        BuildPedestal();
        BuildClaw();
        BuildDressing();
        BuildPlayer();
        BuildUI();
        BuildAudio();
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.16f, 0.17f, 0.19f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.03f, 0.03f, 0.035f);
        RenderSettings.fogDensity = 0.045f;
        RenderSettings.fogMode = FogMode.Exponential;
    }

    private IEnumerator Start()
    {
        hint.text = "COLOQUE AS PEÇAS NOS ENCAIXES  [E]";
        for (float t = 0f; t < 1.2f; t += Time.deltaTime)
        {
            fade.color = new Color(0f, 0f, 0f, 1f - t / 1.2f);
            yield return null;
        }
        fade.color = Color.clear;
    }

    private void Update()
    {
        if (beaconPivot != null)
            beaconPivot.Rotate(0f, (assembling ? 360f : 60f) * Time.deltaTime, 0f);

        // Luz dos encaixes livres pulsando.
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f);
        for (int i = 0; i < slotLights.Count; i++)
            if (i >= slotPieces.Count)
                slotLights[i].sharedMaterial = pulse > 0.5f ? matSlotOn : matSlotOff;

        if (assembling || Finished || player == null)
        {
            if (prompt != null) prompt.text = "";
            return;
        }

        // Mira no encaixe livre seguinte.
        int target = -1;
        Ray ray = player.Cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, 2.8f))
            target = slots.IndexOf(hit.collider.transform);

        int next = slotPieces.Count;
        if (target >= 0 && target >= next)
        {
            prompt.text = "E para colocar a peça " + (next + 1) + "/" + pieceCount;
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                StartCoroutine(Place(next));
        }
        else if (target >= 0)
            prompt.text = "Peça encaixada";
        else
            prompt.text = "";
    }

    // Usado pelos testes: coloca todas as pecas de uma vez.
    public void PlaceAllForTest()
    {
        StartCoroutine(PlaceAllRoutine());
    }

    private IEnumerator PlaceAllRoutine()
    {
        while (slotPieces.Count < pieceCount && !assembling)
            yield return Place(slotPieces.Count);
    }

    private IEnumerator Place(int index)
    {
        if (index >= pieceCount || index < slotPieces.Count) yield break;
        Transform slot = slots[index];
        Transform piece = MakePiece(index);
        Vector3 rest = slot.position + Vector3.up * 0.12f;
        piece.position = rest + Vector3.up * 0.35f;
        slotPieces.Add(piece);
        sfx.PlayOneShot(Beep(660f + index * 110f), 0.6f);
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            piece.position = Vector3.Lerp(rest + Vector3.up * 0.35f, rest, t / 0.25f);
            yield return null;
        }
        piece.position = rest;
        slotLights[index].sharedMaterial = matSlotOn;
        UpdateMonitor();

        if (slotPieces.Count >= pieceCount)
            StartCoroutine(Assemble());
    }

    private IEnumerator Assemble()
    {
        assembling = true;
        player.Locked = true;
        hint.text = "MONTAGEM EM ANDAMENTO";
        beacon.enabled = true;
        ambience.volume = 0.5f;

        // Camera do jogador acompanha a garra.
        Coroutine look = StartCoroutine(FollowClaw());

        for (int i = 0; i < slotPieces.Count; i++)
        {
            Vector3 socket = prototype.TransformPoint(sockets[i % sockets.Count]);
            yield return claw.Transfer(slotPieces[i], socket, prototype.rotation, prototype);
            slotLights[i].sharedMaterial = matSlotOff;
            sparks.transform.position = socket;
            sparks.Play();
            sfx.PlayOneShot(Beep(440f + i * 140f), 0.7f);
            protoLens.sharedMaterial.SetColor("_EmissionColor", Color.white * (0.4f + 0.9f * (i + 1) / slotPieces.Count));
            UpdateMonitor(i + 1);
        }

        yield return claw.Park();
        StopCoroutine(look);
        hint.text = "PROTÓTIPO 1975  //  ONLINE";
        pedestalRing.sharedMaterial = matLampOn;
        yield return claw.Flourish();

        // Prototipo liga: zumbido, lente acende, tela vira estatica e vai para a cutscene.
        sfx.PlayOneShot(Beep(220f, 1.2f), 0.9f);
        for (float t = 0f; t < 1.6f; t += Time.deltaTime)
        {
            player.LookAtPoint(prototype.position + Vector3.up * 0.2f, Time.deltaTime * 3f);
            protoLens.sharedMaterial.SetColor("_EmissionColor", Color.white * (2f + Mathf.Sin(t * 30f) * (FlashSettings.Enabled ? 1.5f : 0.2f)));
            yield return null;
        }
        Finished = true;
        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            fade.color = FlashSettings.Enabled ? new Color(1f, 1f, 1f, t / 0.8f) : new Color(0f, 0f, 0f, t / 0.8f);
            yield return null;
        }
        fade.color = Color.black;
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene(nextScene);
    }

    private IEnumerator FollowClaw()
    {
        while (true)
        {
            player.LookAtPoint(claw.HeadPosition, Time.deltaTime * 2.5f);
            yield return null;
        }
    }

    private void UpdateMonitor(int assembled = -1)
    {
        if (monitorText == null) return;
        monitorText.text = assembled < 0
            ? "MONTAGEM DO PROTÓTIPO\n\nPEÇAS  " + slotPieces.Count + "/" + pieceCount + "\n\n" + (slotPieces.Count < pieceCount ? "AGUARDANDO..." : "INICIANDO GARRA")
            : "MONTAGEM DO PROTÓTIPO\n\nMONTADAS  " + assembled + "/" + pieceCount + "\n\n" + (assembled < pieceCount ? "GARRA EM OPERAÇÃO" : "CONCLUÍDO");
    }

    // ---------- Construcao ----------

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
        matFloor = Mat(new Color(0.2f, 0.2f, 0.21f), 0.35f);
        matWall = Mat(new Color(0.42f, 0.41f, 0.38f), 0.1f);
        matWallDark = Mat(new Color(0.18f, 0.19f, 0.2f), 0.15f);
        matMetal = Mat(new Color(0.55f, 0.56f, 0.57f), 0.55f, 0.6f);
        matDark = Mat(new Color(0.1f, 0.1f, 0.11f), 0.4f, 0.3f);
        matAccent = Mat(new Color(0.55f, 0.12f, 0.1f), 0.35f, 0.2f);
        matStripe = Mat(new Color(0.85f, 0.84f, 0.8f), 0.2f);
        matLampOn = Mat(Color.white, 0.5f, 0f, new Color(2.2f, 2.2f, 2.1f));
        matLampOff = Mat(new Color(0.3f, 0.3f, 0.3f), 0.5f);
        matSlotOn = Mat(new Color(0.8f, 0.85f, 1f), 0.5f, 0f, new Color(0.9f, 1.1f, 1.6f));
        matSlotOff = Mat(new Color(0.12f, 0.13f, 0.15f), 0.5f);
        matScreen = Mat(new Color(0.02f, 0.03f, 0.03f), 0.8f);
    }

    private Transform Box(string name, Transform parent, Vector3 pos, Vector3 size, Material m, bool collider = true)
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

    private Transform Cyl(string name, Transform parent, Vector3 pos, Vector3 size, Material m, bool collider = true)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        Destroy(go.GetComponent<Collider>());
        if (collider) go.AddComponent<BoxCollider>();
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = m;
        return go.transform;
    }

    private void BuildRoom()
    {
        Transform room = new GameObject("Sala").transform;
        float hw = RoomW / 2f, hd = RoomD / 2f;
        Box("Piso", room, new Vector3(0f, -0.05f, 0f), new Vector3(RoomW, 0.1f, RoomD), matFloor);
        // Juntas do piso.
        for (float x = -hw + 1f; x < hw; x += 1f)
            Box("Junta", room, new Vector3(x, 0.002f, 0f), new Vector3(0.02f, 0.004f, RoomD), matDark, false);
        for (float z = -hd + 1f; z < hd; z += 1f)
            Box("Junta", room, new Vector3(0f, 0.002f, z), new Vector3(RoomW, 0.004f, 0.02f), matDark, false);
        Box("Teto", room, new Vector3(0f, RoomH + 0.05f, 0f), new Vector3(RoomW, 0.1f, RoomD), matWallDark);

        // Paredes: lambri escuro embaixo, faixa clara, parede em cima.
        void Wall(Vector3 pos, Vector3 size)
        {
            Box("Parede", room, pos, size, matWall);
            Vector3 low = new Vector3(size.x > size.z ? size.x : size.x + 0.04f, 1.2f, size.z > size.x ? size.z : size.z + 0.04f);
            Box("Lambri", room, new Vector3(pos.x, 0.6f, pos.z), low, matWallDark, false);
            Box("Faixa", room, new Vector3(pos.x, 1.22f, pos.z), new Vector3(low.x + 0.01f, 0.06f, low.z + 0.01f), matStripe, false);
        }
        Wall(new Vector3(0f, RoomH / 2f, hd), new Vector3(RoomW, RoomH, 0.2f));
        Wall(new Vector3(0f, RoomH / 2f, -hd), new Vector3(RoomW, RoomH, 0.2f));
        Wall(new Vector3(hw, RoomH / 2f, 0f), new Vector3(0.2f, RoomH, RoomD));
        Wall(new Vector3(-hw, RoomH / 2f, 0f), new Vector3(0.2f, RoomH, RoomD));

        // Porta de entrada (fechada) atras do jogador.
        Box("Porta", room, new Vector3(0f, 1.1f, -hd + 0.12f), new Vector3(1.3f, 2.2f, 0.06f), matMetal, false);
        Box("PortaJanela", room, new Vector3(0f, 1.6f, -hd + 0.16f), new Vector3(0.5f, 0.35f, 0.02f), matScreen, false);

        // Luminarias de teto (4) com luz.
        foreach (Vector2 p in new[] { new Vector2(-2.6f, -2.2f), new Vector2(2.6f, -2.2f), new Vector2(-2.6f, 2.4f), new Vector2(2.6f, 2.4f) })
        {
            Box("Luminaria", room, new Vector3(p.x, RoomH - 0.08f, p.y), new Vector3(1.4f, 0.08f, 0.3f), matDark, false);
            Box("Tubo", room, new Vector3(p.x, RoomH - 0.13f, p.y), new Vector3(1.3f, 0.03f, 0.12f), matLampOn, false);
            Light l = new GameObject("Luz", typeof(Light)).GetComponent<Light>();
            l.transform.SetParent(room, false);
            l.transform.localPosition = new Vector3(p.x, RoomH - 0.4f, p.y);
            l.type = LightType.Point;
            l.range = 7f;
            l.intensity = 1.6f;
            l.color = new Color(0.9f, 0.93f, 1f);
            l.shadows = LightShadows.Soft;
        }

        // Canaletas e canos nas paredes.
        Cyl("Cano", room, new Vector3(-hw + 0.25f, 3.6f, 0f), new Vector3(0.12f, RoomD / 2f, 0.12f), matMetal, false).localRotation = Quaternion.Euler(90f, 0f, 0f);
        Cyl("Cano", room, new Vector3(-hw + 0.25f, 3.35f, 0f), new Vector3(0.08f, RoomD / 2f, 0.08f), matAccent, false).localRotation = Quaternion.Euler(90f, 0f, 0f);
        Cyl("Cano", room, new Vector3(hw - 0.25f, 3.6f, 0f), new Vector3(0.12f, RoomD / 2f, 0.12f), matMetal, false).localRotation = Quaternion.Euler(90f, 0f, 0f);
        Box("Canaleta", room, new Vector3(0f, RoomH - 0.3f, hd - 0.25f), new Vector3(RoomW - 0.6f, 0.12f, 0.3f), matDark, false);
    }

    private void BuildConsole()
    {
        Transform console = new GameObject("Console").transform;
        console.position = new Vector3(0f, 0f, -1.6f);
        Box("Base", console, new Vector3(0f, 0.45f, 0f), new Vector3(3.6f, 0.9f, 0.8f), matDark);
        Box("Tampo", console, new Vector3(0f, 0.92f, 0f), new Vector3(3.7f, 0.06f, 0.9f), matMetal);
        Box("Frente", console, new Vector3(0f, 0.5f, -0.41f), new Vector3(3.5f, 0.7f, 0.02f), matAccent, false);
        // Etiqueta na frente.
        TextMeshPro label = WorldText(console, new Vector3(0f, 0.62f, -0.43f), Quaternion.identity, "ENCAIXES DE MONTAGEM", 0.16f, new Color(0.9f, 0.88f, 0.84f));
        label.rectTransform.sizeDelta = new Vector2(3.4f, 0.3f);

        for (int i = 0; i < pieceCount; i++)
        {
            float x = Mathf.Lerp(-1.3f, 1.3f, pieceCount == 1 ? 0.5f : i / (float)(pieceCount - 1));
            Transform slot = Box("Encaixe" + (i + 1), console, new Vector3(x, 0.97f, 0f), new Vector3(0.42f, 0.06f, 0.42f), matDark);
            BoxCollider bc = slot.GetComponent<BoxCollider>();
            bc.size = new Vector3(1.2f, 8f, 1.2f);
            bc.center = new Vector3(0f, 3f, 0f);
            Transform ring = Box("Luz", console, new Vector3(x, 1.0f, 0f), new Vector3(0.34f, 0.01f, 0.34f), matSlotOff, false);
            Transform inner = Box("Fundo", console, new Vector3(x, 1.006f, 0f), new Vector3(0.28f, 0.01f, 0.28f), matDark, false);
            TextMeshPro n = WorldText(console, new Vector3(x, 0.951f, -0.31f), Quaternion.Euler(60f, 0f, 0f), (i + 1).ToString("00"), 0.12f, new Color(0.9f, 0.88f, 0.84f));
            n.rectTransform.sizeDelta = new Vector2(0.3f, 0.15f);
            slots.Add(slot);
            slotLights.Add(ring.GetComponent<Renderer>());
        }
    }

    private void BuildPedestal()
    {
        Transform ped = new GameObject("Pedestal").transform;
        ped.position = new Vector3(0f, 0f, 1.8f);
        // Faixas de seguranca no piso em volta.
        for (int i = 0; i < 16; i++)
        {
            float a = i * 22.5f;
            Transform s = Box("FaixaPiso", ped, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.005f, 1.5f), new Vector3(0.4f, 0.01f, 0.18f), i % 2 == 0 ? matAccent : matStripe, false);
            s.localRotation = Quaternion.Euler(0f, a, 0f);
        }
        Cyl("Base", ped, new Vector3(0f, 0.25f, 0f), new Vector3(1.6f, 0.25f, 1.6f), matDark);
        Cyl("Coluna", ped, new Vector3(0f, 0.65f, 0f), new Vector3(0.9f, 0.2f, 0.9f), matMetal);
        pedestalRing = Cyl("Anel", ped, new Vector3(0f, 0.86f, 0f), new Vector3(1.0f, 0.015f, 1.0f), matSlotOff, false).GetComponent<Renderer>();
        Cyl("Mesa", ped, new Vector3(0f, 0.9f, 0f), new Vector3(0.8f, 0.03f, 0.8f), matMetal);

        // Prototipo (carcaca da camera de 1975): corpo, gravador e lente; as pecas encaixam nele.
        prototype = new GameObject("Prototipo").transform;
        prototype.SetParent(ped, false);
        prototype.localPosition = new Vector3(0f, 0.93f, 0f);
        prototype.localRotation = Quaternion.Euler(0f, 180f, 0f);
        Box("Corpo", prototype, new Vector3(0f, 0.14f, 0f), new Vector3(0.5f, 0.28f, 0.32f), matMetal, false);
        Box("Gravador", prototype, new Vector3(-0.38f, 0.1f, 0f), new Vector3(0.26f, 0.2f, 0.3f), matStripe, false);
        Box("Alca", prototype, new Vector3(0f, 0.32f, 0f), new Vector3(0.3f, 0.04f, 0.06f), matDark, false);
        Transform lens = Cyl("Lente", prototype, new Vector3(0.12f, 0.14f, -0.2f), new Vector3(0.16f, 0.05f, 0.16f), Mat(new Color(0.15f, 0.2f, 0.25f), 0.9f, 0f, Color.black), false);
        lens.localRotation = Quaternion.Euler(90f, 0f, 0f);
        protoLens = lens.GetComponent<Renderer>();
        sockets.Add(new Vector3(-0.12f, 0.3f, 0.05f));
        sockets.Add(new Vector3(0.14f, 0.3f, 0.06f));
        sockets.Add(new Vector3(-0.38f, 0.22f, 0f));
        sockets.Add(new Vector3(0.05f, 0.16f, 0.17f));
        sockets.Add(new Vector3(-0.2f, 0.16f, 0.17f));
        sockets.Add(new Vector3(0.2f, 0.16f, 0.17f));

        // Giroflex vermelho no alto (liga na montagem).
        beaconPivot = new GameObject("Giroflex").transform;
        beaconPivot.position = new Vector3(4.9f, 3.9f, 4.4f);
        Cyl("Cupula", beaconPivot, Vector3.zero, new Vector3(0.18f, 0.08f, 0.18f), Mat(new Color(0.6f, 0.05f, 0.05f), 0.6f, 0f, new Color(1.2f, 0.1f, 0.1f)), false);
        beacon = new GameObject("Luz", typeof(Light)).GetComponent<Light>();
        beacon.transform.SetParent(beaconPivot, false);
        beacon.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
        beacon.type = LightType.Spot;
        beacon.spotAngle = 60f;
        beacon.range = 14f;
        beacon.intensity = FlashSettings.Enabled ? 6f : 2f;
        beacon.color = new Color(1f, 0.15f, 0.1f);
        beacon.enabled = false;

        // Faiscas ao encaixar.
        GameObject sp = new GameObject("Faiscas", typeof(ParticleSystem));
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
        var r = sp.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = Mat(Color.white, 0f, 0f, new Color(3f, 2.6f, 2f));
    }

    private void BuildClaw()
    {
        GameObject go = new GameObject("GarraMecanica");
        go.transform.position = new Vector3(0f, 0f, 0f);
        claw = go.AddComponent<ClawMachine>();
        claw.ceilingY = RoomH - 0.45f;
        claw.railX = new Vector2(-2.4f, 2.4f);
        claw.railZ = new Vector2(-2.4f, 3.2f);
        claw.Build(matMetal, matDark, matAccent, matLampOn);
    }

    private void BuildDressing()
    {
        // Monitor CRT na parede do fundo com o status da montagem.
        Transform mon = new GameObject("Monitor").transform;
        mon.position = new Vector3(-3.4f, 2.4f, RoomD / 2f - 0.15f);
        Box("Caixa", mon, Vector3.zero, new Vector3(1.6f, 1.15f, 0.25f), matStripe, false);
        Box("Tela", mon, new Vector3(0f, 0.03f, -0.13f), new Vector3(1.35f, 0.9f, 0.02f), matScreen, false);
        monitorText = WorldText(mon, new Vector3(0f, 0.03f, -0.15f), Quaternion.identity, "", 0.11f, new Color(0.85f, 0.95f, 0.9f));
        monitorText.rectTransform.sizeDelta = new Vector2(1.25f, 0.8f);
        monitorText.alignment = TextAlignmentOptions.Center;
        UpdateMonitor();

        // Placa de aviso.
        Transform sign = new GameObject("Placa").transform;
        sign.position = new Vector3(2.6f, 2.6f, RoomD / 2f - 0.12f);
        Box("Fundo", sign, Vector3.zero, new Vector3(2.2f, 0.7f, 0.04f), matStripe, false);
        Box("Faixa", sign, new Vector3(0f, 0.27f, -0.025f), new Vector3(2.2f, 0.14f, 0.01f), matAccent, false);
        TextMeshPro st = WorldText(sign, new Vector3(0f, -0.05f, -0.03f), Quaternion.identity, "PERIGO  //  GARRA EM OPERAÇÃO\nLAB 1975  -  MONTAGEM DO PROTÓTIPO", 0.11f, new Color(0.08f, 0.08f, 0.08f));
        st.rectTransform.sizeDelta = new Vector2(2.1f, 0.5f);

        // Bancada lateral com ferramentas e armario de cabos (cenario coerente com a sala).
        Box("Bancada", null, new Vector3(-RoomW / 2f + 0.55f, 0.45f, 1.2f), new Vector3(0.9f, 0.9f, 2.6f), matDark);
        Box("BancadaTampo", null, new Vector3(-RoomW / 2f + 0.55f, 0.92f, 1.2f), new Vector3(0.95f, 0.05f, 2.7f), matMetal, false);
        for (int i = 0; i < 3; i++)
            Box("Gaveta", null, new Vector3(-RoomW / 2f + 1.01f, 0.25f + i * 0.22f, 1.2f), new Vector3(0.02f, 0.16f, 2.2f), matWallDark, false);
        Box("Armario", null, new Vector3(RoomW / 2f - 0.45f, 1.1f, 2.5f), new Vector3(0.7f, 2.2f, 1.4f), matWallDark);
        Box("ArmarioPorta", null, new Vector3(RoomW / 2f - 0.81f, 1.1f, 2.5f), new Vector3(0.02f, 2.0f, 1.3f), matMetal, false);
        Box("Painel", null, new Vector3(RoomW / 2f - 0.15f, 1.5f, -2f), new Vector3(0.1f, 1.2f, 1.6f), matDark, false);
        for (int i = 0; i < 6; i++)
            Box("Botao", null, new Vector3(RoomW / 2f - 0.21f, 1.2f + (i / 3) * 0.3f, -2.5f + (i % 3) * 0.5f), new Vector3(0.03f, 0.1f, 0.1f), i % 2 == 0 ? matAccent : matSlotOn, false);
    }

    private void BuildPlayer()
    {
        GameObject p = new GameObject("Jogador");
        p.transform.position = new Vector3(0f, 0.05f, -3.6f);
        player = p.AddComponent<AssemblyPlayer>();
    }

    private void BuildUI()
    {
        GameObject c = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = c.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = c.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        Image dot = new GameObject("Mira", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        dot.transform.SetParent(c.transform, false);
        dot.rectTransform.sizeDelta = new Vector2(4f, 4f);
        dot.color = new Color(1f, 1f, 1f, 0.8f);

        prompt = UiText(c.transform, new Vector2(0f, -60f), 20f, 0.95f);
        hint = UiText(c.transform, new Vector2(0f, 300f), 18f, 0.6f);

        fade = new GameObject("Fade", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        fade.transform.SetParent(c.transform, false);
        fade.rectTransform.anchorMin = Vector2.zero;
        fade.rectTransform.anchorMax = Vector2.one;
        fade.rectTransform.offsetMin = fade.rectTransform.offsetMax = Vector2.zero;
        fade.color = Color.black;
        fade.raycastTarget = false;
    }

    private TextMeshProUGUI UiText(Transform parent, Vector2 pos, float size, float alpha)
    {
        TextMeshProUGUI t = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        t.transform.SetParent(parent, false);
        if (font != null) t.font = font;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(0.9f, 0.9f, 0.88f, alpha);
        t.rectTransform.sizeDelta = new Vector2(900f, 40f);
        t.rectTransform.anchoredPosition = pos;
        t.raycastTarget = false;
        return t;
    }

    private TextMeshPro WorldText(Transform parent, Vector3 pos, Quaternion rot, string text, float size, Color color)
    {
        GameObject go = new GameObject("Texto", typeof(TextMeshPro));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        TextMeshPro t = go.GetComponent<TextMeshPro>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size * 10f;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = false;
        return t;
    }

    private Transform MakePiece(int index)
    {
        GameObject piece;
        if (pieceModel != null)
        {
            piece = Instantiate(pieceModel);
            foreach (Collider col in piece.GetComponentsInChildren<Collider>()) Destroy(col);
            Renderer[] rs = piece.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                foreach (Renderer r in rs) b.Encapsulate(r.bounds);
                float max = Mathf.Max(b.size.x, b.size.y, b.size.z);
                if (max > 0.0001f) piece.transform.localScale *= 0.2f / max;
            }
        }
        else
        {
            piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(piece.GetComponent<Collider>());
            piece.transform.localScale = new Vector3(0.16f, 0.08f, 0.12f);
            piece.GetComponent<Renderer>().sharedMaterial = matAccent;
        }
        piece.name = "Peca" + (index + 1);
        piece.transform.rotation = Quaternion.Euler(0f, index * 37f, 0f);
        return piece.transform;
    }

    private void BuildAudio()
    {
        ambience = gameObject.AddComponent<AudioSource>();
        ambience.loop = true;
        ambience.volume = 0.25f;
        ambience.clip = Hum();
        ambience.Play();
        sfx = gameObject.AddComponent<AudioSource>();
    }

    private static AudioClip Hum()
    {
        const int rate = 44100;
        int n = rate * 2;
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            d[i] = (Mathf.Sin(t * 2f * Mathf.PI * 60f) * 0.5f + Mathf.Sin(t * 2f * Mathf.PI * 120f) * 0.25f + Mathf.Sin(t * 2f * Mathf.PI * 180f) * 0.1f) * 0.3f;
        }
        AudioClip c = AudioClip.Create("SalaHum", n, 1, rate, false);
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
            float env = Mathf.Min(1f, t / 0.005f) * (1f - t / seconds);
            d[i] = (Mathf.Sin(t * 2f * Mathf.PI * freq) >= 0f ? 1f : -1f) * env * 0.35f;
        }
        AudioClip c = AudioClip.Create("Beep", n, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }
}
