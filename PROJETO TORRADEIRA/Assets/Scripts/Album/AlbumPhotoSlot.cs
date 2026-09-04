using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AlbumPhotoSlot :
    MonoBehaviour,
    IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private Image photoImage;
    [SerializeField] private GameObject selectionBorder;

    private AlbumPhoto photo;
    private AlbumController album;
    private int index;

    public int Index => index;

    private void Awake()
    {
        if (selectionBorder != null)
            selectionBorder.SetActive(false);
    }

    public void Initialize(
        AlbumPhoto newPhoto,
        AlbumController newAlbum,
        int newIndex
    )
    {
        photo = newPhoto;
        album = newAlbum;
        index = newIndex;

        if (photoImage == null || photo == null)
            return;

        photoImage.sprite = photo.Sprite;
        photoImage.color = Color.white;

        photoImage.type = Image.Type.Simple;
        photoImage.preserveAspect = false;

        SetSelected(
            album != null &&
            album.SelectedIndex == index
        );
    }

    public void SetSelected(bool selected)
    {
        if (selectionBorder != null)
            selectionBorder.SetActive(selected);
    }

    public void OnPointerClick(
        PointerEventData eventData
    )
    {
        if (album != null)
            album.SelectPhoto(index);
    }
}