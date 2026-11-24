using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

// Espalha os props do PSX Mega Pack pela fase Principal, com otimizacao:
// - caixas encostadas em paredes/cantos (BoxCollider + NavMeshObstacle);
// - tapetes e tijolos no chao, longe de portas, SEM colisor (itens de chao);
// - relogios e tomadas presos nas paredes; cadernos, disquetes, lanternas e cadeados em mesas;
// - tudo estatico (batching + occludee), LODGroup (sombra so de perto, some de longe),
//   materiais com GPU instancing e texturas em filtro de ponto;
// - no fim, refaz o occlusion culling da cena.
// Pode rodar de novo: apaga o grupo "Props_PSX" e recria (semente fixa, mesmo resultado).
public static class PropScatterBuilder
{
    private const string Folder = "Assets/Art/Props/PSXMegaPack";
    private const string RootName = "Props_PSX";
    private const int Seed = 1975;

    private class Prop
    {
        public string Model;
        public string Texture;
        public float Size;        // maior dimensao desejada (m)
        public float CullHeight;  // fracao da tela em que some
        public string Path;       // caminho completo (modelos de outro pacote); vazio = PSX Mega Pack
        public bool KeepMaterials;
        public GameObject Source;
        public Material Material;
    }

    private static readonly Dictionary<string, Prop> Props = new Dictionary<string, Prop>
    {
        { "box", new Prop { Model = "cardboard_box_1", Texture = "cardboard_box_1", Size = 0.55f, CullHeight = 0.012f } },
        { "brick", new Prop { Model = "brick", Texture = "brick_1", Size = 0.22f, CullHeight = 0.02f } },
        { "carpet", new Prop { Model = "carpet_mp_1", Texture = "carpet_mp_1", Size = 2.2f, CullHeight = 0.01f } },
        { "clock", new Prop { Model = "clock_2", Texture = "clock_2", Size = 0.34f, CullHeight = 0.015f } },
        { "outlet", new Prop { Model = "electrical_outlet_1", Texture = "electrical_outlet_1", Size = 0.12f, CullHeight = 0.02f } },
        { "flashlight", new Prop { Model = "flashlight_1", Texture = "flashlight_1", Size = 0.24f, CullHeight = 0.02f } },
        { "floppy", new Prop { Model = "floppy_disc_1", Texture = "floppy_disc_1", Size = 0.09f, CullHeight = 0.025f } },
        { "notebook", new Prop { Model = "notebook_1", Texture = "notebook_1", Size = 0.26f, CullHeight = 0.02f } },
        { "padlock", new Prop { Model = "padlock_1", Texture = "padlock_1", Size = 0.07f, CullHeight = 0.025f } },
        // Moveis de escritorio (PSX Nostalgia), com os materiais originais.
        { "desk", new Prop { Path = "Assets/ThirdParty/PSXNostalgia-FBX/PSXNostalgia-FBX/ComputerSet/Desk.fbx", Size = 1.5f, CullHeight = 0.01f, KeepMaterials = true } },
        { "chair", new Prop { Path = "Assets/ThirdParty/PSXNostalgia-FBX/PSXNostalgia-FBX/ComputerSet/Chair.fbx", Size = 1.0f, CullHeight = 0.01f, KeepMaterials = true } },
        { "pcscreen", new Prop { Path = "Assets/ThirdParty/PSXNostalgia-FBX/PSXNostalgia-FBX/ComputerSet/PcScreen.fbx", Size = 0.45f, CullHeight = 0.015f, KeepMaterials = true } },
        { "pctower", new Prop { Path = "Assets/ThirdParty/PSXNostalgia-FBX/PSXNostalgia-FBX/ComputerSet/PcTower.fbx", Size = 0.45f, CullHeight = 0.015f, KeepMaterials = true } },
    };

    private static System.Random random;
    private static int solidMask;
    private static readonly List<Vector3> used = new List<Vector3>();
    private static readonly List<Vector3> doors = new List<Vector3>();
    private static readonly StringBuilder report = new StringBuilder();

    [MenuItem("Ferramentas/Visual/Espalhar props PSX")]
    public static void Build()
    {
        Build(true);
    }

