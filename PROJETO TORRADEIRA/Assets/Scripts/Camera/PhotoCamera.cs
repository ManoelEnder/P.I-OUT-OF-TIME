
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class PhotoCamera : MonoBehaviour
{
    [Header("Cameras")]
    public Camera photoCam;
    public Camera playerCam;

    [Header("HUD")]
    public GameObject crosshair;
    public GameObject cameraHUD;

    [Header("Pause")]
    [SerializeField] private PauseMenu pauseMenu;

    [Header("Zoom")]
    public float normalFOV = 60f;
    public float zoomFOV = 40f;
    public float minZoomFOV = 25f;
    public float maxZoomFOV = 60f;
    public float zoomSpeed = 2.5f;

    [Header("Battery")]
    [SerializeField] private CameraBattery battery;

    [Header("Flash")]
    public Image flashImage;
    public float flashDuration = 0.07f;
    public float flashFadeDuration = 0.1f;

    [Header("Transition")]
    public float enterTransitionTime = 0.4f;
    public float exitTransitionTime = 0.35f;

    [Header("Camera Effects")]
    [SerializeField] private CameraBlink cameraBlink;
    [SerializeField] private CameraBlackBorders blackBorders;
    [SerializeField] private CameraHUDZoom cameraHUDZoom;

    [Header("Temporal Objects")]
    public float tempoRevelado = 30f;

    [Header("Photo Data")]
    [SerializeField] private CameraPhotoData photoData;

    [Header("Superaquecimento")]
    [SerializeField] private CameraOverheat cameraOverheat;

    private CameraPhotoCapture photoCapture;
    private CameraTemporalController temporalController;

    private float targetFOV;

    private bool cameraMode;
    private bool canShoot = true;
    private bool isTransitioning;
    private bool hasCamera = true;

    private Coroutine transitionCoroutine;
    private Coroutine pauseRestoreCoroutine;

    public bool IsCameraMode => cameraMode;
    public bool HasCamera => hasCamera;

    public bool CanLoseCamera =>
        hasCamera &&
        canShoot &&
        !isTransitioning;

    private void Awake()
    {
        if (battery == null)
            battery = GetComponent<CameraBattery>();

        if (battery == null)
        {
            Debug.LogError(
                "PhotoCamera precisa de um CameraBattery.",
                this
            );

            enabled = false;
            return;
        }

        battery.Initialize();

        photoCapture = new CameraPhotoCapture();

        photoCapture.Initialize(
            photoCam,
            playerCam,
            photoData,
            flashImage,
            flashDuration,
            flashFadeDuration
        );

        temporalController =
            GetComponent<CameraTemporalController>();

        if (temporalController == null)
        {
            temporalController =
                gameObject.AddComponent<CameraTemporalController>();
        }

        temporalController.Initialize(tempoRevelado);

        if (cameraOverheat == null)
            cameraOverheat = GetComponent<CameraOverheat>();

        targetFOV = normalFOV;

        if (playerCam != null)
            playerCam.fieldOfView = normalFOV;
    }

    private void Start()
    {
        if (cameraHUD != null)
            cameraHUD.SetActive(false);

        if (crosshair != null)
            crosshair.SetActive(true);

        if (photoCam != null && photoCam != playerCam)
            photoCam.enabled = false;

        ResetEffects();
        temporalController.SetCameraMode(false);
    }

    private void Update()
    {
        HandleInput();

        if (!cameraMode || isTransitioning)
            return;

        HandleCameraInput();

        battery.Tick(Time.deltaTime);

        if (battery.IsEmpty)
            CloseCamera();
    }

    private void LateUpdate()
    {
        SyncPhotoCamera();
    }

    private void SyncPhotoCamera()
    {
        if (!cameraMode)
            return;

        if (photoCam == null || playerCam == null)
            return;

        if (photoCam == playerCam)
            return;

        photoCam.transform.SetPositionAndRotation(
            playerCam.transform.position,
            playerCam.transform.rotation
        );

        photoCam.fieldOfView = playerCam.fieldOfView;
    }

    private void HandleInput()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (cameraMode)
            {
                TryExitCameraMode();
                return;
            }
        }

        if (Keyboard.current.cKey.wasPressedThisFrame)
            ToggleCameraMode();
    }

    private void HandleCameraInput()
    {
        HandleZoom();

        if (
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            canShoot
        )
        {
            StartCoroutine(TakePhoto());
        }
    }

    private void HandleZoom()
    {
        if (Mouse.current == null)
            return;

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (scroll != 0f)
        {
            targetFOV -= scroll * zoomSpeed;

            targetFOV = Mathf.Clamp(
                targetFOV,
                minZoomFOV,
                maxZoomFOV
            );
        }

        if (playerCam != null)
        {
            playerCam.fieldOfView = Mathf.Lerp(
                playerCam.fieldOfView,
                targetFOV,
                Time.deltaTime * 10f
            );
        }

        UpdateZoomHUD();
    }

    private void UpdateZoomHUD()
    {
        float currentFOV = playerCam != null
            ? playerCam.fieldOfView
            : normalFOV;

        float zoomAmount = Mathf.InverseLerp(
            normalFOV,
            minZoomFOV,
            currentFOV
        );

        zoomAmount = Mathf.Clamp01(zoomAmount);

        if (cameraHUDZoom != null)
            cameraHUDZoom.ApplyZoom(zoomAmount);

        if (blackBorders != null)
            blackBorders.SetZoom(zoomAmount);
    }

    private void ToggleCameraMode()
    {
        if (!hasCamera || isTransitioning)
            return;

        if (cameraMode)
            CloseCamera();
        else
            OpenCamera();
    }

    private void OpenCamera()
    {
        if (
            !hasCamera ||
            cameraMode ||
            isTransitioning ||
            battery.IsEmpty
        )
        {
            return;
        }

        cameraMode = true;

        DisablePauseMenu();

        if (crosshair != null)
            crosshair.SetActive(false);

        temporalController.SetCameraMode(true);

        SyncPhotoCamera();
        StartTransition(true);
    }

    private void CloseCamera()
    {
        if (!cameraMode || isTransitioning)
            return;

        StartTransition(false);
    }

    private void StartTransition(bool entering)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(
            PlayTransition(entering)
        );
    }

    private IEnumerator PlayTransition(bool entering)
    {
        isTransitioning = true;

        float startFOV = playerCam != null
            ? playerCam.fieldOfView
            : normalFOV;

        float target = entering
            ? zoomFOV
            : normalFOV;

        float duration = entering
            ? enterTransitionTime
            : exitTransitionTime;

        duration = Mathf.Max(0.001f, duration);

        Coroutine blinkAnimation = null;
        float time = 0f;

        if (entering)
        {
            SyncPhotoCamera();

            if (photoCam != null && photoCam != playerCam)
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
                blinkAnimation = cameraBlink.Play();
        }
        else
        {
            if (cameraBlink != null)
                blinkAnimation = cameraBlink.PlayClose();
        }

        while (time < duration)
        {
            time += Time.deltaTime;

            float progress = Mathf.Clamp01(
                time / duration
            );

            float smooth = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            if (playerCam != null)
            {
                playerCam.fieldOfView = Mathf.Lerp(
                    startFOV,
                    target,
                    smooth
                );
            }

            targetFOV = Mathf.Lerp(
                startFOV,
                target,
                smooth
            );

            if (entering)
                UpdateZoomHUD();

            yield return null;
        }

        if (blinkAnimation != null)
            yield return blinkAnimation;

        if (playerCam != null)
            playerCam.fieldOfView = target;

        targetFOV = target;

        if (!entering)
        {
            cameraMode = false;

            temporalController.SetCameraMode(false);

            if (photoCam != null && photoCam != playerCam)
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

            if (crosshair != null)
                crosshair.SetActive(true);

            if (cameraBlink != null)
            {
                Coroutine revealAnimation =
                    cameraBlink.PlayReveal();

                if (revealAnimation != null)
                    yield return revealAnimation;

                cameraBlink.ResetBlink();
            }

            RestorePauseMenu();
        }

        isTransitioning = false;
        transitionCoroutine = null;
    }

    private IEnumerator TakePhoto()
    {
        if (
            !hasCamera ||
            !canShoot ||
            battery.IsEmpty ||
            !cameraMode ||
            isTransitioning
        )
        {
            yield break;
        }

        canShoot = false;

        SyncPhotoCamera();
        photoCapture.PrepareCamera();

        yield return new WaitForEndOfFrame();

        SyncPhotoCamera();

        temporalController.Detect(playerCam, this);

        if (photoData != null && playerCam != null)
        {
            photoData.RegisterPhotographedLocations(
                playerCam
            );
        }

        Texture2D photo = photoCapture.Capture();

        photoCapture.ProcessPhoto(photo);

        if (photo != null && cameraOverheat != null)
            cameraOverheat.RegisterPhotoHeat();

        yield return StartCoroutine(
            photoCapture.PlayFlash()
        );

        if (photoData != null)
        {
            yield return new WaitForSeconds(
                photoData.PreviewDuration
            );

            yield return new WaitForSeconds(
                photoData.Cooldown
            );
        }

        battery.Use(1);

        if (battery.IsEmpty && cameraMode && !isTransitioning)
            CloseCamera();

        canShoot = true;
    }

    public bool TryExitCameraMode()
    {
        if (!cameraMode)
            return isTransitioning;

        if (!isTransitioning)
            CloseCamera();

        return true;
    }

    private void DisablePauseMenu()
    {
        if (pauseMenu != null)
            pauseMenu.enabled = false;
    }

    private void RestorePauseMenu()
    {
        if (pauseMenu == null)
            return;

        if (pauseRestoreCoroutine != null)
            StopCoroutine(pauseRestoreCoroutine);

        pauseRestoreCoroutine = StartCoroutine(
            RestorePauseMenuNextFrame()
        );
    }

    private IEnumerator RestorePauseMenuNextFrame()
    {
        yield return null;

        if (pauseMenu != null)
            pauseMenu.enabled = true;

        pauseRestoreCoroutine = null;
    }

    private void ResetEffects()
    {
        targetFOV = normalFOV;

        if (playerCam != null)
            playerCam.fieldOfView = normalFOV;

        if (blackBorders != null)
        {
            blackBorders.ResetBorders();
            blackBorders.SetActive(false);
        }

        if (cameraHUDZoom != null)
            cameraHUDZoom.ResetHUD();

        if (cameraBlink != null)
            cameraBlink.ResetBlink();
    }

    public void AddBattery(int amount)
    {
        if (battery != null)
            battery.Add(amount);
    }

    public bool CanReceiveBattery()
    {
        return battery != null && battery.CanReceive;
    }

    public bool IsBatteryFull()
    {
        return battery != null && battery.IsFull;
    }

    public void OnCameraLost()
    {
        if (!hasCamera)
            return;

        hasCamera = false;

        if (cameraMode && !isTransitioning)
            CloseCamera();
    }

    public void OnCameraRecovered()
    {
        hasCamera = true;
    }

    private void OnDestroy()
    {
        if (pauseRestoreCoroutine != null)
            StopCoroutine(pauseRestoreCoroutine);

        photoCapture?.Release();
    }
}
