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
        {
            // O radar redondo antigo da lugar ao aparelho portatil (mapa + varredura),
            // sempre no canvas principal do HUD.
            GameObject hudCanvas = GameObject.Find("HUDCanvas");
            Transform hud = hudCanvas != null ? hudCanvas.transform : radar.transform.parent;
            RadarDevice device = RadarDevice.Create(hud);
            if (radar != device.gameObject)
                radar.SetActive(false);
            radar = device.gameObject;
            radar.SetActive(true);
        }
    }

    private void Update()
    {
        if (photoCamera == null ||
            radar == null)
        {
            return;
        }

        bool showRadar = !photoCamera.IsCameraMode && !photoCamera.IsTransitioning;

        if (radar.activeSelf != showRadar)
            radar.SetActive(showRadar);
    }
}