using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PhotoViewer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject viewerPanel;
    [SerializeField] private Image largeImage;
    [SerializeField] private GameObject deletedMessage;
    [SerializeField] private GameObject confirmationPanel;

    [Header("Album")]
    [SerializeField] private AlbumController album;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoom = 0.5f;
    [SerializeField] private float maxZoom = 3f;

    [Header("Delete")]
    [SerializeField]
    private float deletedMessageDuration = 2f;

    private IReadOnlyList<AlbumPhoto> photos;

    private int currentIndex;
    private float zoom = 1f;

    private bool confirmingDelete;

    public bool IsOpen =>
        viewerPanel != null &&
        viewerPanel.activeSelf;

    void Start()
    {
        CloseWithoutReturn();

        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);

        if (deletedMessage != null)
            deletedMessage.SetActive(false);
    }

    void Update()
    {
        if (!IsOpen)
            return;

        if (Keyboard.current == null)
            return;

        if (confirmingDelete)
        {
            HandleDeleteConfirmation();
            return;
        }

        HandleNavigation();
        HandleZoom();
        HandleDelete();
    }

    public void Open(
        IReadOnlyList<AlbumPhoto> photoList,
        int index
    )
    {
        if (
            photoList == null ||
            photoList.Count == 0
        )
        {
            return;
        }

        photos = photoList;

        currentIndex =
            Mathf.Clamp(
                index,
                0,
                photos.Count - 1
            );

        zoom = 1f;

        if (viewerPanel != null)
            viewerPanel.SetActive(true);

        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);

        confirmingDelete = false;

        ResetZoom();
        ShowCurrentPhoto();
    }

    void HandleNavigation()
    {
        if (
            Keyboard.current.rightArrowKey
                .wasPressedThisFrame ||
            Keyboard.current.dKey
                .wasPressedThisFrame
        )
        {
            Next();
        }

        if (
            Keyboard.current.leftArrowKey
                .wasPressedThisFrame ||
            Keyboard.current.aKey
                .wasPressedThisFrame
        )
        {
            Previous();
        }

        if (
            Keyboard.current.escapeKey
                .wasPressedThisFrame
        )
        {
            ReturnToAlbum();
        }
    }

    void HandleZoom()
    {
        if (Mouse.current == null)
            return;

        float scroll =
            Mouse.current.scroll
                .ReadValue()
                .y;

        if (Mathf.Approximately(scroll, 0f))
            return;

        zoom +=
            scroll *
            zoomSpeed *
            0.01f;

        zoom =
            Mathf.Clamp(
                zoom,
                minZoom,
                maxZoom
            );

        ApplyZoom();
    }

    void HandleDelete()
    {
        if (
            Keyboard.current.deleteKey
                .wasPressedThisFrame
        )
        {
            StartDeleteConfirmation();
        }
    }

    void HandleDeleteConfirmation()
    {
        if (
            Keyboard.current.yKey
                .wasPressedThisFrame
        )
        {
            ConfirmDelete();
            return;
        }

        if (
            Keyboard.current.nKey
                .wasPressedThisFrame ||
            Keyboard.current.escapeKey
                .wasPressedThisFrame
        )
        {
            CancelDelete();
        }
    }

    public void Next()
    {
        if (!HasPhotos())
            return;

        currentIndex++;

        if (currentIndex >= photos.Count)
            currentIndex = 0;

        ResetZoom();
        ShowCurrentPhoto();
    }

    public void Previous()
    {
        if (!HasPhotos())
            return;

        currentIndex--;

        if (currentIndex < 0)
            currentIndex =
                photos.Count - 1;

        ResetZoom();
        ShowCurrentPhoto();
    }

    void ShowCurrentPhoto()
    {
        if (
            !HasPhotos() ||
            largeImage == null
        )
        {
            return;
        }

        AlbumPhoto photo =
            photos[currentIndex];

        largeImage.sprite =
            photo.Sprite;

        largeImage.color =
            photo.Color;

        if (album != null)
            album.SelectPhoto(currentIndex);
    }

    void StartDeleteConfirmation()
    {
        if (!HasPhotos())
            return;

        confirmingDelete = true;

        if (confirmationPanel != null)
            confirmationPanel.SetActive(true);
    }

    void ConfirmDelete()
    {
        if (!HasPhotos())
            return;

        int indexToRemove =
            currentIndex;

        confirmingDelete = false;

        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);

        if (album != null)
        {
            album.RemovePhoto(
                indexToRemove
            );

            photos =
                album.Photos;
        }

        if (!HasPhotos())
        {
            ReturnToAlbum();
            return;
        }

        if (
            currentIndex >=
            photos.Count
        )
        {
            currentIndex =
                photos.Count - 1;
        }

        ResetZoom();
        ShowCurrentPhoto();

        StartCoroutine(
            ShowDeletedMessage()
        );
    }

    void CancelDelete()
    {
        confirmingDelete = false;

        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }

    IEnumerator ShowDeletedMessage()
    {
        if (deletedMessage == null)
            yield break;

        deletedMessage.SetActive(true);

        yield return new WaitForSeconds(
            deletedMessageDuration
        );

        deletedMessage.SetActive(false);
    }

    void ReturnToAlbum()
    {
        if (album != null)
        {
            album.ReturnFromViewer();
        }
        else
        {
            CloseWithoutReturn();
        }
    }

    public void CloseWithoutReturn()
    {
        if (viewerPanel != null)
            viewerPanel.SetActive(false);

        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);

        confirmingDelete = false;
    }

    void ResetZoom()
    {
        zoom = 1f;

        ApplyZoom();
    }

    void ApplyZoom()
    {
        if (largeImage == null)
            return;

        largeImage.rectTransform.localScale =
            Vector3.one * zoom;
    }

    bool HasPhotos()
    {
        return
            photos != null &&
            photos.Count > 0;
    }
}