    public static void BuildBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Principal.unity", OpenSceneMode.Single);
        Build(true);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        CaptureShots();
    }

    // Fotos de conferencia: um exemplo de cada tipo, de perto (docs/revisao/qa/props/).
    public static void CaptureShots()
    {
        GameObject rootGo = GameObject.Find(RootName);
        if (rootGo == null)
            return;

        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/revisao/qa/props"));
        Directory.CreateDirectory(folder);

        var rt = new RenderTexture(640, 400, 24);
        var camGo = new GameObject("PropReviewCam", typeof(Camera), typeof(Light));
        Camera cam = camGo.GetComponent<Camera>();
        cam.targetTexture = rt;
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.03f;
        Light light = camGo.GetComponent<Light>();
        light.type = LightType.Point;
        light.range = 8f;
        light.intensity = 3f;

        var seen = new HashSet<string>();
        int shots = 0;
        foreach (Transform t in rootGo.transform)
        {
            if (t.name.EndsWith("_LOD1"))
                continue;
            if (!seen.Add(t.name) && !(t.name == "box" && shots < 12 && seen.Count > 8))
                continue;

            Renderer[] rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0)
                continue;
            Bounds b = Measure(rs);
            float dist = Mathf.Clamp(b.size.magnitude * 3f, 0.6f, 3.5f);
            Vector3 dir = (t.forward + Vector3.up * 0.6f).normalized;
            // Itens de parede: a camera fica do lado da sala (onde da para andar).
            if ((t.name == "clock" || t.name == "outlet") &&
                NavMesh.SamplePosition(b.center, out NavMeshHit room, 3f, NavMesh.AllAreas))
            {
                Vector3 flat = room.position - b.center;
                flat.y = 0f;
                if (flat.sqrMagnitude > 0.0001f)
                    dir = flat.normalized;
            }

            cam.transform.position = b.center + dir * dist + Vector3.up * 0.3f;
            cam.transform.LookAt(b.center);
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(folder, t.name + "_" + shots + ".png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            RenderTexture.active = null;
            shots++;
        }

        Object.DestroyImmediate(camGo);
        rt.Release();
    }

    public static void Build(bool bakeOcclusion)
    {
        random = new System.Random(Seed);
        used.Clear();
        doors.Clear();
        report.Clear();
        solidMask = ~LayerMask.GetMask("Ignore Raycast", "UI", "Arms", "PhotoCameraEffect", "GlobalVolume", "Enemy", "Temporal", "TransparentFX");

        if (!LoadProps())
            return;

        GameObject old = GameObject.Find(RootName);
        if (old != null)
            Undo.DestroyObjectImmediate(old);

        GameObject rootGo = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(rootGo, "Props");
        GameObject environment = GameObject.Find("Environment");
        if (environment != null)
            rootGo.transform.SetParent(environment.transform, false);
        Transform root = rootGo.transform;

        foreach (Door door in Object.FindObjectsByType<Door>(FindObjectsSortMode.None))
            doors.Add(door.transform.position);

        Physics.SyncTransforms();

        List<Vector3> floor = FloorSamples();
        report.AppendLine("pontos de chao: " + floor.Count);

        PlaceOffices(root, floor, 6);
        PlaceWallBoxes(root, floor, 22);
        PlaceFloorItems(root, floor, "carpet", 6, 2.0f, false);
        PlaceFloorItems(root, floor, "brick", 10, 0.6f, true);
        PlaceWallMounted(root, floor, "clock", 8, 1.9f, 2.4f);
        PlaceWallMounted(root, floor, "outlet", 14, 0.28f, 0.32f);
        PlaceOnTables(root, floor, new[] { "notebook", "floppy", "flashlight", "padlock" }, 22);

        if (bakeOcclusion)
            BakeOcclusion();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/revisao/qa"));
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "props.txt"), report.ToString());
        Debug.Log("PropScatterBuilder:\n" + report);
    }

    // ---------- Assets ----------

    private static bool LoadProps()
    {
        foreach (var pair in Props)
        {
            Prop prop = pair.Value;
            string modelPath = !string.IsNullOrEmpty(prop.Path) ? prop.Path : Folder + "/Models/" + prop.Model + ".glb";
            prop.Source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (prop.Source == null)
            {
                Debug.LogError("PropScatterBuilder: nao achou " + modelPath);
                return false;
            }

            if (prop.KeepMaterials)
                continue;

            string texPath = Folder + "/Textures/" + prop.Texture + ".png";
            TextureImporter importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (importer != null && (importer.filterMode != FilterMode.Point || importer.maxTextureSize > 256))
            {
                importer.filterMode = FilterMode.Point;
                importer.maxTextureSize = 256;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }

            prop.Material = PropMaterial(prop.Texture, AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        }

        return true;
    }

    private static Material PropMaterial(string name, Texture2D texture)
    {
        string folder = Folder + "/Materials";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder(Folder, "Materials");

        string path = folder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Smoothness", 0.1f);
        material.SetFloat("_Metallic", 0f);
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    // ---------- Pontos do mapa ----------

    // Centros dos triangulos do NavMesh (onde da para andar), subdivididos para cobrir salas grandes.
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

        // Ordem aleatoria estavel.
        return points.OrderBy(_ => random.Next()).ToList();
    }

    private static bool FarFrom(Vector3 p, float minUsed, float minDoor)
    {
        foreach (Vector3 u in used)
            if ((u - p).sqrMagnitude < minUsed * minUsed)
                return false;
        foreach (Vector3 d in doors)
            if (Vector3.Distance(new Vector3(d.x, p.y, d.z), p) < minDoor)
                return false;
        return true;
    }

    // ---------- Colocacao ----------

    private static void PlaceWallBoxes(Transform root, List<Vector3> floor, int count)
    {
        int placed = 0;
        foreach (Vector3 p in floor)
        {
            if (placed >= count)
                break;

            // Perto de parede (borda do NavMesh), mas nao em passagem estreita.
            if (!NavMesh.FindClosestEdge(p, out NavMeshHit edge, NavMesh.AllAreas) || edge.distance > 0.9f)
                continue;
            if (!FarFrom(edge.position, 3.5f, 2.2f) || Narrow(edge.position))
                continue;

            Vector3 normal = edge.normal.sqrMagnitude > 0.01f ? edge.normal : Vector3.forward;
            Vector3 basePos = edge.position - normal * 0.05f;
            float yaw = Mathf.Atan2(normal.x, normal.z) * Mathf.Rad2Deg + (float)(random.NextDouble() * 20 - 10);

            int stack = random.NextDouble() < 0.35 ? 2 : 1;
            float y = 0f;
            for (int s = 0; s < stack; s++)
            {
                GameObject box = Spawn(root, "box", basePos + Vector3.up * y, Quaternion.Euler(0f, yaw + s * 12f, 0f), 1f - s * 0.12f, out Bounds b);
                AddBoxCollider(box, b);
                AddObstacle(box, b);
                y += b.size.y;
            }

            // Vez ou outra uma caixa menor do lado.
            if (random.NextDouble() < 0.4)
            {
                Vector3 side = Vector3.Cross(Vector3.up, normal).normalized * 0.62f;
                GameObject extra = Spawn(root, "box", basePos + side, Quaternion.Euler(0f, yaw - 20f, 0f), 0.75f, out Bounds eb);
                AddBoxCollider(extra, eb);
                AddObstacle(extra, eb);
            }

            used.Add(edge.position);
            placed++;
        }

        report.AppendLine("caixas: " + placed);
    }

    // Posto de trabalho (mesa + cadeira + monitor + gabinete) encostado numa parede livre.
    private static void PlaceOffices(Transform root, List<Vector3> floor, int count)
    {
        int placed = 0;
        foreach (Vector3 p in floor)
        {
            if (placed >= count)
                break;

            if (!NavMesh.FindClosestEdge(p, out NavMeshHit edge, NavMesh.AllAreas) || edge.distance > 0.8f)
                continue;
            if (!FarFrom(edge.position, 8f, 3f) || Narrow(edge.position))
                continue;

            Vector3 normal = edge.normal.sqrMagnitude > 0.01f ? edge.normal : Vector3.forward;
            // Parede de verdade atras (raio na altura da mesa).
            if (!Physics.Raycast(edge.position + Vector3.up * 0.9f + normal * 0.3f, -normal, 1.2f, solidMask, QueryTriggerInteraction.Ignore))
                continue;

            Vector3 deskPos = edge.position + normal * 0.45f;
            Quaternion facing = Quaternion.LookRotation(normal, Vector3.up);

            // Espaco vazio para mesa e cadeira (nada entre 0,2 e 1,3 m de altura).
            Vector3 boxCenter = deskPos + normal * 0.4f + Vector3.up * 0.75f;
            if (Physics.CheckBox(boxCenter, new Vector3(0.9f, 0.5f, 0.85f), facing, solidMask, QueryTriggerInteraction.Ignore))
                continue;

            GameObject desk = Spawn(root, "desk", deskPos, facing, 1f, out Bounds db);
            AddBoxCollider(desk, db);
            AddObstacle(desk, db);

            Vector3 chairPos = deskPos + normal * 0.85f + Vector3.Cross(Vector3.up, normal) * 0.1f;
            GameObject chair = Spawn(root, "chair", chairPos, Quaternion.LookRotation(-normal, Vector3.up) * Quaternion.Euler(0f, (float)(random.NextDouble() * 30 - 15), 0f), 1f, out Bounds cb);
            AddBoxCollider(chair, cb);
            AddObstacle(chair, cb);

            // Em cima da mesa.
            float top = db.max.y;
            Vector3 right = Vector3.Cross(Vector3.up, normal).normalized;
            Spawn(root, "pcscreen", new Vector3(deskPos.x, top, deskPos.z) - normal * 0.05f, Quaternion.LookRotation(normal, Vector3.up), 1f, out _);
            Spawn(root, "pctower", new Vector3(deskPos.x, top, deskPos.z) + right * 0.5f - normal * 0.05f, Quaternion.LookRotation(normal, Vector3.up) * Quaternion.Euler(0f, 180f, 0f), 1f, out _);

            used.Add(edge.position);
            placed++;
        }

        report.AppendLine("postos de trabalho: " + placed);
    }

    private static void PlaceFloorItems(Transform root, List<Vector3> floor, string key, int count, float clearance, bool nearWall)
    {
        int placed = 0;
        foreach (Vector3 p in floor)
        {
            if (placed >= count)
                break;

            NavMesh.FindClosestEdge(p, out NavMeshHit edge, NavMesh.AllAreas);
            if (nearWall ? edge.distance > 0.8f : edge.distance < clearance)
                continue;

            Vector3 pos = nearWall ? edge.position - edge.normal * 0.1f : p;
            if (!FarFrom(pos, key == "carpet" ? 6f : 3f, 2.5f))
                continue;

            Quaternion rot = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
            Spawn(root, key, pos + Vector3.up * 0.005f, rot, key == "brick" ? 0.85f + (float)random.NextDouble() * 0.3f : 1f, out _);

            // Tijolos aparecem em pequenos montes.
            if (key == "brick" && random.NextDouble() < 0.6)
                Spawn(root, key, pos + rot * new Vector3(0.18f, 0.005f, 0.05f), rot * Quaternion.Euler(0f, 70f, 0f), 0.9f, out _);

            used.Add(pos);
            placed++;
        }

        report.AppendLine(key + ": " + placed);
    }

    private static void PlaceWallMounted(Transform root, List<Vector3> floor, string key, int count, float minHeight, float maxHeight)
    {
        int placed = 0;
        foreach (Vector3 p in floor)
        {
            if (placed >= count)
                break;

            if (!NavMesh.FindClosestEdge(p, out NavMeshHit edge, NavMesh.AllAreas) || edge.distance > 0.6f)
                continue;

            float height = Mathf.Lerp(minHeight, maxHeight, (float)random.NextDouble());
            Vector3 from = edge.position + Vector3.up * height + edge.normal * 0.4f;
            if (!Physics.Raycast(from, -edge.normal, out RaycastHit hit, 1.6f, solidMask, QueryTriggerInteraction.Ignore))
                continue;
            if (Mathf.Abs(hit.normal.y) > 0.2f || !FarFrom(hit.point, key == "clock" ? 7f : 3f, 1.4f))
                continue;

            // Parede de verdade: o ponto acima tambem e parede (nao borda de movel).
            if (!Physics.Raycast(from + Vector3.up * 0.5f, -edge.normal, 1.8f, solidMask, QueryTriggerInteraction.Ignore))
                continue;

            GameObject item = Spawn(root, key, hit.point, Quaternion.identity, 1f, out Bounds b);
            // O relogio tem a frente do lado oposto ao eixo fino (a textura mostra o verso).
            AlignToWall(item, hit.point, hit.normal, key == "clock");
            used.Add(hit.point);
            placed++;
        }

        report.AppendLine(key + ": " + placed);
    }

    private static void PlaceOnTables(Transform root, List<Vector3> floor, string[] keys, int count)
    {
        int placed = 0;
        foreach (Vector3 p in floor)
        {
            if (placed >= count)
                break;

            // Procura um tampo de mesa (superficie horizontal entre 0,6 e 1,2 m) perto do ponto.
            for (int tries = 0; tries < 6 && placed < count; tries++)
            {
                Vector3 offset = new Vector3((float)random.NextDouble() * 2f - 1f, 0f, (float)random.NextDouble() * 2f - 1f) * 1.4f;
                Vector3 from = p + offset + Vector3.up * 1.6f;
                if (!Physics.Raycast(from, Vector3.down, out RaycastHit hit, 1.6f, solidMask, QueryTriggerInteraction.Ignore))
                    continue;

                float above = hit.point.y - p.y;
                if (above < 0.6f || above > 1.25f || hit.normal.y < 0.95f)
                    continue;
                if (hit.collider.GetComponentInParent<Door>() != null || !FarFrom(hit.point, 1.6f, 0f))
                    continue;

                // Precisa de espaco livre em cima (nao embaixo de prateleira) e tampo largo o bastante.
                if (Physics.Raycast(hit.point + Vector3.up * 0.05f, Vector3.up, 0.35f, solidMask, QueryTriggerInteraction.Ignore))
                    continue;
                if (!Flat(hit.point, 0.12f, above + p.y))
                    continue;

                // Tampo aberto: nada encostado dos lados (exclui fundo de caixote, gaveta, prateleira funda).
                bool walled = false;
                foreach (Vector3 dir in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                {
                    if (Physics.Raycast(hit.point + Vector3.up * 0.12f, dir, 0.4f, solidMask, QueryTriggerInteraction.Ignore))
                    {
                        walled = true;
                        break;
                    }
                }
                if (walled)
                    continue;

                string key = keys[random.Next(keys.Length)];
                Spawn(root, key, hit.point + Vector3.up * 0.002f, Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f), 1f, out _);
                used.Add(hit.point);
                placed++;
                break;
            }
        }

        report.AppendLine("itens em mesas: " + placed);
    }

    private static bool Flat(Vector3 point, float radius, float y)
    {
        foreach (Vector3 dir in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
        {
            if (!Physics.Raycast(point + dir * radius + Vector3.up * 0.3f, Vector3.down, out RaycastHit h, 0.4f, solidMask, QueryTriggerInteraction.Ignore))
                return false;
            if (Mathf.Abs(h.point.y - y) > 0.02f)
                return false;
        }
        return true;
    }

    private static bool Narrow(Vector3 point)
    {
        // Corredor estreito: parede dos dois lados perto (caixa atrapalharia a passagem).
        int close = 0;
        for (int i = 0; i < 8; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
            if (Physics.Raycast(point + Vector3.up * 0.6f, dir, 1.6f, solidMask, QueryTriggerInteraction.Ignore))
                close++;
        }
        return close >= 5;
    }

    // ---------- Instancia + otimizacao ----------

    private static GameObject Spawn(Transform root, string key, Vector3 position, Quaternion rotation, float scale, out Bounds worldBounds)
    {
        Prop prop = Props[key];
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prop.Source);
        if (go == null)
            go = Object.Instantiate(prop.Source);
        Undo.RegisterCreatedObjectUndo(go, "Props");
        go.name = key;
        go.transform.SetParent(root, true);

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);

        // Tamanho: maior dimensao = Size.
        go.transform.localScale = Vector3.one;
        go.transform.rotation = Quaternion.identity;
        go.transform.position = Vector3.zero;
        Bounds local = Measure(renderers);
        float factor = prop.Size * scale / Mathf.Max(0.0001f, Mathf.Max(local.size.x, Mathf.Max(local.size.y, local.size.z)));
        go.transform.localScale = Vector3.one * factor;

        // Pivo no fundo do objeto (assenta no chao/mesa).
        Bounds scaled = Measure(renderers);
        go.transform.rotation = rotation;
        go.transform.position = position + rotation * new Vector3(-scaled.center.x, -scaled.min.y, -scaled.center.z);

        foreach (Renderer r in renderers)
        {
            if (prop.Material != null)
                r.sharedMaterials = Enumerable.Repeat(prop.Material, r.sharedMaterials.Length).ToArray();
            r.receiveShadows = true;
        }

        foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(c);

        MakeStatic(go);
        AddLod(go, renderers, prop.CullHeight);

        worldBounds = Measure(renderers);
        return go;
    }

    private static Bounds Measure(Renderer[] renderers)
    {
        Bounds b = renderers.Length > 0 ? renderers[0].bounds : new Bounds();
        foreach (Renderer r in renderers)
            b.Encapsulate(r.bounds);
        return b;
    }

    private static void MakeStatic(GameObject go)
    {
        var flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic;
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
    }

    // LOD0: com sombra. LOD1: sem sombra. Depois some (CullHeight).
    private static void AddLod(GameObject go, Renderer[] renderers, float cullHeight)
    {
        LODGroup group = go.AddComponent<LODGroup>();

        var near = new List<Renderer>();
        var far = new List<Renderer>();
        foreach (Renderer r in renderers)
        {
            near.Add(r);

            // Copia so de malha e material, sem sombra, presa na malha original (LOD1).
            MeshFilter filter = r.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                continue;

            GameObject copy = new GameObject(r.gameObject.name + "_LOD1", typeof(MeshFilter), typeof(MeshRenderer));
            copy.transform.SetParent(r.transform, false);
            copy.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            MeshRenderer rc = copy.GetComponent<MeshRenderer>();
            rc.sharedMaterials = r.sharedMaterials;
            rc.shadowCastingMode = ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(copy, GameObjectUtility.GetStaticEditorFlags(r.gameObject));
            far.Add(rc);
            r.shadowCastingMode = ShadowCastingMode.On;
        }

        group.SetLODs(new[]
        {
            new LOD(Mathf.Max(cullHeight * 4f, 0.08f), near.ToArray()),
            new LOD(cullHeight, far.ToArray()),
        });
        group.fadeMode = LODFadeMode.None;
        group.RecalculateBounds();
    }

    private static void AddBoxCollider(GameObject go, Bounds world)
    {
        BoxCollider box = go.AddComponent<BoxCollider>();
        box.center = go.transform.InverseTransformPoint(world.center);
        Vector3 size = go.transform.InverseTransformVector(world.size);
        box.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
    }

    private static void AddObstacle(GameObject go, Bounds world)
    {
        NavMeshObstacle obstacle = go.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.carving = true;
        obstacle.carveOnlyStationary = true;
        obstacle.center = go.transform.InverseTransformPoint(world.center);
        Vector3 size = go.transform.InverseTransformVector(world.size);
        obstacle.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));

        NavMeshModifier mod = go.AddComponent<NavMeshModifier>();
        mod.ignoreFromBuild = true;
    }

    // A face mais fina do objeto vira para fora da parede, encostada nela.
    private static void AlignToWall(GameObject go, Vector3 point, Vector3 normal, bool flip = false)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        go.transform.rotation = Quaternion.identity;
        Bounds b = Measure(renderers);
        Vector3 s = b.size;

        // Eixo mais fino (local) -> normal da parede.
        Vector3 thin = s.x <= s.y && s.x <= s.z ? Vector3.right : (s.y <= s.z ? Vector3.up : Vector3.forward);
        Quaternion rot = Quaternion.FromToRotation(flip ? -thin : thin, normal);
        // Mantem o "em pe" do objeto alinhado ao mundo quando possivel.
        Vector3 up = rot * (thin == Vector3.up ? Vector3.forward : Vector3.up);
        if (Vector3.Dot(up, normal) > 0.99f)
            up = rot * Vector3.forward;
        rot = Quaternion.AngleAxis(Vector3.SignedAngle(up, Vector3.up, normal), normal) * rot;
        go.transform.rotation = rot;

        Bounds rb = Measure(renderers);
        float half = Vector3.Dot(rb.extents, new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z)));
        go.transform.position += point - rb.center + normal * (half + 0.005f);
    }

    // ---------- Occlusion culling ----------

    private static void BakeOcclusion()
    {
        // Mapa grande e estatico como oclusor; props pequenos so como ocluidos.
        StaticOcclusionCulling.smallestOccluder = 1.5f;
        StaticOcclusionCulling.smallestHole = 0.25f;
        StaticOcclusionCulling.backfaceThreshold = 100f;

        bool ok = StaticOcclusionCulling.Compute();
        report.AppendLine("occlusion culling: " + (ok ? "refeito" : "falhou"));
    }
}
