using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Organiza a hierarquia: todo objeto raiz vai para dentro de um grupo por categoria
// ("Player", "Map", ...). Grupos ficam na origem, sem rotacao/escala, entao
// nada muda de lugar. Tem Undo (Ctrl+Z). Reexecutavel: so mexe no que ainda esta solto.
// Fica na raiz o que usa DontDestroyOnLoad (precisa ser raiz para sobreviver a troca de cena).
[InitializeOnLoad]
public static class HierarchyOrganizer
{
    private const string AutoRunKey = "Torradeira.HierarchyOrganizer.AutoRan.v2";

    // Ordem = prioridade (o primeiro que bater ganha) e ordem dos grupos na hierarquia.
    private static readonly (string group, System.Func<GameObject, bool> match)[] Rules =
    {
        ("Systems", go => Has(go, false, "GameManager", "MissionSystem", "SaveSystem", "RadioSystem", "AmbientSoundManager",
                                       "RandomEvents", "EventSystem", "PerfProbe", "SceneBake") && !HasRenderer(go)),
        ("Player", go => Has(go, true, "PlayerMovement")),
        ("Cameras", go => Has(go, true, "Camera", "CinemachineCamera", "CinemachineBrain")),
        ("Enemy", go => Has(go, true, "TemporalEraserController")),
        ("Interface", go => Has(go, false, "Canvas", "EventSystem")),
        ("Post processing", go => Has(go, false, "Volume")),
        ("Doors", go => Has(go, true, "Door")),
        ("Collectables", go => go.name == "BateriasExtras" ||
                                 Has(go, true, "BatteryPickup", "ColectableItem", "TemporalObjectPickup", "LostCameraPickup", "EvidenceObject")),
        ("Missions", go => Has(go, true, "PhotoLocation", "PhotoSpots", "HiddenFigure", "DeliveryPoint", "NPCMissao",
                                    "AssemblyStation", "ClawMachine")),
        ("Navigation", go => Has(go, true, "NavMeshSurface", "NavMeshLink", "NavMeshModifier", "NavMeshModifierVolume", "OffMeshLink")),
        ("Lighting", go => Has(go, false, "Light", "LightProbeGroup", "ReflectionProbe") ||
                                  (Has(go, true, "Light") && !Has(go, true, "MeshCollider", "BoxCollider") && SmallRenderer(go))),
        ("Audio", go => Has(go, true, "AudioSource", "AudioReverbZone") && !HasRenderer(go)),
        ("Furniture", go => go.name == "MobiliaExtra" || IsFurniture(go)),
        ("Map", go => HasRenderer(go) || Has(go, true, "Collider", "Terrain")),
        ("Systems", go => !HasRenderer(go) && go.GetComponentsInChildren<MonoBehaviour>(true).Any(m => m != null)),
        ("Other", go => true),
    };

    private static readonly string[] KeepAtRoot = { "GameManager", "MusicManager", "PerfProbe", "QACoroutineHost", "LoadingScreen" };

