using UnityEngine;

// Ponto de montagem (missao final): bancada onde o jogador entrega as pecas da camera.
// Fica apagado ate todas as missoes estarem prontas; depois acende e pulsa, e o E encerra a fase.
public class DeliveryPoint : MonoBehaviour, IInteractable
{
    [SerializeField] private Light glow;
    [SerializeField] private Renderer[] slots;

    private MissionSystem missions;
    private AudioSource hum;
    private Material[] slotMaterials;

    public static DeliveryPoint Instance { get; private set; }
    public bool Active => missions != null && missions.ReadyToDeliver && !missions.Delivered;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        missions = FindFirstObjectByType<MissionSystem>();

        if (slots != null)
        {
            slotMaterials = new Material[slots.Length];
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null)
                    slotMaterials[i] = slots[i].material;
        }

        hum = gameObject.AddComponent<AudioSource>();
        hum.spatialBlend = 1f;
        hum.minDistance = 1.5f;
        hum.maxDistance = 14f;
        hum.rolloffMode = AudioRolloffMode.Linear;
        hum.loop = true;
        hum.volume = 0f;
        hum.clip = BuildHum();
        hum.Play();
    }

    private void Update()
    {
        bool active = Active;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f);

        if (glow != null)
            glow.intensity = active ? Mathf.Lerp(0.6f, 2.2f, pulse) : 0f;

        hum.volume = Mathf.MoveTowards(hum.volume, active ? 0.35f : 0f, Time.deltaTime);

        if (slotMaterials == null || missions == null)
            return;

        // Um encaixe aceso por peca coletada.
        int pieces = missions.GetPieceCount();
        for (int i = 0; i < slotMaterials.Length; i++)
        {
            Material m = slotMaterials[i];
            if (m == null)
                continue;
            bool filled = i < pieces;
            Color c = filled ? new Color(0.75f, 0.85f, 1f) * (active ? Mathf.Lerp(1.2f, 2.4f, pulse) : 0.8f) : new Color(0.08f, 0.08f, 0.09f);
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c);
            }
            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", filled ? new Color(0.7f, 0.75f, 0.8f) : new Color(0.12f, 0.12f, 0.13f));
        }
    }

    public bool CanInteract()
    {
        return missions != null && !missions.Delivered;
    }

    public string GetInteractionMessage()
    {
        if (missions == null)
            return string.Empty;

        if (missions.ReadyToDeliver)
            return "E para colocar as peças na garra";

        int missing = Mathf.Max(0, missions.PiecesRequired - missions.GetPieceCount());
        return missing > 0
            ? "Ponto de montagem: faltam " + missing + " peças"
            : "Ponto de montagem: termine os objetivos";
    }

    public void Interact()
    {
        if (missions != null && missions.ReadyToDeliver)
            missions.Deliver();
    }

    private static AudioClip BuildHum()
    {
        const int rate = 44100;
        int n = rate * 2;
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            data[i] = (Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.25f + Mathf.Sin(2f * Mathf.PI * 331f * t) * 0.1f) * 0.4f;
        }
        AudioClip clip = AudioClip.Create("DeliveryHum", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
