using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Espalha mobilia (encostada nas paredes) e baterias extras pela cena Principal.
// Reexecutavel: apaga o que gerou antes (raizes "MobiliaExtra" e "BateriasExtras") e refaz.
// No fim refaz o NavMesh do inimigo para ele contornar os moveis novos.
public static class MapDressing
{
    private const string FurnitureRoot = "MobiliaExtra";
    private const string BatteryRoot = "BateriasExtras";
    private const string BatteryPrefab = "Assets/Prefabs/Battery/Battery (1).prefab";
    private static readonly string[] Furniture =
    {
        "Assets/Comodos/Furniture/Prefabs/MetalTable.prefab",
        "Assets/Comodos/Furniture/Prefabs/chair.prefab",
        "Assets/Comodos/Furniture/Prefabs/SmallCloset.prefab",
        "Assets/Comodos/Furniture/Prefabs/BigCloset.prefab",
        "Assets/Comodos/Furniture/Prefabs/closet2.prefab",
        "Assets/Comodos/Furniture/Prefabs/Closet3.prefab",
        "Assets/Comodos/Furniture/Prefabs/RoundTable.prefab",
        "Assets/Comodos/Furniture/Prefabs/MirrorComode.prefab",
    };

    [MenuItem("Ferramentas/Mapa/Espalhar mobilia e baterias")]
    public static void Run()
    {
        Run(22, 10, System.Environment.TickCount);
    }

    public static void Run(int furnitureCount, int batteryCount, int seed)
    {
        Random.InitState(seed);
        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        if (tri.indices.Length == 0)
        {
            Debug.LogError("MapDressing: abra a cena Principal (sem NavMesh carregado).");
            return;
        }

        Clear(FurnitureRoot);
        Clear(BatteryRoot);
        Transform fRoot = new GameObject(FurnitureRoot).transform;
        Transform bRoot = new GameObject(BatteryRoot).transform;
        Undo.RegisterCreatedObjectUndo(fRoot.gameObject, "Mobilia");
        Undo.RegisterCreatedObjectUndo(bRoot.gameObject, "Baterias");

        Vector3 avoid = new Vector3(99999f, 99999f, 99999f);
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) avoid = player.transform.position;
        List<Vector3> doors = new List<Vector3>();
        foreach (Door d in Object.FindObjectsByType<Door>(FindObjectsSortMode.None)) doors.Add(d.transform.position);

