using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Refaz a navegacao do TemporalEraser na cena Principal:
// - NavMesh gerado pelas COLISOES do mapa inteiro, com voxel fino (paredes finas contam);
// - jogador, pecas, objetos fisicos e gatilhos ficam fora da geracao;
// - portas: fora da geracao + NavMeshObstacle que recorta o caminho quando fechadas;
// - visao e ataque bloqueados por qualquer superficie solida;
// - salva docs/revisao/qa/navmesh.png (vista de cima: chao navegavel e paredes) para conferir.
public static class EnemyNavigationBuilder
{
    private const string SurfaceName = "EnemyNavigationArea";
    private const string NavMeshAssetPath = "Assets/Scenes/Principal/NavMesh-EnemyNavigationArea.asset";

    [MenuItem("Ferramentas/Inimigo/Refazer NavMesh e percepcao")]
    public static void Build()
    {
        TemporalEraserController enemy = Object.FindFirstObjectByType<TemporalEraserController>(FindObjectsInactive.Include);
        if (enemy == null)
        {
            Debug.LogError("EnemyNavigationBuilder: abra a cena Principal.");
            return;
        }

        int solid = SolidMask();

        ExcludeFromBake(enemy);
        ConfigureDoors();

        NavMeshSurface surface = FindOrCreateSurface(enemy);
        Undo.RecordObject(surface, "NavMesh");
        surface.agentTypeID = 0;
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = solid;
        surface.defaultArea = 0;
        surface.ignoreNavMeshAgent = true;
        surface.ignoreNavMeshObstacle = true;
        surface.overrideVoxelSize = true;
        surface.voxelSize = 0.05f;
        surface.overrideTileSize = true;
        surface.tileSize = 128;
        surface.minRegionArea = 1f;
        surface.buildHeightMesh = true;

        surface.BuildNavMesh();
        SaveNavMesh(surface);

        ConfigureEnemy(enemy, solid);

        EditorSceneManager.MarkSceneDirty(enemy.gameObject.scene);
        DrawMap("navmesh_depois.png");
        Debug.Log("EnemyNavigationBuilder: NavMesh refeito e percepcao ajustada.");
    }

    // Camadas solidas: tudo menos o que nao deve bloquear (UI, gatilhos, o inimigo, os bracos...).
    private static int SolidMask()
    {
        int mask = ~0;
        foreach (string layer in new[] { "Ignore Raycast", "UI", "Arms", "PhotoCameraEffect", "GlobalVolume", "Enemy", "Temporal", "TransparentFX" })
        {
            int index = LayerMask.NameToLayer(layer);
            if (index >= 0)
                mask &= ~(1 << index);
        }
        return mask;
    }

    private static NavMeshSurface FindOrCreateSurface(TemporalEraserController enemy)
    {
        NavMeshSurface surface = Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(s => s.name == SurfaceName);

        if (surface != null)
            return surface;

        GameObject go = new GameObject(SurfaceName);
        Undo.RegisterCreatedObjectUndo(go, "NavMesh");
        GameObject environment = GameObject.Find("Environment");
        if (environment != null)
            go.transform.SetParent(environment.transform, false);
        return Undo.AddComponent<NavMeshSurface>(go);
    }

    private static void Ignore(GameObject go, bool children)
    {
        NavMeshModifier modifier = go.GetComponent<NavMeshModifier>();
        if (modifier == null)
            modifier = Undo.AddComponent<NavMeshModifier>(go);
        modifier.ignoreFromBuild = true;
        modifier.applyToChildren = children;
    }

    private static void ExcludeFromBake(TemporalEraserController enemy)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            Ignore(player.transform.root.gameObject, true);

        Ignore(enemy.gameObject, true);

