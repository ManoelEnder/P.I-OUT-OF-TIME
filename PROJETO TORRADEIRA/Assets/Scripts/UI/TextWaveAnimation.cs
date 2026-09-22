using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class TextWaveAnimation : MonoBehaviour
{
    [Header("Wave")]
    [SerializeField] private bool enableWave = true;
    [SerializeField] private float waveSpeed = 0.8f;
    [SerializeField] private float waveAmount = 2f;
    [SerializeField] private float waveFrequency = 0.8f;

    [Header("Blink")]
    [SerializeField] private bool enableBlink = true;
    [SerializeField] private float minimumBlinkInterval = 4f;
    [SerializeField] private float maximumBlinkInterval = 9f;
    [SerializeField] private float blinkDuration = 0.12f;
    [SerializeField][Range(0f, 1f)] private float blinkMinimumAlpha = 0.25f;

    private TMP_Text textComponent;

    private TMP_MeshInfo[] originalMeshInfo;
    private string lastText;
    private int lastCharacterCount;

    private float nextBlinkTime;
    private float blinkTimer;
    private bool isBlinking;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();

        RebuildMeshData();
        ScheduleNextBlink();
    }

    private void OnEnable()
    {
        if (textComponent == null)
            textComponent = GetComponent<TMP_Text>();

        ScheduleNextBlink();
    }

    private void Update()
    {
        if (textComponent == null)
            return;

        CheckForTextChanges();

        if (enableWave)
            AnimateWave();

        if (enableBlink)
            HandleBlink();
    }

    private void CheckForTextChanges()
    {
        if (lastText != textComponent.text ||
            lastCharacterCount != textComponent.textInfo.characterCount)
        {
            RebuildMeshData();
        }
    }

    private void RebuildMeshData()
    {
        textComponent.ForceMeshUpdate();

        originalMeshInfo = textComponent.textInfo.CopyMeshInfoVertexData();

        lastText = textComponent.text;
        lastCharacterCount = textComponent.textInfo.characterCount;
    }

    private void AnimateWave()
    {
        if (originalMeshInfo == null)
            return;

        TMP_TextInfo textInfo = textComponent.textInfo;

        if (textInfo == null || textInfo.characterCount == 0)
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

            int vertexCount = Mathf.Min(
                sourceVertices.Length,
                destinationVertices.Length
            );

            for (int j = 0; j < vertexCount; j++)
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

            if (materialIndex < 0 ||
                materialIndex >= textInfo.meshInfo.Length)
                continue;

            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            if (vertices == null ||
                vertexIndex < 0 ||
                vertexIndex + 3 >= vertices.Length)
                continue;

            float offset = Mathf.Sin(
                time + i * waveFrequency
            ) * waveAmount;

            vertices[vertexIndex].y += offset;
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
            mesh.RecalculateBounds();

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

        float progress = Mathf.Clamp01(
            blinkTimer / Mathf.Max(0.01f, blinkDuration)
        );

        float alpha;

        if (progress < 0.5f)
        {
            alpha = Mathf.Lerp(
                1f,
                blinkMinimumAlpha,
                progress * 2f
            );
        }
        else
        {
            alpha = Mathf.Lerp(
                blinkMinimumAlpha,
                1f,
                (progress - 0.5f) * 2f
            );
        }

        SetAlpha(alpha);

        if (progress >= 1f)
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
        float minimum = Mathf.Max(0.1f, minimumBlinkInterval);
        float maximum = Mathf.Max(minimum, maximumBlinkInterval);

        nextBlinkTime = Time.unscaledTime +
                        Random.Range(minimum, maximum);
    }

    private void SetAlpha(float alpha)
    {
        if (textComponent == null)
            return;

        Color color = textComponent.color;
        color.a = alpha;
        textComponent.color = color;
    }

    private void OnDisable()
    {
        if (textComponent != null)
            SetAlpha(1f);
    }
}