        List<Vector3> used = new List<Vector3>();
        int placed = 0;
        for (int attempt = 0; attempt < furnitureCount * 60 && placed < furnitureCount; attempt++)
        {
            Vector3 p = RandomPoint(tri);
            if (TooClose(p, used, 3f) || TooClose(p, doors, 2.5f) || Vector3.Distance(p, avoid) < 4f) continue;
            if (!FindWall(p, out Vector3 wallNormal, out float wallDist)) continue;
            // Corredor livre na frente (nao entope passagens).
            if (Physics.Raycast(p + Vector3.up * 0.5f, wallNormal, 2.6f, ~0, QueryTriggerInteraction.Ignore)) continue;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Furniture[Random.Range(0, Furniture.Length)]);
            if (prefab == null) continue;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, fRoot);
            go.transform.rotation = Quaternion.LookRotation(wallNormal, Vector3.up);
            go.transform.position = p;
            Bounds b = WorldBounds(go);
            // Encosta na parede.
            float depth = Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(wallNormal.x), 0f, Mathf.Abs(wallNormal.z))));
            go.transform.position = p - wallNormal * Mathf.Max(0f, wallDist - depth - 0.03f);
            go.transform.position += Vector3.up * (p.y - WorldBounds(go).min.y);
            b = WorldBounds(go);
            Vector3 half = b.extents * 0.92f;
            half.y *= 0.9f;
            if (OverlapsOther(go, b, half))
            {
                Object.DestroyImmediate(go);
                continue;
            }
            EnsureCollider(go);
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI |
                                                       StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            used.Add(go.transform.position);
            placed++;
        }

        GameObject battery = AssetDatabase.LoadAssetAtPath<GameObject>(BatteryPrefab);
        int batteries = 0;
        List<Vector3> bUsed = new List<Vector3>();
        foreach (BatteryPickup bp in Object.FindObjectsByType<BatteryPickup>(FindObjectsSortMode.None)) bUsed.Add(bp.transform.position);
        for (int attempt = 0; battery != null && attempt < batteryCount * 80 && batteries < batteryCount; attempt++)
        {
            Vector3 p = RandomPoint(tri);
            if (Vector3.Distance(p, avoid) < 6f) continue;
            Vector3 pos = p + Vector3.up * 0.08f;
            // Metade das vezes em cima de um movel novo.
            if (used.Count > 0 && Random.value < 0.5f)
            {
                Vector3 top = used[Random.Range(0, used.Count)] + Vector3.up * 3f;
                if (Physics.Raycast(top, Vector3.down, out RaycastHit hit, 3.5f, ~0, QueryTriggerInteraction.Ignore) &&
                    hit.point.y > p.y + 0.3f && hit.point.y < p.y + 1.3f)
                    pos = hit.point + Vector3.up * 0.05f;
            }
            if (TooClose(pos, bUsed, 9f)) continue;
            GameObject b = (GameObject)PrefabUtility.InstantiatePrefab(battery, bRoot);
            b.transform.position = pos;
            b.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            bUsed.Add(pos);
            batteries++;
        }

        EditorSceneManager.MarkSceneDirty(fRoot.gameObject.scene);
        Debug.Log("MapDressing: " + placed + " moveis e " + batteries + " baterias. Refazendo NavMesh...");
        EnemyNavigationBuilder.Build();
    }

    private static bool OverlapsOther(GameObject self, Bounds b, Vector3 half)
    {
        foreach (Collider c in Physics.OverlapBox(b.center + Vector3.up * 0.05f, half, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            if (!c.transform.IsChildOf(self.transform)) return true;
        return false;
    }

    private static void EnsureCollider(GameObject go)
    {
        if (go.GetComponentInChildren<Collider>() != null) return;
        Bounds b = WorldBounds(go);
        BoxCollider bc = go.AddComponent<BoxCollider>();
        bc.center = go.transform.InverseTransformPoint(b.center);
        Vector3 s = go.transform.lossyScale;
        Vector3 local = Quaternion.Inverse(go.transform.rotation) * b.size;
        bc.size = new Vector3(Mathf.Abs(local.x / s.x), Mathf.Abs(local.y / s.y), Mathf.Abs(local.z / s.z));
    }

    private static bool FindWall(Vector3 p, out Vector3 normal, out float dist)
    {
        normal = Vector3.zero;
        dist = 0f;
        float best = 1.6f;
        Vector3 origin = p + Vector3.up * 0.6f;
        for (int i = 0; i < 8; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
            if (Physics.Raycast(origin, dir, out RaycastHit h, best, ~0, QueryTriggerInteraction.Ignore) && Mathf.Abs(h.normal.y) < 0.2f)
            {
                best = h.distance;
                normal = new Vector3(h.normal.x, 0f, h.normal.z).normalized;
                dist = h.distance;
            }
        }
        return normal != Vector3.zero;
    }

    private static Vector3 RandomPoint(NavMeshTriangulation t)
    {
        int tri = Random.Range(0, t.indices.Length / 3) * 3;
        Vector3 a = t.vertices[t.indices[tri]], b = t.vertices[t.indices[tri + 1]], c = t.vertices[t.indices[tri + 2]];
        float r1 = Random.value, r2 = Random.value;
        if (r1 + r2 > 1f) { r1 = 1f - r1; r2 = 1f - r2; }
        return a + (b - a) * r1 + (c - a) * r2;
    }

    private static bool TooClose(Vector3 p, List<Vector3> list, float d)
    {
        foreach (Vector3 q in list)
            if ((p - q).sqrMagnitude < d * d) return true;
        return false;
    }

    private static Bounds WorldBounds(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.5f);
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    private static void Clear(string name)
    {
        GameObject old = GameObject.Find(name);
        if (old != null) Undo.DestroyObjectImmediate(old);
    }
}
