using UnityEngine;
using UnityEngine.InputSystem;

public class ArmToggle : MonoBehaviour
{
    [SerializeField] private GameObject arm;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            arm.SetActive(!arm.activeSelf);
        }
    }
}