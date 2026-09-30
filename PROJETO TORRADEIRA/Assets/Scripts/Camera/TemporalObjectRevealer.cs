using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TemporalObjectRevealer : MonoBehaviour
{
    private const string TemporalTag = "Temporal";
    private const float RaycastDistance = 100f;

    private readonly MonoBehaviour host;
    private readonly float revealTime;
    private readonly Func<bool> isCameraMode;
    private readonly List<Renderer> revealed = new List<Renderer>();

    private Renderer[] temporals;

    public TemporalObjectRevealer(
        MonoBehaviour host,
        float revealTime,
        Func<bool> isCameraMode
    )
    {
        this.host = host;
        this.revealTime = revealTime;
        this.isCameraMode = isCameraMode;
    }

    public void FindAll()
    {
        List<Renderer> found = new List<Renderer>();

        foreach (Renderer renderer in UnityEngine.Object.FindObjectsByType<Renderer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None))
        {
            if (!renderer.CompareTag(TemporalTag))
                continue;

            found.Add(renderer);
            renderer.enabled = false;
        }

        temporals = found.ToArray();
    }

    public void UpdateVisibility()
    {
        if (temporals == null)
            return;

        bool cameraMode = isCameraMode();

        foreach (Renderer renderer in temporals)
        {
            if (renderer == null)
                continue;

            renderer.enabled = cameraMode || revealed.Contains(renderer);
        }
    }

    public void RevealAtScreenCenter(Camera playerCam)
    {
        if (playerCam == null)
            return;

        Ray ray = playerCam.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (!Physics.Raycast(ray, out RaycastHit hit, RaycastDistance))
            return;

        if (!hit.collider.CompareTag(TemporalTag))
            return;

        Renderer renderer = hit.collider.GetComponent<Renderer>();

        if (renderer != null)
            Reveal(renderer);
    }

    private void Reveal(Renderer renderer)
    {
        if (!revealed.Contains(renderer))
            revealed.Add(renderer);

        renderer.enabled = true;
        SetPickupRevealed(renderer, true);

        host.StartCoroutine(HideAfterDelay(renderer));
    }

    private IEnumerator HideAfterDelay(Renderer renderer)
    {
        yield return new WaitForSeconds(revealTime);

        if (renderer == null)
            yield break;

        revealed.Remove(renderer);
        SetPickupRevealed(renderer, false);

        if (!isCameraMode())
            renderer.enabled = false;
    }

    private void SetPickupRevealed(Renderer renderer, bool value)
    {
        TemporalObjectPickup pickup =
            renderer.GetComponent<TemporalObjectPickup>();

        if (pickup != null)
            pickup.SetRevealed(value);
    }
}