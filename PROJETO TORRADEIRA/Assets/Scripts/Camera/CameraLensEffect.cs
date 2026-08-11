using UnityEngine;
using DG.Tweening;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
public class CameraLensEffect : MonoBehaviour
{
    [Header("Zoom da lente (Field of View)")]
    [SerializeField] private float fovPulseAmount = 3f;
    [SerializeField] private float fovPulseDuration = 2.5f;
    [SerializeField] private Ease fovEase = Ease.InOutSine;

    [Header("Puxada de foco (opcional, precisa de URP Volume com Depth of Field)")]
    [SerializeField] private bool useFocusRack = true;
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private float focusDistanceNear = 0.3f;
    [SerializeField] private float focusDistanceFar = 3f;
    [SerializeField] private float focusRackDuration = 2.5f;

    private Camera cam;
    private float baseFov;
    private DepthOfField depthOfField;
    private Tween fovTween;
    private Tween focusTween;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        baseFov = cam.fieldOfView;

        if (useFocusRack && postProcessVolume != null && postProcessVolume.profile != null)
        {
            postProcessVolume.profile.TryGet(out depthOfField);
        }
    }

    private void OnEnable()
    {
        StartFovPulse();

        if (useFocusRack && depthOfField != null)
            StartFocusRack();
    }

    private void StartFovPulse()
    {
        fovTween?.Kill();
        fovTween = DOTween.To(
                () => cam.fieldOfView,
                v => cam.fieldOfView = v,
                baseFov + fovPulseAmount,
                fovPulseDuration)
            .SetEase(fovEase)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StartFocusRack()
    {
        focusTween?.Kill();
        focusTween = DOTween.To(
                () => depthOfField.focusDistance.value,
                v => depthOfField.focusDistance.value = v,
                focusDistanceFar,
                focusRackDuration)
            .From(focusDistanceNear)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void StopEffect()
    {
        fovTween?.Kill();
        focusTween?.Kill();
    }

    public void ResumeEffect()
    {
        StartFovPulse();
        if (useFocusRack && depthOfField != null)
            StartFocusRack();
    }

    private void OnDisable()
    {
        fovTween?.Kill();
        focusTween?.Kill();
    }
}