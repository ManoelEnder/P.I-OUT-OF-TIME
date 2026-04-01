using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PhotoCamera : MonoBehaviour
{
    public Camera photoCam;
    public Camera playerCam;
    public RawImage photoPreview;
    public Renderer[] temporalRenderers;
    public Image flashImage;

    public AudioSource audioSource;
    public AudioClip shutterSound;

    public Image cooldownBar;
    public TextMeshProUGUI photoCounter;
    public GameObject crosshair;

    public float cooldown = 2f;
    public float flashDuration = 0.07f;

    RenderTexture rt;
    Texture2D photo;

    bool canShoot = true;
    int photoCount = 0;

    void Start()
    {
        rt = new RenderTexture(512, 512, 24);
        photoCam.targetTexture = rt;
        photoCam.enabled = false;

        photoPreview.gameObject.SetActive(false);
        Color p = photoPreview.color;
        p.a = 0f;
        photoPreview.color = p;

        if (flashImage != null)
        {
            Color f = flashImage.color;
            f.a = 0f;
            flashImage.color = f;
        }

        if (cooldownBar != null)
            cooldownBar.fillAmount = 0f;

        if (photoCounter != null)
            photoCounter.text = "Fotos: 0";

        foreach (Renderer r in temporalRenderers)
            r.enabled = false;
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame && canShoot)
            StartCoroutine(TakePhoto());
    }

    IEnumerator TakePhoto()
    {
        canShoot = false;

        if (crosshair != null)
            crosshair.SetActive(false);

        photoCount++;
        if (photoCounter != null)
            photoCounter.text = "Fotos: " + photoCount;

        transform.SetPositionAndRotation(
            playerCam.transform.position,
            playerCam.transform.rotation
        );

        photoCam.fieldOfView = playerCam.fieldOfView;

        foreach (Renderer r in temporalRenderers)
            r.enabled = true;

        yield return new WaitForEndOfFrame();

        photoCam.enabled = true;
        photoCam.Render();
        photoCam.enabled = false;

        foreach (Renderer r in temporalRenderers)
            r.enabled = false;

        if (audioSource != null && shutterSound != null)
            audioSource.PlayOneShot(shutterSound);

        yield return StartCoroutine(FlashCoroutine());

        RenderTexture.active = rt;
        photo = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        photo.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        photo.Apply();
        RenderTexture.active = null;

        photoPreview.texture = photo;
        photoPreview.gameObject.SetActive(true);

        yield return StartCoroutine(FadePhoto(0f, 1f, 0.25f));
        yield return new WaitForSeconds(1.3f);
        yield return StartCoroutine(FadePhoto(1f, 0f, 0.25f));

        photoPreview.gameObject.SetActive(false);

        float t = 0f;
        while (t < cooldown)
        {
            t += Time.deltaTime;
            if (cooldownBar != null)
                cooldownBar.fillAmount = t / cooldown;
            yield return null;
        }

        if (cooldownBar != null)
            cooldownBar.fillAmount = 0f;

        canShoot = true;

        if (crosshair != null)
            crosshair.SetActive(true);

        Ray ray = new Ray(playerCam.transform.position, playerCam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            EvidenceObject evidence = hit.collider.GetComponent<EvidenceObject>();
            if (evidence != null)
            {
                evidence.Collect();
            }
        }
    }

    IEnumerator FlashCoroutine()
    {
        if (flashImage != null)
        {
            flashImage.gameObject.SetActive(true);

            Color c = flashImage.color;
            c.a = 1f;
            flashImage.color = c;

            yield return new WaitForSeconds(flashDuration);

            c.a = 0f;
            flashImage.color = c;
            flashImage.gameObject.SetActive(false);
        }
    }

    IEnumerator FadePhoto(float startAlpha, float endAlpha, float duration)
    {
        float t = 0f;
        Color c = photoPreview.color;

        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, endAlpha, t / duration);
            photoPreview.color = c;
            yield return null;
        }

        c.a = endAlpha;
        photoPreview.color = c;
    }
}