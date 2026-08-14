using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AlbumPhotoSlot : MonoBehaviour, IPointerClickHandler
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

        if (image == null)
            return;

        image.sprite = photo.Sprite;
        image.color = photo.Color;

        image.type = Image.Type.Simple;
        image.preserveAspect = true;

        RectTransform rect = image.rectTransform;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (album != null)
            album.SelectPhoto(index);
    }
}