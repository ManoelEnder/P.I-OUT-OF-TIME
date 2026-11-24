using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Eventos aleatorios na fase: luz piscando, porta abrindo/fechando sozinha e passos atras do
// jogador. Um a cada 25-60 s de jogo, perto (5-20 m) e de preferencia fora da visao.
// Cria-se sozinho quando a cena Principal carrega.
public class RandomEvents : MonoBehaviour
{
    [SerializeField] private Vector2 interval = new Vector2(25f, 60f);
    [SerializeField] private float minDistance = 5f;
    [SerializeField] private float maxDistance = 20f;

    private Transform player;
    private Camera view;
    private float nextEvent;
    private AudioSource steps;
    private static AudioClip stepClip;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Principal" && FindFirstObjectByType<RandomEvents>() == null)
            new GameObject("RandomEvents").AddComponent<RandomEvents>();
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        player = p != null ? p.transform : null;
        view = Camera.main;

        GameObject stepGo = new GameObject("PhantomSteps", typeof(AudioSource));
        stepGo.transform.SetParent(transform, false);
        steps = stepGo.GetComponent<AudioSource>();
        steps.spatialBlend = 1f;
        steps.rolloffMode = AudioRolloffMode.Linear;
        steps.minDistance = 2f;
        steps.maxDistance = 20f;
        steps.playOnAwake = false;
        if (stepClip == null)
            stepClip = BuildStep();

        Schedule();
    }

    private void Schedule()
    {
        nextEvent = Time.time + Random.Range(interval.x, interval.y);
    }

    private void Update()
    {
        // Time.time nao anda na pausa/intro; derrota tambem segura os eventos.
        if (player == null || DefeatController.IsDefeated || Time.time < nextEvent)
            return;

        Schedule();

        switch (Random.Range(0, 3))
        {
            case 0:
                if (!TryFlicker() && !TryDoor())
                    StartCoroutine(Footsteps());
                break;
            case 1:
                if (!TryDoor() && !TryFlicker())
                    StartCoroutine(Footsteps());
                break;
            default:
                StartCoroutine(Footsteps());
                break;
        }
    }

    private bool InRange(Vector3 p)
    {
        float d = Vector3.Distance(p, player.position);
        return d >= minDistance && d <= maxDistance && Mathf.Abs(p.y - player.position.y) < 4f;
    }

    private bool OutOfSight(Vector3 p)
    {
        if (view == null)
            view = Camera.main;
        if (view == null)
            return true;
        Vector3 v = view.WorldToViewportPoint(p);
        return v.z < 0f || v.x < -0.05f || v.x > 1.05f || v.y < -0.05f || v.y > 1.05f;
    }

    // ---------- Luz piscando ----------

    private bool TryFlicker()
    {
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None)
            .Where(l => l.enabled && l.type != LightType.Directional && l.intensity > 0.05f && InRange(l.transform.position))
            .ToArray();
        if (lights.Length == 0)
            return false;

        StartCoroutine(Flicker(lights[Random.Range(0, lights.Length)]));
        return true;
    }

    private IEnumerator Flicker(Light light)
    {
        float original = light.intensity;
        float end = Time.time + Random.Range(0.9f, 1.8f);

        while (Time.time < end && light != null)
        {
            // Com flashes desligados (fotossensibilidade) so esmaece, sem piscar.
            light.intensity = FlashSettings.Enabled
                ? (Random.value < 0.5f ? original * Random.Range(0f, 0.2f) : original)
                : original * (0.45f + 0.15f * Mathf.Sin(Time.time * 6f));
            yield return new WaitForSeconds(FlashSettings.Enabled ? Random.Range(0.03f, 0.12f) : 0.05f);
        }

        if (light != null)
            light.intensity = original;
    }

    // ---------- Porta ----------

    private bool TryDoor()
    {
        Door[] doors = FindObjectsByType<Door>(FindObjectsSortMode.None)
            .Where(d => d.CanInteract() && InRange(d.transform.position))
            .OrderBy(d => OutOfSight(d.transform.position) ? 0 : 1)
            .ToArray();
        if (doors.Length == 0)
            return false;

        doors[0].Interact();
        return true;
    }

    // ---------- Passos atras do jogador ----------

    private IEnumerator Footsteps()
    {
        Vector3 back = -player.forward;
        back.y = 0f;
        Vector3 start = player.position + back.normalized * Random.Range(7f, 11f) + Vector3.up * 0.2f;
        Vector3 side = Vector3.Cross(Vector3.up, back.normalized);
        int count = Random.Range(3, 6);

        for (int i = 0; i < count; i++)
        {
            steps.transform.position = start + side * (i % 2 == 0 ? 0.2f : -0.2f) - back.normalized * i * 0.7f;
            steps.pitch = Random.Range(0.85f, 1f);
            steps.PlayOneShot(stepClip, 0.8f);
            yield return new WaitForSeconds(Random.Range(0.45f, 0.6f));
        }
    }

    private static AudioClip BuildStep()
    {
        const int rate = 44100;
        int length = (int)(rate * 0.2f);
        float[] data = new float[length];
        var random = new System.Random(3);
        for (int i = 0; i < length; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Exp(-t * 30f);
            float thump = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(110f, 55f, t / 0.2f) * t);
            float grit = (float)(random.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 70f) * 0.4f;
            data[i] = (thump * 0.7f + grit) * env;
        }
        AudioClip clip = AudioClip.Create("PhantomStep", length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
