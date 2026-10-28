using UnityEngine;

public class ArmToggle : MonoBehaviour
{
    [SerializeField] private GameObject radar;
    [SerializeField] private PhotoCamera photoCamera;

    private void Start()
    {
        if (photoCamera == null)
            photoCamera = FindFirstObjectByType<PhotoCamera>();

        if (radar != null)
            radar.SetActive(true);
    }

    private void Update()
    {
        if (photoCamera == null ||
            radar == null)
        {
            return;
        }

        radar.SetActive(!photoCamera.IsCameraMode);
    }
}