using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraOverheat : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PhotoCamera photoCamera;
    [SerializeField] private GameObject lostCameraPrefab;
    [SerializeField] private Transform[] teleportPoints;

    [Header("Temperatura")]
    [SerializeField] private float maxHeat = 100f;
    [SerializeField] private float heatPerSecond = 5f;
    [SerializeField] private float heatPerPhoto = 12f;
    [SerializeField] private float coolingPerSecond = 8f;

    [Header("Interface")]
    [SerializeField] private Image heatBar;

    [Header("Distorcao Temporal")]
    [SerializeField] private Volume glitchVolume;
    [SerializeField, Range(0f, 1f)] private float maxChromatic = 0.8f;
    [SerializeField, Range(0f, 1f)] private float maxDistortion = 0.25f;

    private float currentHeat;
    private bool cameraLost;
    private bool rupturePending;

    private GameObject lostCameraInstance;

    private ChromaticAberration chromatic;
    private LensDistortion distortion;

    private float originalChromatic;
    private float originalDistortion;

    public float HeatPercent =>
        currentHeat / Mathf.Max(1f, maxHeat);

    public bool IsCameraLost => cameraLost;

    private void Awake()
    {
        if (photoCamera == null)
            photoCamera = GetComponent<PhotoCamera>();

        if (glitchVolume != null && glitchVolume.profile != null)
        {
            glitchVolume.profile.TryGet(out chromatic);
            glitchVolume.profile.TryGet(out distortion);

            if (chromatic != null)
                originalChromatic = chromatic.intensity.value;

            if (distortion != null)
                originalDistortion = distortion.intensity.value;
        }

        UpdateHeatUI();
        ResetGlitch();
    }

    private void Update()
    {
        if (cameraLost || photoCamera == null)
            return;

        if (!photoCamera.isActiveAndEnabled)
            return;

        if (!rupturePending)
        {
            float heatChange = photoCamera.IsCameraMode
                ? heatPerSecond
                : -coolingPerSecond;

            currentHeat = Mathf.Clamp(
                currentHeat + heatChange * Time.deltaTime,
                0f,
                Mathf.Max(1f, maxHeat)
            );

            if (currentHeat >= Mathf.Max(1f, maxHeat))
                rupturePending = true;
        }

        UpdateHeatUI();
        UpdateGlitch();

        if (rupturePending && photoCamera.CanLoseCamera)
            LoseCamera();
    }

    public void RegisterPhotoHeat()
    {
        if (cameraLost || !enabled || photoCamera == null)
            return;

        currentHeat = Mathf.Clamp(
            currentHeat + heatPerPhoto,
            0f,
            Mathf.Max(1f, maxHeat)
        );

        if (currentHeat >= Mathf.Max(1f, maxHeat))
            rupturePending = true;

        UpdateHeatUI();
        UpdateGlitch();
    }

    private void LoseCamera()
    {
        if (cameraLost || !photoCamera.CanLoseCamera)
            return;

        if (lostCameraPrefab == null)
        {
            Debug.LogError(
                "CameraOverheat: configure o Lost Camera Prefab.",
                this
            );

            return;
        }

        Transform destination = GetRandomPoint();

        if (destination == null)
        {
            Debug.LogError(
                "CameraOverheat: configure pelo menos um Teleport Point.",
                this
            );

            return;
        }

        GameObject instance = Instantiate(
            lostCameraPrefab,
            destination.position,
            destination.rotation
        );

        LostCameraPickup pickup =
            instance.GetComponent<LostCameraPickup>();

        if (pickup == null)
        {
            Debug.LogError(
                "O prefab precisa ter o script LostCameraPickup.",
                instance
            );

            Destroy(instance);
            return;
        }

        pickup.Initialize(this);

        lostCameraInstance = instance;

        cameraLost = true;
        rupturePending = false;

        photoCamera.OnCameraLost();

        ResetGlitch();

        Debug.Log(
            "Ruptura temporal! A camera desapareceu."
        );
    }

    private Transform GetRandomPoint()
    {
        if (teleportPoints == null || teleportPoints.Length == 0)
            return null;

        int validCount = 0;

        foreach (Transform point in teleportPoints)
        {
            if (point != null)
                validCount++;
        }

        if (validCount == 0)
            return null;

        int selected = Random.Range(0, validCount);

        foreach (Transform point in teleportPoints)
        {
            if (point == null)
                continue;

            if (selected == 0)
                return point;

            selected--;
        }

        return null;
    }

    public void RecoverCamera()
    {
        if (!cameraLost)
            return;

        cameraLost = false;
        rupturePending = false;
        currentHeat = 0f;

        photoCamera.OnCameraRecovered();

        if (lostCameraInstance != null)
            Destroy(lostCameraInstance);

        lostCameraInstance = null;

        UpdateHeatUI();
        ResetGlitch();

        Debug.Log("Camera recuperada!");
    }

    private void UpdateHeatUI()
    {
        if (heatBar != null)
            heatBar.fillAmount = HeatPercent;
    }

    private void UpdateGlitch()
    {
        float strength = Mathf.InverseLerp(
            0.25f,
            1f,
            HeatPercent
        );

        float pulse = 0f;

        if (HeatPercent >= 0.65f)
        {
            pulse = Mathf.Pow(
                Mathf.Max(
                    0f,
                    Mathf.Sin(Time.time * 22f)
                ),
                12f
            ) * strength;
        }

        if (chromatic != null)
        {
            float value = Mathf.Clamp01(
                originalChromatic +
                strength * maxChromatic +
                pulse * 0.15f
            );

            chromatic.intensity.Override(value);
        }

        if (distortion != null)
        {
            float value =
                originalDistortion +
                Mathf.Sin(Time.time * 13f) *
                maxDistortion *
                strength +
                pulse * 0.1f;

            distortion.intensity.Override(
                Mathf.Clamp(value, -1f, 1f)
            );
        }
    }

    private void ResetGlitch()
    {
        if (chromatic != null)
            chromatic.intensity.Override(originalChromatic);

        if (distortion != null)
            distortion.intensity.Override(originalDistortion);
    }

    private void OnDisable()
    {
        ResetGlitch();
    }

    private void OnDestroy()
    {
        ResetGlitch();
    }
}