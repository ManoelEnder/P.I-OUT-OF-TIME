using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Figura escondida: so aparece no visor da camera com zoom (com um leve tremor ao surgir).
public class HiddenFigure : MonoBehaviour
{
    private Renderer rend;
    private PhotoCamera photo;
    private float shownSince = -1f;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        photo = FindFirstObjectByType<PhotoCamera>();
        rend.enabled = false;
    }

    private void Update()
    {
        bool show = photo != null && photo.IsCameraMode && photo.ZoomAmount > 0.45f;
        if (show && shownSince < 0f) shownSince = Time.time;
        if (!show) shownSince = -1f;
        // Ao surgir, pisca algumas vezes (como interferencia de fita).
        float t = shownSince < 0f ? 0f : Time.time - shownSince;
        rend.enabled = show && (t > 0.35f || Mathf.Repeat(t * 14f, 1f) < 0.5f);
    }
}
