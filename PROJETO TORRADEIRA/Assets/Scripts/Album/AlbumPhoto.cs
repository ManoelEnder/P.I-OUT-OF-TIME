using UnityEngine;

[System.Serializable]
public class AlbumPhoto
{
    public Texture2D Texture { get; private set; }

    public Sprite Sprite { get; private set; }

    public Color Color { get; private set; }

    public AlbumPhoto(Texture2D texture)
    {
        Texture = texture;

        Sprite = Sprite.Create(
            texture,
            new Rect(
                0,
                0,
                texture.width,
                texture.height
            ),
            new Vector2(0.5f, 0.5f)
        );

        Color = Color.white;
    }

    public void Destroy()
    {
        if (Sprite != null)
        {
            Object.Destroy(Sprite);
            Sprite = null;
        }

        if (Texture != null)
        {
            Object.Destroy(Texture);
            Texture = null;
        }
    }
}