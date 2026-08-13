using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AlbumPhotoSlot : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private Image image;

    private AlbumPhoto photo;
    private AlbumController album;
    private int index;

    public void Initialize(
        AlbumPhoto photo,
        AlbumController album,
        int index
    )
    {
        this.photo = photo;
        this.album = album;
        this.index = index;

        if (image == null)
            image = GetComponent<Image>();

        if (image != null && photo != null)
        {
            image.sprite = photo.Sprite;
            image.color = photo.Color;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (album == null)
            return;

        if (eventData.button == PointerEventData.InputButton.Left)
            album.SelectPhoto(index);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (album != null)
            album.SetHoveredPhoto(index);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (album != null)
            album.ClearHoveredPhoto(index);
    }
}