    static HierarchyOrganizer()
    {
        // Roda uma vez sozinho na cena Principal aberta (depois, so pelo menu).
        EditorApplication.delayCall += () =>
        {
            if (EditorPrefs.GetBool(AutoRunKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene s = SceneManager.GetActiveScene();
            if (s.name != "Principal") return;
            EditorPrefs.SetBool(AutoRunKey, true);
            Organize(s);
        };
    }

    [MenuItem("Ferramentas/Organizar hierarquia da cena")]
    public static void OrganizeActive()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.isLoaded) Organize(s);
        }
    }

    public static void Organize(Scene scene)
    {
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Organizar hierarquia");

        Dictionary<string, Transform> groups = new Dictionary<string, Transform>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (!IsGroup(root)) continue;
            // Grupo da versao antiga ("== JOGADOR ==") vira o nome novo ("Player").
            if (OldNames.TryGetValue(root.name, out string renamed))
            {
                Undo.RecordObject(root, "Renomear grupo");
                root.name = renamed;
            }
            if (groups.TryGetValue(root.name, out Transform existing))
            {
                // Dois grupos com o mesmo nome: junta.
                foreach (Transform c in root.transform.Cast<Transform>().ToList())
                    Undo.SetTransformParent(c, existing, true, "Juntar");
                Undo.DestroyObjectImmediate(root);
                continue;
            }
            groups[root.name] = root.transform;
        }

        int moved = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (IsGroup(root) || Has(root, false, KeepAtRoot)) continue;
            string group = Rules.First(r => r.match(root)).group;
            if (!groups.TryGetValue(group, out Transform parent))
            {
                GameObject g = new GameObject(group);
                SceneManager.MoveGameObjectToScene(g, scene);
                Undo.RegisterCreatedObjectUndo(g, "Grupo");
                parent = g.transform;
                groups[group] = parent;
            }
            Undo.SetTransformParent(root.transform, parent, true, "Mover");
            moved++;
        }

        // Grupos na ordem das regras, e filhos em ordem alfabetica dentro de cada grupo.
        int index = 0;
        foreach (string name in Rules.Select(r => r.group).Distinct())
        {
            if (!groups.TryGetValue(name, out Transform g)) continue;
            g.SetSiblingIndex(index++);
            List<Transform> kids = new List<Transform>();
            foreach (Transform c in g) kids.Add(c);
            kids.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
            for (int i = 0; i < kids.Count; i++)
            {
                Undo.RegisterFullObjectHierarchyUndo(kids[i].gameObject, "Ordenar");
                kids[i].SetSiblingIndex(i);
            }
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"HierarchyOrganizer: {moved} objetos organizados em {groups.Count} grupos na cena {scene.name}. Salve a cena (Ctrl+S).");
    }

    private static readonly Dictionary<string, string> OldNames = new Dictionary<string, string>
    {
        { "== SISTEMAS ==", "Systems" },
        { "== JOGADOR ==", "Player" },
        { "== CAMERAS ==", "Cameras" },
        { "== INIMIGO ==", "Enemy" },
        { "== UI ==", "Interface" },
        { "== POS-PROCESSAMENTO ==", "Post processing" },
        { "== PORTAS ==", "Doors" },
        { "== COLETAVEIS ==", "Collectables" },
        { "== MISSOES ==", "Missions" },
        { "== NAVEGACAO ==", "Navigation" },
        { "== ILUMINACAO ==", "Lighting" },
        { "== AUDIO ==", "Audio" },
        { "== MOBILIA ==", "Furniture" },
        { "== MAPA ==", "Map" },
        { "== OUTROS ==", "Other" },
    };

    // Grupo = objeto vazio (so Transform) com nome de grupo. Assim o objeto "Player" de verdade
    // nunca e confundido com o grupo "Player".
    private static bool IsGroup(GameObject go) =>
        go.GetComponents<Component>().Length == 1 &&
        (Rules.Any(r => r.group == go.name) || OldNames.ContainsKey(go.name));

    // Procura componentes pelo nome do tipo (evita depender de pacotes/assemblies).
    private static bool Has(GameObject go, bool children, params string[] types)
    {
        Component[] comps = children ? go.GetComponentsInChildren<Component>(true) : go.GetComponents<Component>();
        foreach (Component c in comps)
        {
            if (c == null) continue;
            for (System.Type t = c.GetType(); t != null && t != typeof(Component); t = t.BaseType)
                if (types.Contains(t.Name)) return true;
        }
        return false;
    }

    private static bool HasRenderer(GameObject go) => go.GetComponentsInChildren<Renderer>(true).Length > 0;

    private static bool SmallRenderer(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return true;
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b.size.magnitude < 3f;
    }

    private static bool IsFurniture(GameObject go)
    {
        GameObject src = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go);
        if (src == null) return false;
        string path = AssetDatabase.GetAssetPath(src);
        return path.Contains("/Furniture/") || path.Contains("Household") || path.Contains("/Comodos/");
    }
}
