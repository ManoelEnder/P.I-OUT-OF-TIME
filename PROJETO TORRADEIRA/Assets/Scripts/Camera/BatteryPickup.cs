using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class BatteryPickup : MonoBehaviour
{
    public int amount = 3;
    public float distance = 3f;

    public TextMeshProUGUI interactText;

    bool isLooking = false;

    void Update()
    {
        CheckLook();

        if (isLooking && Keyboard.current.eKey.wasPressedThisFrame)
        {
            PhotoCamera player = FindObjectOfType<PhotoCamera>();

            if (player != null)
            {
                player.AddBattery(amount);
                Destroy(gameObject);
            }
        }
    }

    void CheckLook()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, distance))
        {
            if (hit.collider.gameObject == gameObject)
            {
                isLooking = true;

                if (interactText != null)
                    interactText.gameObject.SetActive(true);

                return;
            }
        }

        isLooking = false;

        if (interactText != null)
            interactText.gameObject.SetActive(false);
    }
}