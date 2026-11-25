using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;

// Planta da fase: grade sobre o NavMesh de um andar, separada em salas pelas portas.
// Cada sala = regiao conectada de chao andavel (as portas cortam a ligacao).
public class RoomMap
{
    public const float Cell = 0.25f;

    public class Room
    {
        public int Id;
        public List<Vector2Int> Cells = new List<Vector2Int>();
        public Vector2Int Min, Max;
        public float Area => Cells.Count * Cell * Cell;
        public Vector3 Center;
        public List<int> Doors = new List<int>();
        public string Kind = "";
    }

    public float FloorY;
    public Vector2 Origin;
    public int W, H;
    public bool[,] Walk;
    public int[,] Label;
    public bool[,] DoorCell;
    public List<Room> Rooms = new List<Room>();
    public List<Bounds> DoorBounds = new List<Bounds>();

    public Vector3 World(int x, int z) => new Vector3(Origin.x + (x + 0.5f) * Cell, FloorY, Origin.y + (z + 0.5f) * Cell);
    public Vector2Int GridOf(Vector3 p) => new Vector2Int(Mathf.FloorToInt((p.x - Origin.x) / Cell), Mathf.FloorToInt((p.z - Origin.y) / Cell));
    public bool In(int x, int z) => x >= 0 && z >= 0 && x < W && z < H;

    public static List<float> Floors()
    {
        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        var counts = new Dictionary<float, int>();
        foreach (Vector3 v in tri.vertices)
        {
            float y = Mathf.Round(v.y * 2f) / 2f;
            counts[y] = counts.TryGetValue(y, out int c) ? c + 1 : 1;
        }
        return counts.Where(p => p.Value > 40).Select(p => p.Key).OrderBy(y => y).ToList();
    }

    public static RoomMap Build(float floorY)
    {
        var map = new RoomMap { FloorY = floorY };
        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector3 v in tri.vertices)
        {
            if (Mathf.Abs(v.y - floorY) > 1f)
                continue;
            min = Vector2.Min(min, new Vector2(v.x, v.z));
            max = Vector2.Max(max, new Vector2(v.x, v.z));
        }
        map.Origin = min - Vector2.one;
        map.W = Mathf.CeilToInt((max.x - min.x + 2f) / Cell);
        map.H = Mathf.CeilToInt((max.y - min.y + 2f) / Cell);
        map.Walk = new bool[map.W, map.H];
        map.DoorCell = new bool[map.W, map.H];
        map.Label = new int[map.W, map.H];

        for (int x = 0; x < map.W; x++)
            for (int z = 0; z < map.H; z++)
            {
                Vector3 p = map.World(x, z);
                if (NavMesh.SamplePosition(p, out NavMeshHit hit, 0.6f, NavMesh.AllAreas) &&
                    Mathf.Abs(hit.position.y - floorY) < 0.45f &&
                    new Vector2(hit.position.x - p.x, hit.position.z - p.z).sqrMagnitude < 0.15f * 0.15f)
                    map.Walk[x, z] = true;
            }

        foreach (Door door in Object.FindObjectsByType<Door>(FindObjectsSortMode.None))
        {
            Renderer[] rs = door.GetComponentsInChildren<Renderer>();
            Collider[] cs = door.GetComponentsInChildren<Collider>();
            Bounds b;
            if (rs.Length > 0) { b = rs[0].bounds; foreach (Renderer r in rs) b.Encapsulate(r.bounds); }
            else if (cs.Length > 0) { b = cs[0].bounds; foreach (Collider c in cs) b.Encapsulate(c.bounds); }
            else b = new Bounds(door.transform.position, new Vector3(1f, 2f, 1f));
            if (Mathf.Abs(b.min.y - floorY) > 1.2f)
                continue;
            b.Expand(new Vector3(0.7f, 0f, 0.7f));
            map.DoorBounds.Add(b);
            Vector2Int a = map.GridOf(b.min), c2 = map.GridOf(b.max);
            for (int x = a.x; x <= c2.x; x++)
                for (int z = a.y; z <= c2.y; z++)
                    if (map.In(x, z))
                        map.DoorCell[x, z] = true;
        }

