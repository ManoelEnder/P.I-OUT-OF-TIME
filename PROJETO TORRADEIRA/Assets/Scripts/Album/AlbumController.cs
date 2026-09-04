using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class AlbumController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject albumPanel;
    [SerializeField] private Transform photoContainer;
    [SerializeField] private GameObject photoPrefab;
    [SerializeField] private GameObject crosshair;

    [Header("Delete Confirmation")]
    [SerializeField] private GameObject deleteConfirmationPanel;
    [SerializeField] private TMP_Text deleteConfirmationText;
    [SerializeField] private float deletedMessageDuration = 2f;

    [Header("Input")]
    [SerializeField] private Key albumKey = Key.F;

    [Header("Input Lock")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PhotoCamera photoCamera;

    [Header("Other Scripts")]
    [SerializeField] private MonoBehaviour pauseMenuScript;

    [Header("Album Settings")]
    [SerializeField] private int maxPhotos = 6;

    [Header("Album Layout")]
    [SerializeField] private int columns = 2;
    [SerializeField] private int rows = 3;

    private readonly List<AlbumPhoto> photos =
        new List<AlbumPhoto>();

    private readonly List<AlbumPhotoSlot> slots =
        new List<AlbumPhotoSlot>();

    private int selectedIndex = -1;
    private int currentPage;

    private bool confirmingDelete;
    private Coroutine deletedMessageCoroutine;

    public IReadOnlyList<AlbumPhoto> Photos =>
        photos;

    public int SelectedIndex =>
        selectedIndex;

    public bool IsAlbumOpen =>
        albumPanel != null &&
        albumPanel.activeSelf;

    private int PhotosPerPage =>
        columns * rows;

    void Start()
    {
        if (albumPanel != null)
            albumPanel.SetActive(false);

        if (deleteConfirmationPanel != null)
            deleteConfirmationPanel.SetActive(false);

        Time.timeScale = 1f;

        SetCrosshairVisible(true);
        SetGameInputLocked(false);
    }

    void Update()
    {
        if (Keyboard.current == null)
            return;

        if (
            IsAlbumOpen &&
            confirmingDelete
        )
        {
            HandleDeleteConfirmation();
            return;
        }

        if (
            IsAlbumOpen &&
            Keyboard.current.escapeKey.wasPressedThisFrame
        )
        {
            CloseAlbum();
            return;
        }

        if (
            Keyboard.current[albumKey].wasPressedThisFrame
        )
        {
            ToggleAlbum();
            return;
        }

        if (!IsAlbumOpen)
            return;

        HandleAlbumNavigation();

        if (
            Keyboard.current.deleteKey.wasPressedThisFrame
        )
        {
            StartDeleteConfirmation();
        }
    }

    public void ToggleAlbum()
    {
        if (IsAlbumOpen)
        {
            CloseAlbum();
        }
        else
        {
            OpenAlbum();
        }
    }

    public void OpenAlbum()
    {
        if (albumPanel == null)
            return;

        Time.timeScale = 0f;

        albumPanel.SetActive(true);

        if (pauseMenuScript != null)
            pauseMenuScript.enabled = false;

        SetCrosshairVisible(false);
        SetGameInputLocked(true);

        confirmingDelete = false;

        if (deleteConfirmationPanel != null)
            deleteConfirmationPanel.SetActive(false);

        if (
            selectedIndex < 0 &&
            photos.Count > 0
        )
        {
            selectedIndex = 0;
        }

        UpdatePageFromSelection();
    }

    public void CloseAlbum()
    {
        Time.timeScale = 1f;

        if (albumPanel != null)
            albumPanel.SetActive(false);

        if (deleteConfirmationPanel != null)
            deleteConfirmationPanel.SetActive(false);

        confirmingDelete = false;

        if (deletedMessageCoroutine != null)
        {
            StopCoroutine(
                deletedMessageCoroutine
            );

            deletedMessageCoroutine = null;
        }

        if (pauseMenuScript != null)
            pauseMenuScript.enabled = true;

        SetCrosshairVisible(true);
        SetGameInputLocked(false);
    }

    void HandleAlbumNavigation()
    {
        if (photos.Count == 0)
            return;

        if (selectedIndex < 0)
        {
            SelectPhoto(0);
            return;
        }

        int newIndex = selectedIndex;

        if (
            Keyboard.current.rightArrowKey.wasPressedThisFrame
        )
        {
            int column =
                selectedIndex % columns;

            if (
                column < columns - 1 &&
                selectedIndex + 1 < photos.Count
            )
            {
                newIndex++;
            }
        }

        if (
            Keyboard.current.leftArrowKey.wasPressedThisFrame
        )
        {
            int column =
                selectedIndex % columns;

            if (column > 0)
                newIndex--;
        }

        if (
            Keyboard.current.downArrowKey.wasPressedThisFrame
        )
        {
            int targetIndex =
                selectedIndex + columns;

            if (targetIndex < photos.Count)
                newIndex = targetIndex;
        }

        if (
            Keyboard.current.upArrowKey.wasPressedThisFrame
        )
        {
            int targetIndex =
                selectedIndex - columns;

            if (targetIndex >= 0)
                newIndex = targetIndex;
        }

        if (newIndex != selectedIndex)
            SelectPhoto(newIndex);
    }

    void StartDeleteConfirmation()
    {
        if (!IsValidIndex(selectedIndex))
            return;

        if (deletedMessageCoroutine != null)
        {
            StopCoroutine(
                deletedMessageCoroutine
            );

            deletedMessageCoroutine = null;
        }

        confirmingDelete = true;

        if (deleteConfirmationPanel != null)
            deleteConfirmationPanel.SetActive(true);

        if (deleteConfirmationText != null)
        {
            deleteConfirmationText.text =
                "Deseja apagar a foto?\n\n[Y] SIM     [N] NÃO";
        }
    }

    void HandleDeleteConfirmation()
    {
        if (
            Keyboard.current.yKey.wasPressedThisFrame
        )
        {
            ConfirmDelete();
            return;
        }

        if (
            Keyboard.current.nKey.wasPressedThisFrame ||
            Keyboard.current.escapeKey.wasPressedThisFrame
        )
        {
            CancelDelete();
        }
    }

    void ConfirmDelete()
    {
        int indexToRemove =
            selectedIndex;

        confirmingDelete = false;

        RemovePhoto(indexToRemove);

        ShowPhotoDeletedMessage();
    }

    void CancelDelete()
    {
        confirmingDelete = false;

        if (deleteConfirmationPanel != null)
            deleteConfirmationPanel.SetActive(false);
    }

    void ShowPhotoDeletedMessage()
    {
        if (
            deleteConfirmationPanel == null ||
            deleteConfirmationText == null
        )
        {
            return;
        }

        if (deletedMessageCoroutine != null)
        {
            StopCoroutine(
                deletedMessageCoroutine
            );
        }

        deleteConfirmationPanel.SetActive(true);

        deleteConfirmationText.text =
            "FOTO APAGADA";

        deletedMessageCoroutine =
            StartCoroutine(
                HideDeletedMessage()
            );
    }

    IEnumerator HideDeletedMessage()
    {
        yield return new WaitForSecondsRealtime(
            deletedMessageDuration
        );

        if (deleteConfirmationPanel != null)
            deleteConfirmationPanel.SetActive(false);

        deletedMessageCoroutine = null;
    }

    public void AddPhoto(Texture2D texture)
    {
        if (texture == null)
            return;

        if (photos.Count >= maxPhotos)
        {
            Object.Destroy(texture);
            return;
        }

        AlbumPhoto photo =
            new AlbumPhoto(texture);

        photos.Add(photo);

        selectedIndex =
            photos.Count - 1;

        UpdatePageFromSelection();
    }

    public void SelectPhoto(int index)
    {
        if (!IsValidIndex(index))
            return;

        selectedIndex = index;

        int previousPage =
            currentPage;

        currentPage =
            selectedIndex / PhotosPerPage;

        if (previousPage != currentPage)
        {
            RebuildCurrentPage();
        }
        else
        {
            UpdateSelectionVisual();
        }
    }

    void UpdatePageFromSelection()
    {
        if (photos.Count == 0)
        {
            currentPage = 0;

            ClearSlots();

            return;
        }

        if (selectedIndex < 0)
            selectedIndex = 0;

        currentPage =
            selectedIndex / PhotosPerPage;

        RebuildCurrentPage();
    }

    void RebuildCurrentPage()
    {
        ClearSlots();

        if (
            photoPrefab == null ||
            photoContainer == null
        )
        {
            return;
        }

        int startIndex =
            currentPage * PhotosPerPage;

        int endIndex =
            Mathf.Min(
                startIndex + PhotosPerPage,
                photos.Count
            );

        for (
            int i = startIndex;
            i < endIndex;
            i++
        )
        {
            CreateSlot(
                photos[i],
                i
            );
        }

        UpdateSelectionVisual();
    }

    void CreateSlot(
        AlbumPhoto photo,
        int index
    )
    {
        GameObject slotObject =
            Instantiate(
                photoPrefab,
                photoContainer,
                false
            );

        AlbumPhotoSlot slot =
            slotObject.GetComponent<
                AlbumPhotoSlot
            >();

        if (slot == null)
        {
            slot =
                slotObject.AddComponent<
                    AlbumPhotoSlot
                >();
        }

        slot.Initialize(
            photo,
            this,
            index
        );

        slots.Add(slot);
    }

    void ClearSlots()
    {
        foreach (
            AlbumPhotoSlot slot
            in slots
        )
        {
            if (slot != null)
            {
                Destroy(
                    slot.gameObject
                );
            }
        }

        slots.Clear();
    }

    void UpdateSelectionVisual()
    {
        foreach (
            AlbumPhotoSlot slot
            in slots
        )
        {
            if (slot == null)
                continue;

            slot.SetSelected(
                slot.Index == selectedIndex
            );
        }
    }

    public void RemovePhoto(int index)
    {
        if (!IsValidIndex(index))
            return;

        AlbumPhoto photo =
            photos[index];

        photos.RemoveAt(index);

        photo.Destroy();

        if (photos.Count == 0)
        {
            selectedIndex = -1;
            currentPage = 0;

            ClearSlots();

            return;
        }

        if (selectedIndex >= photos.Count)
        {
            selectedIndex =
                photos.Count - 1;
        }

        UpdatePageFromSelection();
    }

    public List<Sprite> GetSprites()
    {
        List<Sprite> result =
            new List<Sprite>();

        foreach (
            AlbumPhoto photo
            in photos
        )
        {
            result.Add(
                photo.Sprite
            );
        }

        return result;
    }

    public List<Color> GetColors()
    {
        List<Color> result =
            new List<Color>();

        foreach (
            AlbumPhoto photo
            in photos
        )
        {
            result.Add(
                photo.Color
            );
        }

        return result;
    }

    public bool HasPhotos()
    {
        return photos.Count > 0;
    }

    bool IsValidIndex(int index)
    {
        return
            index >= 0 &&
            index < photos.Count;
    }

    void SetCrosshairVisible(bool visible)
    {
        if (crosshair != null)
            crosshair.SetActive(visible);
    }

    void SetGameInputLocked(bool locked)
    {
        if (playerMovement != null)
            playerMovement.enabled = !locked;

        if (photoCamera != null)
            photoCamera.enabled = !locked;

        if (locked)
        {
            Cursor.visible = true;

            Cursor.lockState =
                CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;

            Cursor.lockState =
                CursorLockMode.Locked;
        }
    }
}