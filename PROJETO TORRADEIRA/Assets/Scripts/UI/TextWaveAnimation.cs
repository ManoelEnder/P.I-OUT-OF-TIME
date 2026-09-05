using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class TextWaveAnimation : MonoBehaviour
{
    [Header("Wave")]
    [SerializeField] private float waveSpeed = 0.8f;
    [SerializeField] private float waveAmount = 2f;
    [SerializeField] private float waveFrequency = 0.8f;

    [Header("Blink")]
    [SerializeField] private bool enableBlink = true;
    [SerializeField] private float minimumBlinkInterval = 4f;
    [SerializeField] private float maximumBlinkInterval = 9f;
    [SerializeField] private float blinkDuration = 0.12f;

    private TMP_Text textComponent;
    private TMP_MeshInfo[] originalMeshInfo;

    private float nextBlinkTime;
    private float blinkTimer;
    private bool isBlinking;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();

        textComponent.ForceMeshUpdate();
        originalMeshInfo = textComponent.textInfo.CopyMeshInfoVertexData();

        ScheduleNextBlink();
    }

    private void Update()
    {
        if (textComponent == null || originalMeshInfo == null)
            return;

        AnimateWave();

        if (enableBlink)
        {
            HandleBlink();
        }
    }

    private void AnimateWave()
    {
        if (textComponent == null)
            return;

        TMP_TextInfo textInfo = textComponent.textInfo;

        if (textInfo == null)
            return;

        float time = Time.unscaledTime * waveSpeed;

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            if (i >= originalMeshInfo.Length)
                continue;

            Vector3[] sourceVertices = originalMeshInfo[i].vertices;
            Vector3[] destinationVertices = textInfo.meshInfo[i].vertices;

            if (sourceVertices == null || destinationVertices == null)
                continue;

            int length = Mathf.Min(
                sourceVertices.Length,
                destinationVertices.Length
            );

            for (int j = 0; j < length; j++)
            {
                destinationVertices[j] = sourceVertices[j];
            }
        }

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo character = textInfo.characterInfo[i];

            if (!character.isVisible)
                continue;

            int materialIndex = character.materialReferenceIndex;
            int vertexIndex = character.vertexIndex;

            if (materialIndex >= textInfo.meshInfo.Length)
                continue;

            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            if (vertices == null || vertexIndex + 3 >= vertices.Length)
                continue;

            float offset = Mathf.Sin(
                time + i * waveFrequency
            ) * waveAmount;

            vertices[vertexIndex + 0].y += offset;
            vertices[vertexIndex + 1].y += offset;
            vertices[vertexIndex + 2].y += offset;
            vertices[vertexIndex + 3].y += offset;
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            Mesh mesh = textInfo.meshInfo[i].mesh;

            if (mesh == null)
                continue;

            mesh.vertices = textInfo.meshInfo[i].vertices;

            textComponent.UpdateGeometry(mesh, i);
        }
    }

    private void HandleBlink()
    {
        if (!isBlinking && Time.unscaledTime >= nextBlinkTime)
        {
            StartBlink();
        }

        if (!isBlinking)
            return;

        blinkTimer += Time.unscaledDeltaTime;

        float progress = blinkTimer / blinkDuration;

        float alpha;

        if (progress < 0.5f)
        {
            alpha = Mathf.Lerp(
                1f,
                0.25f,
                progress * 2f
            );
        }
        else
        {
            alpha = Mathf.Lerp(
                0.25f,
                1f,
                (progress - 0.5f) * 2f
            );
        }

        SetAlpha(alpha);

        if (blinkTimer >= blinkDuration)
        {
            SetAlpha(1f);
            isBlinking = false;

            ScheduleNextBlink();
        }
    }

    private void StartBlink()
    {
        isBlinking = true;
        blinkTimer = 0f;
    }

    private void ScheduleNextBlink()
    {
        nextBlinkTime = Time.unscaledTime +
                        Random.Range(
                            minimumBlinkInterval,
                            maximumBlinkInterval
                        );
    }

    private void SetAlpha(float alpha)
    {
        if (textComponent == null)
            return;

        Color color = textComponent.color;
        color.a = alpha;

        textComponent.color = color;
    }
}