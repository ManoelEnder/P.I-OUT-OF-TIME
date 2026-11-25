using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Rodada final: baterias extras espalhadas e o ponto de montagem
// da missao final perto do PlayerSpawn. Pode rodar de novo (apaga e recria os grupos).
public static class FinalRoundBuilder
{
    private const string BatteryPrefab = "Assets/Prefabs/Battery/Battery (1).prefab";
    private const string TablePrefab = "Assets/Comodos/Furniture/Prefabs/MetalTable.prefab";
    private const string BatteriesRoot = "Baterias_Extra";
    private const string DeliveryRoot = "PontoDeMontagem";

    private static readonly StringBuilder report = new StringBuilder();
    private static System.Random random;
    private static int solidMask;

    public static void BuildBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Principal.unity", OpenSceneMode.Single);
        report.Clear();
        Build();
        StaticOcclusionCulling.Compute();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        CaptureDelivery();
    }

    [MenuItem("Tools/OUT OF TIME/Rodada final (baterias + ponto de montagem)")]
    public static void Build()
    {
        random = new System.Random(1975);
        solidMask = ~LayerMask.GetMask("Ignore Raycast", "UI", "Arms", "PhotoCameraEffect", "GlobalVolume", "Enemy", "Temporal", "TransparentFX");
        Physics.SyncTransforms();

        FixPieceParticles();
        PlaceBatteries(9);
        PlaceDelivery();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/revisao/qa"));
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "final_round.txt"), report.ToString());
        Debug.Log("FinalRoundBuilder:\n" + report);
    }

    // Os pacotes de moveis vieram com shader Standard (pipeline antigo): no URP ficam rosa.
    // Troca para URP/Lit mantendo textura e cor.
    public static void ConvertFurnitureMaterials()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        int converted = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Comodos" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null || m.shader == null)
                continue;
            string shaderName = m.shader.name;
            if (!(shaderName == "Standard" || shaderName == "Standard (Specular setup)" || shaderName.StartsWith("Legacy") || shaderName.Contains("InternalErrorShader")))
                continue;

            Texture main = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
            Color color = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
            Texture normal = m.HasProperty("_BumpMap") ? m.GetTexture("_BumpMap") : null;

            m.shader = lit;
            if (main != null)
                m.SetTexture("_BaseMap", main);
            m.SetColor("_BaseColor", color);
            if (normal != null)
            {
                m.SetTexture("_BumpMap", normal);
                m.EnableKeyword("_NORMALMAP");
            }
            m.SetFloat("_Smoothness", 0.15f);
            m.SetFloat("_Metallic", 0f);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            converted++;
        }
        AssetDatabase.SaveAssets();
        report.AppendLine("materiais convertidos para URP: " + converted);
    }

    private static List<Vector3> FloorSamples()
    {
        var points = new List<Vector3>();
        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        for (int t = 0; t < tri.indices.Length; t += 3)
        {
            Vector3 a = tri.vertices[tri.indices[t]], b = tri.vertices[tri.indices[t + 1]], c = tri.vertices[tri.indices[t + 2]];
            float area = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
            int samples = Mathf.Clamp(Mathf.RoundToInt(area / 4f), 1, 12);
            for (int i = 0; i < samples; i++)
            {
                float r1 = (float)random.NextDouble(), r2 = (float)random.NextDouble();
                if (r1 + r2 > 1f) { r1 = 1f - r1; r2 = 1f - r2; }
                points.Add(a + (b - a) * r1 + (c - a) * r2);
            }
        }
        return points.OrderBy(_ => random.Next()).ToList();
    }

    // ---------- Particulas das pecas ----------

    // Cada peca ganha a sua particula de pista centrada nela (antes ficavam soltas e longe).
    private static void FixPieceParticles()
    {
        var hints = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(ps => ps.name.StartsWith("TemporalHintParticles")).ToList();
        if (hints.Count == 0)
        {
            report.AppendLine("particulas: nenhuma TemporalHintParticles");
            return;
        }
        ParticleSystem template = hints[0];
        var free = new List<ParticleSystem>(hints);

        foreach (TemporalObjectPickup piece in Object.FindObjectsByType<TemporalObjectPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Renderer[] rs = piece.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
            Bounds b = rs.Length > 0 ? Measure(rs) : new Bounds(piece.transform.position, Vector3.one * 0.3f);

            ParticleSystem ps = free.OrderBy(h => (h.transform.position - b.center).sqrMagnitude).FirstOrDefault();
            float before = ps != null ? Vector3.Distance(ps.transform.position, b.center) : -1f;
            if (ps == null || before > 6f)
            {
                ps = Object.Instantiate(template);
                ps.name = "TemporalHintParticles";
                Undo.RegisterCreatedObjectUndo(ps.gameObject, "Particulas");
            }
            else
            {
                free.Remove(ps);
            }

            Undo.RecordObject(ps.transform, "Particulas");
            ps.transform.SetParent(piece.transform, true);
            ps.transform.position = b.center;
            ps.transform.rotation = Quaternion.identity;

            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = Mathf.Clamp(b.extents.magnitude * 0.6f, 0.08f, 0.5f);
            shape.position = Vector3.zero;
            var vel = ps.velocityOverLifetime;
            vel.enabled = false;
            EditorUtility.SetDirty(ps);

            report.AppendLine("particula da peca " + piece.name + ": estava a " + before.ToString("F2") + " m, agora no centro (raio " + shape.radius.ToString("F2") + ")");
        }

        // Sobras sem peca perto: removidas.
        foreach (ParticleSystem extra in free)
        {
            if (extra.GetComponentInParent<TemporalObjectPickup>() == null)
            {
                report.AppendLine("particula solta removida: " + extra.transform.position.ToString("F1"));
                Undo.DestroyObjectImmediate(extra.gameObject);
            }
        }
    }

    // ---------- Baterias ----------

    private static void PlaceBatteries(int count)
    {
        GameObject old = GameObject.Find(BatteriesRoot);
        if (old != null)
            Undo.DestroyObjectImmediate(old);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BatteryPrefab);
        if (prefab == null)
        {
            report.AppendLine("ERRO: bateria nao encontrada em " + BatteryPrefab);
            return;
        }

        GameObject root = new GameObject(BatteriesRoot);
        Undo.RegisterCreatedObjectUndo(root, "Baterias");

        var taken = new List<Vector3>();
        foreach (BatteryPickup b in Object.FindObjectsByType<BatteryPickup>(FindObjectsSortMode.None))
            taken.Add(b.transform.position);
        Transform spawn = GameObject.Find("PlayerSpawn") != null ? GameObject.Find("PlayerSpawn").transform : null;
        if (spawn != null)
            taken.Add(spawn.position);

        int placed = 0;
        foreach (Vector3 p in FloorSamples())
        {
            if (placed >= count)
                break;
            if (taken.Any(t => (t - p).sqrMagnitude < 13f * 13f))
                continue;

            // Em cima de algum movel perto (mesa, armario baixo) ou no chao encostada na parede.
            Vector3 pos;
            if (!OnSurfaceNear(p, out pos))
            {
                if (!NavMesh.FindClosestEdge(p, out NavMeshHit edge, NavMesh.AllAreas) || edge.distance > 0.7f)
                    continue;
                pos = edge.position - edge.normal * 0.05f;
            }

            GameObject battery = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            battery.transform.position = pos;
            battery.transform.rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);

            // Assenta pela base do modelo.
            Renderer[] renderers = battery.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                foreach (Renderer r in renderers)
                    b.Encapsulate(r.bounds);
                battery.transform.position += Vector3.up * (pos.y - b.min.y + 0.005f);
            }

            // Nada encostado nela (movel, parede).
            Bounds bb = new Bounds(battery.transform.position, Vector3.zero);
            foreach (Renderer r in battery.GetComponentsInChildren<Renderer>()) bb.Encapsulate(r.bounds);
            Collider[] own = battery.GetComponentsInChildren<Collider>();
            bool blocked = Physics.OverlapBox(bb.center + Vector3.up * 0.01f, bb.extents * 0.98f, Quaternion.identity, solidMask, QueryTriggerInteraction.Ignore)
                .Any(c => !own.Contains(c));
            if (blocked)
            {
                Object.DestroyImmediate(battery);
                continue;
            }

            taken.Add(p);
            placed++;
        }

        report.AppendLine("baterias extras: " + placed);
    }

    private static bool OnSurfaceNear(Vector3 p, out Vector3 point)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector3 offset = new Vector3((float)random.NextDouble() * 2f - 1f, 0f, (float)random.NextDouble() * 2f - 1f) * 1.5f;
            if (!Physics.Raycast(p + offset + Vector3.up * 1.6f, Vector3.down, out RaycastHit hit, 1.6f, solidMask, QueryTriggerInteraction.Ignore))
                continue;
            float above = hit.point.y - p.y;
            if (above < 0.55f || above > 1.3f || hit.normal.y < 0.95f)
                continue;
            if (hit.collider.GetComponentInParent<Door>() != null)
                continue;
            // Moveis novos tem itens decorativos sem colisor no tampo: bateria so no chao.
            if (hit.collider.transform.root.name == "Environment" && hit.collider.GetComponentInParent<Transform>().GetComponentsInParent<Transform>().Any(t => t.name == "Mobilia"))
                continue;
            if (Physics.Raycast(hit.point + Vector3.up * 0.03f, Vector3.up, 0.3f, solidMask, QueryTriggerInteraction.Ignore))
                continue;
            point = hit.point;
            return true;
        }
        point = Vector3.zero;
        return false;
    }

    // ---------- Ponto de montagem ----------

    private static void PlaceDelivery()
    {
        GameObject old = GameObject.Find(DeliveryRoot);
        if (old != null)
            Undo.DestroyObjectImmediate(old);

        GameObject spawnGo = GameObject.Find("PlayerSpawn");
        GameObject tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TablePrefab);
        if (spawnGo == null || tablePrefab == null)
        {
            report.AppendLine("ERRO: sem PlayerSpawn ou mesa");
            return;
        }

        Vector3 spawn = spawnGo.transform.position;
        Vector3 bestPos = Vector3.zero, bestNormal = Vector3.forward;
        float bestScore = float.MaxValue;

        // Parede livre a 3-10 m do inicio, no mesmo andar, alcancavel.
        for (int i = 0; i < 400; i++)
        {
            Vector2 c = Random.insideUnitCircle;
            Vector3 probe = spawn + new Vector3(c.x, 0f, c.y) * 10f;
            if (!NavMesh.SamplePosition(probe, out NavMeshHit hit, 2f, NavMesh.AllAreas) || Mathf.Abs(hit.position.y - spawn.y) > 1.2f)
                continue;
            if (!NavMesh.FindClosestEdge(hit.position, out NavMeshHit edge, NavMesh.AllAreas) || edge.distance > 0.8f)
                continue;
            float d = Vector3.Distance(edge.position, spawn);
            if (d < 3f || d > 10f)
                continue;
            Vector3 n = edge.normal;
            if (!Physics.Raycast(edge.position + Vector3.up + n * 0.3f, -n, out RaycastHit wall, 1.4f, solidMask, QueryTriggerInteraction.Ignore) || Mathf.Abs(wall.normal.y) > 0.2f)
                continue;
            n = new Vector3(wall.normal.x, 0f, wall.normal.z).normalized;
            Vector3 center = new Vector3(wall.point.x, edge.position.y, wall.point.z) + n * 0.55f;
            if (Physics.CheckBox(center + Vector3.up * 0.75f, new Vector3(1.0f, 0.6f, 0.5f), Quaternion.LookRotation(n), solidMask, QueryTriggerInteraction.Ignore))
                continue;
            NavMeshPath path = new NavMeshPath();
            if (!NavMesh.CalculatePath(spawn, edge.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                continue;

            float score = Mathf.Abs(d - 5f);
            if (score < bestScore)
            {
                bestScore = score;
                bestPos = new Vector3(wall.point.x, edge.position.y, wall.point.z);
                bestNormal = n;
            }
        }

        if (bestScore == float.MaxValue)
        {
            report.AppendLine("ERRO: nenhuma parede livre perto do inicio");
            return;
        }

        GameObject root = new GameObject(DeliveryRoot);
        Undo.RegisterCreatedObjectUndo(root, "Ponto de montagem");
        Quaternion facing = Quaternion.LookRotation(bestNormal, Vector3.up);

        // Mesa de metal (1,8 m), encostada na parede.
        GameObject table = (GameObject)PrefabUtility.InstantiatePrefab(tablePrefab);
        PrefabUtility.UnpackPrefabInstance(table, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        table.name = "Bancada";
        foreach (Collider c in table.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(c);
        Renderer[] tr = table.GetComponentsInChildren<Renderer>();
        Bounds lb = Measure(tr);
        table.transform.localScale = Vector3.one * (1.8f / Mathf.Max(lb.size.x, lb.size.z));
        table.transform.rotation = Quaternion.LookRotation(Mathf.Abs(Measure(tr).size.x) >= Measure(tr).size.z ? bestNormal : Vector3.Cross(Vector3.up, bestNormal), Vector3.up);
        Bounds tb = Measure(tr);
        float depth = Vector3.Dot(tb.extents, new Vector3(Mathf.Abs(bestNormal.x), 0f, Mathf.Abs(bestNormal.z)));
        Vector3 basePos = bestPos + bestNormal * (depth + 0.03f);
        table.transform.position += basePos - new Vector3(tb.center.x, tb.min.y, tb.center.z);
        tb = Measure(tr);
        table.transform.SetParent(root.transform, true);

        // Bandeja de montagem com 5 encaixes (acendem por peca coletada).
        float top = tb.max.y;
        Vector3 topCenter = new Vector3(tb.center.x, top, tb.center.z);
        Material trayMat = MakeMaterial("Delivery_Tray", new Color(0.1f, 0.1f, 0.11f));
        Material slotMat = MakeMaterial("Delivery_Slot", new Color(0.12f, 0.12f, 0.13f));

        GameObject tray = Cube(root.transform, "Bandeja", topCenter + Vector3.up * 0.02f, facing, new Vector3(1.0f, 0.04f, 0.45f), trayMat);
        var slots = new List<Renderer>();
        Vector3 right = Vector3.Cross(Vector3.up, bestNormal).normalized;
        for (int i = 0; i < 5; i++)
        {
            Vector3 p = topCenter + Vector3.up * 0.07f + right * ((i - 2) * 0.18f);
            slots.Add(Cube(root.transform, "Encaixe_" + (i + 1), p, facing, new Vector3(0.12f, 0.06f, 0.12f), slotMat).GetComponent<Renderer>());
        }

        // Placa na parede.
        GameObject sign = Cube(root.transform, "Placa", bestPos + Vector3.up * 1.6f + bestNormal * 0.02f, facing, new Vector3(0.9f, 0.3f, 0.02f), MakeMaterial("Delivery_Sign", new Color(0.82f, 0.8f, 0.74f)));
        AddLabel(sign.transform, "MONTAGEM  //  PROTOTIPO 1975", bestNormal);

        // Luz fria acima (acende quando da para entregar).
        GameObject lightGo = new GameObject("Luz", typeof(Light));
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.position = topCenter + Vector3.up * 0.9f + bestNormal * 0.2f;
        Light light = lightGo.GetComponent<Light>();
        light.type = LightType.Point;
        light.range = 4.5f;
        light.intensity = 0f;
        light.color = new Color(0.8f, 0.88f, 1f);
        light.shadows = LightShadows.None;

        // Colisor de interacao (a mesa toda) e obstaculo para o inimigo.
        BoxCollider box = root.AddComponent<BoxCollider>();
        Bounds all = tb;
        all.Encapsulate(tray.GetComponent<Renderer>().bounds);
        box.center = root.transform.InverseTransformPoint(all.center);
        box.size = all.size;
        NavMeshObstacle obstacle = root.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.carving = true;
        obstacle.center = box.center;
        obstacle.size = box.size;
        root.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

        DeliveryPoint dp = root.AddComponent<DeliveryPoint>();
        var so = new SerializedObject(dp);
        so.FindProperty("glow").objectReferenceValue = light;
        SerializedProperty sp = so.FindProperty("slots");
        sp.arraySize = slots.Count;
        for (int i = 0; i < slots.Count; i++)
            sp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        report.AppendLine("ponto de montagem: " + bestPos.ToString("F1") + " (" + Vector3.Distance(bestPos, spawn).ToString("F1") + " m do inicio)");
    }

    private static GameObject Cube(Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 size, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.rotation = rotation;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    private static void AddLabel(Transform sign, string text, Vector3 normal)
    {
        GameObject label = new GameObject("Texto", typeof(TextMesh));
        label.transform.SetParent(sign.parent, false);
        label.transform.position = sign.position + normal * 0.015f;
        label.transform.rotation = Quaternion.LookRotation(-normal, Vector3.up);
        TextMesh tm = label.GetComponent<TextMesh>();
        tm.text = text;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = 0.012f;
        tm.fontSize = 64;
        tm.color = new Color(0.12f, 0.11f, 0.1f);
    }

    private static Material MakeMaterial(string name, Color color)
    {
        string folder = "Assets/Art/Materials";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Art", "Materials");
        string path = folder + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", 0.2f);
        EditorUtility.SetDirty(m);
        return m;
    }

    private static Bounds Measure(Renderer[] renderers)
    {
        Bounds b = renderers.Length > 0 ? renderers[0].bounds : new Bounds();
        foreach (Renderer r in renderers)
            b.Encapsulate(r.bounds);
        return b;
    }

    // Foto de conferencia do ponto de montagem.
    public static void CaptureDelivery()
    {
        GameObject root = GameObject.Find(DeliveryRoot);
        if (root == null)
            return;
        DeliveryPoint dp = root.GetComponent<DeliveryPoint>();
        Vector3 n = root.GetComponentInChildren<Light>().transform.position - root.transform.position;
        Bounds b = root.GetComponent<BoxCollider>().bounds;

        GameObject camGo = new GameObject("QA_Cam", typeof(Camera));
        Camera cam = camGo.GetComponent<Camera>();
        Vector3 fwd = root.transform.Find("Placa").forward;
        cam.transform.position = b.center + fwd * 3.2f + Vector3.up * 0.9f;
        cam.transform.LookAt(b.center + Vector3.up * 0.3f);
        cam.fieldOfView = 60f;
        var rt = new RenderTexture(960, 540, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
        RenderTexture.active = null;
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/revisao/qa/props"));
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, "delivery_point.png"), tex.EncodeToPNG());
        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }
}
