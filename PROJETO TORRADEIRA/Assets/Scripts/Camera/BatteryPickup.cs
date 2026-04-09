using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class BatteryPickup : MonoBehaviour
{
    public int amount = 3;
    public float distance = 3f;

    public static BatteryPickup currentTarget;

    public TextMeshProUGUI interactText;

    void Update()
    {
        CheckLook();

        if (currentTarget == this && Keyboard.current.eKey.wasPressedThisFrame)
        {
            PhotoCamera player = FindObjectOfType<PhotoCamera>();

            if (player != null)
            {
                player.AddBattery(amount);
                interactText.gameObject.SetActive(false);
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
                if (currentTarget != this)
                {
                    currentTarget = this;

                    if (interactText != null)
                    {
                        interactText.text = "[E] Pegar bateria";
                        interactText.gameObject.SetActive(true);
                    }
                }

                return;
            }
        }

        if (currentTarget == this)
        {
            currentTarget = null;

            if (interactText != null)
                interactText.gameObject.SetActive(false);
        }
    }
}