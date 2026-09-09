using System.Collections;
using UnityEngine;

public class RandomLightFlicker : MonoBehaviour
{
    [Header("Light")]
    [SerializeField] private Light targetLight;

    [Header("Lamp Material")]
    [SerializeField] private Renderer lampRenderer;
    [SerializeField] private float emissionIntensity = 2f;

    [Header("Flicker Settings")]
    [SerializeField] private float minTime = 0.05f;
    [SerializeField] private float maxTime = 0.3f;
    [SerializeField] private float chanceToTurnOff = 0.5f;

    private Material lampMaterial;
    private readonly int emissionColor = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        if (targetLight == null)
        {
            targetLight = GetComponent<Light>();
        }

        if (lampRenderer != null)
        {
            lampMaterial = lampRenderer.material;
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

            if (Random.value <= chanceToTurnOff)
            {
                bool isOn = !targetLight.enabled;

                targetLight.enabled = isOn;
                UpdateLampEmission(isOn);
            }
        }
    }

    private void UpdateLampEmission(bool isOn)
    {
        if (lampMaterial == null)
            return;

        if (isOn)
        {
            lampMaterial.EnableKeyword("_EMISSION");
            lampMaterial.SetColor(
                emissionColor,
                Color.white * emissionIntensity
            );
        }
        else
        {
            lampMaterial.SetColor(
                emissionColor,
                Color.black
            );
        }
    }
}