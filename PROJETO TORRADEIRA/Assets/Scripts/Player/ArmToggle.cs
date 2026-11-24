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
            radar.SetActive(false);
            radar = RadarDevice.Create(hud).gameObject;
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

        bool showRadar = !photoCamera.IsCameraMode;

        if (radar.activeSelf != showRadar)
            radar.SetActive(showRadar);
    }
}