using UnityEngine;

public class animacao : MonoBehaviour
{
    [Header("Configurações da Câmera")]
    public Transform mainCamera;
    public Transform insideBarnPosition;
    public float transitionSpeed = 1.5f;

    [Header("Telas de Interface (UI)")]
    public GameObject mainMenuCanvas;

    private bool isMoving = false;

    void Start()
    {
        if (mainMenuCanvas != null)
            mainMenuCanvas.SetActive(false);

        isMoving = true;
    }

    void Update()
    {
        if (isMoving)
        {
            mainCamera.position = Vector3.Lerp(
                mainCamera.position,
                insideBarnPosition.position,
                transitionSpeed * Time.deltaTime
            );

            mainCamera.rotation = Quaternion.Lerp(
                mainCamera.rotation,
                insideBarnPosition.rotation,
                transitionSpeed * Time.deltaTime
            );

            if (Vector3.Distance(mainCamera.position, insideBarnPosition.position) < 0.1f)
            {
                isMoving = false;

                if (mainMenuCanvas != null)
                    mainMenuCanvas.SetActive(true);
            }
        }
    }
}