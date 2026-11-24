using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Fotografa cada vidro (objetos "Glass*") de frente e de lado para revisar encaixe nas paredes.
//   Unity.exe -batchmode -projectPath <projeto> -executeMethod GlassReview.Capture -quit
// Saida: docs/revisao/qa/glass/*.png e glass.txt (medidas e o quanto cada vidro sai da parede).
public static class GlassReview
{
    // Vidro girado 90 graus fica atravessado no corredor. Para cada vidro com poucas bordas
    // encostadas em parede, testa girar 90 graus e so aplica se ele passar a encaixar melhor.
    [MenuItem("Ferramentas/Visual/Encaixar vidros nas paredes")]
    public static void FixRotated()
    {
        var report = new StringBuilder();

        foreach (MeshRenderer glass in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name.StartsWith("Glass")))
        {
            int before = Touching(glass);
            if (before >= 4)
                continue;

            Vector3 center = glass.bounds.center;
            Undo.RecordObject(glass.transform, "Vidro");
            glass.transform.RotateAround(center, Vector3.up, 90f);
            Physics.SyncTransforms();
            int after = Touching(glass);

            if (after > before)
            {
                report.AppendLine(glass.name + ": girado 90 graus (" + before + "/4 -> " + after + "/4)");
                continue;
            }

            glass.transform.RotateAround(center, Vector3.up, -90f);
            Physics.SyncTransforms();
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("GlassReview: vidros corrigidos:\n" + report);
    }

    public static void FixRotatedBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Principal.unity", OpenSceneMode.Single);
        FixRotated();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    private static int Touching(MeshRenderer glass)
    {
        Bounds b = glass.bounds;
        int touching = 0;
        foreach (Vector3 dir in new[] { glass.transform.right, -glass.transform.right, glass.transform.up, -glass.transform.up })
        {
            Vector3 edge = b.center + Vector3.Scale(dir, b.extents) * 0.98f;
            if (Physics.OverlapSphere(edge, 0.12f).Any(c => c.GetComponent<MeshRenderer>() != glass))
                touching++;
        }
        return touching;
    }

    public static void Capture()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Principal.unity", OpenSceneMode.Single);
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/revisao/qa/glass"));
        Directory.CreateDirectory(folder);

        var report = new StringBuilder();
        var rt = new RenderTexture(640, 400, 24);
        var camGo = new GameObject("GlassReviewCam", typeof(Camera), typeof(Light));
        Camera cam = camGo.GetComponent<Camera>();
        cam.targetTexture = rt;
        cam.fieldOfView = 70f;
        cam.nearClipPlane = 0.05f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.2f, 0.25f, 0.3f);
        Light light = camGo.GetComponent<Light>();
        light.type = LightType.Point;
        light.range = 15f;
        light.intensity = 4f;

        foreach (MeshRenderer glass in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name.StartsWith("Glass")).OrderBy(r => r.name))
        {
            Bounds b = glass.bounds;
            Vector3 normal = glass.transform.forward;

            // Quanto do vidro esta "no ar": testa pontos na borda do vidro procurando parede encostada.
            int touching = 0, samples = 0;
            foreach (Vector3 dir in new[] { glass.transform.right, -glass.transform.right, glass.transform.up, -glass.transform.up })
            {
                Vector3 edge = b.center + Vector3.Scale(dir, b.extents) * 0.98f;
                samples++;
                if (Physics.OverlapSphere(edge, 0.12f).Any(c => c.GetComponent<MeshRenderer>() != glass))
                    touching++;
            }

            report.AppendLine(glass.name + " pos " + glass.transform.position.ToString("F2") + " tam " + b.size.ToString("F2") + " bordas encostadas " + touching + "/" + samples);

            int shot = 0;
            foreach (Vector3 side in new[] { normal, -normal, (normal + glass.transform.right).normalized })
            {
                cam.transform.position = b.center + side * Mathf.Max(2.2f, b.size.magnitude * 1.1f) + Vector3.up * 0.3f;
                cam.transform.LookAt(b.center);
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(folder, glass.name.Replace(" ", "_").Replace("(", "").Replace(")", "") + "_" + shot++ + ".png"), tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                RenderTexture.active = null;
            }
        }

        File.WriteAllText(Path.Combine(folder, "glass.txt"), report.ToString());
        Object.DestroyImmediate(camGo);
        rt.Release();
        Debug.Log("GlassReview:\n" + report);
    }
}
