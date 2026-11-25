using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.AI.Navigation;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Mobilia sala por sala (laboratorios e escritorios da Kodak, 1975).
// Cada sala tem uma funcao e um arranjo proprio; cada peca passa por checagens:
// - encostada numa parede de verdade (3 alturas batem no mesmo plano) ou em ilha com corredor livre;
// - sem sobrepor nada (OverlapBox no volume real), com chao embaixo dos 4 cantos, frente livre;
// - longe de portas, pecas da missao, spawn e inimigo; o caminho pela sala continua aberto;
// - colisor do tamanho da malha, NavMeshObstacle, LODGroup com malhas reduzidas (LOD1/LOD2) e
//   flags de occlusion (grandes = ocultadores); no fim o occlusion culling e refeito.
// Itens de mesa ficam no tampo real (raycast na malha), sem encostar uns nos outros.
public static class RoomFurnisher
{
    private const string RootName = "Mobilia";
    private const string LodFolder = "Assets/Art/Props/LOD";

    // A fase foi montada ~1,4x maior que o real (porta 2,94 m, mesas da equipe 1,04 m, olhos do
    // jogador a 1,83 m). Todos os moveis seguem essa escala para combinar com o jogador.
    public const float WorldScale = 1.38f;

    public enum Kind { Lab, Office, Director, Archive, Storage, Break, Waiting, Corridor }

    public class RoomDef
    {
        public string Id;
        public Kind Kind;
        public float Y;
        public Rect Area; // x,z mundo
        public RoomDef(string id, Kind kind, float y, float x0, float x1, float z0, float z1)
        {
            Id = id; Kind = kind; Y = y;
            Area = Rect.MinMaxRect(Mathf.Min(x0, x1), Mathf.Min(z0, z1), Mathf.Max(x0, x1), Mathf.Max(z0, z1));
        }
    }

    // Salas desenhadas sobre a planta (docs/revisao/qa/salas/detalhe_y*.png).
    public static readonly List<RoomDef> Rooms = new List<RoomDef>
    {
        // Terreo (y = 0)
        new RoomDef("T1_laboratorio", Kind.Lab, 0f, -45.9f, -22.0f, -165.0f, -144.6f),
        new RoomDef("T2_escritorio", Kind.Office, 0f, -20.4f, -7.0f, -166.0f, -143.4f),
        new RoomDef("T3_diretoria", Kind.Director, 0f, -4.3f, 3.1f, -149.2f, -144.3f),
        new RoomDef("T4_arquivo", Kind.Archive, 0f, -4.3f, 3.1f, -155.4f, -150.5f),
        new RoomDef("T5_deposito", Kind.Storage, 0f, -4.3f, 3.1f, -161.0f, -156.5f),
        new RoomDef("T6_copa", Kind.Break, 0f, -18.7f, -14.1f, -179.1f, -171.4f),
        new RoomDef("T7_corredor_norte", Kind.Corridor, 0f, -23.3f, -10.9f, -142.9f, -139.3f),
        new RoomDef("T8_corredor_sul", Kind.Corridor, 0f, -42.4f, -14.0f, -170.1f, -166.5f),
        new RoomDef("T9_corredor_leste", Kind.Corridor, 0f, -10.5f, -6.6f, -139.0f, -130.0f),
        // Mezanino (y = 3)
        new RoomDef("M1_arquivo", Kind.Archive, 3f, -43.4f, -33.4f, -143.0f, -129.0f),
        new RoomDef("M2_arquivo_corredor", Kind.Corridor, 3f, -32.7f, -20.0f, -134.2f, -129.6f),
        // Andar de cima (y = 12)
        new RoomDef("S1_diretoria", Kind.Director, 12f, -26.2f, -20.3f, -118.2f, -109.4f),
        new RoomDef("S2_deposito", Kind.Storage, 12f, -28.4f, -24.4f, -142.2f, -138.8f),
        new RoomDef("S3_copa", Kind.Break, 12f, -44.3f, -37.8f, -190.1f, -183.0f),
        new RoomDef("S4_espera", Kind.Waiting, 12f, -29.9f, -3.2f, -194.7f, -191.2f),
        new RoomDef("S5_corredor_oeste", Kind.Corridor, 12f, -23.7f, -19.7f, -178.7f, -143.0f),
        new RoomDef("S6_corredor_sul", Kind.Corridor, 12f, -37.0f, -5.3f, -182.6f, -179.2f),
        new RoomDef("S7_corredor_leste", Kind.Corridor, 12f, -10.6f, -6.6f, -143.0f, -120.0f),
    };

    private static readonly StringBuilder report = new StringBuilder();
    private static System.Random rng;
    private static int solidMask;
    private static Transform root;
    private static readonly List<(Vector3 p, float r)> keepOut = new List<(Vector3, float)>();
    private static readonly List<Bounds> doorZones = new List<Bounds>();
    private static readonly Dictionary<Mesh, Mesh[]> lodCache = new Dictionary<Mesh, Mesh[]>();
    private static readonly Dictionary<float, RoomMap> maps = new Dictionary<float, RoomMap>();

    // Estado da sala atual (caminho livre).
    private static RoomDef room;
    private static RoomMap map;
    private static List<Vector2Int> roomCells;
    private static HashSet<Vector2Int> entrances;
    private static readonly List<(Vector3 c, Vector3 h, Quaternion r)> placedBoxes = new List<(Vector3, Vector3, Quaternion)>();
    private static float aisle;
    private static int roomCount;

