using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSave : MonoBehaviour
{
    [Header("Save System")]
    [SerializeField] private SaveSystem saveSystem;

    [Header("Auto Save")]
    [SerializeField] private float autoSaveInterval = 180f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI saveMessageText;
    [SerializeField] private float messageDuration = 2f;

    [Header("Fade")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;

    private CharacterController characterController;
    private float autoSaveTimer;
    private Coroutine messageCoroutine;
    private bool isLoading;

    private void Awake()
    {
        if (saveSystem == null)
        {
            saveSystem = FindFirstObjectByType<SaveSystem>();
        }

        characterController = GetComponent<CharacterController>();

        autoSaveTimer = autoSaveInterval;

        if (saveMessageText != null)
        {
            saveMessageText.gameObject.SetActive(false);
        }

        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.r = 0f;
            color.g = 0f;
            color.b = 0f;
            color.a = 0f;

            fadeImage.color = color;
        }
    }

    private void Update()
    {
        if (isLoading)
        {
            return;
        }

        HandleAutoSave();
        HandleInput();
    }

    private void HandleAutoSave()
    {
        if (autoSaveInterval <= 0f)
        {
            return;
        }

        autoSaveTimer -= Time.deltaTime;

        if (autoSaveTimer <= 0f)
        {
            SaveGame();

            autoSaveTimer = autoSaveInterval;
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SaveGame();
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            LoadGame();
        }
    }

    public void SaveGame()
    {
        if (saveSystem == null || isLoading)
        {
            return;
        }

        ShowMessage("SALVANDO...");

        SaveData data = new();

        data.playerX = transform.position.x;
        data.playerY = transform.position.y;
        data.playerZ = transform.position.z;

        data.playerRotX = transform.eulerAngles.x;
        data.playerRotY = transform.eulerAngles.y;
        data.playerRotZ = transform.eulerAngles.z;

        SaveableObject[] saveableObjects =
            FindObjectsByType<SaveableObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (SaveableObject saveableObject in saveableObjects)
        {
            if (saveableObject.gameObject == gameObject)
            {
                continue;
            }

            data.objects.Add(
                saveableObject.CreateData()
            );
        }

        if (saveSystem.Save(data))
        {
            StartCoroutine(SaveCompleted());
        }
    }

    private IEnumerator SaveCompleted()
    {
        yield return new WaitForSeconds(0.5f);

        ShowMessage("JOGO SALVO!");
    }

    public void LoadGame()
    {
        if (saveSystem == null || isLoading)
        {
            return;
        }

        StartCoroutine(LoadGameRoutine());
    }

    private IEnumerator LoadGameRoutine()
    {
        isLoading = true;

        if (!saveSystem.Load(out SaveData data))
        {
            ShowMessage("NENHUM SAVE ENCONTRADO!");

            isLoading = false;

            yield break;
        }

        ShowMessage("CARREGANDO...");

        yield return StartCoroutine(
            Fade(0f, 1f)
        );

        LoadPlayer(data);
        LoadObjects(data);

        yield return null;

        yield return StartCoroutine(
            Fade(1f, 0f)
        );

        ShowMessage("JOGO CARREGADO!");

        isLoading = false;
    }

    private void LoadPlayer(SaveData data)
    {
        Vector3 position = new(
            data.playerX,
            data.playerY,
            data.playerZ
        );

        Quaternion rotation = Quaternion.Euler(
            data.playerRotX,
            data.playerRotY,
            data.playerRotZ
        );

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        transform.SetPositionAndRotation(
            position,
            rotation
        );

        if (characterController != null)
        {
            characterController.enabled = true;
        }
    }

    private void LoadObjects(SaveData data)
    {
        Dictionary<string, ObjectSaveData> savedObjects =
            new();

        foreach (ObjectSaveData objectData in data.objects)
        {
            if (!string.IsNullOrEmpty(objectData.id))
            {
                savedObjects[objectData.id] =
                    objectData;
            }
        }

        SaveableObject[] sceneObjects =
            FindObjectsByType<SaveableObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (SaveableObject sceneObject in sceneObjects)
        {
            if (
                savedObjects.TryGetValue(
                    sceneObject.ID,
                    out ObjectSaveData objectData
                )
            )
            {
                sceneObject.ApplyData(objectData);
            }
        }
    }

    private IEnumerator Fade(
        float startAlpha,
        float endAlpha
    )
    {
        if (fadeImage == null)
        {
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / fadeDuration
            );

            progress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            Color color = fadeImage.color;

            color.a = Mathf.Lerp(
                startAlpha,
                endAlpha,
                progress
            );

            fadeImage.color = color;

            yield return null;
        }

        Color finalColor = fadeImage.color;
        finalColor.a = endAlpha;

        fadeImage.color = finalColor;
    }

    private void ShowMessage(string message)
    {
        if (saveMessageText == null)
        {
            return;
        }

        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }

        saveMessageText.gameObject.SetActive(true);
        saveMessageText.text = message;

        messageCoroutine = StartCoroutine(
            HideMessageAfterTime()
        );
    }

    private IEnumerator HideMessageAfterTime()
    {
        yield return new WaitForSecondsRealtime(
            messageDuration
        );

        if (saveMessageText != null)
        {
            saveMessageText.gameObject.SetActive(false);
        }

        messageCoroutine = null;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            SaveGame();
        }
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }
}
