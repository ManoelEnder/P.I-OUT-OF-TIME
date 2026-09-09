using System.Collections;
using UnityEngine;

public class RandomLightFlicker : MonoBehaviour
{
    [Header("Light")]
    [SerializeField] private Light targetLight;

    [Header("Lamp Material")]
    [SerializeField] private Renderer lampRenderer;
    [SerializeField] private float emissionIntensity = 2f;

    [Header("Flicker Timing")]
    [SerializeField] private float minTime = 0.05f;
    [SerializeField] private float maxTime = 0.3f;

    [Header("Flicker Style")]
    [Tooltip("Se true, alterna liga/desliga. Se false, varia a intensidade.")]
    [SerializeField] private bool toggleOnOff = true;
    [SerializeField, Range(0f, 1f)] private float chanceToToggle = 0.5f;
    [SerializeField, Range(0f, 1f)] private float minIntensityFactor = 0.2f;

    private Material lampMaterial;
    private MaterialPropertyBlock propBlock;
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private float baseLightIntensity;

    private void Awake()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();

        if (targetLight != null)
            baseLightIntensity = targetLight.intensity;

        if (lampRenderer != null)
        {
            propBlock = new MaterialPropertyBlock();
            lampMaterial = lampRenderer.sharedMaterial;

            if (lampMaterial != null)
                lampMaterial.EnableKeyword("_EMISSION");
        }
    }

    private void Start()
    {
        StartCoroutine(FlickerLight());
    }

    private IEnumerator FlickerLight()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minTime, maxTime));
            Debug.Log($"[Flicker] Tick - isOn atual: {targetLight.enabled}");
            if (toggleOnOff)
            {
                if (Random.value <= chanceToToggle && targetLight != null)
                {
                    bool isOn = !targetLight.enabled;
                    targetLight.enabled = isOn;
                    UpdateLampEmission(isOn ? 1f : 0f);
                }
            }
            else
            {
                float factor = Random.Range(minIntensityFactor, 1f);

                if (targetLight != null)
                    targetLight.intensity = baseLightIntensity * factor;

                UpdateLampEmission(factor);
            }
        }
    }

    private void UpdateLampEmission(float factor)
    {
        if (lampRenderer == null || lampMaterial == null)
            return;

        lampRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor(EmissionColorId, Color.white * emissionIntensity * factor);
        lampRenderer.SetPropertyBlock(propBlock);
    }
}