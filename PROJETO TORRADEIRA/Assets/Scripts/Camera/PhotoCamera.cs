using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class PhotoCamera : MonoBehaviour
{
    public Camera photoCam;
    public Camera playerCam;

    public RawImage photoPreview;
    public Image flashImage;

    public AudioSource audioSource;
    public AudioClip shutterSound;

    public TextMeshProUGUI photoCounter;
    public GameObject crosshair;
    public GameObject cameraHUD;
    public Image fadeImage;

    public float cooldown = 2f;
    public float flashDuration = 0.07f;

    public float zoomFOV = 40f;
    public float normalFOV = 60f;
    public float transitionTime = 0.4f;

    public float minZoomFOV = 25f;
    public float maxZoomFOV = 60f;

    public float tempoRevelado = 30f;

    public MissionSystem missionSystem;
    public AlbumFotos albumFotos;

    public int maxBattery = 10;
    private int currentBattery;

    public Image[] batteryBars;

    public float batteryDrainInterval = 3f;
    private float batteryTimer;

    private RenderTexture rt;
    private Texture2D photo;

    private bool canShoot = true;
    private int photoCount;
    private bool cameraMode;
    private float targetFOV;
    private bool isTransitioning;

    private Renderer[] temporais;
    private readonly List<Renderer> revelados = new List<Renderer>();

    private Coroutine transitionCoroutine;

    void Start()
    {
        rt = new RenderTexture(512, 512, 24);

        photoCam.targetTexture = rt;
        photoCam.enabled = false;

        targetFOV = normalFOV;

        currentBattery = maxBattery;
        UpdateBatteryUI();

        List<Renderer> lista = new List<Renderer>();
        Renderer[] todos = FindObjectsOfType<Renderer>(true);

        foreach (Renderer r in todos)
        {
            if (r.CompareTag("Temporal"))
            {
                lista.Add(r);
                r.enabled = false;
            }
        }

        temporais = lista.ToArray();

        if (photoPreview != null)
            photoPreview.gameObject.SetActive(false);

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

        AtualizarVisibilidade();
    }

    void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.cKey.wasPressedThisFrame)
        {
            ToggleCameraMode();
        }

        if (cameraMode &&
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            canShoot)
        {
            StartCoroutine(TakePhoto());
        }

        if (cameraMode && !isTransitioning)
        {
            if (Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;

                if (scroll != 0)
                {
                    targetFOV -= scroll * 2.5f;
                    targetFOV = Mathf.Clamp(
                        targetFOV,
                        minZoomFOV,
                        maxZoomFOV
                    );
                }
            }

            playerCam.fieldOfView = Mathf.Lerp(
                playerCam.fieldOfView,
                targetFOV,
                Time.deltaTime * 10f
            );
        }

        if (cameraMode)
        {
            batteryTimer += Time.deltaTime;

            if (batteryTimer >= batteryDrainInterval)
            {
                batteryTimer = 0f;
                UseBattery(1);
            }
        }
        else
        {
            batteryTimer = 0f;
        }
    }

    void ToggleCameraMode()
    {
        if (!cameraMode && currentBattery <= 0)
            return;

        cameraMode = !cameraMode;

        if (crosshair != null)
            crosshair.SetActive(!cameraMode);

        AtualizarVisibilidade();

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine =
            StartCoroutine(CameraTransition(cameraMode));
    }

    IEnumerator CameraTransition(bool entering)
    {
        isTransitioning = true;

        float startFOV = playerCam.fieldOfView;
        float target = entering ? zoomFOV : normalFOV;

        float t = 0f;

        if (cameraHUD != null)
            cameraHUD.SetActive(true);

        while (t < transitionTime)
        {
            t += Time.deltaTime;

            float progress = Mathf.Clamp01(t / transitionTime);

            playerCam.fieldOfView = Mathf.Lerp(
                startFOV,
                target,
                progress
            );

            if (fadeImage != null)
            {
                Color c = fadeImage.color;

                c.a = entering
                    ? Mathf.Lerp(0f, 0.35f, progress)
                    : Mathf.Lerp(0.35f, 0f, progress);

                fadeImage.color = c;
            }

            yield return null;
        }

        playerCam.fieldOfView = target;
        targetFOV = target;

        if (!entering && cameraHUD != null)
            cameraHUD.SetActive(false);

        isTransitioning = false;
    }

    IEnumerator TakePhoto()
    {
        if (!canShoot || currentBattery <= 0)
            yield break;

        canShoot = false;

        UseBattery(1);

        photoCount++;

        if (photoCounter != null)
            photoCounter.text = "Fotos: " + photoCount;

        if (missionSystem != null)
            missionSystem.AddFoto();

        photoCam.transform.SetPositionAndRotation(
            playerCam.transform.position,
            playerCam.transform.rotation
        );

        photoCam.fieldOfView = playerCam.fieldOfView;

        yield return new WaitForEndOfFrame();

        Ray ray = playerCam.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (hit.collider.CompareTag("Temporal"))
            {
                Renderer r = hit.collider.GetComponent<Renderer>();

                if (r != null)
                    RevelarPorTempo(r);
            }
        }

        AtualizarVisibilidade();

        photoCam.Render();

        if (audioSource != null && shutterSound != null)
            audioSource.PlayOneShot(shutterSound);

        yield return StartCoroutine(FlashCoroutine());

        RenderTexture.active = rt;

        if (photo != null)
            Destroy(photo);

        photo = new Texture2D(
            rt.width,
            rt.height,
            TextureFormat.RGB24,
            false
        );

        photo.ReadPixels(
            new Rect(0, 0, rt.width, rt.height),
            0,
            0
        );

        photo.Apply();

        RenderTexture.active = null;

        if (albumFotos != null)
            albumFotos.AdicionarFoto(photo);

        if (photoPreview != null)
        {
            photoPreview.texture = photo;
            photoPreview.gameObject.SetActive(true);

            yield return new WaitForSeconds(1.3f);

            photoPreview.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(cooldown);

        canShoot = true;
    }

    void RevelarPorTempo(Renderer r)
    {
        if (r == null)
            return;

        if (!revelados.Contains(r))
            revelados.Add(r);

        r.enabled = true;

        TemporalObjectPickup pickup =
            r.GetComponent<TemporalObjectPickup>();

        if (pickup != null)
            pickup.SetRevealed(true);

        StartCoroutine(EsconderDepois(r));

        if (missionSystem != null &&
            r.gameObject.name == "Object (1)")
        {
            missionSystem.DescobriuPeca();
        }
    }

    IEnumerator EsconderDepois(Renderer r)
    {
        yield return new WaitForSeconds(tempoRevelado);

        revelados.Remove(r);

        if (r == null)
            yield break;

        TemporalObjectPickup pickup =
            r.GetComponent<TemporalObjectPickup>();

        if (pickup != null)
            pickup.SetRevealed(false);

        if (!cameraMode)
            r.enabled = false;
    }

    void AtualizarVisibilidade()
    {
        if (temporais == null)
            return;

        foreach (Renderer r in temporais)
        {
            if (r == null)
                continue;

            r.enabled =
                cameraMode ||
                revelados.Contains(r);
        }
    }

    IEnumerator FlashCoroutine()
    {
        if (flashImage == null)
            yield break;

        flashImage.gameObject.SetActive(true);

        Color c = flashImage.color;
        c.a = 1f;
        flashImage.color = c;

        yield return new WaitForSeconds(flashDuration);

        c.a = 0f;
        flashImage.color = c;

        flashImage.gameObject.SetActive(false);
    }

    void UseBattery(int amount)
    {
        currentBattery -= amount;

        currentBattery = Mathf.Clamp(
            currentBattery,
            0,
            maxBattery
        );

        UpdateBatteryUI();
    }

    public void AddBattery(int amount)
    {
        currentBattery += amount;

        currentBattery = Mathf.Clamp(
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

        for (int i = 0; i < batteryBars.Length; i++)
        {
            if (batteryBars[i] != null)
                batteryBars[i].enabled = i < currentBattery;
        }
    }
}