        // Objetos fisicos soltos (caem, sao empurrados) e pecas coletaveis.
        foreach (Rigidbody body in Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!body.isKinematic && body.gameObject.scene.IsValid())
                Ignore(body.gameObject, true);
        }

        // Objetos so com gatilhos (areas de missao, interacao...).
        foreach (Collider collider in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Collider[] all = collider.GetComponents<Collider>();
            if (all.All(c => c.isTrigger))
                Ignore(collider.gameObject, false);
        }
    }

    // Portas giram: nao entram na geracao (o vao fica livre) e recortam o caminho quando paradas.
    private static void ConfigureDoors()
    {
        foreach (Door door in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Ignore(door.gameObject, true);

            Renderer[] renderers = door.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                continue;

            Bounds world = renderers[0].bounds;
            foreach (Renderer r in renderers)
                world.Encapsulate(r.bounds);

            NavMeshObstacle obstacle = door.GetComponent<NavMeshObstacle>();
            if (obstacle == null)
                obstacle = Undo.AddComponent<NavMeshObstacle>(door.gameObject);

            Undo.RecordObject(obstacle, "Porta");
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;

            // Caixa em espaco local da porta.
            Vector3 localCenter = door.transform.InverseTransformPoint(world.center);
            Vector3 size = door.transform.InverseTransformVector(world.size);
            obstacle.center = localCenter;
            obstacle.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        }
    }

    private static void SaveNavMesh(NavMeshSurface surface)
    {
        NavMeshData data = surface.navMeshData;
        if (data == null)
        {
            Debug.LogError("EnemyNavigationBuilder: a geracao nao produziu NavMesh.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshAssetPath) != null)
            AssetDatabase.DeleteAsset(NavMeshAssetPath);

        AssetDatabase.CreateAsset(data, NavMeshAssetPath);
        surface.navMeshData = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavMeshAssetPath);
        surface.RemoveData();
        surface.AddData();
        EditorUtility.SetDirty(surface);
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureEnemy(TemporalEraserController enemy, int solid)
    {
        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        Undo.RecordObject(agent, "Inimigo");
        agent.agentTypeID = 0;
        agent.radius = 0.35f;
        agent.height = 1.95f;
        agent.baseOffset = 0f;
        agent.acceleration = 8f;
        agent.angularSpeed = 300f;
        agent.stoppingDistance = 0.3f;
        agent.autoBraking = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        // Corpo solido do mesmo tamanho do agente: o jogador nao atravessa o inimigo.
        CapsuleCollider body = enemy.GetComponent<CapsuleCollider>();
        if (body != null)
        {
            Undo.RecordObject(body, "Inimigo");
            body.isTrigger = false;
            body.radius = 0.32f;
            body.height = 1.9f;
            body.center = new Vector3(0f, 0.95f, 0f);
        }

        SerializedObject vision = new SerializedObject(enemy.GetComponent<TemporalEraserVision>());
        vision.FindProperty("obstacleMask").intValue = solid;
        vision.ApplyModifiedProperties();

        TemporalEraserAttack attack = enemy.GetComponentInChildren<TemporalEraserAttack>(true);
        if (attack != null)
        {
            SerializedObject so = new SerializedObject(attack);
            so.FindProperty("obstacleMask").intValue = solid;
            so.FindProperty("reach").floatValue = 1.3f;
            so.ApplyModifiedProperties();

            SphereCollider sphere = attack.GetComponent<SphereCollider>();
            if (sphere != null)
            {
                Undo.RecordObject(sphere, "Inimigo");
                sphere.radius = 0.6f;
                sphere.center = new Vector3(0f, 1.1f, 0.45f);
            }
        }

        // Glitch mais raro e sem "teleportar" o modelo (parecia bug).
        TemporalEraserPresence presence = enemy.GetComponent<TemporalEraserPresence>();
        if (presence != null)
        {
            SerializedObject so = new SerializedObject(presence);
            so.FindProperty("glitchInterval").vector2Value = new Vector2(3f, 8f);
            so.FindProperty("jitter").floatValue = 0.015f;
            so.ApplyModifiedProperties();
        }

        // Olhos virados para onde ele anda.
        Transform eye = enemy.transform.Find("EyePoint");
        if (eye != null)
        {
            Undo.RecordObject(eye, "Inimigo");
            eye.localPosition = new Vector3(0f, 1.7f, 0.12f);
            eye.localRotation = Quaternion.identity;
        }
    }

    // ---------- Mapa para conferencia ----------

    [MenuItem("Ferramentas/Inimigo/Salvar mapa do NavMesh")]
    public static void DrawMapMenu()
    {
        DrawMap("navmesh.png");
    }

    public static void DrawMap(string fileName)
    {
        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        if (tri.vertices.Length == 0)
        {
            Debug.LogError("EnemyNavigationBuilder: nenhum NavMesh carregado.");
            return;
        }

        Bounds bounds = new Bounds(tri.vertices[0], Vector3.zero);
        foreach (Vector3 v in tri.vertices)
            bounds.Encapsulate(v);
        bounds.Expand(new Vector3(2f, 0f, 2f));

        const int size = 900;
        float scale = size / Mathf.Max(bounds.size.x, bounds.size.z);
        int w = Mathf.CeilToInt(bounds.size.x * scale), h = Mathf.CeilToInt(bounds.size.z * scale);
        var pixels = new Color32[w * h];
        Color32 background = new Color32(18, 18, 20, 255);
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = background;

        // Paredes e obstaculos: raio de cima para baixo; o que for alto (> 0.6 m acima do chao) e parede.
        float floor = bounds.min.y;
        int solid = SolidMask();
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Vector3 p = new Vector3(bounds.min.x + (x + 0.5f) / scale, bounds.max.y + 3f, bounds.min.z + (y + 0.5f) / scale);
                if (Physics.Raycast(p, Vector3.down, out RaycastHit hit, 30f, solid, QueryTriggerInteraction.Ignore) &&
                    hit.point.y > floor + 0.6f && hit.point.y < floor + 2.6f)
                    pixels[y * w + x] = new Color32(150, 30, 30, 255);
            }
        }

        // Chao navegavel por cima.
        Color32 walk = new Color32(120, 160, 200, 255);
        for (int t = 0; t < tri.indices.Length; t += 3)
            FillTriangle(pixels, w, h, ToPixel(tri.vertices[tri.indices[t]], bounds, scale), ToPixel(tri.vertices[tri.indices[t + 1]], bounds, scale), ToPixel(tri.vertices[tri.indices[t + 2]], bounds, scale), walk);

        // Inimigo (branco) e jogador (vermelho claro).
        TemporalEraserController enemy = Object.FindFirstObjectByType<TemporalEraserController>(FindObjectsInactive.Include);
        if (enemy != null)
            Dot(pixels, w, h, ToPixel(enemy.transform.position, bounds, scale), new Color32(255, 255, 255, 255));
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            Dot(pixels, w, h, ToPixel(player.transform.position, bounds, scale), new Color32(255, 140, 140, 255));

        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/revisao/qa"));
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, fileName), texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        Debug.Log("EnemyNavigationBuilder: mapa salvo em " + folder + "/" + fileName + " (" + tri.indices.Length / 3 + " triangulos).");
    }

    private static Vector2 ToPixel(Vector3 world, Bounds bounds, float scale)
    {
        return new Vector2((world.x - bounds.min.x) * scale, (world.z - bounds.min.z) * scale);
    }

    private static void Dot(Color32[] pixels, int w, int h, Vector2 c, Color32 color)
    {
        for (int y = -4; y <= 4; y++)
            for (int x = -4; x <= 4; x++)
            {
                int px = (int)c.x + x, py = (int)c.y + y;
                if (px >= 0 && py >= 0 && px < w && py < h && x * x + y * y <= 16)
                    pixels[py * w + px] = color;
            }
    }

    private static void FillTriangle(Color32[] pixels, int w, int h, Vector2 a, Vector2 b, Vector2 c, Color32 color)
    {
        int minX = Mathf.Max(0, (int)Mathf.Min(a.x, Mathf.Min(b.x, c.x)));
        int maxX = Mathf.Min(w - 1, (int)Mathf.Max(a.x, Mathf.Max(b.x, c.x)) + 1);
        int minY = Mathf.Max(0, (int)Mathf.Min(a.y, Mathf.Min(b.y, c.y)));
        int maxY = Mathf.Min(h - 1, (int)Mathf.Max(a.y, Mathf.Max(b.y, c.y)) + 1);
        float area = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        if (Mathf.Abs(area) < 1e-5f)
            return;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float w0 = ((b.x - p.x) * (c.y - p.y) - (b.y - p.y) * (c.x - p.x)) / area;
                float w1 = ((c.x - p.x) * (a.y - p.y) - (c.y - p.y) * (a.x - p.x)) / area;
                float w2 = 1f - w0 - w1;
                if (w0 >= 0f && w1 >= 0f && w2 >= 0f)
                {
                    Color32 old = pixels[y * w + x];
                    // Chao por cima de parede vira amarelo: sinal de NavMesh atravessando parede.
                    pixels[y * w + x] = old.r == 150 && old.g == 30 ? new Color32(255, 220, 40, 255) : color;
                }
            }
        }
    }
}
