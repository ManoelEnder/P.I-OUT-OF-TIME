using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTemporalController : MonoBehaviour
{
    private readonly HashSet<Renderer> revealed =
        new HashSet<Renderer>();

    private readonly Dictionary<Renderer, Coroutine> hideRoutines =
        new Dictionary<Renderer, Coroutine>();

    private Renderer[] temporalObjects = new Renderer[0];

    private float revealedDuration;
    private bool cameraMode;

    public void Initialize(float duration)
    {
        revealedDuration = Mathf.Max(0f, duration);
        revealed.Clear();
        hideRoutines.Clear();

        List<Renderer> objects = new List<Renderer>();

        Renderer[] renderers = Object.FindObjectsByType<Renderer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Renderer renderer in renderers)
        {
            if (!renderer.CompareTag("Temporal"))
                continue;

            objects.Add(renderer);
            renderer.enabled = false;
        }

        temporalObjects = objects.ToArray();
    }

    public void SetCameraMode(bool active)
    {
        cameraMode = active;

        foreach (Renderer renderer in temporalObjects)
        {
            if (renderer != null)
            {
                renderer.enabled =
                    active || revealed.Contains(renderer);
            }
        }
    }

    public void Detect(Camera playerCam, MonoBehaviour runner)
    {
        if (playerCam == null || runner == null)
            return;

        Ray ray = playerCam.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
            return;

        if (!hit.collider.CompareTag("Temporal"))
            return;

        Renderer renderer = hit.collider.GetComponent<Renderer>();

        if (renderer == null)
            renderer = hit.collider.GetComponentInParent<Renderer>();

        if (renderer == null)
            return;

        revealed.Add(renderer);

        renderer.enabled = true;
        SetPickupRevealed(renderer, true);

        if (hideRoutines.TryGetValue(renderer, out Coroutine previous))
            runner.StopCoroutine(previous);

        hideRoutines[renderer] =
            runner.StartCoroutine(HideAfterDelay(renderer));
    }

    private IEnumerator HideAfterDelay(Renderer renderer)
    {
        yield return new WaitForSeconds(revealedDuration);

        hideRoutines.Remove(renderer);

        if (renderer == null)
            yield break;

        revealed.Remove(renderer);

        SetPickupRevealed(renderer, false);

        renderer.enabled = cameraMode;
    }

    private static void SetPickupRevealed(
        Renderer renderer,
        bool active
    )
    {
        TemporalObjectPickup pickup =
            renderer.GetComponent<TemporalObjectPickup>();

        if (pickup != null)
            pickup.SetRevealed(active);
    }
}