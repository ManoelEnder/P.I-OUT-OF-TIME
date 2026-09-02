using UnityEngine;

public class MouseCursor : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private Texture2D cursorTexture;

    private void Start()
    {
        Cursor.SetCursor(
            cursorTexture,
            Vector2.zero,
            CursorMode.Auto
        );
    }
}