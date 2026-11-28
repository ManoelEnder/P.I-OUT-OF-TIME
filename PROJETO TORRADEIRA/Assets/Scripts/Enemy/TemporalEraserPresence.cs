using UnityEngine;
using UnityEngine.AI;

// Tudo o que faz o TemporalEraser "existir" na cena, sem mexer na IA:
// - velocidade da animacao de andar segue o NavMeshAgent (parado = congela no meio do passo);
// - drone grave em loop + passos, mais fortes quando esta perseguindo;
// - glitch "apagado do tempo": o modelo treme e some por instantes.
[RequireComponent(typeof(NavMeshAgent))]
public class TemporalEraserPresence : MonoBehaviour
{
    [Header("Animacao")]
    [SerializeField] private Animator animator;
    [SerializeField] private float clipWalkSpeed = 1.3f;
    [SerializeField] private float maxAnimationSpeed = 2.2f;

    [Header("Som")]
    [SerializeField] private AudioSource droneSource;
    [SerializeField] private AudioSource stepSource;
    [SerializeField] private float roamingVolume = 0.35f;
    [SerializeField] private float chasingVolume = 0.8f;
    [SerializeField] private float strideLength = 0.8f;

    [Header("Glitch")]
    [SerializeField] private Transform model;
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Vector2 glitchInterval = new Vector2(0.6f, 2.5f);
    [SerializeField] private float jitter = 0.06f;

    private NavMeshAgent agent;
    private TemporalEraserController controller;
    private Vector3 modelRest;
    private float nextGlitch;
    private float glitchEnd;
    private float stepDistance;

    private static readonly int SpeedId = Animator.StringToHash("Speed");
    private bool hasSpeedParameter;

    // Mixagem: o inimigo estava alto demais. Tudo dele passa por este fator.
    private const float Loudness = 0.4f;

    private static AudioClip droneClip;
    private static AudioClip stepClip;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        controller = GetComponent<TemporalEraserController>();

        // Aviso de proximidade (batida de coracao + bordas escuras).
        if (controller != null && FindFirstObjectByType<EnemyProximityWarning>() == null)
        {
            var warning = new GameObject("EnemyProximityWarning").AddComponent<EnemyProximityWarning>();
            warning.Init(controller);
        }

        if (model != null)
            modelRest = model.localPosition;

