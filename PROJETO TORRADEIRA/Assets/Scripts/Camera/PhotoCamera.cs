using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PhotoCamera : MonoBehaviour
{
    public Camera photoCam;
    public Camera playerCam;
    public RawImage photoPreview;
    public Image flashImage;

    public AudioSource audioSource;
    public AudioClip shutterSound;

    public Image cooldownBar;
    public TextMeshProUGUI photoCounter;
    public GameObject crosshair;
    public GameObject cameraHUD;
    public Image fadeImage;

    public float cooldown = 2f;
    public float flashDuration = 0.07f;

    public float zoomFOV = 40f;
    public float normalFOV = 60f;
    public float transitionTime = 0.4f;

    public float tempoRevelado = 30f;

    RenderTexture rt;
    Texture2D photo;

    bool canShoot = true;
    int photoCount = 0;
    bool cameraMode = false;

    Renderer[] temporais;
    List<Renderer> revelados = new List<Renderer>();

    Coroutine transitionCoroutine;


    void Start()
    {
        rt = new RenderTexture(512, 512, 24);
        photoCam.targetTexture = rt;
        photoCam.enabled = false;

        temporais = GameObject.FindGameObjectsWithTag("Temporal")
            .SelectMany(obj => obj.GetComponentsInChildren<Renderer>())
            .ToArray();

        foreach (Renderer r in temporais)
            r.enabled = false;

        photoPreview.gameObject.SetActive(false);
        Color p = photoPreview.color;
        p.a = 0f;
        photoPreview.color = p;

        if (cameraHUD != null)
            cameraHUD.SetActive(false);

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }

        if (photoCounter != null)
            photoCounter.text = "Fotos: 0";
    }

    void Update()
    {
        if (Keyboard.current.cKey.wasPressedThisFrame)
            ToggleCameraMode();

        if (cameraMode && Mouse.current.leftButton.wasPressedThisFrame && canShoot)
            StartCoroutine(TakePhoto());
    }

    void ToggleCameraMode()
    {
        cameraMode = !cameraMode;

        if (crosshair != null)
            crosshair.SetActive(!cameraMode);

        AtualizarVisibilidade();

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(CameraTransition(cameraMode));
    }

    IEnumerator CameraTransition(bool entering)
    {
        float startFOV = playerCam.fieldOfView;
        float targetFOV = entering ? zoomFOV : normalFOV;

        float t = 0f;

        if (cameraHUD != null)
            cameraHUD.SetActive(true);

        while (t < transitionTime)
        {
            t += Time.deltaTime;
            float lerp = t / transitionTime;

            playerCam.fieldOfView = Mathf.Lerp(startFOV, targetFOV, lerp);

            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                c.a = entering
                    ? Mathf.Lerp(0f, 0.35f, lerp)
                    : Mathf.Lerp(0.35f, 0f, lerp);

                fadeImage.color = c;
            }

            yield return null;
        }

        playerCam.fieldOfView = targetFOV;

        if (!entering)
        {
            if (cameraHUD != null)
                cameraHUD.SetActive(false);
        }
    }

    IEnumerator TakePhoto()
    {
        canShoot = false;

        photoCount++;
        if (photoCounter != null)
            photoCounter.text = "Fotos: " + photoCount;

        transform.SetPositionAndRotation(
            playerCam.transform.position,
            playerCam.transform.rotation
        );

        photoCam.fieldOfView = playerCam.fieldOfView;

        yield return new WaitForEndOfFrame();

        foreach (Renderer r in temporais)
        {
            if (r != null)
                RevelarPorTempo(r);
        }

        photoCam.enabled = true;
        photoCam.Render();
        photoCam.enabled = false;

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

        yield return new WaitForSeconds(cooldown);

        canShoot = true;
    }

    void RevelarPorTempo(Renderer r)
    {
        if (r == null) return;

        if (!revelados.Contains(r))
            revelados.Add(r);

        r.enabled = true;
        StartCoroutine(FadeOut(r));
    }

    IEnumerator FadeOut(Renderer r)
    {
        Material mat = r.material;
        Color corOriginal = mat.color;

        float tempo = 0f;

        while (tempo < tempoRevelado)
        {
            tempo += Time.deltaTime;

            float alpha = Mathf.Lerp(1f, 0f, tempo / tempoRevelado);

            Color novaCor = corOriginal;
            novaCor.a = alpha;
            mat.color = novaCor;

            yield return null;
        }

        revelados.Remove(r);

        if (!cameraMode && r != null)
            r.enabled = false;

        mat.color = corOriginal;
    }

    void AtualizarVisibilidade()
    {
        foreach (Renderer r in temporais)
        {
            if (r == null) continue;

            if (cameraMode || revelados.Contains(r))
                r.enabled = true;
            else
                r.enabled = false;
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