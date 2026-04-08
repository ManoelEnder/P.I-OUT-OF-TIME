using UnityEngine;

[ExecuteInEditMode]
public class CRTPostProcess : MonoBehaviour
{
    public Material material;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (material == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        material.SetTexture("_MainTex", src);
        Graphics.Blit(src, dest, material);
    }
}