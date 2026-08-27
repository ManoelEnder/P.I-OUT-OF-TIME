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

    [Header("Zoom")]
    public float zoomFOV = 40f;
    public float normalFOV = 60f;
    public float minZoomFOV = 25f;
    public float maxZoomFOV = 60f;
    public float zoomSpeed = 2.5f;

    [Header("HUD Zoom")]
    public RectTransform cameraHUDContent;
    public RectTransform[] cameraCorners;
    public Image[] batteryBars;

    public float normalHUDScale = 1f;
    public float maxZoomHUDScale = 0.72f;
    public float hudZoomSmoothness = 10f;

    [Header("Black Zoom Borders")]
    public Image blackBorderTop;
    public Image blackBorderBottom;
    public Image blackBorderLeft;
    public Image blackBorderRight;

    public float maxBlackBorderSize = 140f;

    [Header("Camera Blink")]
    public Image blinkTop;
    public Image blinkBottom;
    public float blinkDuration = 0.5f;
    public float blinkClosedSize = 600f;

    [Header("Transition")]
    public float enterTransitionTime = 0.4f;
    public float exitTransitionTime = 0.12f;

    [Header("Flash")]
    public float flashDuration = 0.07f;

    [Header("Battery")]
    public int maxBattery = 10;
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
    private Coroutine blinkCoroutine;

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

        SetupTransparentImage(fadeImage);
        SetupTransparentImage(flashImage);

        PrepareBlackBorders();
        PrepareBlink();

        SetBlackBorders(0f);
        SetHUDScale(1f);

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

        float targetScale =
            Mathf.Lerp(
                normalHUDScale,
                maxZoomHUDScale,
                zoomAmount
            );

        SetHUDScale(targetScale);

        float borderSize =
            Mathf.Lerp(
                0f,
                maxBlackBorderSize,
                zoomAmount
            );

        SetBlackBorders(borderSize);
    }

    private void SetHUDScale(float scale)
    {
        if (cameraHUDContent != null)
        {
            cameraHUDContent.localScale =
                Vector3.Lerp(
                    cameraHUDContent.localScale,
                    Vector3.one * scale,
                    Time.deltaTime * hudZoomSmoothness
                );
        }

        if (cameraCorners != null)
        {
            foreach (RectTransform corner in cameraCorners)
            {
                if (corner == null)
                    continue;

                corner.localScale =
                    Vector3.Lerp(
                        corner.localScale,
                        Vector3.one * scale,
                        Time.deltaTime * hudZoomSmoothness
                    );
            }
        }

        if (batteryBars != null)
        {
            foreach (Image battery in batteryBars)
            {
                if (battery == null)
                    continue;

                battery.rectTransform.localScale =
                    Vector3.Lerp(
                        battery.rectTransform.localScale,
                        Vector3.one * scale,
                        Time.deltaTime * hudZoomSmoothness
                    );
            }
        }
    }

    private void SetBlackBorders(float size)
    {
        SetTopBorder(size);
        SetBottomBorder(size);
        SetLeftBorder(size);
        SetRightBorder(size);
    }

    private void SetTopBorder(float size)
    {
        if (blackBorderTop == null)
            return;

        RectTransform rect =
            blackBorderTop.rectTransform;

        rect.anchorMin =
            new Vector2(0f, 1f);

        rect.anchorMax =
            new Vector2(1f, 1f);

        rect.pivot =
            new Vector2(0.5f, 1f);

        rect.offsetMin =
            new Vector2(0f, -size);

        rect.offsetMax =
            Vector2.zero;
    }

    private void SetBottomBorder(float size)
    {
        if (blackBorderBottom == null)
            return;

        RectTransform rect =
            blackBorderBottom.rectTransform;

        rect.anchorMin =
            new Vector2(0f, 0f);

        rect.anchorMax =
            new Vector2(1f, 0f);

        rect.pivot =
            new Vector2(0.5f, 0f);

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            new Vector2(0f, size);
    }

    private void SetLeftBorder(float size)
    {
        if (blackBorderLeft == null)
            return;

        RectTransform rect =
            blackBorderLeft.rectTransform;

        rect.anchorMin =
            new Vector2(0f, 0f);

        rect.anchorMax =
            new Vector2(0f, 1f);

        rect.pivot =
            new Vector2(0f, 0.5f);

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            new Vector2(size, 0f);
    }

    private void SetRightBorder(float size)
    {
        if (blackBorderRight == null)
            return;

        RectTransform rect =
            blackBorderRight.rectTransform;

        rect.anchorMin =
            new Vector2(1f, 0f);

        rect.anchorMax =
            new Vector2(1f, 1f);

        rect.pivot =
            new Vector2(1f, 0.5f);

        rect.offsetMin =
            new Vector2(-size, 0f);

        rect.offsetMax =
            Vector2.zero;
    }

    private void PrepareBlackBorders()
    {
        PrepareBlackBorder(blackBorderTop);
        PrepareBlackBorder(blackBorderBottom);
        PrepareBlackBorder(blackBorderLeft);
        PrepareBlackBorder(blackBorderRight);

        SetImageAlpha(blackBorderTop, 1f);
        SetImageAlpha(blackBorderBottom, 1f);
        SetImageAlpha(blackBorderLeft, 1f);
        SetImageAlpha(blackBorderRight, 1f);

        SetBlackBorderActive(false);
    }

    private void PrepareBlackBorder(Image image)
    {
        if (image == null)
            return;

        image.color = Color.black;
        image.raycastTarget = false;
    }

    private void SetBlackBorderActive(bool active)
    {
        if (blackBorderTop != null)
            blackBorderTop.gameObject.SetActive(active);

        if (blackBorderBottom != null)
            blackBorderBottom.gameObject.SetActive(active);

        if (blackBorderLeft != null)
            blackBorderLeft.gameObject.SetActive(active);

        if (blackBorderRight != null)
            blackBorderRight.gameObject.SetActive(active);
    }

    private void PrepareBlink()
    {
        PrepareBlinkImage(blinkTop);
        PrepareBlinkImage(blinkBottom);

        SetBlinkActive(false);
    }

    private void PrepareBlinkImage(Image image)
    {
        if (image == null)
            return;

        image.color = Color.black;
        image.raycastTarget = false;
    }

    private void SetBlinkActive(bool active)
    {
        if (blinkTop != null)
            blinkTop.gameObject.SetActive(active);

        if (blinkBottom != null)
            blinkBottom.gameObject.SetActive(active);
    }

    private IEnumerator CameraBlink()
    {
        if (blinkTop == null ||
            blinkBottom == null)
        {
            yield break;
        }

        SetBlinkActive(true);

        blinkTop.transform.SetAsLastSibling();
        blinkBottom.transform.SetAsLastSibling();

        float time = 0f;

        while (time < blinkDuration)
        {
            time += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    time / blinkDuration
                );

            float smooth =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            float size =
                Mathf.Lerp(
                    blinkClosedSize,
                    0f,
                    smooth
                );

            SetBlinkSize(size);

            yield return null;
        }

        SetBlinkSize(0f);
        SetBlinkActive(false);

        blinkCoroutine = null;
    }

    private void SetBlinkSize(float size)
    {
        if (blinkTop != null)
        {
            RectTransform top =
                blinkTop.rectTransform;

            top.anchorMin =
                new Vector2(0f, 1f);

            top.anchorMax =
                new Vector2(1f, 1f);

            top.pivot =
                new Vector2(0.5f, 1f);

            top.offsetMin =
                new Vector2(0f, -size);

            top.offsetMax =
                Vector2.zero;
        }

        if (blinkBottom != null)
        {
            RectTransform bottom =
                blinkBottom.rectTransform;

            bottom.anchorMin =
                new Vector2(0f, 0f);

            bottom.anchorMax =
                new Vector2(1f, 0f);

            bottom.pivot =
                new Vector2(0.5f, 0f);

            bottom.offsetMin =
                Vector2.zero;

            bottom.offsetMax =
                new Vector2(0f, size);
        }
    }

    private void HandleBattery()
    {
        batteryTimer +=
            Time.deltaTime;

        if (batteryTimer >=
            batteryDrainInterval)
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

    private void ToggleCameraMode()
    {
        if (!cameraMode &&
            currentBattery <= 0)
        {
            return;
        }

        cameraMode =
            !cameraMode;

        if (crosshair != null)
            crosshair.SetActive(!cameraMode);

        UpdateTemporalVisibility();

        if (transitionCoroutine != null)
            StopCoroutine(
                transitionCoroutine
            );

        transitionCoroutine =
            StartCoroutine(
                CameraTransition(cameraMode)
            );
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

            SetBlackBorderActive(true);
            SetBlackBorders(0f);
            SetHUDScale(1f);

            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }

            blinkCoroutine =
                StartCoroutine(
                    CameraBlink()
                );
        }

        while (time < duration)
        {
            time += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    time / duration
                );

            float smoothProgress =
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
                        smoothProgress
                    );
            }

            targetFOV =
                Mathf.Lerp(
                    startFOV,
                    target,
                    smoothProgress
                );

            UpdateZoomHUD();

            if (fadeImage != null)
            {
                Color color =
                    fadeImage.color;

                color.a =
                    entering
                        ? Mathf.Lerp(
                            0f,
                            0.35f,
                            smoothProgress
                        )
                        : Mathf.Lerp(
                            0.35f,
                            0f,
                            smoothProgress
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
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }

            SetBlinkActive(false);

            if (photoCam != null)
                photoCam.enabled = false;

            if (cameraHUD != null)
                cameraHUD.SetActive(false);

            SetBlackBorderActive(false);
            SetBlackBorders(0f);
            SetHUDScale(1f);
        }

        isTransitioning = false;
    }

    private IEnumerator TakePhoto()
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

    private Texture2D CreatePhoto()
    {
        RenderTexture.active = renderTexture;

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

    private void RevealTemporalObject(Renderer renderer)
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

    private IEnumerator HideTemporalObject(Renderer renderer)
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

    private void SetupTransparentImage(Image image)
    {
        if (image == null)
            return;

        Color color =
            image.color;

        color.a = 0f;

        image.color = color;

        image.gameObject.SetActive(false);
    }

    private void SetImageAlpha(
        Image image,
        float alpha
    )
    {
        if (image == null)
            return;

        Color color =
            image.color;

        color.a = alpha;

        image.color = color;
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