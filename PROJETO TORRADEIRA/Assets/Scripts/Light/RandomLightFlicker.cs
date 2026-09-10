using UnityEngine;
using System.Collections;

public class RandomLightFlicker : MonoBehaviour
{
    [Header("Light")]
    [SerializeField] private Light targetLight;

    [Header("Lamp Mesh")]
    [SerializeField] private Renderer lampRenderer;

    [Header("Emission")]
    [SerializeField] private Color emissionColor = Color.white;
    [SerializeField] private float emissionIntensity = 2f;

    [Header("Flicker Timing")]
    [SerializeField] private float minTime = 0.05f;
    [SerializeField] private float maxTime = 0.3f;

    [Header("Flicker Style")]
    [SerializeField] private bool toggleOnOff = true;
    [SerializeField][Range(0f, 1f)] private float chanceToToggle = 0.5f;
    [SerializeField][Range(0f, 1f)] private float minIntensityFactor = 0.2f;

    private float originalIntensity;
    private Material lampMaterial;
    private Color baseEmission;

    private void Start()
    {
        if (targetLight != null)
            originalIntensity = targetLight.intensity;

        if (lampRenderer != null)
        {
            lampMaterial = lampRenderer.material;

            if (lampMaterial.HasProperty("_EmissionColor"))
            {
                lampMaterial.EnableKeyword("_EMISSION");

                baseEmission =
                    emissionColor * emissionIntensity;

                lampMaterial.SetColor(
                    "_EmissionColor",
                    baseEmission
                );
            }
        }

        StartCoroutine(Flicker());
    }

    private IEnumerator Flicker()
    {
        while (true)
        {
            yield return new WaitForSeconds(
                Random.Range(minTime, maxTime)
            );

            if (toggleOnOff &&
                Random.value < chanceToToggle)
            {
                bool state =
                    targetLight != null &&
                    targetLight.enabled;

                SetState(!state);
            }
            else
            {
                float factor =
                    Random.Range(
                        minIntensityFactor,
                        1f
                    );

                ApplyFlicker(factor);
            }
        }
    }

    private void ApplyFlicker(float factor)
    {
        if (targetLight != null)
        {
            targetLight.enabled = true;

            targetLight.intensity =
                originalIntensity * factor;
        }

        if (lampRenderer != null)
            lampRenderer.enabled = true;

        if (lampMaterial != null &&
            lampMaterial.HasProperty("_EmissionColor"))
        {
            lampMaterial.SetColor(
                "_EmissionColor",
                baseEmission * factor
            );
        }
    }

    private void SetState(bool state)
    {
        if (targetLight != null)
        {
            targetLight.enabled = state;

            if (state)
                targetLight.intensity =
                    originalIntensity;
        }

        if (lampRenderer != null)
            lampRenderer.enabled = true;

        if (lampMaterial != null &&
            lampMaterial.HasProperty("_EmissionColor"))
        {
            lampMaterial.SetColor(
                "_EmissionColor",
                state
                    ? baseEmission
                    : Color.black
            );
        }
    }
}