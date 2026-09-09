using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LensDistortionCompensator : MonoBehaviour
{
    public static LensDistortionCompensator Instance { get; private set; }

    public Volume targetVolume;

    private LensDistortion lensDistortion;

    private void Awake()
    {
        Instance = this;

        if (targetVolume != null && targetVolume.profile.TryGet(out lensDistortion))
        {
        }
        else
        {
            Debug.LogWarning("LensDistortionCompensator: não encontrou Lens Distortion no Volume informado.");
        }
    }

    public Vector2 UndistortScreenPoint(Vector2 screenPoint)
    {
        if (lensDistortion == null || !lensDistortion.active || lensDistortion.intensity.value == 0f)
            return screenPoint;

        float intensity = lensDistortion.intensity.value;
        float xMultiplier = lensDistortion.xMultiplier.value;
        float yMultiplier = lensDistortion.yMultiplier.value;
        float scale = lensDistortion.scale.value;
        Vector2 center = lensDistortion.center.value;

        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        Vector2 uv = new Vector2(screenPoint.x / screenSize.x, screenPoint.y / screenSize.y);
        Vector2 offsetFromCenter = (uv - center) * 2f;

        float aspect = screenSize.x / screenSize.y;
        offsetFromCenter.x *= aspect;

        Vector2 undistorted = offsetFromCenter;
        for (int i = 0; i < 5; i++)
        {
            float r2 = undistorted.x * undistorted.x * xMultiplier + undistorted.y * undistorted.y * yMultiplier;
            float factor = 1f + intensity * r2;
            if (Mathf.Approximately(factor, 0f)) factor = 0.0001f;
            undistorted = offsetFromCenter / factor;
        }

        undistorted /= scale;

        undistorted.x /= aspect;
        Vector2 correctedUv = (undistorted / 2f) + center;
        Vector2 correctedScreenPoint = new Vector2(correctedUv.x * screenSize.x, correctedUv.y * screenSize.y);

        return correctedScreenPoint;
    }
}