using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Foto do album como foto instantanea: papel claro, foto recuada com margem larga embaixo,
// legenda "FOTO NN", leve inclinacao e fita no topo. A selecionada fica reta com marcador vermelho.
public class AlbumPhotoSlot :
    MonoBehaviour,
    IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private Image photoImage;
    [SerializeField] private GameObject selectionBorder;

    private static readonly Color Paper = new Color(0.9f, 0.88f, 0.83f, 1f);
    private static readonly Color Ink = new Color(0.16f, 0.15f, 0.14f, 1f);
    private static readonly Color Marker = new Color(0.78f, 0.12f, 0.1f, 1f);

    private AlbumPhoto photo;
    private AlbumController album;
    private int index;
    private float tilt;
    private bool styled;
    private TextMeshProUGUI caption;
    private Image selectionImage;

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

        Style();

        photoImage.sprite = photo.Sprite;
        photoImage.color = Color.white;
        photoImage.type = Image.Type.Simple;
        photoImage.preserveAspect = true;

        if (caption != null)
            caption.text = "FOTO " + (index + 1).ToString("00");

        SetSelected(
            album != null &&
            album.SelectedIndex == index
        );
    }

    private void Style()
    {
        if (styled)
            return;
        styled = true;

        // Papel.
        Image card = GetComponent<Image>();
        if (card != null)
        {
            card.sprite = null;
            card.color = Paper;
        }
        Shadow shadow = gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(5f, -6f);

        // Foto recuada (margem larga embaixo, como foto instantanea), fundo escuro atras.
        RectTransform photoRect = photoImage.rectTransform;
        GameObject backing = new GameObject("PhotoBacking", typeof(RectTransform), typeof(Image));
        backing.transform.SetParent(transform, false);
        backing.transform.SetSiblingIndex(photoRect.GetSiblingIndex());
        Image backingImage = backing.GetComponent<Image>();
        backingImage.color = new Color(0.08f, 0.08f, 0.08f, 1f);
        backingImage.raycastTarget = false;
        Inset(backing.GetComponent<RectTransform>());
        Inset(photoRect);

        // Legenda.
        GameObject label = new GameObject("Caption", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(transform, false);
        RectTransform lr = label.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0.06f, 0f);
        lr.anchorMax = new Vector2(0.94f, 0.2f);
        lr.offsetMin = lr.offsetMax = Vector2.zero;
        caption = label.GetComponent<TextMeshProUGUI>();
        caption.color = Ink;
        caption.fontSize = 20f;
        caption.enableAutoSizing = true;
        caption.fontSizeMin = 10f;
        caption.fontSizeMax = 22f;
        caption.alignment = TextAlignmentOptions.MidlineLeft;
        caption.raycastTarget = false;
        TMP_Text anyText = FindFirstObjectByType<TMP_Text>();
        if (anyText != null && anyText != caption)
            caption.font = anyText.font;

        // Fita no topo.
        GameObject tape = new GameObject("Tape", typeof(RectTransform), typeof(Image));
        tape.transform.SetParent(transform, false);
        RectTransform tr = tape.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 1f);
        tr.sizeDelta = new Vector2(70f, 20f);
        tr.anchoredPosition = new Vector2(0f, 2f);
        tr.localRotation = Quaternion.Euler(0f, 0f, (index % 2 == 0 ? -1f : 1f) * 4f);
        Image tapeImage = tape.GetComponent<Image>();
        tapeImage.color = new Color(0.82f, 0.8f, 0.74f, 0.75f);
        tapeImage.raycastTarget = false;

        // Marcador de selecao: borda vermelha fina.
        if (selectionBorder != null)
        {
            selectionImage = selectionBorder.GetComponent<Image>();
            if (selectionImage != null)
                selectionImage.color = Marker;
        }

        tilt = ((index * 37) % 7 - 3) * 0.9f;
        transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
    }

    private static void Inset(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0.06f, 0.2f);
        rect.anchorMax = new Vector2(0.94f, 0.95f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    public void SetSelected(bool selected)
    {
        if (selectionBorder != null)
            selectionBorder.SetActive(selected);

        // A selecionada endireita e cresce um pouco.
        transform.localRotation = Quaternion.Euler(0f, 0f, selected ? 0f : tilt);
        transform.localScale = Vector3.one * (selected ? 1.04f : 1f);
    }

    public void OnPointerClick(
        PointerEventData eventData
    )
    {
        if (album != null)
            album.SelectPhoto(index);
    }
}
