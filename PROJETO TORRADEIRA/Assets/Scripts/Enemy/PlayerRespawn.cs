using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// "Tentar de novo" sem recarregar a fase: tudo que o jogador conquistou (missoes, fotos,
// pecas, baterias, portas) continua como estava. O jogador acorda atordoado num ponto
// aleatorio do mapa, sempre longe do inimigo e fora da vista dele.
public static class PlayerRespawn
{
    private const float MinEnemyDistance = 22f;   // em linha reta
    private const float MinPathDistance = 30f;    // andando pelo NavMesh

    public static int Deaths { get; private set; }
    public static void ResetDeaths() => Deaths = 0;

    // So para a cena de teste do cientista.
    public static int SimulateDeath() => ++Deaths;

    public static bool TryRespawn()
    {
        PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
        TemporalEraserController enemy = Object.FindFirstObjectByType<TemporalEraserController>();
        if (player == null) return false;

        // Desfaz o que o jumpscare deixou travado.
        // O jumpscare desliga o Cinemachine para controlar a camera: liga de novo
        // (sem isso a camera fica presa e nao olha para cima/baixo).
        if (RunCameraRig.Instance != null) RunCameraRig.Instance.SetBrainEnabled(true);

        GameObject hud = GameObject.Find("HUDCanvas");
        if (hud != null && hud.TryGetComponent(out Canvas hudCanvas)) hudCanvas.enabled = true;

        Vector3 enemyPos = player.transform.position + Vector3.forward * 999f;
        if (enemy != null)
        {
            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = true;
            enemy.enabled = true;
            Animator anim = enemy.GetComponentInChildren<Animator>();
            if (anim != null) anim.enabled = true;
            enemy.ResetToStart(3f); // volta a cacar logo (antes ficava ~12s parado)
            enemyPos = enemy.transform.position;
        }

        if (!FindSpawn(enemyPos, player.transform.position, out Vector3 spawn))
            return false;

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        Vector3 look = enemyPos - spawn;
        look.y = 0f;
        // Acorda olhando para o lado oposto ao inimigo.
        Quaternion rot = look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(-look.normalized) : player.transform.rotation;
        player.transform.SetPositionAndRotation(spawn + Vector3.up * 0.05f, rot);
        if (cc != null) cc.enabled = true;
        Physics.SyncTransforms();

        player.gameObject.AddComponent<PlayerStunned>();
        Deaths++;
        // O cientista fala pelo radio logo depois dos olhos abrirem.
        ScientistRadio.Say(Deaths, 1.6f);
        return true;
    }

    private static bool FindSpawn(Vector3 enemy, Vector3 deathPoint, out Vector3 best)
    {
        best = Vector3.zero;
        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        if (tri.indices.Length == 0) return false;

        float bestScore = float.MinValue;
        NavMeshPath path = new NavMeshPath();
        for (int i = 0; i < 160; i++)
        {
            int t = Random.Range(0, tri.indices.Length / 3) * 3;
            Vector3 a = tri.vertices[tri.indices[t]], b = tri.vertices[tri.indices[t + 1]], c = tri.vertices[tri.indices[t + 2]];
            float r1 = Random.value, r2 = Random.value;
            if (r1 + r2 > 1f) { r1 = 1f - r1; r2 = 1f - r2; }
            Vector3 p = a + (b - a) * r1 + (c - a) * r2;

            if (!NavMesh.SamplePosition(p, out NavMeshHit hit, 0.5f, NavMesh.AllAreas)) continue;
            p = hit.position;
            // Precisa caber o jogador (nada em cima nem encostado).
            if (Physics.CheckCapsule(p + Vector3.up * 0.5f, p + Vector3.up * 1.6f, 0.35f, ~0, QueryTriggerInteraction.Ignore)) continue;

            float straight = Vector3.Distance(p, enemy);
            if (straight < MinEnemyDistance) continue;

            float walk = straight;
            if (NavMesh.CalculatePath(enemy, p, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                walk = 0f;
                for (int k = 1; k < path.corners.Length; k++) walk += Vector3.Distance(path.corners[k - 1], path.corners[k]);
            }
            else continue; // area isolada: o jogador ficaria preso

            if (walk < MinPathDistance) continue;
            bool visible = !Physics.Linecast(enemy + Vector3.up * 1.6f, p + Vector3.up * 1.6f, ~0, QueryTriggerInteraction.Ignore);
            if (visible) continue;

            // Aleatorio, com preferencia leve por longe do inimigo e longe de onde morreu.
            float score = Mathf.Min(walk, 60f) + Mathf.Min(Vector3.Distance(p, deathPoint), 20f) * 0.5f + Random.value * 25f;
            if (score > bestScore) { bestScore = score; best = p; }
        }
        return bestScore > float.MinValue;
    }
}

// Atordoamento ao renascer: acorda piscando, visao borrada e torta, som abafado com zumbido,
// respiracao ofegante; anda devagar e nao corre ate passar.
public class PlayerStunned : MonoBehaviour
{
    private const float Duration = 7f;

    private PlayerMovement movement;
    private float baseWalk, baseSprint;
    private Volume volume;
    private DepthOfField dof;
    private ChromaticAberration chroma;
    private LensDistortion lens;
    private ColorAdjustments color;
    private MotionBlur blur;
    private Image eyelids;
    private GameObject canvasGo;
    private AudioSource tinnitus, breath;
    private AudioLowPassFilter muffle;
    private float t;

    private void Start()
    {
        movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            baseWalk = movement.moveSpeed;
            baseSprint = movement.sprintSpeed;
        }

