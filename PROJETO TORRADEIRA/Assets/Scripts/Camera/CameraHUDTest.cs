using UnityEngine;
using UnityEngine.InputSystem;

public class CameraHUDTest : MonoBehaviour
{
    [SerializeField] private GameObject cameraHUD;

    void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.cKey.wasPressedThisFrame)
        {
            cameraHUD.SetActive(!cameraHUD.activeSelf);
        }
    }
}