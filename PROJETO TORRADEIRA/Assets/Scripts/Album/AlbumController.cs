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

    private readonly List<AlbumPhoto> photos =
        new List<AlbumPhoto>();

    private readonly List<AlbumPhotoSlot> slots =
        new List<AlbumPhotoSlot>();

    private int selectedIndex = -1;
    private int hoveredIndex = -1;

    public IReadOnlyList<AlbumPhoto> Photos => photos;

    public int SelectedIndex => selectedIndex;

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

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            ToggleViewer();
        }

        if (Mouse.current != null &&
            Mouse.current.rightButton.wasPressedThisFrame)
        {
            ToggleAlbum();
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenSelectedPhoto();
        }
    }

    public void ToggleAlbum()
    {
        if (albumPanel == null)
            return;

        bool abrir = !albumPanel.activeSelf;

        albumPanel.SetActive(abrir);

        SetGameInputLocked(abrir);
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
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void ToggleViewer()
    {
        if (photoViewer == null ||
            photos.Count == 0)
        {
            return;
        }

        if (photoViewer.IsOpen)
        {
            photoViewer.Close();
            return;
        }

        photoViewer.Open(
            photos,
            selectedIndex >= 0
                ? selectedIndex
                : 0
        );
    }

    public void AddPhoto(Texture2D texture)
    {
        if (texture == null)
            return;

        AlbumPhoto photo =
            new AlbumPhoto(texture);

        photos.Add(photo);

        CreateSlot(
            photo,
            photos.Count - 1
        );

        selectedIndex =
            photos.Count - 1;
    }

    void CreateSlot(
        AlbumPhoto photo,
        int index
    )
    {
        if (photoPrefab == null ||
            photoContainer == null)
        {
            return;
        }

        GameObject slotObject =
            Instantiate(
                photoPrefab,
                photoContainer,
                false
            );

        AlbumPhotoSlot slot =
            slotObject.GetComponent<AlbumPhotoSlot>();

        if (slot == null)
        {
            slot =
                slotObject.AddComponent<AlbumPhotoSlot>();
        }

        slot.Initialize(
            photo,
            this,
            index
        );

        slots.Add(slot);
    }

    public void SelectPhoto(int index)
    {
        if (!IsValidIndex(index))
            return;

        selectedIndex = index;

        Debug.Log(
            "Foto selecionada: " +
            index
        );
    }

    public void SetHoveredPhoto(int index)
    {
        if (IsValidIndex(index))
            hoveredIndex = index;
    }

    public void ClearHoveredPhoto(int index)
    {
        if (hoveredIndex == index)
            hoveredIndex = -1;
    }

    public void OpenSelectedPhoto()
    {
        if (photoViewer == null ||
            !IsValidIndex(selectedIndex))
        {
            return;
        }

        photoViewer.Open(
            photos,
            selectedIndex
        );
    }

    public void RemovePhoto(int index)
    {
        if (!IsValidIndex(index))
            return;

        AlbumPhoto photo =
            photos[index];

        photos.RemoveAt(index);

        photo.Destroy();

        RebuildSlots();

        if (photos.Count == 0)
        {
            selectedIndex = -1;
            hoveredIndex = -1;
            return;
        }

        if (selectedIndex >= photos.Count)
            selectedIndex = photos.Count - 1;

        if (selectedIndex > index)
            selectedIndex--;

        if (hoveredIndex >= photos.Count)
            hoveredIndex = -1;
        else if (hoveredIndex > index)
            hoveredIndex--;
    }

    void RebuildSlots()
    {
        foreach (AlbumPhotoSlot slot in slots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }

        slots.Clear();

        for (int i = 0; i < photos.Count; i++)
        {
            CreateSlot(
                photos[i],
                i
            );
        }
    }

    public List<Sprite> GetSprites()
    {
        List<Sprite> result =
            new List<Sprite>();

        foreach (AlbumPhoto photo in photos)
        {
            result.Add(photo.Sprite);
        }

        return result;
    }

    public List<Color> GetColors()
    {
        List<Color> result =
            new List<Color>();

        foreach (AlbumPhoto photo in photos)
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
        return index >= 0 &&
               index < photos.Count;
    }
}