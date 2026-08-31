using UnityEngine;

public class ArmToggle : MonoBehaviour
{
    [SerializeField] private GameObject arm;
    [SerializeField] private PhotoCamera photoCamera;

    private void Start()
    {
        if (photoCamera == null)
            photoCamera = FindFirstObjectByType<PhotoCamera>();

        if (arm != null)
            arm.SetActive(true);
    }

    private void Update()
    {
        if (photoCamera == null ||
            arm == null)
        {
            return;
        }

        arm.SetActive(!photoCamera.IsCameraMode);
    }
}