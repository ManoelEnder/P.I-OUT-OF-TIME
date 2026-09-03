using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(GraphicRaycaster))]
public class CanvasEventCamera : MonoBehaviour
{
    [SerializeField] private Camera eventCamera;

    private Canvas canvas;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();

        if (eventCamera != null)
        {
            canvas.worldCamera = eventCamera;
        }
    }
}