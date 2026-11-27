using Unity.Cinemachine;
using UnityEngine;

// Copia o FOV que os scripts do jogo escrevem na Camera para a lente da CinemachineCamera,
// antes do Brain aplicar (ordem de execucao bem cedo no LateUpdate).
[DefaultExecutionOrder(-10000)]
public class FovBridge : MonoBehaviour
{
    [SerializeField, HideInInspector] private Camera cam;
    [SerializeField, HideInInspector] private CinemachineCamera vcam;
    [SerializeField, HideInInspector] private RunCameraRig rig;
    [SerializeField, HideInInspector] private float lastApplied = -1f;
    [SerializeField, HideInInspector] private float requested;

    public void Init(Camera c, CinemachineCamera v, RunCameraRig r, float startFov)
    {
        InitValues(c, v, r, startFov);
    }

    private void InitValues(Camera c, CinemachineCamera v, RunCameraRig r, float startFov)
    {
        cam = c;
        vcam = v;
        rig = r;
        requested = startFov;
        lastApplied = startFov;
    }

    private void LateUpdate()
    {
        if (cam == null || vcam == null)
            return;
        // Se alguem mudou o FOV da camera desde o ultimo quadro, esse e o novo pedido.
        if (Mathf.Abs(cam.fieldOfView - lastApplied) > 0.001f && cam.fieldOfView > 1f)
            requested = cam.fieldOfView;
        float fov = rig != null ? rig.RunFov(requested) : requested;
        LensSettings lens = vcam.Lens;
        lens.FieldOfView = fov;
        lens.NearClipPlane = cam.nearClipPlane;
        vcam.Lens = lens;
        lastApplied = fov;
    }
}