        if (animator != null)
        {
            animator.applyRootMotion = false;

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash == SpeedId && parameter.type == AnimatorControllerParameterType.Float)
                    hasSpeedParameter = true;
            }
        }

        if (droneSource != null)
        {
            if (droneClip == null)
                droneClip = BuildDrone();

            droneSource.clip = droneClip;
            droneSource.loop = true;
            droneSource.volume = roamingVolume * Loudness;
            Make3D(droneSource);
            droneSource.Play();
        }
        if (stepSource != null)
            Make3D(stepSource);
        {
        }

        if (stepClip == null)
            stepClip = BuildStep();

        ScheduleGlitch();
    }

    private void Update()
    {
        bool chasing = controller != null && controller.IsChasing;
        float speed = agent.velocity.magnitude;

        if (animator != null)
        {
            if (hasSpeedParameter)
            {
                // Idle/Walk/Run misturados pela velocidade real: o passo acompanha o chao.
                animator.speed = 1f;
                animator.SetFloat(SpeedId, speed, 0.12f, Time.deltaTime);
            }
            else
            {
                animator.speed = Mathf.Clamp(speed / Mathf.Max(0.1f, clipWalkSpeed), 0f, maxAnimationSpeed);
            }
        }

        if (droneSource != null)
        {
            float target = (chasing ? chasingVolume : roamingVolume) * Loudness;
            droneSource.volume = Mathf.MoveTowards(droneSource.volume, target, Time.deltaTime * 0.8f);
            droneSource.pitch = Mathf.MoveTowards(droneSource.pitch, chasing ? 1.15f : 1f, Time.deltaTime * 0.5f);
        }

        UpdateSteps(speed, chasing);
        UpdateGlitch(chasing);
    }

    private void UpdateSteps(float speed, bool chasing)
    {
        if (stepSource == null || speed < 0.2f)
            return;

        stepDistance += speed * Time.deltaTime;

        if (stepDistance < strideLength)
            return;

        stepDistance = 0f;
        stepSource.pitch = Random.Range(0.85f, 1.05f);
        stepSource.PlayOneShot(stepClip, (chasing ? 0.9f : 0.55f) * Loudness * 1.5f);
    }

    // Som 3D que some com a distancia (antes dava para ouvir alto pelo mapa todo).
    private static void Make3D(AudioSource s)
    {
        s.spatialBlend = 1f;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.minDistance = 2f;
        s.maxDistance = 22f;
        s.dopplerLevel = 0f;
    }

    private void UpdateGlitch(bool chasing)
    {
        if (model == null || Time.timeScale <= 0f)
            return;

        if (Time.time < glitchEnd)
        {
            model.localPosition = modelRest + Random.insideUnitSphere * jitter;

            // Some e volta em quadros alternados; com flashes desligados so treme.
            if (FlashSettings.Enabled)
                SetVisible(Random.value > 0.45f);

            return;
        }

        if (glitchEnd > 0f)
        {
            glitchEnd = 0f;
            model.localPosition = modelRest;
            SetVisible(true);
        }

        if (Time.time >= nextGlitch)
        {
            glitchEnd = Time.time + Random.Range(0.06f, chasing ? 0.25f : 0.15f);
            ScheduleGlitch(chasing ? 0.4f : 1f);
        }
    }

    // Glitch forte (nocaute pelo flash).
    public void Shock(float seconds)
    {
        glitchEnd = Time.time + seconds;
        ScheduleGlitch(2f);
    }

    private void ScheduleGlitch(float scale = 1f)
    {
        nextGlitch = Time.time + Random.Range(glitchInterval.x, glitchInterval.y) * scale;
    }

    private void SetVisible(bool visible)
    {
        if (renderers == null)
            return;

        foreach (Renderer r in renderers)
        {
            if (r != null)
                r.enabled = visible;
        }
    }

    private void OnDisable()
    {
        if (model != null)
            model.localPosition = modelRest;

        SetVisible(true);
    }

    // Drone: duas senoides graves desafinadas (batimento) + ruido filtrado que "chia".
    private static AudioClip BuildDrone()
    {
        const int rate = 44100;
        const float seconds = 4f;
        int length = (int)(rate * seconds);
        float[] data = new float[length];
        var random = new System.Random(1975);
        float noise = 0f;

        for (int i = 0; i < length; i++)
        {
            float t = i / (float)rate;
            // Frequencias inteiras de ciclos em 4 s para o loop nao estalar.
            float s = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.35f
                    + Mathf.Sin(2f * Mathf.PI * 55.5f * t) * 0.3f
                    + Mathf.Sin(2f * Mathf.PI * 110.25f * t) * 0.12f;

            noise = noise * 0.97f + (float)(random.NextDouble() * 2.0 - 1.0) * 0.03f;
            float crackle = Mathf.Sin(2f * Mathf.PI * 0.75f * t) > 0.92f ? noise * 4f : 0f;

            data[i] = Mathf.Clamp((s + noise * 1.5f + crackle) * 0.6f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("EraserDrone", length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Passo: baque grave curto com um pouco de ruido.
    private static AudioClip BuildStep()
    {
        const int rate = 44100;
        int length = (int)(rate * 0.18f);
        float[] data = new float[length];
        var random = new System.Random(7);

        for (int i = 0; i < length; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Exp(-t * 28f);
            float thump = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(90f, 45f, t / 0.18f) * t);
            float grit = (float)(random.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 60f) * 0.35f;
            data[i] = (thump * 0.8f + grit) * env;
        }

        AudioClip clip = AudioClip.Create("EraserStep", length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
