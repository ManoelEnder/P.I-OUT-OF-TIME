using System.Collections;
using TMPro;
using UnityEngine;

// Creditos no CRT: quando cada nome chega ao meio da tela, uma camera da sala dispara
// e a TV do prototipo congela a "foto" 100x100 por alguns segundos.
public class CreditsSnapshots : MonoBehaviour
{
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform[] entries;
    [SerializeField] private PhotoFlash[] cameras;
    [SerializeField] private CrtCameraFeed feed;
    [SerializeField] private TMP_Text feedLabel;
    [SerializeField] private float holdSeconds = 2.5f;

    private int nextEntry;
    private int nextCamera;
    private int photoCount;
    private string idleLabel;
    private Coroutine labelRoutine;

    private void Awake()
    {
        if (feedLabel != null)
            idleLabel = feedLabel.text;
    }

    private void Update()
    {
        if (entries == null || viewport == null)
            return;

        while (nextEntry < entries.Length && ReachedCenter(entries[nextEntry]))
        {
            Shoot();
            nextEntry++;
        }
    }

    private bool ReachedCenter(RectTransform entry)
    {
        if (entry == null)
            return true;

        Vector3 center = viewport.InverseTransformPoint(entry.TransformPoint(entry.rect.center));
        return center.y >= viewport.rect.center.y;
    }

    private void Shoot()
    {
        // O flash acende a luz na hora; a foto e tirada no mesmo frame.
        if (cameras != null && cameras.Length > 0)
        {
            PhotoFlash camera = cameras[nextCamera % cameras.Length];
            nextCamera++;

            if (camera != null)
                camera.Fire();
        }

        if (feed != null)
            feed.Snapshot(holdSeconds);

        photoCount++;

        if (feedLabel != null)
        {
            if (labelRoutine != null)
                StopCoroutine(labelRoutine);

            labelRoutine = StartCoroutine(ShowPhotoLabel());
        }
    }

    private IEnumerator ShowPhotoLabel()
    {
        feedLabel.text = "FOTO " + photoCount.ToString("00");
        yield return new WaitForSecondsRealtime(holdSeconds);
        feedLabel.text = idleLabel;
        labelRoutine = null;
    }
}
