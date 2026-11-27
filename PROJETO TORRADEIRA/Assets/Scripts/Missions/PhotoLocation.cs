using System.Collections.Generic;
using UnityEngine;

public class PhotoLocation : MonoBehaviour
{
    public static readonly List<PhotoLocation> All = new List<PhotoLocation>();

    [SerializeField] private string locationId = "praca";
    [SerializeField] private string displayName = "Praça";
    [SerializeField] private float maxDistance = 25f;
    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private bool useRendererBounds = true;
    [SerializeField] private bool debugLog = true;

    [SerializeField] private GameObject marker;
    [SerializeField] private bool billboard = true;
    [SerializeField] private bool hideMarkerWhenPhotographed = true;
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float bobSpeed = 2f;

    // Configurado em codigo (PhotoSpots): exige zoom do visor e ignora vidro/o proprio objeto na visao.
    [SerializeField, HideInInspector] private bool requireZoom;
    [SerializeField, HideInInspector] private Transform losOwner;

    private Renderer[] renderers;
    private Vector3 markerStartLocalPos;
    private Camera mainCam;

    public string Id => locationId;
    public string DisplayName => displayName;

    public Vector3 TargetPoint
    {
        get
        {
            if (!useRendererBounds || renderers == null || renderers.Length == 0)
                return transform.position;

            Bounds bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds.center;
        }
    }

    private void Awake()
    {
        renderers = CollectRenderers();

        if (marker != null)
            markerStartLocalPos = marker.transform.localPosition;
    }

    private void OnEnable()
    {
        if (!All.Contains(this))
            All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    private void LateUpdate()
    {
        if (marker == null || !marker.activeSelf)
            return;

        if (billboard)
        {
            if (mainCam == null)
                mainCam = Camera.main;

            if (mainCam != null)
                marker.transform.rotation = mainCam.transform.rotation;
        }

        if (bobHeight > 0f)
        {
            marker.transform.localPosition =
                markerStartLocalPos +
                Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        }
    }

    public void Configure(string id, string name, GameObject newMarker, float distance, bool zoomOnly, Transform owner)
    {
        locationId = id;
        displayName = name;
        maxDistance = distance;
        requireZoom = zoomOnly;
        losOwner = owner;
        useRendererBounds = false;
        debugLog = false;
        bobHeight = 0.08f;
        if (newMarker != null)
        {
            // O marcador antigo da cena da lugar ao brilho.
            if (marker != null && marker != newMarker)
                marker.SetActive(false);
            marker = newMarker;
            markerStartLocalPos = marker.transform.localPosition;
        }
        renderers = CollectRenderers();
    }

    public bool HasMarker => marker != null;

    public bool Photographed { get; private set; }

    public void MarkPhotographed()
    {
        Photographed = true;
        if (marker != null && hideMarkerWhenPhotographed)
            marker.SetActive(false);
    }

    public bool IsInFrame(Camera cam)
    {
        Vector3 pos = TargetPoint;

        float distance = Vector3.Distance(cam.transform.position, pos);

        if (distance > maxDistance)
        {
            Log("longe demais (" + distance.ToString("F1") + " > " + maxDistance + ")");
            return false;
        }

        if (requireZoom)
        {
            PhotoCamera photo = FindFirstObjectByType<PhotoCamera>();
            if (photo == null || photo.ZoomAmount < 0.45f)
            {
                Log("precisa de zoom");
                return false;
            }
        }

        Vector3 vp = cam.WorldToViewportPoint(pos);

        if (vp.z <= 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f)
        {
            Log("fora do enquadramento (viewport " + vp + ", ponto " + pos + ")");
            return false;
        }

        if (requireLineOfSight)
        {
            // Vidro e o proprio objeto fotografado nao bloqueiam.
            foreach (RaycastHit hit in Physics.RaycastAll(cam.transform.position, (pos - cam.transform.position).normalized,
                         Vector3.Distance(cam.transform.position, pos) - 0.05f, ~0, QueryTriggerInteraction.Ignore))
            {
                Transform t = hit.transform;
                if (t == transform || t.IsChildOf(transform)) continue;
                if (losOwner != null && (t == losOwner || t.IsChildOf(losOwner))) continue;
                if (t.CompareTag("Player") || t.root.CompareTag("Player")) continue;
                string n = t.name.ToLowerInvariant();
                if (n.Contains("glass") || n.Contains("vidro") || n.Contains("window") || n.Contains("janela")) continue;
                Log("bloqueado por " + t.name);
                return false;
            }
        }

        Log("visível");
        return true;
    }

    private Renderer[] CollectRenderers()
    {
        Renderer[] all = GetComponentsInChildren<Renderer>();
        List<Renderer> filtered = new List<Renderer>();

        foreach (Renderer r in all)
        {
            if (marker != null && r.transform.IsChildOf(marker.transform))
                continue;

            filtered.Add(r);
        }

        return filtered.ToArray();
    }

    private void Log(string message)
    {
        if (debugLog)
            Debug.Log("[PhotoLocation " + locationId + "] " + message, this);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        Vector3 point = transform.position;

        if (useRendererBounds)
        {
            Renderer[] found = CollectRenderers();

            if (found.Length > 0)
            {
                Bounds bounds = found[0].bounds;

                for (int i = 1; i < found.Length; i++)
                    bounds.Encapsulate(found[i].bounds);

                point = bounds.center;
            }
        }

        Gizmos.DrawWireSphere(point, 0.5f);
    }
}