    public static void BuildBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Principal.unity", OpenSceneMode.Single);
        FinalRoundBuilder.ConvertFurnitureMaterials();
        Build(false);
        // Baterias, ponto de montagem e particulas das pecas depois da mobilia; occlusion por ultimo.
        FinalRoundBuilder.Build();
        BakeOcclusion();
        Audit();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        ReviewShots();
    }

    public static void Build(bool bake)
    {
        report.Clear();
        lodCache.Clear();
        maps.Clear();
        solidMask = ~LayerMask.GetMask("Ignore Raycast", "UI", "Arms", "PhotoCameraEffect", "GlobalVolume", "Enemy", "Temporal", "TransparentFX", "Player");
        if (!FurnitureCatalog.Load())
            return;

        foreach (string n in new[] { "Props_PSX", RootName, "Baterias_Extra", "PontoDeMontagem" })
        {
            GameObject old = GameObject.Find(n);
            if (old != null)
                Object.DestroyImmediate(old);
        }
        GameObject rootGo = new GameObject(RootName);
        GameObject env = GameObject.Find("Environment");
        if (env != null)
            rootGo.transform.SetParent(env.transform, false);
        root = rootGo.transform;
        Physics.SyncTransforms();

        CollectKeepOut();

        int total = 0;
        for (int i = 0; i < Rooms.Count; i++)
        {
            rng = new System.Random(1975 + i * 31);
            room = Rooms[i];
            if (!maps.TryGetValue(room.Y, out map))
                maps[room.Y] = map = RoomMap.Build(room.Y);
            int before = root.childCount;
            Transform roomRoot = new GameObject(room.Id).transform;
            roomRoot.SetParent(root, false);
            BeginRoom();
            var walls = FindWalls();
            FindWalls(true);
            report.AppendLine(room.Id + ": " + roomCells.Count + " celulas, " + entrances.Count + " entradas, " + walls.Count + " paredes (" + string.Join(", ", walls.Select(w => (w.To - w.From).ToString("F1"))) + ")");
            rejects.Clear();
            hitNames.Clear();
            Furnish(roomRoot);
            report.AppendLine("   recusas: " + string.Join(", ", rejects.Select(kv => kv.Key + "=" + kv.Value)) +
                              "  | bate em: " + string.Join(", ", hitNames.OrderByDescending(kv => kv.Value).Take(4).Select(kv => kv.Key + "=" + kv.Value)));
            int n = roomRoot.childCount;
            total += n;
            report.AppendLine(room.Id + " (" + room.Kind + "): " + n + " pecas");
        }

        report.AppendLine("total: " + total);
        if (bake)
            BakeOcclusion();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/revisao/qa/salas"));
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "mobilia.txt"), report.ToString());
        Debug.Log("RoomFurnisher\n" + report);
    }

    private static void CollectKeepOut()
    {
        keepOut.Clear();
        doorZones.Clear();
        foreach (TemporalObjectPickup p in Object.FindObjectsByType<TemporalObjectPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            keepOut.Add((p.transform.position, 1.8f));
        foreach (PhotoLocation p in Object.FindObjectsByType<PhotoLocation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            keepOut.Add((p.transform.position, 1.5f));
        GameObject spawn = GameObject.Find("PlayerSpawn");
        if (spawn != null)
            keepOut.Add((spawn.transform.position, 2.5f));
        TemporalEraserController enemy = Object.FindFirstObjectByType<TemporalEraserController>();
        if (enemy != null)
            keepOut.Add((enemy.transform.position, 2.5f));
        foreach (Door door in Object.FindObjectsByType<Door>(FindObjectsSortMode.None))
        {
            Renderer[] rs = door.GetComponentsInChildren<Renderer>();
            Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(door.transform.position, Vector3.one);
            foreach (Renderer r in rs) b.Encapsulate(r.bounds);
            b.Expand(new Vector3(2.4f, 0f, 2.4f));
            doorZones.Add(b);
        }
    }

    // ---------- Arranjos por tipo de sala ----------

    private static void Furnish(Transform parent)
    {
        switch (room.Kind)
        {
            case Kind.Lab:
                aisle = 0.55f;
                StationGrid(parent, 3.7f, 3.3f, 1.3f, new (Station, int)[] { (LabBench, 5), (LabPair, 3), (WorkTable, 2), (StorageIsland, 1), (BoxPile, 1) }, true);
                WallRuns(parent, new[] { "shelfC", "shelfD", "shelfB", "drawerA", "drawerB", "cupboardB", "kitchensink", "cupboardC", "fridgeB", "shelfE", "radiator" }, 12, 0.05f, 0.4f, true, 99, DressWall, 0.45f);
                Scatter(parent, new[] { "bin", "plantB", "crate4", "cardboard2", "coil", "lampB", "broom", "ladder", "crate7", "cardboard4" }, 14);
                WallClock(parent);
                break;
            case Kind.Office:
                aisle = 0.55f;
                StationGrid(parent, 3.4f, 3.2f, 1.2f, new (Station, int)[] { (DeskStation, 5), (DeskPair, 4), (Meeting, 1), (DividerShelf, 1) }, true);
                WallRuns(parent, new[] { "drawerA", "drawerB", "shelfA", "shelfE", "smallcloset", "commode", "safeB", "radiator", "tvstand" }, 12, 0.1f, 0.5f, true, 99, DressWall, 0.5f);
                Scatter(parent, new[] { "plantA", "plantB", "bin", "lampB", "coil", "cardboard2", "drawerB" }, 12);
                WallClock(parent);
                break;
            case Kind.Director:
                aisle = 0.5f;
                PlaceCarpet(parent);
                WallDesk(parent, "officedesk", DressDirector);
                WallRuns(parent, new[] { "shelfE", "shelfB", "shelfC", "safeB", "safe", "commode", "tvstand", "drawerB", "smallcloset" }, 5, 0.1f, 0.4f, false, 12, DressWall, 0.6f);
                WallRuns(parent, new[] { "sofaC", "armchair" }, 1, 0.4f, 0.6f, false, 2, SofaTable);
                Scatter(parent, new[] { "plantA", "lampB", "bin", "plantB", "lampA" }, 7);
                WallClock(parent);
                break;
            case Kind.Archive:
                aisle = 0.5f;
                StationGrid(parent, 2.6f, 2.6f, 0.9f, new (Station, int)[] { (StorageIsland, 4), (BoxPile, 1), (WorkTable, 1) }, false);
                WallRuns(parent, new[] { "shelfC", "shelfD", "shelfA", "drawerA", "drawerB", "shelfE" }, 8, 0.02f, 0.3f, true, 99, DressWall, 0.3f);
                Scatter(parent, new[] { "cardboardA", "cardboardB", "cardboard2", "cardboard4", "ladder", "crate7", "bin", "lampB" }, 10);
                break;
            case Kind.Storage:
                aisle = 0.45f;
                WallRuns(parent, new[] { "shelfD", "shelfC", "crateB", "crate9", "crate2", "crateA", "crate10", "crate4", "cardboard2" }, 6, 0.05f, 0.3f, false, 99, DressWall);
                StationGrid(parent, 2.0f, 2.0f, 0.8f, new (Station, int)[] { (BoxPile, 3), (StorageIsland, 1) }, false);
                Stacks(parent, 4);
                Scatter(parent, new[] { "coil", "broom", "ladder", "cardboard4", "crate5", "crate8", "cardboard2" }, 9);
                break;
            case Kind.Break:
                aisle = 0.5f;
                WallRuns(parent, new[] { "kitchenA", "cupboardC", "cupboardD", "cupboardE", "kitchensink", "oven", "fridge", "fridgeB" }, 2, 0.0f, 0.05f, true, 7, DressCounter, 0.4f);
                StationGrid(parent, 2.3f, 2.3f, 0.9f, new (Station, int)[] { (BreakTable, 1) }, false, 1);
                Scatter(parent, new[] { "bin", "plantB", "broom" }, 3);
                WallClock(parent);
                break;
            case Kind.Waiting:
                aisle = 0.7f;
                PlaceCarpet(parent);
                WallRuns(parent, new[] { "sofaA", "sofaB", "sofaD", "sofaC", "armchair", "plantA", "lampB", "plantB", "bin", "radiator" }, 6, 0.2f, 0.6f, false, 18, SofaTable, 0f, true);
                break;
            case Kind.Corridor:
                aisle = 0.85f;
                Sparse(parent, new[] { "radiator", "radiatorB", "plantA", "plantB", "bin", "drawerB", "seats", "lampB", "radiator", "seats", "plantB", "cardboard2", "cardboard4", "crate7", "coil", "broom", "commode", "drawerB", "bin", "plantA" }, 1.5f);
                break;
        }
    }

    // ---------- Estacoes (cada celula da grade sorteia uma) ----------

    private delegate bool Station(Transform parent, Vector3 center, Vector3 front);

    private static readonly string[] LabItems = { "lamp", "book1", "book2", "mug", "mugB", "notes", "booksrow", "globe", "notebookP", "floppy", "flashlight", "padlock", "smallbox", "speakerP", "progbook", "crt", "fan", "phone", "ashtray", "cigpack" };
    private static readonly string[] DeskItems = { "lamp", "phone", "mug", "mugB", "ashtray", "cigpack", "notes", "book1", "progbook", "notebookP", "floppy", "speakerP", "vase" };
    private static readonly string[] MeetingItems = { "mug", "mugB", "ashtray", "cigpack", "notebookP", "book1", "vase", "phone", "glass" };
    private static readonly string[] ShelfItems = { "booksrow", "booksB", "vase", "smallbox", "globe", "plate", "book2", "flashlight", "progbook" };
    private static readonly string[] CounterItems = { "microwave", "kettle", "toaster", "knifeblock", "kitchenroll", "plate", "bowl", "glass", "mug", "pan", "pot" };
    private static readonly string[] CoffeeItems = { "ashtray", "vase", "book1", "booksB", "mug", "cigpack", "glass" };

    private static string Pick(string[] keys) => keys[rng.Next(keys.Length)];

    private static float DepthAlong(Placed p, Vector3 dir) => Mathf.Abs(Vector3.Dot(p.Rot * Vector3.forward, dir)) > 0.5f ? p.Half.z : p.Half.x;
    private static float WidthAcross(Placed p, Vector3 dir) => Mathf.Abs(Vector3.Dot(p.Rot * Vector3.forward, dir)) > 0.5f ? p.Half.x : p.Half.z;

    private static Placed TryPlace(Transform parent, string key, Vector3 at, Vector3 front, float yaw = 0f, bool needFront = false, float clearance = 0f)
    {
        Placed p = Create(parent, key, at, front, yaw);
        if (clearance > 0f && Physics.CheckBox(p.Center + Vector3.up * 0.08f, p.Half + new Vector3(clearance, -0.1f, clearance), p.Rot, solidMask, QueryTriggerInteraction.Ignore))
        {
            Object.DestroyImmediate(p.Go);
            return null;
        }
        return Accept(p, needFront) ? p : null;
    }

    // Peca colada atras de outra (costas com costas), virada para o lado oposto.
    private static Placed BackToBack(Transform parent, Placed first, string key, Vector3 front, bool needFront)
    {
        Placed probe = Create(parent, key, Vector3.zero, -front);
        float d = DepthAlong(probe, front);
        Object.DestroyImmediate(probe.Go);
        Vector3 at = first.Center - front * (DepthAlong(first, front) + d + 0.03f);
        return TryPlace(parent, key, new Vector3(at.x, room.Y, at.z), -front, 0f, needFront);
    }

    // Cadeira encostada no lado "side" do movel, deslocada "lateral" ao longo dele, virada para ele.
    private static void Seat(Transform parent, Placed host, string chairKey, Vector3 side, float lateral)
    {
        Placed chair = Create(parent, chairKey, Vector3.zero, -side, (float)(rng.NextDouble() * 24 - 12));
        float chairDepth = Mathf.Max(chair.Half.x, chair.Half.z);
        Vector3 across = Vector3.Cross(Vector3.up, side).normalized;
        Vector3 pos = host.Center + side * (DepthAlong(host, side) + chairDepth + 0.05f) + across * lateral;
        Vector3 c = chair.Center;
        chair.Go.transform.position += new Vector3(pos.x - c.x, room.Y - (c.y - chair.Half.y), pos.z - c.z);
        Accept(chair, false);
    }

    private static bool LabBench(Transform parent, Vector3 c, Vector3 f)
    {
        Placed b = TryPlace(parent, "bench", c, f, 0f, false, 0.45f);
        if (b == null) return false;
        float w = WidthAcross(b, f);
        int stools = rng.Next(1, 3);
        string stool = new[] { "chairB", "chairC", "chairD" }[rng.Next(3)];
        for (int i = 0; i < stools; i++)
            Seat(parent, b, stool, f, stools == 1 ? ((float)rng.NextDouble() - 0.5f) * w : (i == 0 ? -0.45f : 0.45f) * w);
        Dress(parent, b, LabItems, rng.Next(2, 6), false);
        return true;
    }

    private static bool LabPair(Transform parent, Vector3 c, Vector3 f)
    {
        Placed a = TryPlace(parent, "bench", c + f * 0.6f * WorldScale, f, 0f, false, 0.45f);
        if (a == null) return false;
        Placed b = BackToBack(parent, a, "bench", f, false);
        string stool = new[] { "chairB", "chairC", "chairD" }[rng.Next(3)];
        Seat(parent, a, stool, f, ((float)rng.NextDouble() - 0.5f) * WidthAcross(a, f));
        Dress(parent, a, LabItems, rng.Next(2, 5), false);
        if (b != null)
        {
            Seat(parent, b, stool, -f, ((float)rng.NextDouble() - 0.5f) * WidthAcross(b, f));
            Dress(parent, b, LabItems, rng.Next(1, 5), false);
        }
        return true;
    }

    private static bool WorkTable(Transform parent, Vector3 c, Vector3 f)
    {
        Placed t = TryPlace(parent, rng.NextDouble() < 0.6 ? "tableA" : "tableB", c, f, 0f, false, 0.55f);
        if (t == null) return false;
        string chair = new[] { "chairA", "chairD", "chairC" }[rng.Next(3)];
        float w = WidthAcross(t, f);
        Seat(parent, t, chair, f, -0.25f * w);
        Seat(parent, t, chair, -f, 0.25f * w);
        if (rng.NextDouble() < 0.5) Seat(parent, t, chair, f, 0.3f * w);
        Dress(parent, t, rng.NextDouble() < 0.5 ? LabItems : MeetingItems, rng.Next(3, 6), false);
        return true;
    }

    private static bool StorageIsland(Transform parent, Vector3 c, Vector3 f)
    {
        string key = new[] { "shelfD", "shelfC", "shelfE", "shelfD" }[rng.Next(4)];
        Placed a = TryPlace(parent, key, c, f, 0f, true, 0.6f);
        if (a == null) return false;
        Placed b = BackToBack(parent, a, key, f, true);
        if (rng.NextDouble() < 0.6) Dress(parent, a, new[] { "smallbox", "booksrow", "globe" }, 1, false);
        if (b != null && rng.NextDouble() < 0.4) Dress(parent, b, new[] { "smallbox", "booksB" }, 1, false);
        return true;
    }

    private static readonly string[] Crates = { "crateA", "crateB", "crate2", "crate4", "crate5", "crate6", "crate7", "crate8", "crate9", "crate10", "cardboardA", "cardboardB", "cardboard2", "cardboard4" };

    private static bool BoxPile(Transform parent, Vector3 c, Vector3 f)
    {
        int placed = 0;
        int n = rng.Next(3, 6);
        Placed firstBig = null;
        for (int i = 0; i < n * 3 && placed < n; i++)
        {
            Vector3 off = new Vector3((float)rng.NextDouble() * 1.6f - 0.8f, 0f, (float)rng.NextDouble() * 1.6f - 0.8f) * WorldScale;
            Placed p = TryPlace(parent, Pick(Crates), c + off, f, (float)rng.NextDouble() * 360f);
            if (p == null) continue;
            placed++;
            if (firstBig == null && p.Half.y > 0.25f) firstBig = p;
        }
        // Uma caixa menor em cima da maior.
        if (firstBig != null)
            StackOn(parent, firstBig, new[] { "cardboard4", "cardboardB", "smallbox" }[rng.Next(3)]);
        return placed > 0;
    }

    private static void StackOn(Transform parent, Placed bottom, string key)
    {
        Placed top = Create(parent, key, Vector3.zero, bottom.Front, (float)(rng.NextDouble() * 30 - 15));
        if (top.Half.x > bottom.Half.x || top.Half.z > bottom.Half.z)
        {
            Object.DestroyImmediate(top.Go);
            return;
        }
        Vector3 target = bottom.Center + Vector3.up * bottom.Half.y;
        Vector3 tc = top.Center;
        top.Go.transform.position += new Vector3(target.x - tc.x, target.y - (tc.y - top.Half.y) + 0.002f, target.z - tc.z);
        if (Physics.CheckBox(top.Center + Vector3.up * 0.01f, top.Half - new Vector3(0.01f, 0.02f, 0.01f), top.Rot, solidMask, QueryTriggerInteraction.Ignore))
            Object.DestroyImmediate(top.Go);
        else
            Finish(top);
    }

    private static bool DeskStation(Transform parent, Vector3 c, Vector3 f)
    {
        bool exec = rng.NextDouble() < 0.2;
        Placed d = TryPlace(parent, exec ? "officedesk" : "desk", c, f, 0f, false, 0.6f);
        if (d == null) return false;
        Seat(parent, d, exec ? "chairE" : (rng.NextDouble() < 0.7 ? "chair" : "chairE"), f, 0f);
        Dress(parent, d, DeskItems, rng.Next(2, 5), !exec && rng.NextDouble() < 0.75);
        return true;
    }

    private static bool DeskPair(Transform parent, Vector3 c, Vector3 f)
    {
        Placed a = TryPlace(parent, "desk", c + f * 0.5f * WorldScale, f, 0f, false, 0.6f);
        if (a == null) return false;
        Seat(parent, a, rng.NextDouble() < 0.7 ? "chair" : "chairE", f, 0f);
        Dress(parent, a, DeskItems, rng.Next(1, 4), rng.NextDouble() < 0.8);
        Placed b = BackToBack(parent, a, "desk", f, false);
        if (b != null)
        {
            Seat(parent, b, rng.NextDouble() < 0.7 ? "chair" : "chairE", -f, 0f);
            Dress(parent, b, DeskItems, rng.Next(1, 4), rng.NextDouble() < 0.6);
        }
        return true;
    }

    private static bool Meeting(Transform parent, Vector3 c, Vector3 f)
    {
        Placed t = TryPlace(parent, "tableA", c, f, 0f, false, 0.9f);
        if (t == null) return false;
        float w = WidthAcross(t, f);
        foreach (float s in new[] { -0.3f, 0.3f })
        {
            Seat(parent, t, "chairA", f, s * w);
            Seat(parent, t, "chairA", -f, s * w);
        }
        Dress(parent, t, MeetingItems, rng.Next(3, 7), false);
        return true;
    }

    private static bool DividerShelf(Transform parent, Vector3 c, Vector3 f) => StorageIsland(parent, c, f);

    private static bool BreakTable(Transform parent, Vector3 c, Vector3 f)
    {
        foreach (string key in new[] { "roundB", "roundtable", "roundC" })
        {
            Placed t = TryPlace(parent, key, c, f, 0f, false, 0.55f);
            if (t == null) continue;
            string chair = rng.NextDouble() < 0.5 ? "chairA" : "chairD";
            Seat(parent, t, chair, f, 0f);
            Seat(parent, t, chair, -f, 0f);
            Seat(parent, t, chair, Vector3.Cross(Vector3.up, f), 0f);
            Dress(parent, t, MeetingItems, rng.Next(2, 4), false);
            return true;
        }
        return false;
    }

    private static void StationGrid(Transform parent, float cellX, float cellZ, float margin, (Station s, int w)[] stations, bool rotate, int max = 999)
    {
        Rect a = room.Area;
        // Moveis cresceram 1,38x; o espacamento cresce menos (salas cheias, corredor livre garantido pelo caminho).
        cellX *= 1.12f;
        cellZ *= 1.12f;
        margin *= 1.1f;
        int row = 0, done = 0;
        int total = stations.Sum(x => x.w);
        for (float z = a.yMin + margin + cellZ * 0.5f; z <= a.yMax - margin - cellZ * 0.25f; z += cellZ, row++)
            for (float x = a.xMin + margin + cellX * 0.5f; x <= a.xMax - margin - cellX * 0.25f; x += cellX)
            {
                if (done >= max) return;
                Vector3 f = row % 2 == 0 ? Vector3.forward : Vector3.back;
                if (rotate && rng.NextDouble() < 0.25) f = rng.NextDouble() < 0.5 ? Vector3.right : Vector3.left;
                Vector3 jitter = new Vector3(((float)rng.NextDouble() - 0.5f) * 0.4f, 0f, ((float)rng.NextDouble() - 0.5f) * 0.3f);
                int r = rng.Next(total);
                Station st = stations[0].s;
                foreach (var o in stations) { if (r < o.w) { st = o.s; break; } r -= o.w; }
                if (st != null && st(parent, new Vector3(x, room.Y, z) + jitter, f))
                    done++;
            }
    }

    // ---------- Complementos de parede ----------

    private static void DressWall(Transform parent, Placed p)
    {
        if (!p.Item.Collider) return;
        float height = p.Half.y * 2f / WorldScale;
        if (height < 1.25f && rng.NextDouble() < 0.6)
            Dress(parent, p, rng.NextDouble() < 0.5 ? ShelfItems : DeskItems, rng.Next(1, 3), false);
        else if (height > 1.7f && rng.NextDouble() < 0.35)
            Dress(parent, p, new[] { "smallbox", "booksrow", "cardboard4" }, 1, false);
    }

    private static void SofaTable(Transform parent, Placed sofa)
    {
        if (!sofa.Item.Key.StartsWith("sofa") && sofa.Item.Key != "armchair") return;
        Vector3 f = sofa.Front;
        Placed probe = Create(parent, "roundC", Vector3.zero, f);
        float d = DepthAlong(probe, f);
        Object.DestroyImmediate(probe.Go);
        Vector3 at = sofa.Center + f * (DepthAlong(sofa, f) + 0.45f + d);
        Placed t = TryPlace(parent, sofa.Item.Key == "armchair" ? "roundC" : (rng.NextDouble() < 0.5 ? "tableC" : "roundC"), new Vector3(at.x, room.Y, at.z), f);
        if (t != null)
            Dress(parent, t, CoffeeItems, rng.Next(1, 4), false);
    }

    private static void ShelfAbove(Transform parent, WallRun run, Placed below)
    {
        string key = rng.NextDouble() < 0.5 ? "wallshelfA" : "wallshelfB";
        Placed s = Create(parent, key, Vector3.zero, run.Normal);
        float depth = DepthAlong(s, run.Normal);
        float along = Vector3.Dot(below.Center, run.Along);
        Vector3 pos = WallPoint(run, along, depth + 0.01f);
        float floorY = below.Center.y - below.Half.y;
        Vector3 c = s.Center;
        s.Go.transform.position += new Vector3(pos.x - c.x, floorY + 1.5f * WorldScale - (c.y - s.Half.y), pos.z - c.z);
        bool blocked = Physics.CheckBox(s.Center, s.Half - Vector3.one * 0.01f, s.Rot, solidMask, QueryTriggerInteraction.Ignore);
        bool wall = Physics.Raycast(s.Center, -run.Normal, depth + 0.12f, solidMask, QueryTriggerInteraction.Ignore);
        if (blocked || !wall || floorY + 1.5f * WorldScale < below.Center.y + below.Half.y + 0.35f)
        {
            Object.DestroyImmediate(s.Go);
            return;
        }
        Finish(s);
        Dress(parent, s, ShelfItems, rng.Next(1, 4), false);
    }

    private static void PlaceCarpet(Transform parent)
    {
        Vector2 c = room.Area.center;
        for (int i = 0; i < 12; i++)
        {
            Vector3 at = new Vector3(c.x + ((float)rng.NextDouble() - 0.5f) * 1.5f, room.Y, c.y + ((float)rng.NextDouble() - 0.5f) * 1.5f);
            Placed p = Create(parent, rng.NextDouble() < 0.5 ? "carpetA" : "carpetB", at, room.Area.width > room.Area.height ? Vector3.right : Vector3.forward);
            if (Accept(p, false, false))
                return;
        }
    }

    // ---------- Sala atual: grade e caminho livre ----------

    private static void BeginRoom()
    {
        placedBoxes.Clear();
        wallCache = null;
        wallCacheRelaxed = null;
        roomCells = new List<Vector2Int>();
        entrances = new HashSet<Vector2Int>();
        Vector2Int a = map.GridOf(new Vector3(room.Area.xMin, 0f, room.Area.yMin));
        Vector2Int b = map.GridOf(new Vector3(room.Area.xMax, 0f, room.Area.yMax));
        for (int x = a.x; x <= b.x; x++)
            for (int z = a.y; z <= b.y; z++)
            {
                if (!map.In(x, z) || !map.Walk[x, z])
                    continue;
                roomCells.Add(new Vector2Int(x, z));
                bool edge = x == a.x || x == b.x || z == a.y || z == b.y;
                if (edge)
                {
                    foreach (Vector2Int d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                    {
                        Vector2Int o = new Vector2Int(x, z) + d;
                        bool outside = o.x < a.x || o.x > b.x || o.y < a.y || o.y > b.y;
                        if (outside && map.In(o.x, o.y) && map.Walk[o.x, o.y])
                            entrances.Add(new Vector2Int(x, z));
                    }
                }
            }
    }

    // O retangulo foi desenhado sobre a area andavel (0,4 m afastada das paredes): moveis encostados
    // na parede tem o centro um pouco fora dele.
    private static bool InRoom(Vector3 p)
    {
        Rect r = room.Area;
        return p.x > r.xMin - 0.9f && p.x < r.xMax + 0.9f && p.z > r.yMin - 0.9f && p.z < r.yMax + 0.9f;
    }

    private static bool Blocked(Vector2Int c, IEnumerable<(Vector3 c, Vector3 h, Quaternion r)> boxes, float inflate)
    {
        Vector3 w = map.World(c.x, c.y);
        foreach (var box in boxes)
        {
            Vector3 local = Quaternion.Inverse(box.r) * (w - box.c);
            if (Mathf.Abs(local.x) < box.h.x + inflate && Mathf.Abs(local.z) < box.h.z + inflate)
                return true;
        }
        return false;
    }

    // O caminho entre as entradas da sala continua aberto e nao sobram bolsoes isolados.
    private static bool KeepsPath((Vector3 c, Vector3 h, Quaternion r) extra)
    {
        var boxes = new List<(Vector3, Vector3, Quaternion)>(placedBoxes) { extra };
        var free = new HashSet<Vector2Int>();
        foreach (Vector2Int c in roomCells)
            if (!Blocked(c, boxes, aisle))
                free.Add(c);
        foreach (Vector2Int e in entrances)
            if (!free.Contains(e))
                return false;
        if (free.Count == 0)
            return false;

        Vector2Int start = entrances.Count > 0 ? entrances.First() : free.First();
        var seen = new HashSet<Vector2Int> { start };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            Vector2Int c = queue.Dequeue();
            foreach (Vector2Int d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
            {
                Vector2Int n = c + d;
                if (free.Contains(n) && seen.Add(n))
                    queue.Enqueue(n);
            }
        }
        foreach (Vector2Int e in entrances)
            if (!seen.Contains(e))
                return false;
        return seen.Count >= free.Count * 0.9f;
    }

    // ---------- Instancia, medidas e checagens ----------

    private class Placed
    {
        public GameObject Go;
        public FurnitureCatalog.Item Item;
        public Bounds Local;
        public Vector3 Center => Go.transform.TransformPoint(Local.center);
        public Vector3 Half => Vector3.Scale(Local.extents, Go.transform.lossyScale);
        public Quaternion Rot => Go.transform.rotation;
        public Vector3 Front;
    }

    private static Placed Create(Transform parent, string key, Vector3 bottomCenter, Vector3 front, float extraYaw = 0f)
    {
        FurnitureCatalog.Item item = FurnitureCatalog.Get(key);
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(item.Source);
        if (model == null)
            model = Object.Instantiate(item.Source);
        if (PrefabUtility.IsPartOfPrefabInstance(model))
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        // Embrulha o modelo: a rotacao de importacao (FBX do Blender vem com -90 em X) fica no filho.
        GameObject go = new GameObject(key);
        go.transform.SetParent(parent, false);
        Quaternion baseRot = item.Source.transform.localRotation;
        Vector3 baseScale = item.Source.transform.localScale;
        model.transform.SetParent(go.transform, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = baseRot;
        model.transform.localScale = baseScale;
        foreach (Collider col in go.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(col);
        foreach (LODGroup g in go.GetComponentsInChildren<LODGroup>(true))
            Object.DestroyImmediate(g);
        Material forced = !string.IsNullOrEmpty(item.MaterialPath) ? AssetDatabase.LoadAssetAtPath<Material>(item.MaterialPath)
                        : item.Tint.a > 0f ? TintMaterial(item.Tint) : null;
        if (forced != null)
        {
            Material m = forced;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterials = Enumerable.Repeat(m, r.sharedMaterials.Length).ToArray();
        }

        go.transform.localScale = Vector3.one;
        go.transform.rotation = Quaternion.identity;
        go.transform.position = Vector3.zero;
        Bounds lb = LocalBounds(go);
        float factor = (item.Height > 0f ? item.Height / lb.size.y : item.Length / Mathf.Max(lb.size.x, lb.size.z)) * WorldScale;
        go.transform.localScale = Vector3.one * factor;

        Vector3 f = new Vector3(front.x, 0f, front.z).normalized;
        Quaternion rot = Quaternion.LookRotation(item.FrontNegZ ? -f : f, Vector3.up) * Quaternion.Euler(0f, extraYaw, 0f);
        go.transform.rotation = rot;
        var placed = new Placed { Go = go, Item = item, Local = lb, Front = f };
        // Assenta pela base e centraliza no ponto.
        Vector3 c = placed.Center;
        go.transform.position += bottomCenter - new Vector3(c.x, c.y - placed.Half.y, c.z);
        return placed;
    }

    private static readonly Dictionary<Color, Material> tintCache = new Dictionary<Color, Material>();

    private static Material TintMaterial(Color c)
    {
        if (tintCache.TryGetValue(c, out Material cached) && cached != null)
            return cached;
        string folder = "Assets/Art/Materials/Mobilia";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Art/Materials", "Mobilia");
        string path = folder + "/Mobilia_" + ColorUtility.ToHtmlStringRGB(c) + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", 0.18f);
        m.SetFloat("_Metallic", 0f);
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        tintCache[c] = m;
        return m;
    }

    private static Bounds LocalBounds(GameObject go)
    {
        bool any = false;
        Bounds b = new Bounds();
        Matrix4x4 toRoot = go.transform.worldToLocalMatrix;
        foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            Bounds mb = mf.sharedMesh.bounds;
            Matrix4x4 m = toRoot * mf.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 p = m.MultiplyPoint3x4(corner);
                if (!any) { b = new Bounds(p, Vector3.zero); any = true; }
                else b.Encapsulate(p);
            }
        }
        return b;
    }

    private static bool Valid(Placed p, bool needFront, out string why)
    {
        Vector3 c = p.Center, h = p.Half;
        Quaternion r = p.Rot;
        float bottom = c.y - h.y;
        why = "";

        if (!InRoom(c)) { why = "fora"; return false; }
        foreach (var k in keepOut)
            if (Vector3.Distance(new Vector3(k.p.x, c.y, k.p.z), c) < k.r + Mathf.Max(h.x, h.z) && Mathf.Abs(k.p.y - bottom) < 3f) { why = "reservado"; return false; }
        Bounds world = new Bounds(c, new Vector3(h.x, h.y, h.z) * 2f);
        world.Expand(new Vector3(Mathf.Max(h.x, h.z), 0f, Mathf.Max(h.x, h.z)));
        foreach (Bounds d in doorZones)
            if (d.Intersects(new Bounds(c, new Vector3(h.x * 2f, h.y * 2f, h.z * 2f))) && Mathf.Abs(d.min.y - bottom) < 2f) { why = "porta"; return false; }

        // Volume livre (levanta 4 cm do chao).
        // Ignora os 8 cm de baixo: o piso tem uma borda junto das paredes que so encosta na base.
        Vector3 vc = c + Vector3.up * 0.045f;
        Vector3 vh = new Vector3(Mathf.Max(0.01f, h.x - 0.01f), Mathf.Max(0.01f, h.y - 0.055f), Mathf.Max(0.01f, h.z - 0.01f));
        Collider[] hits = Physics.OverlapBox(vc, vh, r, solidMask, QueryTriggerInteraction.Ignore);
        if (hits.Length > 0 && h.y > 0.15f)
        {
            Collider[] upper = Physics.OverlapBox(c + Vector3.up * 0.1f, new Vector3(vh.x, Mathf.Max(0.01f, h.y - 0.1f), vh.z), r, solidMask, QueryTriggerInteraction.Ignore);
            hits = hits.Where(x => !x.name.StartsWith("Floor") || upper.Contains(x)).ToArray();
        }
        if (hits.Length > 0)
        {
            why = "colide";
            string n = hits[0].name;
            hitNames[n] = hitNames.TryGetValue(n, out int k) ? k + 1 : 1;
            return false;
        }

        // Chao embaixo dos cantos e do centro, no mesmo nivel.
        for (int i = 0; i < 5; i++)
        {
            Vector3 o = i == 4 ? Vector3.zero : new Vector3((i & 1) == 0 ? -1 : 1, 0f, (i & 2) == 0 ? -1 : 1);
            Vector3 q = c + r * Vector3.Scale(o, new Vector3(h.x - 0.05f, 0f, h.z - 0.05f));
            if (!Physics.Raycast(new Vector3(q.x, bottom + 0.25f, q.z), Vector3.down, out RaycastHit hit, 0.5f, solidMask, QueryTriggerInteraction.Ignore) ||
                Mathf.Abs(hit.point.y - bottom) > 0.05f) { why = "sem chao"; return false; }
        }

        // Frente livre para usar o movel (gavetas, portas, assento).
        if (needFront)
        {
            Vector3 f = p.Front;
            float depth = Mathf.Abs(Vector3.Dot(r * Vector3.forward, f)) > 0.5f ? h.z : h.x;
            float width = Mathf.Abs(Vector3.Dot(r * Vector3.forward, f)) > 0.5f ? h.x : h.z;
            Vector3 fc = c + f * (depth + 0.5f);
            fc.y = bottom + 1.0f;
            if (Physics.CheckBox(fc, new Vector3(width * 0.9f, 0.85f, 0.45f), Quaternion.LookRotation(f), solidMask, QueryTriggerInteraction.Ignore)) { why = "frente"; return false; }
        }
        return true;
    }

    private static readonly Dictionary<string, int> rejects = new Dictionary<string, int>();
    private static readonly Dictionary<string, int> hitNames = new Dictionary<string, int>();

    // Assenta no piso real medido embaixo da peca (o NavMesh fica alguns cm acima ou abaixo).
    private static bool Settle(Placed p)
    {
        Vector3 c = p.Center;
        float bottom = c.y - p.Half.y;
        float best = float.NegativeInfinity;
        foreach (int i in new[] { 4, 0, 1, 2, 3 })
        {
            Vector3 o = i == 4 ? Vector3.zero : new Vector3((i & 1) == 0 ? -1 : 1, 0f, (i & 2) == 0 ? -1 : 1);
            Vector3 q = c + p.Rot * Vector3.Scale(o, new Vector3(p.Half.x * 0.8f, 0f, p.Half.z * 0.8f));
            if (Physics.Raycast(new Vector3(q.x, bottom + 0.6f, q.z), Vector3.down, out RaycastHit hit, 1.2f, solidMask, QueryTriggerInteraction.Ignore) && hit.normal.y > 0.9f)
            {
                // O centro manda (os cantos podem pegar o topo do rodape); os cantos so valem sem o centro.
                if (i == 4) { best = hit.point.y; break; }
                best = Mathf.Max(best, hit.point.y);
            }
        }
        if (float.IsNegativeInfinity(best))
            return false;
        p.Go.transform.position += Vector3.up * (best + 0.003f - bottom);
        return true;
    }

    private static bool Accept(Placed p, bool needFront, bool path = true)
    {
        string why;
        if (!Settle(p))
        {
            rejects["sem chao"] = rejects.TryGetValue("sem chao", out int sc) ? sc + 1 : 1;
            Object.DestroyImmediate(p.Go);
            return false;
        }
        bool ok = Valid(p, needFront, out why);
        if (ok && path && !KeepsPath((p.Center, p.Half, p.Rot))) { ok = false; why = "caminho"; }
        if (!ok)
        {
            rejects[why] = rejects.TryGetValue(why, out int n) ? n + 1 : 1;
            Object.DestroyImmediate(p.Go);
            return false;
        }
        Finish(p);
        return true;
    }

    // Colisor justo, obstaculo do NavMesh, estatico e LOD.
    private static void Finish(Placed p)
    {
        if (p.Item.Collider)
        {
            BoxCollider box = p.Go.AddComponent<BoxCollider>();
            box.center = p.Local.center;
            box.size = p.Local.size;
            NavMeshObstacle obstacle = p.Go.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = p.Local.center;
            obstacle.size = p.Local.size;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
            p.Go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            placedBoxes.Add((p.Center, p.Half, p.Rot));
        }
        AddLods(p);
        var flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;
        if (p.Item.Occluder)
            flags |= StaticEditorFlags.OccluderStatic;
        foreach (Transform t in p.Go.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
        Physics.SyncTransforms();
    }

    // ---------- Paredes ----------

    private class WallRun
    {
        public Vector3 Normal;   // para dentro da sala
        public Vector3 Along;
        public float Plane;      // posicao da parede no eixo da normal
        public float From, To;   // extensao ao longo
        public float Y;
    }

    private static List<WallRun> wallCache, wallCacheRelaxed;

    private static List<WallRun> FindWalls(bool relaxed = false)
    {
        if (relaxed ? wallCacheRelaxed != null : wallCache != null)
            return relaxed ? wallCacheRelaxed : wallCache;
        var found = MeasureWalls(relaxed);
        if (relaxed) wallCacheRelaxed = found; else wallCache = found;
        return found;
    }

    private static List<WallRun> MeasureWalls(bool relaxed)
    {
        var slots = new List<(Vector3 n, float plane, float along)>();
        foreach (Vector2Int c in roomCells)
        {
            foreach (Vector2Int d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
            {
                Vector2Int o = c + d;
                if (map.In(o.x, o.y) && map.Walk[o.x, o.y])
                    continue;
                Vector3 dir = new Vector3(d.x, 0f, d.y);
                Vector3 w = map.World(c.x, c.y);
                // Parede de verdade em toda a altura; o plano usado e o ponto mais saliente
                // (rodape, lambri), para o movel nao entrar nele.
                float dist = float.MaxValue, far = 0f;
                bool ok = true;
                foreach (float hgt in relaxed ? new[] { 0.06f, 0.3f, 0.7f, 1.0f } : new[] { 0.06f, 0.3f, 0.7f, 1.0f, 1.5f, 2.0f })
                {
                    if (!Physics.Raycast(new Vector3(w.x, room.Y + hgt, w.z), dir, out RaycastHit hit, 1.2f, solidMask, QueryTriggerInteraction.Ignore) ||
                        hit.collider.GetComponentInParent<Door>() != null)
                    { ok = false; break; }
                    Vector3 sz = hit.collider.bounds.size;
                    if (hgt >= 1.0f && (Vector3.Dot(hit.normal, -dir) < 0.95f || (Mathf.Max(sz.x, sz.z) < 2.5f && sz.y < 2.3f))) { ok = false; break; }
                    dist = Mathf.Min(dist, hit.distance);
                    far = Mathf.Max(far, hit.distance);
                }
                if (!ok || far - dist > 0.35f) continue;
                Vector3 hitPoint = new Vector3(w.x, 0f, w.z) + dir * dist;
                float plane = Vector3.Dot(hitPoint, dir);
                float along = Vector3.Dot(w, new Vector3(-dir.z, 0f, dir.x));
                slots.Add((-dir, plane, along));
            }
        }

        var runs = new List<WallRun>();
        foreach (var g in slots.GroupBy(s => (s.n, Mathf.Round(s.plane * 10f))))
        {
            var list = g.OrderBy(s => s.along).ToList();
            WallRun run = null;
            float last = float.MinValue;
            foreach (var s in list)
            {
                if (run == null || s.along - last > RoomMap.Cell * 1.5f)
                {
                    run = new WallRun { Normal = s.n, Along = new Vector3(s.n.z, 0f, -s.n.x), Plane = -s.plane, From = s.along, To = s.along, Y = room.Y };
                    runs.Add(run);
                }
                run.To = s.along;
                last = s.along;
            }
        }
        // Along = (-dir.z, dir.x) com dir = -Normal  =>  (Normal.z, -Normal.x)
        return runs.Where(r => r.To - r.From > 0.9f).OrderByDescending(r => r.To - r.From).ToList().AsReadOnly().ToList();
    }

    private static Vector3 WallPoint(WallRun run, float along, float offset)
    {
        // Ponto na parede: dot(p, Normal) = Plane e dot(p, Along) = along (eixos horizontais ortogonais).
        Vector3 basePoint = run.Normal * run.Plane + run.Along * along;
        return new Vector3(basePoint.x, run.Y, basePoint.z) + run.Normal * offset;
    }

    // Enche trechos de parede com uma sequencia de moveis lado a lado.
    private static void WallRuns(Transform parent, string[] keys, int maxRuns, float gapMin, float gapMax, bool needFront, int maxPieces = 99, System.Action<Transform, Placed> dress = null, float shelfAbove = 0f, bool relaxed = false)
    {
        int placedCount = 0, runsUsed = 0;
        foreach (WallRun run in FindWalls(relaxed))
        {
            if (runsUsed >= maxRuns || placedCount >= maxPieces)
                break;
            float cursor = run.From - 0.35f;
            int k = rng.Next(keys.Length);
            bool usedRun = false;
            while (cursor < run.To + 0.35f && placedCount < maxPieces)
            {
                string key = keys[(k + rng.Next(2)) % keys.Length];
                Placed p = Create(parent, key, Vector3.zero, run.Normal);
                float width = Mathf.Abs(Vector3.Dot(p.Rot * Vector3.right, run.Along)) > 0.5f ? p.Half.x : p.Half.z;
                float depth = Mathf.Abs(Vector3.Dot(p.Rot * Vector3.forward, run.Normal)) > 0.5f ? p.Half.z : p.Half.x;
                Vector3 pos = WallPoint(run, cursor + width, depth + 0.05f);
                Vector3 c = p.Center;
                p.Go.transform.position += new Vector3(pos.x - c.x, run.Y - (c.y - p.Half.y), pos.z - c.z);
                if (Accept(p, needFront))
                {
                    dress?.Invoke(parent, p);
                    if (shelfAbove > 0f && p.Item.Collider && p.Half.y * 2f < 1.15f * WorldScale && rng.NextDouble() < shelfAbove)
                        ShelfAbove(parent, run, p);
                    placedCount++;
                    usedRun = true;
                    cursor += width * 2f + Mathf.Lerp(gapMin, gapMax, (float)rng.NextDouble());
                    k += 1 + rng.Next(keys.Length);
                }
                else
                {
                    cursor += 0.25f;
                }
            }
            if (usedRun)
                runsUsed++;
        }
    }

    // Uma mesa de trabalho encostada na parede, com cadeira na frente.
    private static void WallDesk(Transform parent, string key, System.Action<Transform, Placed> dress)
    {
        foreach (WallRun run in FindWalls())
        {
            float mid = (run.From + run.To) * 0.5f;
            for (float off = 0f; off < (run.To - run.From) * 0.5f; off += 0.25f)
                foreach (float s in new[] { 1f, -1f })
                {
                    Placed p = Create(parent, key, Vector3.zero, run.Normal);
                    float depth = Mathf.Abs(Vector3.Dot(p.Rot * Vector3.forward, run.Normal)) > 0.5f ? p.Half.z : p.Half.x;
                    Vector3 pos = WallPoint(run, mid + off * s, depth + 0.05f);
                    Vector3 c = p.Center;
                    p.Go.transform.position += new Vector3(pos.x - c.x, run.Y - (c.y - p.Half.y), pos.z - c.z);
                    if (Accept(p, false))
                    {
                        Seat(parent, p, "chairE", -run.Normal * -1f, 0f);
                        dress(parent, p);
                        return;
                    }
                }
        }
    }

    // ---------- Itens soltos ----------

    private static void Scatter(Transform parent, string[] keys, int count)
    {
        int placed = 0;
        foreach (WallRun run in FindWalls())
        {
            if (placed >= count) break;
            // Cantos: comeco ou fim do trecho de parede.
            foreach (float along in new[] { run.From - 0.2f, run.To + 0.2f, (run.From + run.To) * 0.5f })
            {
                if (placed >= count) break;
                string key = keys[rng.Next(keys.Length)];
                Placed p = Create(parent, key, Vector3.zero, run.Normal, (float)(rng.NextDouble() * 360));
                float r = Mathf.Max(p.Half.x, p.Half.z);
                Vector3 pos = WallPoint(run, along + (along < run.From ? r : -r), r + 0.03f);
                Vector3 c = p.Center;
                p.Go.transform.position += new Vector3(pos.x - c.x, run.Y - (c.y - p.Half.y), pos.z - c.z);
                if (Accept(p, false)) placed++;
            }
        }
    }

    // Corredor: poucas pecas rasas, espacadas, sempre deixando passagem larga.
    private static void Sparse(Transform parent, string[] keys, float spacing)
    {
        int k = rng.Next(keys.Length);
        foreach (WallRun run in FindWalls().Where(r => r.To - r.From > 1.5f))
        {
            for (float along = run.From + 0.8f; along < run.To - 0.5f; along += spacing + (float)rng.NextDouble() * 1.5f)
            {
                string key = keys[(k++ + rng.Next(3)) % keys.Length];
                if (key == "seats")
                {
                    // Duas cadeiras lado a lado, de frente para o corredor.
                    string chair = rng.NextDouble() < 0.5 ? "chairA" : "chairD";
                    foreach (float s2 in new[] { -0.3f * WorldScale, 0.3f * WorldScale })
                    {
                        Placed ch = Create(parent, chair, Vector3.zero, run.Normal);
                        float dd = DepthAlong(ch, run.Normal);
                        Vector3 cp = WallPoint(run, along + s2, dd + 0.05f);
                        Vector3 cc = ch.Center;
                        ch.Go.transform.position += new Vector3(cp.x - cc.x, run.Y - (cc.y - ch.Half.y), cp.z - cc.z);
                        Accept(ch, false);
                    }
                    continue;
                }
                Placed p = Create(parent, key, Vector3.zero, run.Normal);
                float depth = Mathf.Abs(Vector3.Dot(p.Rot * Vector3.forward, run.Normal)) > 0.5f ? p.Half.z : p.Half.x;
                if (depth * 2f > 0.75f * WorldScale) { Object.DestroyImmediate(p.Go); continue; }
                Vector3 pos = WallPoint(run, along, depth + 0.05f);
                Vector3 c = p.Center;
                p.Go.transform.position += new Vector3(pos.x - c.x, run.Y - (c.y - p.Half.y), pos.z - c.z);
                Accept(p, false);
            }
        }
    }

    // Caixas empilhadas com cuidado (a de cima menor, apoiada na de baixo).
    private static void Stacks(Transform parent, int count)
    {
        int placed = 0;
        foreach (WallRun run in FindWalls())
        {
            if (placed >= count) break;
            for (float along = run.From; along < run.To && placed < count; along += 0.25f)
            {
                Placed bottom = Create(parent, "crateA", Vector3.zero, run.Normal);
                Vector3 pos = WallPoint(run, along, bottom.Half.z + 0.02f);
                Vector3 c = bottom.Center;
                bottom.Go.transform.position += new Vector3(pos.x - c.x, run.Y - (c.y - bottom.Half.y), pos.z - c.z);
                if (!Accept(bottom, false)) continue;
                placed++;
                along += 1.2f;
                Placed top = Create(parent, "cardboardA", Vector3.zero, run.Normal, (float)(rng.NextDouble() * 20 - 10));
                Vector3 tc = top.Center;
                Vector3 target = bottom.Center + Vector3.up * bottom.Half.y;
                top.Go.transform.position += new Vector3(target.x - tc.x, target.y - (tc.y - top.Half.y) + 0.002f, target.z - tc.z);
                if (Physics.CheckBox(top.Center + Vector3.up * 0.01f, top.Half - new Vector3(0.01f, 0.02f, 0.01f), top.Rot, solidMask, QueryTriggerInteraction.Ignore) ||
                    top.Half.x > bottom.Half.x || top.Half.z > bottom.Half.z)
                    Object.DestroyImmediate(top.Go);
                else
                    Finish(top);
            }
        }
    }

    private static void WallClock(Transform parent)
    {
        foreach (WallRun run in FindWalls())
        {
            float mid = (run.From + run.To) * 0.5f;
            Vector3 from = WallPoint(run, mid, 0.5f) + Vector3.up * 2.1f * WorldScale;
            if (!Physics.Raycast(from, -run.Normal, out RaycastHit hit, 1.2f, solidMask, QueryTriggerInteraction.Ignore) || Vector3.Dot(hit.normal, run.Normal) < 0.95f)
                continue;
            Placed p = Create(parent, "clock", Vector3.zero, run.Normal);
            // O relogio: eixo fino vira a normal; a textura mostra a frente do lado oposto ao eixo fino.
            Bounds lb = p.Local;
            Vector3 thin = lb.size.x <= lb.size.y && lb.size.x <= lb.size.z ? Vector3.right : (lb.size.y <= lb.size.z ? Vector3.up : Vector3.forward);
            Quaternion rot = Quaternion.FromToRotation(-thin, run.Normal);
            Vector3 up = rot * (thin == Vector3.up ? Vector3.forward : Vector3.up);
            rot = Quaternion.AngleAxis(Vector3.SignedAngle(up, Vector3.up, run.Normal), run.Normal) * rot;
            p.Go.transform.rotation = rot;
            Vector3 half = new Vector3(Mathf.Abs((rot * Vector3.right).x) * p.Half.x, 0f, 0f);
            float d = Vector3.Dot(new Vector3(Mathf.Abs((rot * Vector3.right).x), Mathf.Abs((rot * Vector3.up).x), Mathf.Abs((rot * Vector3.forward).x)), p.Half);
            float dz = Vector3.Dot(new Vector3(Mathf.Abs((rot * Vector3.right).z), Mathf.Abs((rot * Vector3.up).z), Mathf.Abs((rot * Vector3.forward).z)), p.Half);
            float depth = Mathf.Abs(run.Normal.x) > 0.5f ? d : dz;
            Vector3 c = p.Center;
            Vector3 target = hit.point + run.Normal * (depth + 0.005f);
            p.Go.transform.position += target - c;
            if (Physics.CheckBox(p.Center, p.Half * 0.9f, p.Rot, solidMask, QueryTriggerInteraction.Ignore))
            {
                Object.DestroyImmediate(p.Go);
                continue;
            }
            Finish(p);
            return;
        }
    }

    // ---------- Itens em cima dos moveis ----------

    private static void DressLab(Transform parent, Placed host) => Dress(parent, host, new[] { "lamp", "book1", "book2", "mug", "notes", "booksrow", "globe" }, 3, false);
    private static void DressDesk(Transform parent, Placed host) => Dress(parent, host, new[] { "lamp", "phone", "mug", "ashtray", "notes", "book1" }, 3, true);
    private static void DressDirector(Transform parent, Placed host) => Dress(parent, host, new[] { "lamp", "phone", "globe", "book1", "book2", "ashtray" }, 4, false);
    private static void DressTable(Transform parent, Placed host) => Dress(parent, host, new[] { "mug", "mug", "ashtray" }, 2, false);
    private static void DressCounter(Transform parent, Placed host)
    {
        if (host.Item.Key != "fridge" && host.Item.Key != "fridgeB")
            Dress(parent, host, host.Item.Key == "oven" ? new[] { "pan", "pot", "kettle" } : CounterItems, rng.Next(1, 4), false);
    }

    private static void Dress(Transform parent, Placed host, string[] keys, int count, bool terminal)
    {
        var temp = new List<MeshCollider>();
        foreach (MeshFilter mf in host.Go.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null || mf.name.EndsWith("_LOD1") || mf.name.EndsWith("_LOD2")) continue;
            MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            temp.Add(mc);
        }
        Physics.SyncTransforms();

        var rects = new List<(Vector3 c, Vector3 h, Quaternion r)>();
        float top = host.Center.y + host.Half.y;

        // Terminal (CRT + teclado) virado para o lado da cadeira.
        if (terminal)
        {
            PlaceOnTop(parent, host, temp, rects, "crt", host.Center - host.Front * 0.12f, host.Front, top);
            PlaceOnTop(parent, host, temp, rects, "keyboard", host.Center + host.Front * 0.22f, host.Front, top);
        }

        for (int i = 0, tries = 0; i < count && tries < 40; tries++)
        {
            string key = keys[rng.Next(keys.Length)];
            Vector3 local = new Vector3((float)rng.NextDouble() * 2f - 1f, 0f, (float)rng.NextDouble() * 2f - 1f);
            Vector3 p = host.Center + host.Rot * Vector3.Scale(local, new Vector3(host.Half.x - 0.12f, 0f, host.Half.z - 0.12f));
            Vector3 face = Quaternion.Euler(0f, (float)rng.NextDouble() * 80f - 40f, 0f) * host.Front;
            if (PlaceOnTop(parent, host, temp, rects, key, p, face, top))
                i++;
        }

        foreach (MeshCollider mc in temp)
            Object.DestroyImmediate(mc);
        Physics.SyncTransforms();
    }

    private static bool PlaceOnTop(Transform parent, Placed host, List<MeshCollider> surfaces, List<(Vector3 c, Vector3 h, Quaternion r)> rects, string key, Vector3 at, Vector3 face, float top)
    {
        Placed item = Create(parent, key, Vector3.zero, face);
        Vector3 h = item.Half;
        Quaternion r = item.Rot;
        // Os 4 cantos e o centro precisam bater no tampo, na mesma altura (superficie plana e de cima).
        float y = float.NaN;
        for (int i = 0; i < 5; i++)
        {
            Vector3 o = i == 4 ? Vector3.zero : new Vector3((i & 1) == 0 ? -1 : 1, 0f, (i & 2) == 0 ? -1 : 1);
            Vector3 q = at + r * Vector3.Scale(o, new Vector3(h.x, 0f, h.z));
            Ray ray = new Ray(new Vector3(q.x, top + 0.3f, q.z), Vector3.down);
            float best = float.NegativeInfinity;
            foreach (MeshCollider mc in surfaces)
                if (mc.Raycast(ray, out RaycastHit hit, 0.6f) && hit.normal.y > 0.95f)
                    best = Mathf.Max(best, hit.point.y);
            if (float.IsNegativeInfinity(best) || best < top - 0.06f || (!float.IsNaN(y) && Mathf.Abs(best - y) > 0.012f))
            {
                Object.DestroyImmediate(item.Go);
                return false;
            }
            y = float.IsNaN(y) ? best : Mathf.Max(y, best);
        }
        // Sem encostar em outro item (2 cm de folga) e nada em cima.
        foreach (var o in rects)
            if (Overlap2D(at, h + new Vector3(0.02f, 0f, 0.02f), r, o.c, o.h, o.r))
            {
                Object.DestroyImmediate(item.Go);
                return false;
            }
        Vector3 c = item.Center;
        item.Go.transform.position += new Vector3(at.x - c.x, y + 0.001f - (c.y - h.y), at.z - c.z);
        if (Physics.CheckBox(item.Center + Vector3.up * 0.01f, h - new Vector3(0f, 0.005f, 0f), r, solidMask, QueryTriggerInteraction.Ignore))
        {
            Object.DestroyImmediate(item.Go);
            return false;
        }
        rects.Add((item.Center, h, r));
        Finish(item);
        return true;
    }

    private static bool Overlap2D(Vector3 ac, Vector3 ah, Quaternion ar, Vector3 bc, Vector3 bh, Quaternion br)
    {
        Vector2[] axes =
        {
            Flat(ar * Vector3.right), Flat(ar * Vector3.forward), Flat(br * Vector3.right), Flat(br * Vector3.forward),
        };
        Vector2 d = new Vector2(bc.x - ac.x, bc.z - ac.z);
        foreach (Vector2 axis in axes)
        {
            float ra = Mathf.Abs(Vector2.Dot(Flat(ar * Vector3.right), axis)) * ah.x + Mathf.Abs(Vector2.Dot(Flat(ar * Vector3.forward), axis)) * ah.z;
            float rb = Mathf.Abs(Vector2.Dot(Flat(br * Vector3.right), axis)) * bh.x + Mathf.Abs(Vector2.Dot(Flat(br * Vector3.forward), axis)) * bh.z;
            if (Mathf.Abs(Vector2.Dot(d, axis)) > ra + rb)
                return false;
        }
        return true;
    }

    private static Vector2 Flat(Vector3 v) => new Vector2(v.x, v.z).normalized;

    // ---------- LOD ----------

    private static void AddLods(Placed p)
    {
        bool small = !p.Item.Collider;
        var lod0 = new List<Renderer>();
        var lod1 = new List<Renderer>();
        var lod2 = new List<Renderer>();
        foreach (MeshRenderer r in p.Go.GetComponentsInChildren<MeshRenderer>(true))
        {
            MeshFilter mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            lod0.Add(r);
            r.shadowCastingMode = small ? ShadowCastingMode.Off : ShadowCastingMode.On;
            Mesh[] meshes = LodMeshes(mf.sharedMesh);
            lod1.Add(LodCopy(r, meshes[0] != null ? meshes[0] : mf.sharedMesh, "_LOD1", small ? ShadowCastingMode.Off : ShadowCastingMode.On));
            if (!small)
                lod2.Add(LodCopy(r, meshes[1] != null ? meshes[1] : meshes[0] != null ? meshes[0] : mf.sharedMesh, "_LOD2", ShadowCastingMode.Off));
        }
        if (lod0.Count == 0) return;
        LODGroup group = p.Go.AddComponent<LODGroup>();
        LOD[] lods = small
            ? new[] { new LOD(0.05f, lod0.ToArray()), new LOD(0.018f, lod1.ToArray()) }
            : new[] { new LOD(0.3f, lod0.ToArray()), new LOD(0.1f, lod1.ToArray()), new LOD(0.025f, lod2.ToArray()) };
        group.SetLODs(lods);
        group.fadeMode = LODFadeMode.None;
        group.RecalculateBounds();
    }

    private static Renderer LodCopy(MeshRenderer src, Mesh mesh, string suffix, ShadowCastingMode shadows)
    {
        GameObject copy = new GameObject(src.gameObject.name + suffix, typeof(MeshFilter), typeof(MeshRenderer));
        copy.transform.SetParent(src.transform, false);
        copy.GetComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer r = copy.GetComponent<MeshRenderer>();
        r.sharedMaterials = src.sharedMaterials;
        r.shadowCastingMode = shadows;
        return r;
    }

    // Malhas reduzidas (MeshLodUtility da Unity 6.2): nivel 1 ~ metade, nivel 2 ~ um quarto dos triangulos.
    private static Mesh[] LodMeshes(Mesh src)
    {
        if (lodCache.TryGetValue(src, out Mesh[] cached))
            return cached;
        var result = new Mesh[2];
        lodCache[src] = result;
        if (!AssetDatabase.IsValidFolder(LodFolder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Art/Props")) AssetDatabase.CreateFolder("Assets/Art", "Props");
            AssetDatabase.CreateFolder("Assets/Art/Props", "LOD");
        }

        string id = src.name.Replace(" ", "_") + "_" + (AssetDatabase.GetAssetPath(src).GetHashCode() & 0xffff).ToString("x4");
        for (int k = 0; k < 2; k++)
        {
            string path = LodFolder + "/" + id + "_LOD" + (k + 1) + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) result[k] = existing;
        }
        if (result[0] != null)
            return result;

        int tris = 0;
        for (int s = 0; s < src.subMeshCount; s++) tris += (int)src.GetIndexCount(s) / 3;
        if (tris < 150)
            return result; // malha ja e simples: LOD1 usa a mesma, sem sombra

        try
        {
            Mesh work = Object.Instantiate(src);
            MeshLodUtility.GenerateMeshLods(work, 6);
            int count = work.lodCount;
            int[] all;
            using (Mesh.MeshDataArray data = Mesh.AcquireReadOnlyMeshData(work))
            {
                Mesh.MeshData md = data[0];
                if (md.indexFormat == IndexFormat.UInt32)
                    all = md.GetIndexData<int>().ToArray();
                else
                    all = md.GetIndexData<ushort>().ToArray().Select(v => (int)v).ToArray();
            }
            for (int k = 0; k < 2; k++)
            {
                int level = Mathf.Min(k + 1, count - 1);
                if (level <= 0) continue;
                Mesh m = Object.Instantiate(src);
                m.name = src.name + "_LOD" + (k + 1);
                int newTris = 0;
                for (int s = 0; s < work.subMeshCount; s++)
                {
                    MeshLodRange range = work.GetLod(s, level);
                    SubMeshDescriptor desc = work.GetSubMesh(s);
                    int start = (int)range.indexStart;
                    if (start + (int)range.indexCount > all.Length) start += desc.indexStart;
                    int[] idx = new int[range.indexCount];
                    System.Array.Copy(all, start, idx, 0, idx.Length);
                    m.SetIndices(idx, MeshTopology.Triangles, s, false, desc.baseVertex);
                    newTris += idx.Length / 3;
                }
                if (newTris >= tris * 0.9f || newTris < 4)
                {
                    Object.DestroyImmediate(m);
                    continue;
                }
                AssetDatabase.CreateAsset(m, LodFolder + "/" + id + "_LOD" + (k + 1) + ".asset");
                result[k] = m;
                report.AppendLine("  LOD " + src.name + ": " + tris + " -> " + newTris + " triangulos (nivel " + level + ")");
            }
            Object.DestroyImmediate(work);
        }
        catch (System.Exception e)
        {
            report.AppendLine("  LOD falhou em " + src.name + ": " + e.Message);
        }
        return result;
    }

    // ---------- Occlusion ----------

    public static void BakeOcclusion()
    {
        StaticOcclusionCulling.smallestOccluder = 1.5f;
        StaticOcclusionCulling.smallestHole = 0.25f;
        StaticOcclusionCulling.backfaceThreshold = 100f;
        bool ok = StaticOcclusionCulling.Compute();
        report.AppendLine("occlusion culling: " + (ok ? "refeito" : "FALHOU"));
    }

    // ---------- Auditoria ----------

    // Confere cada peca da Mobilia: apoio embaixo, sobreposicao com qualquer colisor (inclusive
    // outros moveis), colisor do tamanho da malha, LODGroup completo e flags de occlusion.
    public static void Audit()
    {
        GameObject rootGo = GameObject.Find(RootName);
        if (rootGo == null) return;
        int mask = ~LayerMask.GetMask("Ignore Raycast", "UI", "Arms", "PhotoCameraEffect", "GlobalVolume", "Enemy", "Temporal", "TransparentFX", "Player");
        int pieces = 0, floating = 0, overlaps = 0, badCollider = 0, noLod = 0, noOcclusion = 0, occluders = 0;
        var problems = new StringBuilder();
        var perRoom = new StringBuilder();
        Physics.SyncTransforms();
        foreach (Transform roomT in rootGo.transform)
        {
            perRoom.AppendLine("  " + roomT.name + ": " + string.Join(" ", roomT.Cast<Transform>().GroupBy(t => t.name).Select(g => g.Key + "x" + g.Count())));
            foreach (Transform t in roomT)
            {
                pieces++;
                GameObject go = t.gameObject;
                Bounds lb = LocalBounds(go);
                Vector3 c = t.TransformPoint(lb.center), h = Vector3.Scale(lb.extents, t.lossyScale);
                float bottom = c.y - h.y;

                // Apoio: algo solido logo embaixo do centro (chao ou movel).
                var own = go.GetComponentsInChildren<Collider>();
                bool supported = Physics.RaycastAll(new Vector3(c.x, bottom + 0.05f, c.z), Vector3.down, 0.08f, mask, QueryTriggerInteraction.Ignore).Any(hh => !own.Contains(hh.collider));
                if (!supported)
                {
                    // Itens de mesa: apoio na malha do movel embaixo (sem colisor proprio).
                    supported = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Length > 0 && SupportedByMesh(go, new Vector3(c.x, bottom, c.z));
                }
                if (!supported && FurnitureCatalog.Get(t.name).WallMounted)
                    supported = Physics.CheckSphere(c, Mathf.Max(h.x, h.z) + 0.03f, mask, QueryTriggerInteraction.Ignore); // relogio preso na parede
                if (!supported) { floating++; problems.AppendLine("    flutuando: " + roomT.name + "/" + t.name + " " + c.ToString("F2")); }

                if (go.GetComponent<BoxCollider>() != null)
                {
                    var hits = Physics.OverlapBox(c + Vector3.up * 0.03f, new Vector3(h.x - 0.015f, h.y - 0.04f, h.z - 0.015f), t.rotation, mask, QueryTriggerInteraction.Ignore).Where(x => !own.Contains(x)).ToArray();
                    if (hits.Length > 0) { overlaps++; problems.AppendLine("    sobrepoe: " + roomT.name + "/" + t.name + " x " + hits[0].name); }
                    BoxCollider box = go.GetComponent<BoxCollider>();
                    if ((box.size - lb.size).magnitude > 0.01f || (box.center - lb.center).magnitude > 0.01f) badCollider++;
                }
                LODGroup g = go.GetComponent<LODGroup>();
                if (g == null || g.lodCount < 2 || g.GetLODs().Any(l => l.renderers.Length == 0 || l.renderers.Any(r => r == null))) noLod++;
                var flags = GameObjectUtility.GetStaticEditorFlags(go.GetComponentInChildren<MeshRenderer>().gameObject);
                if ((flags & StaticEditorFlags.OccludeeStatic) == 0) noOcclusion++;
                if ((flags & StaticEditorFlags.OccluderStatic) != 0) occluders++;
            }
        }
        report.AppendLine("AUDITORIA: " + pieces + " pecas | flutuando=" + floating + " | sobrepostas=" + overlaps + " | colisor errado=" + badCollider +
                          " | sem LOD=" + noLod + " | sem occludee=" + noOcclusion + " | ocultadores=" + occluders);
        report.Append(problems);
        report.AppendLine("CONTEUDO:");
        report.Append(perRoom);
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/revisao/qa/salas"));
        File.WriteAllText(Path.Combine(folder, "mobilia.txt"), report.ToString());
    }

    private static bool SupportedByMesh(GameObject item, Vector3 bottom)
    {
        Transform roomT = item.transform.parent;
        foreach (Transform other in roomT)
        {
            if (other == item.transform) continue;
            Renderer[] ors = other.GetComponentsInChildren<Renderer>();
            if (ors.Length == 0) continue;
            Bounds ob = ors[0].bounds; foreach (Renderer r in ors) ob.Encapsulate(r.bounds);
            if (bottom.x < ob.min.x || bottom.x > ob.max.x || bottom.z < ob.min.z || bottom.z > ob.max.z || bottom.y < ob.min.y || bottom.y > ob.max.y + 0.05f) continue;
            var temp = new List<MeshCollider>();
            foreach (MeshFilter mf in other.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null || mf.name.EndsWith("_LOD1") || mf.name.EndsWith("_LOD2")) continue;
                MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                temp.Add(mc);
            }
            Physics.SyncTransforms();
            bool hit = temp.Any(mc => mc.Raycast(new Ray(bottom + Vector3.up * 0.05f, Vector3.down), out RaycastHit hh, 0.08f));
            foreach (MeshCollider mc in temp) Object.DestroyImmediate(mc);
            if (hit) return true;
        }
        return false;
    }

    // ---------- Fotos de conferencia ----------

    // Por sala: vista de cima (sem teto) e vista na altura dos olhos, sem pos-processamento.
    public static void ReviewShots()
    {
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/revisao/qa/salas/fotos"));
        Directory.CreateDirectory(folder);
        foreach (string f in Directory.GetFiles(folder, "*.png")) File.Delete(f);

        RenderSettings.ambientMode = AmbientMode.Flat;
        Color ambient = RenderSettings.ambientLight;
        RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.52f);
        GameObject sun = new GameObject("ReviewSun", typeof(Light));
        Light sl = sun.GetComponent<Light>();
        sl.type = LightType.Directional;
        sl.intensity = 0.9f;
        sl.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(65f, 35f, 0f);

        GameObject camGo = new GameObject("ReviewCam", typeof(Camera));
        Camera cam = camGo.GetComponent<Camera>();
        camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.nearClipPlane = 0.05f;

        foreach (RoomDef r in Rooms)
        {
            Rect a = r.Area;
            // Esconde teto e o que esta acima (vista de cima).
            var hidden = new List<Renderer>();
            foreach (Renderer rd in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if (rd.enabled && rd.bounds.min.y > r.Y + 2.15f && rd.bounds.max.y > r.Y)
                { rd.enabled = false; hidden.Add(rd); }

            float w = a.width + 2f, h = a.height + 2f;
            int pw = Mathf.Clamp(Mathf.RoundToInt(w * 40f), 300, 1400);
            int ph = Mathf.Clamp(Mathf.RoundToInt(pw * h / w), 200, 1400);
            pw = Mathf.RoundToInt(ph * w / h);
            var top = Shot(cam, pw, ph, true, new Vector3(a.center.x, r.Y + 2.1f, a.center.y), Quaternion.Euler(90f, 0f, 0f), h * 0.5f);
            foreach (Renderer rd in hidden) rd.enabled = true;

            // Altura dos olhos: de um canto da sala olhando para o canto oposto.
            Vector3 eye = new Vector3(a.xMin + 0.6f, r.Y + 1.6f, a.yMin + 0.6f);
            Vector3 look = new Vector3(a.xMax, r.Y + 0.6f, a.yMax);
            if (a.width < 6f || a.height < 6f)
            {
                bool longX = a.width > a.height;
                eye = longX ? new Vector3(a.xMin + 0.4f, r.Y + 1.6f, a.center.y) : new Vector3(a.center.x, r.Y + 1.6f, a.yMin + 0.4f);
                look = longX ? new Vector3(a.xMax, r.Y + 0.7f, a.center.y) : new Vector3(a.center.x, r.Y + 0.7f, a.yMax);
            }
            var fills = new List<GameObject>();
            for (float fx = a.xMin + 2f; fx <= a.xMax; fx += 4f)
                for (float fz = a.yMin + 2f; fz <= a.yMax; fz += 4f)
                {
                    GameObject fill = new GameObject("ReviewFill", typeof(Light));
                    Light fl = fill.GetComponent<Light>();
                    fl.type = LightType.Point;
                    fl.range = 6f;
                    fl.intensity = 3f;
                    fill.transform.position = new Vector3(fx, r.Y + 2f, fz);
                    fills.Add(fill);
                }
            var eyeShot = Shot(cam, 960, 600, false, eye, Quaternion.LookRotation(look - eye), 0f);
            foreach (GameObject fill in fills) Object.DestroyImmediate(fill);

            File.WriteAllBytes(Path.Combine(folder, r.Id + "_cima.png"), top.EncodeToPNG());
            File.WriteAllBytes(Path.Combine(folder, r.Id + "_olhos.png"), eyeShot.EncodeToPNG());
            Object.DestroyImmediate(top);
            Object.DestroyImmediate(eyeShot);
        }
        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(sun);
        RenderSettings.ambientLight = ambient;
    }

    private static Texture2D Shot(Camera cam, int w, int h, bool ortho, Vector3 pos, Quaternion rot, float orthoSize)
    {
        var rt = new RenderTexture(w, h, 24);
        cam.targetTexture = rt;
        cam.orthographic = ortho;
        if (ortho) { cam.orthographicSize = orthoSize; cam.farClipPlane = 4f; }
        else { cam.fieldOfView = 70f; cam.farClipPlane = 80f; }
        cam.aspect = (float)w / h;
        cam.transform.SetPositionAndRotation(pos, rot);
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        return tex;
    }
}
