using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Brilho do lugar de foto: pulsa devagar.
public class PhotoMarkerPulse : MonoBehaviour
{
    private Renderer rend;
    private PhotoCamera photo;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        photo = FindFirstObjectByType<PhotoCamera>();
    }

    private void Update()
    {
        float s = 1f + Mathf.Sin(Time.time * 2.2f) * 0.12f;
        transform.localScale = Vector3.one * 1.6f * s;
    }
}
