using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PhotoCamera : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private Camera playerCam;
    [SerializeField] private PhotoCapture photoCapture;

    [Header("Módulos")]
    [SerializeField] private CameraHUD hud;
    [SerializeField] private CameraZoom zoom;
    [SerializeField] private CameraBattery battery;
    [SerializeField] private CameraFlash flash;

    [Header("Transição")]
    [SerializeField] private float enterTransitionTime = 0.4f;
    [SerializeField] private float exitTransitionTime = 0.12f;

    [Header("Objetos Temporais")]
    [SerializeField] private float tempoRevelado = 30f;

    [Header("Dados da Foto")]
    [SerializeField] private CameraPhotoData photoData;

    private TemporalObjectRevealer temporalRevealer;
    private Coroutine transitionCoroutine;

    private bool cameraMode;
    private bool canShoot = true;
    private bool isTransitioning;

    public bool IsCameraMode => cameraMode;

    private bool CanTakePhoto =>
        canShoot &&
        cameraMode &&
        !battery.IsEmpty &&
        !PauseMenu.IsPaused;

    private void Start()
    {
        photoCapture.Initialize();
        zoom.Initialize(playerCam);
        battery.Initialize();
        flash.Initialize();
        hud.Initialize();

        temporalRevealer = new TemporalObjectRevealer(
            this,
            tempoRevelado,
            () => cameraMode
        );

        temporalRevealer.FindAll();
        temporalRevealer.UpdateVisibility();

        if (photoData != null)
            photoData.Initialize();
    }

    private void Update()
    {
        if (PauseMenu.IsPaused)
            return;

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
            HandleShootInput();
        }

        HandleBattery();
    }

    public void AddBattery(int amount)
    {
        battery.Add(amount);
    }

    public bool CanReceiveBattery()
    {
        return battery.CanReceive;
    }

    public bool IsBatteryFull()
    {
        return battery.IsFull;
    }

    public bool TryExitCameraMode()
    {
        if (!cameraMode)
            return false;

        CloseCamera();
        return true;
    }

    private void HandleShootInput()
    {
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            canShoot)
        {
            StartCoroutine(TakePhoto());
        }
    }

    private void HandleZoom()
    {
        if (Mouse.current == null)
            return;

        float scroll = Mouse.current.scroll.ReadValue().y;

        zoom.Update(playerCam, scroll, Time.deltaTime);
        UpdateZoomHUD();
    }

    private void UpdateZoomHUD()
    {
        hud.ApplyZoom(zoom.GetZoomAmount(playerCam));
    }

    private void HandleBattery()
    {
        bool drained = battery.Tick(Time.deltaTime);

        if (drained && battery.IsEmpty && cameraMode)
            CloseCamera();
    }

    private void ToggleCameraMode()
    {
        if (isTransitioning)
            return;

        if (cameraMode)
        {
            CloseCamera();
            return;
        }

        if (!battery.IsEmpty)
            OpenCamera();
    }

    private void OpenCamera()
    {
        cameraMode = true;
        hud.SetCrosshairVisible(false);
        temporalRevealer.UpdateVisibility();
        StartTransition(true);
    }

    private void CloseCamera()
    {
        if (!cameraMode)
            return;

        cameraMode = false;
        hud.SetCrosshairVisible(true);
        temporalRevealer.UpdateVisibility();
        StartTransition(false);
    }

    private void StartTransition(bool entering)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(CameraTransition(entering));
    }

    private IEnumerator CameraTransition(bool entering)
    {
        isTransitioning = true;

        float startFOV = playerCam != null
            ? playerCam.fieldOfView
            : zoom.NormalFOV;

        float target = entering ? zoom.ZoomFOV : zoom.NormalFOV;
        float duration = entering ? enterTransitionTime : exitTransitionTime;
        float time = 0f;

        if (entering)
        {
            photoCapture.SetEnabled(true);
            hud.Show();
        }

        while (time < duration)
        {
            while (PauseMenu.IsPaused)
                yield return null;

            time += Time.deltaTime;

            float smooth = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(time / duration)
            );

            float fov = Mathf.Lerp(startFOV, target, smooth);

            if (playerCam != null)
                playerCam.fieldOfView = fov;

            zoom.TargetFOV = fov;

            if (entering)
                UpdateZoomHUD();

            yield return null;
        }

        if (playerCam != null)
            playerCam.fieldOfView = target;

        zoom.TargetFOV = target;

        if (!entering)
        {
            photoCapture.SetEnabled(false);
            hud.Hide();
        }

        isTransitioning = false;
    }

    private IEnumerator TakePhoto()
    {
        if (!CanTakePhoto)
            yield break;

        canShoot = false;

        photoCapture.SyncWith(playerCam);

        yield return new WaitForEndOfFrame();

        if (PauseMenu.IsPaused)
        {
            canShoot = true;
            yield break;
        }

        temporalRevealer.RevealAtScreenCenter(playerCam);
        temporalRevealer.UpdateVisibility();

        Texture2D photo = photoCapture.Capture();

        yield return StartCoroutine(flash.Play());

        if (photoData != null)
        {
            photoData.ProcessPhoto(photo);

            yield return new WaitForSeconds(photoData.PreviewDuration);
            yield return new WaitForSeconds(photoData.Cooldown);
        }

        battery.Use(1);

        if (battery.IsEmpty && cameraMode)
            CloseCamera();

        canShoot = true;
    }
}