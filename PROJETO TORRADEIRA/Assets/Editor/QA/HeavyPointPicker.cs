using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Escolhe os 3 pontos mais pesados da cena Principal (geometria e luzes dentro do campo de visao,
// a partir de pontos andaveis do NavMesh) e grava em Scripts/Diagnostics/PerfPoints.cs.
// Os mesmos pontos servem para o benchmark e para as capturas antes/depois.
public static class HeavyPointPicker
{
    private const string ScenePath = "Assets/Scenes/Principal.unity";
    private const string OutputPath = "Assets/Scripts/Diagnostics/PerfPoints.cs";
    private const float EyeHeight = 1.6f;
    private const float GridSize = 4f;
    private const float MinSpacing = 10f;

    [MenuItem("Ferramentas/QA/Escolher pontos pesados da fase")]
    public static void Pick()
    {
        EditorSceneManager.OpenScene(ScenePath);

        Camera reference = Camera.main;
        float far = reference != null ? reference.farClipPlane : 100f;

        var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
            .Where(r => r.enabled && r.gameObject.activeInHierarchy)
            .Select(r => new { r.bounds, Tris = Triangles(r) })
            .ToList();
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
            .Where(l => l.enabled && l.gameObject.activeInHierarchy && l.type != LightType.Directional)
            .ToList();

        var samples = new Dictionary<Vector3Int, Vector3>();
        foreach (Vector3 v in NavMesh.CalculateTriangulation().vertices)
        {
            var cell = Vector3Int.RoundToInt(v / GridSize);
            if (!samples.ContainsKey(cell))
                samples[cell] = v;
        }

        GameObject probe = new GameObject("HeavyProbe");
        Camera cam = probe.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cam.aspect = 16f / 9f;
        cam.farClipPlane = far;

        var scored = new List<(PerfPoints.Point point, float score)>();

        foreach (Vector3 ground in samples.Values)
        {
            for (int yaw = 0; yaw < 360; yaw += 90)
            {
                probe.transform.SetPositionAndRotation(ground + Vector3.up * EyeHeight, Quaternion.Euler(0f, yaw, 0f));
                Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);

                float tris = renderers.Where(r => GeometryUtility.TestPlanesAABB(planes, r.bounds)).Sum(r => (float)r.Tris);
                int nearLights = lights.Count(l => Vector3.Distance(l.transform.position, ground) < l.range + far * 0.25f &&
                                                   GeometryUtility.TestPlanesAABB(planes, new Bounds(l.transform.position, Vector3.one * l.range)));
                int shadowed = lights.Count(l => l.shadows != LightShadows.None &&
                                                 GeometryUtility.TestPlanesAABB(planes, new Bounds(l.transform.position, Vector3.one * l.range)));

                scored.Add((new PerfPoints.Point("", ground, yaw), tris / 1000f + nearLights * 10f + shadowed * 50f));
            }
        }

        Object.DestroyImmediate(probe);

        var chosen = new List<PerfPoints.Point>();
        foreach (var s in scored.OrderByDescending(s => s.score))
        {
            if (chosen.Any(c => Vector3.Distance(c.Position, s.point.Position) < MinSpacing))
                continue;

            chosen.Add(new PerfPoints.Point("p" + (chosen.Count + 1), s.point.Position, s.point.Yaw));
            Debug.Log("HEAVY p" + chosen.Count + " " + s.point.Position + " yaw " + s.point.Yaw + " score " + s.score.ToString("0"));

            if (chosen.Count == 3)
                break;
        }

        Write(chosen);
        AssetDatabase.ImportAsset(OutputPath);
    }

    public static void PickFromCommandLine()
    {
        Pick();
        EditorApplication.Exit(0);
    }

    private static int Triangles(MeshRenderer renderer)
    {
        MeshFilter filter = renderer.GetComponent<MeshFilter>();
        Mesh mesh = filter != null ? filter.sharedMesh : null;
        if (mesh == null)
            return 0;

        long count = 0;
        for (int i = 0; i < mesh.subMeshCount; i++)
            count += mesh.GetIndexCount(i);
        return (int)(count / 3);
    }

    private static void Write(List<PerfPoints.Point> points)
    {
        string source = File.ReadAllText(OutputPath);
        int start = source.IndexOf("Heavy =");
        int open = source.IndexOf('{', start);
        int close = source.IndexOf("};", open);

        var sb = new StringBuilder("\n");
        foreach (PerfPoints.Point p in points)
            sb.AppendLine("        new Point(\"" + p.Name + "\", new Vector3(" + F(p.Position.x) + "f, " + F(p.Position.y) + "f, " + F(p.Position.z) + "f), " + F(p.Yaw) + "f),");
        sb.Append("    ");

        File.WriteAllText(OutputPath, source.Substring(0, open + 1) + sb + source.Substring(close));
    }

    private static string F(float v)
    {
        return v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }
}
