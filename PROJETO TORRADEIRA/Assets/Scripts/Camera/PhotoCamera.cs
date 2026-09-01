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

    [Header("Zoom")]
    public float normalFOV = 60f;
    public float zoomFOV = 40f;
    public float minZoomFOV = 25f;
    public float maxZoomFOV = 60f;
    public float zoomSpeed = 2.5f;

    [Header("Battery")]
    public int maxBattery = 10;
    public Image[] batteryBars;
    public float batteryDrainInterval = 3f;

    [Header("Flash")]
    public Image flashImage;
    public float flashDuration = 0.07f;
    public float flashFadeDuration = 0.1f;

    [Header("Transition")]
    public float enterTransitionTime = 0.4f;
    public float exitTransitionTime = 0.12f;

    [Header("Camera Effects")]
    [SerializeField] private CameraBlink cameraBlink;
    [SerializeField] private CameraBlackBorders blackBorders;
    [SerializeField] private CameraHUDZoom cameraHUDZoom;

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

    private void Start()
    {
        renderTexture =
            new RenderTexture(512, 512, 24);

        renderTexture.Create();

        if (photoCam != null)
        {
            photoCam.targetTexture =
                renderTexture;

            photoCam.enabled = false;
        }

        targetFOV = normalFOV;

        if (playerCam != null)
            playerCam.fieldOfView = normalFOV;

        currentBattery = maxBattery;

        UpdateBatteryUI();

        FindTemporalObjects();

        if (cameraHUD != null)
            cameraHUD.SetActive(false);

        if (crosshair != null)
            crosshair.SetActive(true);

        SetupFlash();

        if (blackBorders != null)
        {
            blackBorders.ResetBorders();
            blackBorders.SetActive(false);
        }

        if (cameraHUDZoom != null)
            cameraHUDZoom.ResetHUD();

        if (photoData != null)
            photoData.Initialize();

        UpdateTemporalVisibility();
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.cKey.wasPressedThisFrame)
        {
            ToggleCameraMode();
        }

        if (!cameraMode)
            return;

        if (!isTransitioning)
        {
            HandleZoom();

            if (Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame &&
                canShoot)
            {
                StartCoroutine(TakePhoto());
            }
        }

        HandleBattery();
    }

    private void SetupFlash()
    {
        if (flashImage == null)
            return;

        Color color = flashImage.color;
        color.a = 0f;

        flashImage.color = color;
        flashImage.gameObject.SetActive(false);
    }

    private void HandleZoom()
    {
        if (Mouse.current == null)
            return;

        float scroll =
            Mouse.current.scroll.ReadValue().y;

        if (scroll != 0f)
        {
            targetFOV -=
                scroll * zoomSpeed;

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

        UpdateZoomHUD();
    }

    private void UpdateZoomHUD()
    {
        float currentFOV =
            playerCam != null
                ? playerCam.fieldOfView
                : normalFOV;

        float zoomAmount =
            Mathf.InverseLerp(
                normalFOV,
                minZoomFOV,
                currentFOV
            );

        zoomAmount =
            Mathf.Clamp01(zoomAmount);

        if (cameraHUDZoom != null)
            cameraHUDZoom.ApplyZoom(zoomAmount);

        if (blackBorders != null)
            blackBorders.SetZoom(zoomAmount);
    }

    private void HandleBattery()
    {
        batteryTimer += Time.deltaTime;

        if (batteryTimer < batteryDrainInterval)
            return;

        batteryTimer = 0f;

        UseBattery(1);

        if (currentBattery <= 0 &&
            cameraMode)
        {
            CloseCamera();
        }
    }

    private void ToggleCameraMode()
    {
        if (isTransitioning)
            return;

        if (!cameraMode &&
            currentBattery <= 0)
        {
            return;
        }

        if (cameraMode)
        {
            CloseCamera();
        }
        else
        {
            OpenCamera();
        }
    }

    private void OpenCamera()
    {
        if (currentBattery <= 0)
            return;

        cameraMode = true;

        if (crosshair != null)
            crosshair.SetActive(false);

        UpdateTemporalVisibility();

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine =
            StartCoroutine(CameraTransition(true));
    }

    private void CloseCamera()
    {
        if (!cameraMode)
            return;

        cameraMode = false;

        if (crosshair != null)
            crosshair.SetActive(true);

        UpdateTemporalVisibility();

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine =
            StartCoroutine(CameraTransition(false));
    }

    private IEnumerator CameraTransition(bool entering)
    {
        isTransitioning = true;

        float startFOV =
            playerCam != null
                ? playerCam.fieldOfView
                : normalFOV;

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

            if (blackBorders != null)
            {
                blackBorders.SetActive(true);
                blackBorders.ResetBorders();
            }

            if (cameraHUDZoom != null)
                cameraHUDZoom.ResetHUD();

            if (cameraBlink != null)
                cameraBlink.Play();
        }

        while (time < duration)
        {
            time += Time.deltaTime;

            float progress =
                Mathf.Clamp01(time / duration);

            float smooth =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            if (playerCam != null)
            {
                playerCam.fieldOfView =
                    Mathf.Lerp(
                        startFOV,
                        target,
                        smooth
                    );
            }

            targetFOV =
                Mathf.Lerp(
                    startFOV,
                    target,
                    smooth
                );

            if (entering)
                UpdateZoomHUD();

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

            if (blackBorders != null)
            {
                blackBorders.ResetBorders();
                blackBorders.SetActive(false);
            }

            if (cameraHUDZoom != null)
                cameraHUDZoom.ResetHUD();
        }

        isTransitioning = false;
    }

    private IEnumerator TakePhoto()
    {
        if (!canShoot ||
            currentBattery <= 0 ||
            !cameraMode)
        {
            yield break;
        }

        canShoot = false;

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

        yield return StartCoroutine(
            FlashCoroutine()
        );

        if (photoData != null)
        {
            photoData.ProcessPhoto(photo);

            yield return new WaitForSeconds(
                photoData.previewDuration
            );

            yield return new WaitForSeconds(
                photoData.cooldown
            );
        }

        UseBattery(1);

        if (currentBattery <= 0 &&
            cameraMode)
        {
            CloseCamera();
        }

        canShoot = true;
    }

    private Texture2D CreatePhoto()
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

    private void DetectTemporalObject()
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

        if (!hit.collider.CompareTag("Temporal"))
            return;

        Renderer renderer =
            hit.collider.GetComponent<Renderer>();

        if (renderer != null)
            RevealTemporalObject(renderer);
    }

    private void RevealTemporalObject(
        Renderer renderer
    )
    {
        if (renderer == null)
            return;

        if (!revelados.Contains(renderer))
            revelados.Add(renderer);

        renderer.enabled = true;

        TemporalObjectPickup pickup =
            renderer.GetComponent<TemporalObjectPickup>();

        if (pickup != null)
            pickup.SetRevealed(true);

        StartCoroutine(
            HideTemporalObject(renderer)
        );
    }

    private IEnumerator HideTemporalObject(
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
            renderer.GetComponent<TemporalObjectPickup>();

        if (pickup != null)
            pickup.SetRevealed(false);

        if (!cameraMode)
            renderer.enabled = false;
    }

    private void FindTemporalObjects()
    {
        List<Renderer> lista =
            new List<Renderer>();

        Renderer[] todos =
            FindObjectsOfType<Renderer>(true);

        foreach (Renderer renderer in todos)
        {
            if (!renderer.CompareTag("Temporal"))
                continue;

            lista.Add(renderer);
            renderer.enabled = false;
        }

        temporais =
            lista.ToArray();
    }

    private void UpdateTemporalVisibility()
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

    private IEnumerator FlashCoroutine()
    {
        if (flashImage == null)
            yield break;

        flashImage.gameObject.SetActive(true);
        flashImage.transform.SetAsLastSibling();

        Color color =
            flashImage.color;

        color.a = 1f;
        flashImage.color = color;

        yield return new WaitForSeconds(
            flashDuration
        );

        float elapsedTime = 0f;

        while (elapsedTime < flashFadeDuration)
        {
            elapsedTime += Time.deltaTime;

            color.a =
                Mathf.Lerp(
                    1f,
                    0f,
                    elapsedTime /
                    flashFadeDuration
                );

            flashImage.color = color;

            yield return null;
        }

        color.a = 0f;
        flashImage.color = color;

        flashImage.gameObject.SetActive(false);
    }

    private void UseBattery(int amount)
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

    private void UpdateBatteryUI()
    {
        if (batteryBars == null)
            return;

        for (
            int i = 0;
            i < batteryBars.Length;
            i++
        )
        {
            if (batteryBars[i] != null)
            {
                batteryBars[i].enabled =
                    i < currentBattery;
            }
        }
    }
}