        // Regioes (4-vizinhos), portas cortam.
        int next = 0;
        var queue = new Queue<Vector2Int>();
        for (int x = 0; x < map.W; x++)
            for (int z = 0; z < map.H; z++)
            {
                if (!map.Walk[x, z] || map.DoorCell[x, z] || map.Label[x, z] != 0)
                    continue;
                var room = new Room { Id = ++next, Min = new Vector2Int(x, z), Max = new Vector2Int(x, z) };
                map.Label[x, z] = room.Id;
                queue.Enqueue(new Vector2Int(x, z));
                while (queue.Count > 0)
                {
                    Vector2Int c = queue.Dequeue();
                    room.Cells.Add(c);
                    room.Min = Vector2Int.Min(room.Min, c);
                    room.Max = Vector2Int.Max(room.Max, c);
                    foreach (Vector2Int d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                    {
                        Vector2Int n = c + d;
                        if (map.In(n.x, n.y) && map.Walk[n.x, n.y] && !map.DoorCell[n.x, n.y] && map.Label[n.x, n.y] == 0)
                        {
                            map.Label[n.x, n.y] = room.Id;
                            queue.Enqueue(n);
                        }
                    }
                }
                Vector3 sum = Vector3.zero;
                foreach (Vector2Int c in room.Cells) sum += map.World(c.x, c.y);
                room.Center = sum / room.Cells.Count;
                map.Rooms.Add(room);
            }

        // Ligacoes pelas portas.
        for (int i = 0; i < map.DoorBounds.Count; i++)
        {
            Bounds b = map.DoorBounds[i];
            b.Expand(new Vector3(0.6f, 0f, 0.6f));
            Vector2Int a = map.GridOf(b.min), c = map.GridOf(b.max);
            var touched = new HashSet<int>();
            for (int x = a.x; x <= c.x; x++)
                for (int z = a.y; z <= c.y; z++)
                    if (map.In(x, z) && map.Label[x, z] > 0)
                        touched.Add(map.Label[x, z]);
            foreach (int id in touched)
                map.Rooms[id - 1].Doors.Add(i);
        }

        return map;
    }

    // ---------- Desenho ----------

    private static readonly string[] Digits =
    {
        "111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001",
        "111100111001111", "111100111101111", "111001001001001", "111101111101111", "111101111001111",
    };

    public Texture2D Draw(int scale, System.Func<int, Color> roomColor)
    {
        var tex = new Texture2D(W * scale, H * scale, TextureFormat.RGB24, false);
        var px = new Color32[tex.width * tex.height];
        for (int x = 0; x < W; x++)
            for (int z = 0; z < H; z++)
            {
                Color c = DoorCell[x, z] && Walk[x, z] ? new Color(0.9f, 0.15f, 0.1f)
                        : Label[x, z] > 0 ? roomColor(Label[x, z])
                        : Walk[x, z] ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.08f, 0.08f, 0.09f);
                for (int i = 0; i < scale; i++)
                    for (int j = 0; j < scale; j++)
                        px[(z * scale + j) * tex.width + x * scale + i] = c;
            }
        foreach (Room room in Rooms)
        {
            if (room.Area < 2f)
                continue;
            Vector2Int g = GridOf(room.Center);
            DrawNumber(px, tex.width, tex.height, g.x * scale, g.y * scale, room.Id, scale >= 3 ? 2 : 1);
        }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    private static void DrawNumber(Color32[] px, int w, int h, int cx, int cy, int n, int s)
    {
        string str = n.ToString();
        int x0 = cx - str.Length * 4 * s / 2;
        for (int k = 0; k < str.Length; k++)
        {
            string d = Digits[str[k] - '0'];
            for (int row = 0; row < 5; row++)
                for (int col = 0; col < 3; col++)
                    if (d[row * 3 + col] == '1')
                        for (int a = -1; a <= s; a++)
                            for (int b = -1; b <= s; b++)
                            {
                                int px0 = x0 + k * 4 * s + col * s + a, py0 = cy + (4 - row) * s + b;
                                if (px0 >= 0 && py0 >= 0 && px0 < w && py0 < h)
                                    px[py0 * w + px0] = (a < 0 || b < 0 || a == s || b == s) ? (Color32)Color.black : (Color32)Color.white;
                            }
        }
    }

    // ---------- Levantamento (batch) ----------

    public static void Survey()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Principal.unity", OpenSceneMode.Single);
        GameObject old = GameObject.Find("Props_PSX");
        if (old != null)
            Object.DestroyImmediate(old);

