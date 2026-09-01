using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class AlbumController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject albumPanel;
    [SerializeField] private Transform photoContainer;
    [SerializeField] private GameObject photoPrefab;

    [Header("Viewer")]
    [SerializeField] private PhotoViewer photoViewer;

    [Header("Input Lock")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PhotoCamera photoCamera;

    [Header("Album Layout")]
    [SerializeField] private int columns = 4;
    [SerializeField] private int rows = 3;

    private readonly List<AlbumPhoto> photos =
        new List<AlbumPhoto>();

    private readonly List<AlbumPhotoSlot> slots =
        new List<AlbumPhotoSlot>();

    private int selectedIndex = -1;
    private int currentPage;

    public IReadOnlyList<AlbumPhoto> Photos =>
        photos;

    public int SelectedIndex =>
        selectedIndex;

    private int PhotosPerPage =>
        columns * rows;

    void Start()
    {
        if (albumPanel != null)
            albumPanel.SetActive(false);

        SetGameInputLocked(false);
    }

    void Update()
    {
        if (Keyboard.current == null)
            return;

        if (
            Mouse.current != null &&
            Mouse.current.rightButton.wasPressedThisFrame &&
            (photoViewer == null ||
             !photoViewer.IsOpen)
        )
        {
            ToggleAlbum();
        }

        if (
            photoViewer != null &&
            photoViewer.IsOpen
        )
        {
            return;
        }

        if (
            albumPanel == null ||
            !albumPanel.activeSelf
        )
        {
            return;
        }

        HandleAlbumNavigation();

        if (
            Keyboard.current.eKey.wasPressedThisFrame
        )
        {
            OpenSelectedPhoto();
        }

        if (
            Keyboard.current.escapeKey.wasPressedThisFrame
        )
        {
            CloseAlbum();
        }
    }

    public void ToggleAlbum()
    {
        if (albumPanel == null)
            return;

        if (albumPanel.activeSelf)
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

        albumPanel.SetActive(true);

        SetGameInputLocked(true);

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
        if (albumPanel != null)
            albumPanel.SetActive(false);

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
            Keyboard.current.rightArrowKey
                .wasPressedThisFrame
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
            Keyboard.current.leftArrowKey
                .wasPressedThisFrame
        )
        {
            int column =
                selectedIndex % columns;

            if (column > 0)
                newIndex--;
        }

        if (
            Keyboard.current.downArrowKey
                .wasPressedThisFrame
        )
        {
            int targetIndex =
                selectedIndex + columns;

            if (targetIndex < photos.Count)
            {
                newIndex = targetIndex;
            }
        }

        if (
            Keyboard.current.upArrowKey
                .wasPressedThisFrame
        )
        {
            int targetIndex =
                selectedIndex - columns;

            if (targetIndex >= 0)
            {
                newIndex = targetIndex;
            }
        }

        if (newIndex != selectedIndex)
        {
            SelectPhoto(newIndex);
        }
    }

    public void AddPhoto(Texture2D texture)
    {
        if (texture == null)
            return;

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
            selectedIndex /
            PhotosPerPage;

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
            selectedIndex /
            PhotosPerPage;

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
            currentPage *
            PhotosPerPage;

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
                slot.Index ==
                selectedIndex
            );
        }
    }

    public void OpenSelectedPhoto()
    {
        if (
            photoViewer == null ||
            !IsValidIndex(selectedIndex)
        )
        {
            return;
        }

        if (albumPanel != null)
            albumPanel.SetActive(false);

        photoViewer.Open(
            photos,
            selectedIndex
        );
    }

    public void ReturnFromViewer()
    {
        if (photoViewer != null)
            photoViewer.CloseWithoutReturn();

        if (photos.Count == 0)
        {
            CloseAlbum();
            return;
        }

        if (selectedIndex < 0)
            selectedIndex = 0;

        if (albumPanel != null)
            albumPanel.SetActive(true);

        SetGameInputLocked(true);

        UpdatePageFromSelection();
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

        if (index < selectedIndex)
        {
            selectedIndex--;
        }

        if (
            selectedIndex >= photos.Count
        )
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
            result.Add(photo.Sprite);
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
            result.Add(photo.Color);
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