using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AlbumPhotoSlot :
    MonoBehaviour,
    IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private Image image;
    [SerializeField] private Outline selectionOutline;

    private AlbumPhoto photo;
    private AlbumController album;
    private int index;

    public int Index => index;

    public void Initialize(
        AlbumPhoto newPhoto,
        AlbumController newAlbum,
        int newIndex
    )
    {
        photo = newPhoto;
        album = newAlbum;
        index = newIndex;

        if (image == null)
            image = GetComponent<Image>();

        if (selectionOutline == null)
            selectionOutline = GetComponent<Outline>();

        if (image == null || photo == null)
            return;

        image.sprite = photo.Sprite;
        image.color = photo.Color;

        image.type = Image.Type.Simple;
        image.preserveAspect = true;

        SetSelected(
            album != null &&
            album.SelectedIndex == index
        );
    }

    public void SetSelected(bool selected)
    {
        if (selectionOutline != null)
            selectionOutline.enabled = selected;
    }

    public void OnPointerClick(
        PointerEventData eventData
    )
    {
        if (album != null)
            album.SelectPhoto(index);
    }
}