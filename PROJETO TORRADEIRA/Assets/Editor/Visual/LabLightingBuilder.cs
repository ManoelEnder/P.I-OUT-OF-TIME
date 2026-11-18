using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Iluminacao que guia o jogador na fase (revisao, Parte 2). Reexecutavel.
// - Uma luminaria pequena sobre cada porta: placa emissiva branca fria + luz pontual sem sombra.
// - Pecas temporais ganham a camada Retro/TemporalRim (borda brilhante com linhas de glitch).
public static class LabLightingBuilder
{
    public const string ScenePath = "Assets/Scenes/Principal.unity";
    public const string GuidesGroup = "Guides";
    private const string LightingRoot = "Lighting";
    private const string LampMaterialPath = "Assets/Art/Materials/GuideLamp.mat";
    private const string RimShaderPath = "Assets/Art/Shaders/TemporalRim.shader";
    private const string RimMaterialPath = "Assets/Art/Materials/TemporalRim.mat";

    private static readonly Color Cold = new Color(0.85f, 0.9f, 1f);

    [MenuItem("Ferramentas/Visual/Iluminar laboratorio")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);

        Transform root = FindOrCreate(null, LightingRoot);
        Transform guides = FindOrCreate(root, GuidesGroup);
        for (int i = guides.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(guides.GetChild(i).gameObject);

        Material lamp = LampMaterial();
        foreach (Door door in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            AddGuide(guides, door, lamp);

        Material rim = RimMaterial();
        if (rim != null)
        {
            foreach (TemporalObjectPickup piece in Object.FindObjectsByType<TemporalObjectPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Renderer r = piece.GetComponent<Renderer>();
                if (r == null || r.sharedMaterials.Any(m => m != null && m.shader == rim.shader))
                    continue;

                Undo.RecordObject(r, "Brilho temporal");
                r.sharedMaterials = r.sharedMaterials.Concat(new[] { rim }).ToArray();
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("LIGHTING " + guides.childCount + " luzes guia");
    }

    public static void BuildFromCommandLine()
    {
        Build();
        EditorApplication.Exit(0);
    }

    // Topo da porta (centro dos renderers), onde fica a luminaria.
    public static Vector3 DoorTop(Door door)
    {
        Renderer[] renderers = door.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return door.transform.position + Vector3.up * 2.1f;

        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers)
            b.Encapsulate(r.bounds);

        return new Vector3(b.center.x, b.max.y + 0.15f, b.center.z);
    }

    private static void AddGuide(Transform parent, Door door, Material lampMaterial)
    {
        Vector3 top = DoorTop(door);

        GameObject guide = new GameObject("Guide_" + door.name);
        Undo.RegisterCreatedObjectUndo(guide, "Luz guia");
        guide.transform.SetParent(parent, false);
        guide.transform.SetPositionAndRotation(top, Quaternion.LookRotation(Flat(door.transform.forward), Vector3.up));

        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(plate.GetComponent<Collider>());
        plate.name = "Lamp";
        plate.transform.SetParent(guide.transform, false);
        plate.transform.localScale = new Vector3(0.45f, 0.06f, 0.12f);
        MeshRenderer renderer = plate.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = lampMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;

        Light light = guide.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = Cold;
        light.intensity = 1.6f;
        light.range = 4f;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.Auto;
    }

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude < 0.001f ? Vector3.forward : v.normalized;
    }

    private static Material LampMaterial()
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(LampMaterialPath);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(m, LampMaterialPath);
        }

        // HDR acima de 1: o bloom faz a placa brilhar.
        m.SetColor("_BaseColor", Cold * 2.5f);
        EditorUtility.SetDirty(m);
        return m;
    }

    private static Material RimMaterial()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(RimShaderPath);
        if (shader == null)
        {
            Debug.LogError("LabLightingBuilder: shader nao encontrado em " + RimShaderPath);
            return null;
        }

        Material m = AssetDatabase.LoadAssetAtPath<Material>(RimMaterialPath);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, RimMaterialPath);
        }

        m.shader = shader;
        EditorUtility.SetDirty(m);
        return m;
    }

    private static Transform FindOrCreate(Transform parent, string name)
    {
        Transform found = parent != null
            ? parent.Find(name)
            : EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == name)?.transform;

        if (found != null)
            return found;

        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Iluminacao");
        go.transform.SetParent(parent, false);
        return go.transform;
    }
}