        var sb = new StringBuilder();
        var lodType = typeof(Editor).Assembly.GetTypes().FirstOrDefault(t => t.Name == "MeshLodUtility");
        sb.AppendLine("MeshLodUtility: " + (lodType != null ? lodType.FullName : "nao"));
        if (lodType != null)
            foreach (var m in lodType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                sb.AppendLine("  " + m);
        var mi = typeof(ModelImporter).GetProperties().Where(p => p.Name.ToLower().Contains("lod")).Select(p => p.Name);
        sb.AppendLine("ModelImporter lod props: " + string.Join(",", mi));

        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/revisao/qa/salas"));
        Directory.CreateDirectory(folder);

        foreach (float y in Floors())
        {
            RoomMap map = Build(y);
            sb.AppendLine("ANDAR y=" + y + " grade " + map.W + "x" + map.H + " portas=" + map.DoorBounds.Count);
            foreach (Room r in map.Rooms.Where(r => r.Area >= 2f).OrderByDescending(r => r.Area))
            {
                Vector2 size = new Vector2((r.Max.x - r.Min.x + 1) * Cell, (r.Max.y - r.Min.y + 1) * Cell);
                sb.AppendLine("  sala " + r.Id + " area=" + r.Area.ToString("F1") + " caixa=" + size.ToString("F1") + " centro=" + r.Center.ToString("F1") + " portas=" + r.Doors.Count);
            }
            Texture2D tex = map.Draw(3, id => Color.HSVToRGB((id * 0.137f) % 1f, 0.45f, 0.75f));
            File.WriteAllBytes(Path.Combine(folder, "planta_y" + y + ".png"), tex.EncodeToPNG());
            RenderTopDown(map, Path.Combine(folder, "topo_y" + y + ".png"));
        }

        File.WriteAllText(Path.Combine(folder, "salas.txt"), sb.ToString());
        Debug.Log("SURVEY\n" + sb);
    }

