using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PhotoCamera : MonoBehaviour
{
    [Header("Cameras")]
    public Camera photoCam;
    public Camera playerCam;

    [Header("HUD")]
    public GameObject crosshair;
    public GameObject cameraHUD;
    public Image fadeImage;
    public Image flashImage;

    [Header("Camera Settings")]
    public float zoomFOV = 40f;
    public float normalFOV = 60f;
    public float minZoomFOV = 25f;
    public float maxZoomFOV = 60f;

    public float enterTransitionTime = 0.4f;
    public float exitTransitionTime = 0.12f;

    [Header("Flash")]
    public float flashDuration = 0.07f;

    [Header("Battery")]
    public int maxBattery = 10;
    public Image[] batteryBars;
    public float batteryDrainInterval = 3f;

    [Header("Temporal Objects")]
    public float tempoRevelado = 30f;

    [Header("Photo Data")]
    [SerializeField] private CameraPhotoData photoData;

    private RenderTexture renderTexture;

    private int currentBattery;
    private float batteryTimer;

    private bool cameraMode;
    private bool canShoot = true;
    private bool isTransitioning;

    private float targetFOV;

    private Renderer[] temporais;

    private readonly List<Renderer> revelados =
        new List<Renderer>();

    private Coroutine transitionCoroutine;

    public bool IsCameraMode => cameraMode;

    void Start()
    {
        renderTexture =
            new RenderTexture(512, 512, 24);

        photoCam.targetTexture =
            renderTexture;

        photoCam.enabled = false;

        targetFOV = normalFOV;

        currentBattery = maxBattery;

        UpdateBatteryUI();

        FindTemporalObjects();

        if (cameraHUD != null)
            cameraHUD.SetActive(false);

        if (crosshair != null)
            crosshair.SetActive(true);

        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = 0f;
            fadeImage.color = color;
        }

        if (flashImage != null)
        {
            Color color = flashImage.color;
            color.a = 0f;
            flashImage.color = color;

            flashImage.gameObject.SetActive(false);
        }

        if (photoData != null)
            photoData.Initialize();

        UpdateTemporalVisibility();
    }

    void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.cKey.wasPressedThisFrame)
        {
            ToggleCameraMode();
        }

        if (!cameraMode)
            return;

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            canShoot &&
            !isTransitioning)
        {
            StartCoroutine(TakePhoto());
        }

        if (!isTransitioning)
            HandleZoom();

        HandleBattery();
    }

    void HandleZoom()
    {
        if (Mouse.current == null)
            return;

        float scroll =
            Mouse.current.scroll.ReadValue().y;

        if (scroll != 0f)
        {
            targetFOV -= scroll * 2.5f;

            targetFOV =
                Mathf.Clamp(
                    targetFOV,
                    minZoomFOV,
                    maxZoomFOV
                );
        }

        if (playerCam != null)
        {
            playerCam.fieldOfView =
                Mathf.Lerp(
                    playerCam.fieldOfView,
                    targetFOV,
                    Time.deltaTime * 10f
                );
        }
    }

    void HandleBattery()
    {
        batteryTimer += Time.deltaTime;

        if (batteryTimer >= batteryDrainInterval)
        {
            batteryTimer = 0f;

            UseBattery(1);

            if (currentBattery <= 0 &&
                cameraMode)
            {
                ToggleCameraMode();
            }
        }
    }

    void ToggleCameraMode()
    {
        if (!cameraMode &&
            currentBattery <= 0)
        {
            return;
        }

        cameraMode = !cameraMode;

        if (photoData != null)
            photoData.SetCameraVolume(cameraMode);

        if (crosshair != null)
            crosshair.SetActive(!cameraMode);

        UpdateTemporalVisibility();

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine =
            StartCoroutine(
                CameraTransition(cameraMode)
            );
    }

    IEnumerator CameraTransition(bool entering)
    {
        isTransitioning = true;

        float startFOV =
            playerCam.fieldOfView;

        float target =
            entering
                ? zoomFOV
                : normalFOV;

        float duration =
            entering
                ? enterTransitionTime
                : exitTransitionTime;

        float time = 0f;

        if (entering)
        {
            if (photoCam != null)
                photoCam.enabled = true;

            if (cameraHUD != null)
                cameraHUD.SetActive(true);
        }

        while (time < duration)
        {
            time += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    time / duration
                );

            if (playerCam != null)
            {
                playerCam.fieldOfView =
                    Mathf.Lerp(
                        startFOV,
                        target,
                        progress
                    );
            }

            if (fadeImage != null)
            {
                Color color =
                    fadeImage.color;

                color.a =
                    entering
                        ? Mathf.Lerp(
                            0f,
                            0.35f,
                            progress
                        )
                        : Mathf.Lerp(
                            0.35f,
                            0f,
                            progress
                        );

                fadeImage.color = color;
            }

            yield return null;
        }

        if (playerCam != null)
            playerCam.fieldOfView = target;

        targetFOV = target;

        if (!entering)
        {
            if (photoCam != null)
                photoCam.enabled = false;

            if (cameraHUD != null)
                cameraHUD.SetActive(false);
        }

        isTransitioning = false;
    }

    IEnumerator TakePhoto()
    {
        if (!canShoot ||
            currentBattery <= 0)
        {
            yield break;
        }

        canShoot = false;

        UseBattery(1);

        if (photoCam != null &&
            playerCam != null)
        {
            photoCam.transform.SetPositionAndRotation(
                playerCam.transform.position,
                playerCam.transform.rotation
            );

            photoCam.fieldOfView =
                playerCam.fieldOfView;
        }

        yield return new WaitForEndOfFrame();

        DetectTemporalObject();

        UpdateTemporalVisibility();

        if (photoCam != null)
            photoCam.Render();

        Texture2D photo =
            CreatePhoto();

        if (photoData != null)
        {
            photoData.ProcessPhoto(photo);

            yield return StartCoroutine(
                FlashCoroutine()
            );

            yield return new WaitForSeconds(
                photoData.previewDuration
            );

            yield return new WaitForSeconds(
                photoData.cooldown
            );
        }
        else
        {
            yield return StartCoroutine(
                FlashCoroutine()
            );

            yield return new WaitForSeconds(2f);
        }

        canShoot = true;
    }

    Texture2D CreatePhoto()
    {
        RenderTexture.active =
            renderTexture;

        Texture2D photo =
            new Texture2D(
                renderTexture.width,
                renderTexture.height,
                TextureFormat.RGB24,
                false
            );

        photo.ReadPixels(
            new Rect(
                0,
                0,
                renderTexture.width,
                renderTexture.height
            ),
            0,
            0
        );

        photo.Apply();

        RenderTexture.active = null;

        return photo;
    }

    void DetectTemporalObject()
    {
        if (playerCam == null)
            return;

        Ray ray =
            playerCam.ViewportPointToRay(
                new Vector3(
                    0.5f,
                    0.5f,
                    0f
                )
            );

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            100f
        ))
        {
            return;
        }

        if (!hit.collider.CompareTag(
            "Temporal"
        ))
        {
            return;
        }

        Renderer renderer =
            hit.collider.GetComponent<Renderer>();

        if (renderer != null)
            RevealTemporalObject(renderer);
    }

    void RevealTemporalObject(
        Renderer renderer
    )
    {
        if (renderer == null)
            return;

        if (!revelados.Contains(renderer))
            revelados.Add(renderer);

        renderer.enabled = true;

        TemporalObjectPickup pickup =
            renderer.GetComponent<
                TemporalObjectPickup
            >();

        if (pickup != null)
            pickup.SetRevealed(true);

        StartCoroutine(
            HideTemporalObject(renderer)
        );
    }

    IEnumerator HideTemporalObject(
        Renderer renderer
    )
    {
        yield return new WaitForSeconds(
            tempoRevelado
        );

        if (renderer == null)
            yield break;

        revelados.Remove(renderer);

        TemporalObjectPickup pickup =
            renderer.GetComponent<
                TemporalObjectPickup
            >();

        if (pickup != null)
            pickup.SetRevealed(false);

        if (!cameraMode)
            renderer.enabled = false;
    }

    void FindTemporalObjects()
    {
        List<Renderer> lista =
            new List<Renderer>();

        Renderer[] todos =
            FindObjectsOfType<Renderer>(true);

        foreach (Renderer renderer in todos)
        {
            if (!renderer.CompareTag(
                "Temporal"
            ))
            {
                continue;
            }

            lista.Add(renderer);

            renderer.enabled = false;
        }

        temporais =
            lista.ToArray();
    }

    void UpdateTemporalVisibility()
    {
        if (temporais == null)
            return;

        foreach (Renderer renderer in temporais)
        {
            if (renderer == null)
                continue;

            renderer.enabled =
                cameraMode ||
                revelados.Contains(renderer);
        }
    }

    IEnumerator FlashCoroutine()
    {
        if (flashImage == null)
            yield break;

        flashImage.gameObject.SetActive(true);

        Color color =
            flashImage.color;

        color.a = 1f;

        flashImage.color = color;

        yield return new WaitForSeconds(
            flashDuration
        );

        color.a = 0f;

        flashImage.color = color;

        flashImage.gameObject.SetActive(false);
    }

    void UseBattery(int amount)
    {
        currentBattery -= amount;

        currentBattery =
            Mathf.Clamp(
                currentBattery,
                0,
                maxBattery
            );

        UpdateBatteryUI();
    }

    public void AddBattery(int amount)
    {
        currentBattery += amount;

        currentBattery =
            Mathf.Clamp(
                currentBattery,
                0,
                maxBattery
            );

        UpdateBatteryUI();
    }

    public bool CanReceiveBattery()
    {
        return currentBattery < maxBattery;
    }

    public bool IsBatteryFull()
    {
        return currentBattery >= maxBattery;
    }

    void UpdateBatteryUI()
    {
        if (batteryBars == null)
            return;

        for (int i = 0;
            i < batteryBars.Length;
            i++)
        {
            if (batteryBars[i] != null)
            {
                batteryBars[i].enabled =
                    i < currentBattery;
            }
        }
    }
}