        GameObject vGo = new GameObject("AtordoadoVolume");
        vGo.transform.SetParent(transform, false);
        volume = vGo.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 200f;
        VolumeProfile p = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.profile = p;
        dof = p.Add<DepthOfField>(true);
        dof.mode.Override(DepthOfFieldMode.Gaussian);
        dof.gaussianStart.Override(0f);
        dof.gaussianEnd.Override(2f);
        dof.gaussianMaxRadius.Override(1.5f);
        chroma = p.Add<ChromaticAberration>(true);
        lens = p.Add<LensDistortion>(true);
        color = p.Add<ColorAdjustments>(true);
        blur = p.Add<MotionBlur>(true);
        blur.intensity.Override(1f);
        blur.quality.Override(MotionBlurQuality.High);

        canvasGo = new GameObject("Palpebras", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        Canvas c = canvasGo.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 31000;
        GameObject iGo = new GameObject("Preto", typeof(RectTransform), typeof(Image));
        iGo.transform.SetParent(canvasGo.transform, false);
        RectTransform r = (RectTransform)iGo.transform;
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        eyelids = iGo.GetComponent<Image>();
        eyelids.color = Color.black;
        eyelids.raycastTarget = false;

        tinnitus = NewSource(BuildTinnitus(), 0.12f, true);
        breath = NewSource(BuildGasp(), 0.5f, false);
        breath.Play();

        AudioListener listener = FindFirstObjectByType<AudioListener>();
        if (listener != null)
        {
            muffle = listener.GetComponent<AudioLowPassFilter>();
            if (muffle == null) muffle = listener.gameObject.AddComponent<AudioLowPassFilter>();
            muffle.enabled = true;
        }
    }

    private AudioSource NewSource(AudioClip clip, float vol, bool loop)
    {
        AudioSource s = gameObject.AddComponent<AudioSource>();
        s.clip = clip;
        s.volume = vol;
        s.loop = loop;
        s.spatialBlend = 0f;
        s.bypassListenerEffects = true; // o zumbido nao e abafado
        if (loop) s.Play();
        return s;
    }

    private void Update()
    {
        if (PauseMenu.IsPaused) return;
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / Duration);      // 0 = acabou de acordar, 1 = normal
        float fade = 1f - Mathf.SmoothStep(0f, 1f, k);

        // Piscadas pesadas nos primeiros segundos.
        float blink = 0f;
        if (t < 0.6f) blink = 1f;
        else if (t < 3.2f) blink = Mathf.Pow(Mathf.Abs(Mathf.Sin((t - 0.6f) * 1.6f)), 6f) * (1f - (t - 0.6f) / 2.6f);
        float openFade = t < 1.4f ? Mathf.Clamp01(1f - (t - 0.6f) / 0.8f) : 0f;
        eyelids.color = new Color(0f, 0f, 0f, Mathf.Max(blink, openFade));

        float wobble = Mathf.Sin(t * 1.7f) * 0.5f + Mathf.Sin(t * 0.9f) * 0.5f;
        dof.gaussianEnd.Override(Mathf.Lerp(30f, 1.5f, fade));
        chroma.intensity.Override(fade);
        lens.intensity.Override(wobble * 0.35f * fade);
        lens.scale.Override(1f + 0.05f * fade);
        color.saturation.Override(-60f * fade);
        color.postExposure.Override(-0.8f * fade);
        volume.weight = fade > 0.01f ? 1f : 0f;

        if (movement != null)
        {
            movement.moveSpeed = Mathf.Lerp(baseWalk * 0.45f, baseWalk, k);
            movement.sprintSpeed = Mathf.Lerp(baseWalk * 0.45f, baseSprint, k * k);
        }

        if (muffle != null) muffle.cutoffFrequency = Mathf.Lerp(22000f, 500f, fade * fade);
        tinnitus.volume = 0.12f * fade;

        if (t >= Duration) Destroy(this);
    }

    private void OnDestroy()
    {
        if (movement != null)
        {
            movement.moveSpeed = baseWalk;
            movement.sprintSpeed = baseSprint;
        }
        if (muffle != null) { muffle.cutoffFrequency = 22000f; muffle.enabled = false; }
        if (volume != null) Destroy(volume.gameObject);
        if (canvasGo != null) Destroy(canvasGo);
        if (tinnitus != null) Destroy(tinnitus);
        if (breath != null) Destroy(breath);
    }

    private static AudioClip BuildTinnitus()
    {
        const int rate = 44100;
        int n = rate * 2;
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float x = (float)i / rate;
            d[i] = (Mathf.Sin(2f * Mathf.PI * 5200f * x) * 0.6f + Mathf.Sin(2f * Mathf.PI * 5230f * x) * 0.4f) * 0.25f;
        }
        AudioClip c = AudioClip.Create("Zumbido", n, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    // Puxada de ar forte + duas respiracoes ofegantes.
    private static AudioClip BuildGasp()
    {
        const int rate = 44100;
        int n = (int)(rate * 3.2f);
        float[] d = new float[n];
        var rng = new System.Random(11);
        float lp = 0f;
        float[] starts = { 0.0f, 1.3f, 2.3f };
        float[] lens = { 0.9f, 0.7f, 0.6f };
        float[] amps = { 1f, 0.55f, 0.4f };
        for (int i = 0; i < n; i++)
        {
            float x = (float)i / rate;
            float env = 0f, bright = 0.2f;
            for (int b = 0; b < 3; b++)
            {
                float u = (x - starts[b]) / lens[b];
                if (u < 0f || u > 1f) continue;
                env += Mathf.Sin(u * Mathf.PI) * amps[b] * (b == 0 ? Mathf.Lerp(1f, 0.6f, u) : 1f);
                bright = b == 0 ? 0.45f : 0.2f;   // a primeira e um "arfar" mais agudo
            }
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += (noise - lp) * bright;
            d[i] = lp * env * 0.7f;
        }
        AudioClip c = AudioClip.Create("Arfar", n, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }
}