    // Vista de cima sem teto (esconde o que esta acima de 2,3 m do chao), sem pos-processamento.
    public static void RenderTopDown(RoomMap map, string path)
    {
        var hidden = new List<Renderer>();
        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            if (r.enabled && r.bounds.min.y > map.FloorY + 2.0f)
            {
                r.enabled = false;
                hidden.Add(r);
            }

        float wm = map.W * Cell, hm = map.H * Cell;
        int pw = Mathf.Min(2400, Mathf.RoundToInt(wm * 12f)), ph = Mathf.RoundToInt(pw * hm / wm);
        var rt = new RenderTexture(pw, ph, 24);
        GameObject camGo = new GameObject("TopCam", typeof(Camera));
        Camera cam = camGo.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = hm / 2f;
        cam.aspect = wm / hm;
        cam.transform.position = new Vector3(map.Origin.x + wm / 2f, map.FloorY + 2.2f, map.Origin.y + hm / 2f);
        cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 4f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.targetTexture = rt;
        var data = camGo.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = false;

        GameObject sun = new GameObject("TopSun", typeof(Light));
        Light l = sun.GetComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.2f;
        sun.transform.rotation = Quaternion.Euler(70f, 30f, 0f);
        Color amb = RenderSettings.ambientLight;
        var mode = RenderSettings.ambientMode;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);

        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(pw, ph, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, pw, ph), 0, 0);
        RenderTexture.active = null;
        File.WriteAllBytes(path, tex.EncodeToPNG());

        RenderSettings.ambientLight = amb;
        RenderSettings.ambientMode = mode;
        Object.DestroyImmediate(sun);
        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(rt);
        foreach (Renderer r in hidden) r.enabled = true;
    }

    // Planta detalhada para desenhar as salas a mao: alcancavel a partir do PlayerSpawn (verde),
    // fora de alcance (cinza), moveis existentes (marrom), paredes (preto), portas (vermelho),
    // linhas a cada 5 m (coordenadas mundo nas bordas).
    public static void Survey2()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Principal.unity", OpenSceneMode.Single);
        GameObject old = GameObject.Find("Props_PSX");
        if (old != null) Object.DestroyImmediate(old);
        foreach (string n in new[] { "PontoDeMontagem", "Baterias_Extra" })
        {
            GameObject g = GameObject.Find(n);
            if (g != null) Object.DestroyImmediate(g);
        }
        Physics.SyncTransforms();

        Vector3 spawn = GameObject.Find("PlayerSpawn").transform.position;
        NavMesh.SamplePosition(spawn, out NavMeshHit sh, 3f, NavMesh.AllAreas);
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/revisao/qa/salas"));
        var sb = new StringBuilder();
        sb.AppendLine("spawn " + spawn.ToString("F1"));

        foreach (float y in Floors())
        {
            RoomMap map = Build(y);
            int reach = 0;
            var reachable = new bool[map.W, map.H];
            var path = new NavMeshPath();
            // Alcance por amostras (1 a cada 4 celulas) e espalha para as vizinhas.
            for (int x = 0; x < map.W; x += 4)
                for (int z = 0; z < map.H; z += 4)
                {
                    if (!map.Walk[x, z]) continue;
                    if (NavMesh.CalculatePath(sh.position, map.World(x, z), NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete)
                    {
                        for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++)
                            if (map.In(x + i, z + j)) reachable[x + i, z + j] = true;
                        reach++;
                    }
                }
            sb.AppendLine("andar " + y + ": amostras alcancaveis=" + reach);
            if (reach == 0) continue;

            int s = 4;
            var tex = new Texture2D(map.W * s, map.H * s, TextureFormat.RGB24, false);
            var px = new Color32[tex.width * tex.height];
            int mask = ~LayerMask.GetMask("Ignore Raycast", "UI", "Arms", "PhotoCameraEffect", "GlobalVolume", "Enemy", "Temporal", "TransparentFX", "Player");
            for (int x = 0; x < map.W; x++)
                for (int z = 0; z < map.H; z++)
                {
                    Color c;
                    if (map.Walk[x, z])
                        c = map.DoorCell[x, z] ? new Color(0.9f, 0.15f, 0.1f) : reachable[x, z] ? new Color(0.45f, 0.75f, 0.5f) : new Color(0.45f, 0.45f, 0.45f);
                    else
                    {
                        c = new Color(0.07f, 0.07f, 0.08f);
                        Collider[] hits = Physics.OverlapBox(map.World(x, z) + Vector3.up * 0.9f, new Vector3(0.1f, 0.5f, 0.1f), Quaternion.identity, mask, QueryTriggerInteraction.Ignore);
                        foreach (Collider h in hits)
                        {
                            Vector3 sz = h.bounds.size;
                            if (Mathf.Max(sz.x, sz.z) < 3.2f && sz.y < 2.4f) { c = new Color(0.55f, 0.36f, 0.2f); break; }
                        }
                        if (hits.Length == 0) c = new Color(0.18f, 0.18f, 0.2f);
                    }
                    for (int i = 0; i < s; i++) for (int j = 0; j < s; j++)
                        px[(z * s + j) * tex.width + x * s + i] = c;
                }
            // Linhas a cada 5 m e rotulos (valor absoluto, sem sinal; x cresce p/ direita, z p/ cima).
            for (int x = 0; x < map.W; x++)
            {
                float wx = map.Origin.x + x * Cell;
                if (Mathf.Abs(wx / 5f - Mathf.Round(wx / 5f)) < 0.001f)
                    for (int py = 0; py < tex.height; py += 2) px[py * tex.width + x * s] = new Color32(90, 140, 230, 255);
            }
            for (int z = 0; z < map.H; z++)
            {
                float wz = map.Origin.y + z * Cell;
                if (Mathf.Abs(wz / 5f - Mathf.Round(wz / 5f)) < 0.001f)
                    for (int pxx = 0; pxx < tex.width; pxx += 2) px[z * s * tex.width + pxx] = new Color32(90, 140, 230, 255);
            }
            for (int x = 0; x < map.W; x++)
            {
                float wx = map.Origin.x + x * Cell;
                if (Mathf.Abs(wx / 10f - Mathf.Round(wx / 10f)) < 0.001f)
                {
                    DrawNumber(px, tex.width, tex.height, x * s + 12, 6, Mathf.RoundToInt(Mathf.Abs(wx)), 2);
                    DrawNumber(px, tex.width, tex.height, x * s + 12, tex.height - 16, Mathf.RoundToInt(Mathf.Abs(wx)), 2);
                }
            }
            for (int z = 0; z < map.H; z++)
            {
                float wz = map.Origin.y + z * Cell;
                if (Mathf.Abs(wz / 10f - Mathf.Round(wz / 10f)) < 0.001f)
                {
                    DrawNumber(px, tex.width, tex.height, 20, z * s + 3, Mathf.RoundToInt(Mathf.Abs(wz)), 2);
                    DrawNumber(px, tex.width, tex.height, tex.width - 24, z * s + 3, Mathf.RoundToInt(Mathf.Abs(wz)), 2);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(folder, "detalhe_y" + y + ".png"), tex.EncodeToPNG());
            sb.AppendLine("  origem " + map.Origin.ToString("F2") + " grade " + map.W + "x" + map.H);
        }
        File.WriteAllText(Path.Combine(folder, "detalhe.txt"), sb.ToString());
        Debug.Log("SURVEY2 " + sb);
    }



}
