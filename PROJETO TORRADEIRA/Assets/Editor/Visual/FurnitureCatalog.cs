using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Catalogo de moveis usados na mobilia: caminho, tamanho real e para onde fica a frente.
public static class FurnitureCatalog
{
    public class Item
    {
        public string Key;
        public string Path;
        public float Height;          // altura real (m); 0 = usa Length
        public float Length;          // maior lado horizontal (m), quando Height = 0
        public bool FrontNegZ;        // frente do modelo aponta para -Z
        public bool Occluder;         // grande e opaco: ocultador no occlusion culling
        public bool Collider = true;  // pequenos itens de mesa nao tem colisor
        public string MaterialPath;   // material forcado (modelos .glb do PSX Mega Pack)
        public bool WallMounted;
        public Color Tint = Color.clear; // cor do material proprio (o pacote domestico vem sem textura, todo vermelho)
        public GameObject Source;
    }

    private const string H = "Assets/Comodos/Low Poly Household Items/Low Poly Furniture/";
    private const string P = "Assets/ThirdParty/PSXNostalgia-FBX/PSXNostalgia-FBX/";
    private const string F = "Assets/Comodos/Furniture/Prefabs/";
    private const string J = "Assets/Comodos/JustPlay/Furniture used set/Prefabs/";
    private const string A = "Assets/Comodos/Abandoned World/Wood Box Free/Prefabs/";

    private static readonly Color Wood = new Color(0.36f, 0.26f, 0.18f);
    private static readonly Color Metal = new Color(0.46f, 0.47f, 0.47f);
    private static readonly Color Olive = new Color(0.38f, 0.39f, 0.35f);
    private static readonly Color Leather = new Color(0.22f, 0.16f, 0.12f);
    private static readonly Color Dark = new Color(0.16f, 0.17f, 0.17f);
    private static readonly Color Enamel = new Color(0.78f, 0.76f, 0.7f);
    private static readonly Color Plant = new Color(0.22f, 0.27f, 0.2f);
    private static readonly Color Book = new Color(0.42f, 0.3f, 0.26f);

    public static readonly List<Item> Items = new List<Item>
    {
        // Mesas e bancadas
        new Item { Key = "desk", Path = P + "ComputerSet/Desk.fbx", Height = 0.76f },
        new Item { Tint = Wood, Key = "officedesk", Path = H + "Tables/Desk.fbx", Height = 0.76f },
        new Item { Key = "bench", Path = F + "MetalTable.prefab", Height = 0.9f },
        new Item { Tint = Wood, Key = "tableA", Path = H + "Tables/Table A.fbx", Height = 0.75f },
        new Item { Tint = Wood, Key = "tableB", Path = H + "Tables/Table B.fbx", Height = 0.75f },
        new Item { Tint = Wood, Key = "roundtable", Path = H + "Tables/Rounded Table A.fbx", Height = 0.75f },
        // Cadeiras e sofas
        new Item { Key = "chair", Path = P + "ComputerSet/Chair.fbx", Height = 0.95f, FrontNegZ = true },
        new Item { Tint = Wood, Key = "chairA", Path = H + "Chairs/Chair A.fbx", Height = 0.9f },
        new Item { Tint = Metal, Key = "chairB", Path = H + "Chairs/Chair B.fbx", Height = 0.9f },
        new Item { Tint = Leather, Key = "sofaA", Path = H + "Sofas/Sofa A.fbx", Height = 0.8f },
        new Item { Tint = Leather, Key = "sofaB", Path = H + "Sofas/Sofa B.fbx", Height = 0.8f },
        new Item { Key = "armchair", Path = F + "Fotel.prefab", Height = 0.85f },
        // Armazenamento
        new Item { Tint = Metal, Key = "shelfA", Path = H + "Shelves/Shelf A.fbx", Height = 1.9f, Occluder = true },
        new Item { Tint = Metal, Key = "shelfB", Path = H + "Shelves/Shelf B.fbx", Height = 1.9f, Occluder = true },
        new Item { Tint = Metal, Key = "shelfC", Path = H + "Shelves/Shelf C.fbx", Height = 1.9f, Occluder = true },
        new Item { Tint = Olive, Key = "drawerA", Path = H + "Drawers/Drawer A.fbx", Height = 1.25f },
        new Item { Tint = Olive, Key = "drawerB", Path = H + "Drawers/Drawer B.fbx", Height = 0.8f },
        new Item { Key = "cupboardA", Path = J + "Cupboard_A.prefab", Height = 1.85f, FrontNegZ = true, Occluder = true },
        new Item { Key = "cupboardB", Path = J + "Cupboard_B.prefab", Height = 1.85f, FrontNegZ = true, Occluder = true },
        new Item { Key = "bigcloset", Path = F + "BigCloset.prefab", Height = 2.0f, Occluder = true },
        new Item { Key = "smallcloset", Path = F + "SmallCloset.prefab", Height = 1.0f },
        new Item { Key = "commode", Path = J + "Сommode_A.prefab", Height = 0.85f },
        new Item { Tint = Dark, Key = "safe", Path = H + "Miscellaneous/Safe A.fbx", Height = 0.9f },
        new Item { Tint = Enamel, Key = "kitchenA", Path = H + "Kitchen/Cupboard A.fbx", Height = 0.9f },
        new Item { Tint = Enamel, Key = "kitchensink", Path = H + "Kitchen/Cupboard Sink.fbx", Height = 0.9f },
        new Item { Tint = Enamel, Key = "fridge", Path = H + "Kitchen/Fridge A.fbx", Height = 1.75f, Occluder = true },
        new Item { Key = "crateA", Path = A + "SM_WoodBox_1.prefab", Height = 0.6f },
        new Item { Key = "crateB", Path = A + "SM_WoodBox_3.prefab", Height = 0.7f },
        new Item { Key = "cardboardA", Path = A + "SM_CardboardBox_1.prefab", Height = 0.45f },
        new Item { Key = "cardboardB", Path = A + "SM_CardboardBox_3.prefab", Height = 0.4f },
        // Pecas de parede e chao
        new Item { Tint = Enamel, Key = "radiator", Path = H + "Miscellaneous/Radiator A.fbx", Height = 0.65f },
        new Item { Tint = Plant, Key = "plantA", Path = H + "Miscellaneous/Plant A.fbx", Height = 1.1f },
        new Item { Tint = Plant, Key = "plantB", Path = H + "Miscellaneous/Plant B.fbx", Height = 0.9f },
        new Item { Tint = Metal, Key = "bin", Path = H + "Miscellaneous/Bin.fbx", Height = 0.4f },
        new Item { Tint = Metal, Key = "fan", Path = H + "Miscellaneous/Fan.fbx", Height = 0.45f, Collider = false },
        // Itens de mesa
        new Item { Key = "crt", Path = P + "ComputerSet/PcScreen.fbx", Height = 0.36f, Collider = false },
        new Item { Key = "keyboard", Path = P + "ComputerSet/Keyboard.fbx", Length = 0.44f, Collider = false },
        new Item { Key = "lamp", Path = P + "Accessories/Lamp.fbx", Height = 0.45f, Collider = false },
        new Item { Key = "mug", Path = P + "Accessories/Mugs/MugRed.fbx", Height = 0.1f, Collider = false },
        new Item { Key = "book1", Path = P + "Accessories/Books/GenericBook1.fbx", Length = 0.24f, Collider = false },
        new Item { Key = "book2", Path = P + "Accessories/Books/GenericBook3.fbx", Length = 0.24f, Collider = false },
        new Item { Key = "ashtray", Path = P + "Accessories/Cigarettes/AshtrayFull.fbx", Length = 0.13f, Collider = false },
        new Item { Key = "notes", Path = P + "Accessories/StickyNotes.fbx", Length = 0.1f, Collider = false },
        new Item { Tint = Dark, Key = "phone", Path = H + "Electronics/Phone.fbx", Length = 0.22f, Collider = false },
        new Item { Tint = Wood, Key = "globe", Path = H + "Miscellaneous/Globe.fbx", Height = 0.4f, Collider = false },
        new Item { Tint = Enamel, Key = "microwave", Path = H + "Kitchen/Microwave.fbx", Length = 0.5f, Collider = false },
        new Item { Tint = Metal, Key = "kettle", Path = H + "Kitchen/Kettle.fbx", Height = 0.24f, Collider = false },
        new Item { Key = "clock", Path = "Assets/Art/Props/PSXMegaPack/Models/clock_2.glb", Length = 0.34f, Collider = false, WallMounted = true, MaterialPath = "Assets/Art/Props/PSXMegaPack/Materials/clock_2.mat" },
        new Item { Tint = Book, Key = "booksrow", Path = H + "Miscellaneous/Books A.fbx", Length = 0.35f, Collider = false },
        // ---- Rodada 2: variedade ----
        new Item { Tint = Metal, Key = "shelfD", Path = H + "Shelves/Shelf D.fbx", Height = 1.9f, Occluder = true },
        new Item { Tint = Wood, Key = "shelfE", Path = H + "Shelves/Shelf E.fbx", Height = 1.8f, Occluder = true },
        new Item { Tint = Wood, Key = "wallshelfA", Path = H + "Shelves/Wall Shelf A.fbx", Length = 0.9f, Collider = false, WallMounted = true },
        new Item { Tint = Wood, Key = "wallshelfB", Path = H + "Shelves/Wall Shelf B.fbx", Length = 0.9f, Collider = false, WallMounted = true },
        new Item { Tint = Wood, Key = "tableC", Path = H + "Tables/Table C.fbx", Height = 0.42f },
        new Item { Tint = Metal, Key = "tableD", Path = H + "Tables/Table D.fbx", Height = 0.45f },
        new Item { Tint = Wood, Key = "roundB", Path = H + "Tables/Rounded Table B.fbx", Height = 0.72f },
        new Item { Tint = Wood, Key = "roundC", Path = H + "Tables/Rounded Table C.fbx", Height = 0.55f },
        new Item { Tint = Wood, Key = "chairC", Path = H + "Chairs/Chair C.fbx", Height = 0.9f },
        new Item { Tint = Metal, Key = "chairD", Path = H + "Chairs/Chair D.fbx", Height = 0.9f },
        new Item { Tint = Leather, Key = "chairE", Path = H + "Chairs/Chair E.fbx", Height = 0.95f },
        new Item { Tint = Leather, Key = "sofaC", Path = H + "Sofas/Sofa C.fbx", Height = 0.8f },
        new Item { Tint = Leather, Key = "sofaD", Path = H + "Sofas/Sofa D.fbx", Height = 0.8f },
        new Item { Tint = Enamel, Key = "cupboardC", Path = H + "Kitchen/Cupboard C.fbx", Height = 0.9f },
        new Item { Tint = Enamel, Key = "cupboardD", Path = H + "Kitchen/Cupboard D.fbx", Height = 0.9f },
        new Item { Tint = Enamel, Key = "cupboardE", Path = H + "Kitchen/Cupboard E.fbx", Height = 0.9f },
        new Item { Tint = Enamel, Key = "cupboardF", Path = H + "Kitchen/Cupboard F.fbx", Height = 0.9f },
        new Item { Tint = Enamel, Key = "fridgeB", Path = H + "Kitchen/Fridge B.fbx", Height = 1.6f, Occluder = true },
        new Item { Tint = Enamel, Key = "oven", Path = H + "Kitchen/Oven.fbx", Height = 0.9f },
        new Item { Tint = Metal, Key = "toaster", Path = H + "Kitchen/Toaster.fbx", Length = 0.28f, Collider = false },
        new Item { Tint = Wood, Key = "knifeblock", Path = H + "Kitchen/Knife Block.fbx", Height = 0.25f, Collider = false },
        new Item { Tint = Enamel, Key = "kitchenroll", Path = H + "Kitchen/Kitchen Roll.fbx", Height = 0.28f, Collider = false },
        new Item { Tint = Enamel, Key = "plate", Path = H + "Miscellaneous/Plate.fbx", Length = 0.24f, Collider = false },
        new Item { Tint = Enamel, Key = "bowl", Path = H + "Miscellaneous/Bowl.fbx", Length = 0.17f, Collider = false },
        new Item { Tint = Enamel, Key = "glass", Path = H + "Miscellaneous/Glass.fbx", Height = 0.12f, Collider = false },
        new Item { Tint = Metal, Key = "pan", Path = H + "Miscellaneous/Pan.fbx", Length = 0.42f, Collider = false },
        new Item { Tint = Plant, Key = "vase", Path = H + "Miscellaneous/Vase.fbx", Height = 0.32f, Collider = false },
        new Item { Tint = Book, Key = "booksB", Path = H + "Miscellaneous/Books B.fbx", Length = 0.3f, Collider = false },
        new Item { Tint = Dark, Key = "tvA", Path = H + "Electronics/TV A.fbx", Height = 0.5f, Collider = false },
        new Item { Tint = Dark, Key = "speakerH", Path = H + "Miscellaneous/Speaker.fbx", Height = 0.35f, Collider = false },
        new Item { Tint = Metal, Key = "lampA", Path = H + "Lights/Lamp A.fbx", Height = 1.6f },
        new Item { Tint = Metal, Key = "lampB", Path = H + "Lights/Lamp B.fbx", Height = 1.5f },
        new Item { Tint = Enamel, Key = "radiatorB", Path = H + "Miscellaneous/Radiator B.fbx", Height = 0.65f },
        new Item { Tint = Dark, Key = "safeB", Path = H + "Miscellaneous/Safe B.fbx", Height = 1.1f },
        new Item { Tint = Wood, Key = "ladder", Path = H + "Miscellaneous/Ladder.fbx", Height = 1.8f },
        new Item { Tint = Wood, Key = "broom", Path = H + "Miscellaneous/Broom.fbx", Height = 1.3f, Collider = false },
        new Item { Tint = Leather, Key = "carpetA", Path = H + "Carpets/Carpet A.fbx", Length = 2.4f, Collider = false },
        new Item { Tint = Book, Key = "carpetB", Path = H + "Carpets/Carpet B.fbx", Length = 2.2f, Collider = false },
        new Item { Tint = Metal, Key = "pot", Path = H + "Miscellaneous/Pot.fbx", Height = 0.35f, Collider = false },
        new Item { Key = "tvP", Path = P + "TVSet/TV.fbx", Height = 0.45f, Collider = false },
        new Item { Key = "tvstand", Path = P + "TVSet/TVStand.fbx", Height = 0.55f },
        new Item { Key = "speakerP", Path = P + "ComputerSet/Speaker.fbx", Height = 0.2f, Collider = false },
        new Item { Key = "mousepad", Path = P + "ComputerSet/MousePad.fbx", Length = 0.24f, Collider = false },
        new Item { Key = "cigpack", Path = P + "Accessories/Cigarettes/CigarettesPack2.fbx", Length = 0.09f, Collider = false },
        new Item { Key = "progbook", Path = P + "Accessories/Books/BookProgramming.fbx", Length = 0.24f, Collider = false },
        new Item { Key = "mugB", Path = P + "Accessories/Mugs/MugBlue.fbx", Height = 0.1f, Collider = false },
        new Item { Key = "notebookP", Path = "Assets/Art/Props/PSXMegaPack/Models/notebook_1.glb", Length = 0.26f, Collider = false, MaterialPath = "Assets/Art/Props/PSXMegaPack/Materials/notebook_1.mat" },
        new Item { Key = "floppy", Path = "Assets/Art/Props/PSXMegaPack/Models/floppy_disc_1.glb", Length = 0.09f, Collider = false, MaterialPath = "Assets/Art/Props/PSXMegaPack/Materials/floppy_disc_1.mat" },
        new Item { Key = "flashlight", Path = "Assets/Art/Props/PSXMegaPack/Models/flashlight_1.glb", Length = 0.24f, Collider = false, MaterialPath = "Assets/Art/Props/PSXMegaPack/Materials/flashlight_1.mat" },
        new Item { Key = "padlock", Path = "Assets/Art/Props/PSXMegaPack/Models/padlock_1.glb", Length = 0.07f, Collider = false, MaterialPath = "Assets/Art/Props/PSXMegaPack/Materials/padlock_1.mat" },
        new Item { Key = "smallbox", Path = "Assets/Art/Props/PSXMegaPack/Models/cardboard_box_1.glb", Length = 0.32f, Collider = false, MaterialPath = "Assets/Art/Props/PSXMegaPack/Materials/cardboard_box_1.mat" },
        new Item { Key = "crate2", Path = A + "SM_WoodBox_2.prefab", Height = 0.5f },
        new Item { Key = "crate4", Path = A + "SM_WoodBox_4.prefab", Height = 0.6f },
        new Item { Key = "crate5", Path = A + "SM_WoodBox_5.prefab", Height = 0.55f },
        new Item { Key = "crate6", Path = A + "SM_WoodBox_6.prefab", Height = 0.7f },
        new Item { Key = "crate7", Path = A + "SM_WoodBox_7.prefab", Height = 0.45f },
        new Item { Key = "crate8", Path = A + "SM_WoodBox_8.prefab", Height = 0.65f },
        new Item { Key = "crate9", Path = A + "SM_WoodBox_9.prefab", Height = 0.5f },
        new Item { Key = "crate10", Path = A + "SM_WoodBox_10.prefab", Height = 0.6f },
        new Item { Key = "cardboard2", Path = A + "SM_CardboardBox_2.prefab", Height = 0.4f },
        new Item { Key = "cardboard4", Path = A + "SM_CardboardBox_4.prefab", Height = 0.35f },
        new Item { Key = "coil", Path = A + "SM_Coil.prefab", Height = 0.5f },
    };

    public static Item Get(string key) => Items.First(i => i.Key == key);

    public static bool Load()
    {
        bool ok = true;
        foreach (Item item in Items)
        {
            item.Source = AssetDatabase.LoadAssetAtPath<GameObject>(item.Path);
            if (item.Source == null)
            {
                Debug.LogError("Catalogo: nao achou " + item.Path);
                ok = false;
            }
        }
        return ok;
    }

    // Folha de contato: cada item visto de +Z (esquerda) e -Z (direita), sem pos-processamento.
    public static void ContactSheet() => ContactSheet(0);

    public static void ContactSheetNew() => ContactSheet(Items.FindIndex(x => x.Key == "shelfD"));

    public static void ContactSheet(int first)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Load();
        GameObject sun = new GameObject("Sun", typeof(Light));
        sun.GetComponent<Light>().type = LightType.Directional;
        sun.GetComponent<Light>().intensity = 1.3f;
        sun.transform.rotation = Quaternion.Euler(40f, 30f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);

        const int cell = 160;
        int cols = 4;
        var list = Items.Skip(first).ToList();
        int rows = Mathf.CeilToInt(list.Count / (float)cols);
        var sheet = new Texture2D(cols * cell * 2, rows * cell, TextureFormat.RGB24, false);
        var rt = new RenderTexture(cell, cell, 24);
        Camera cam = new GameObject("Cam", typeof(Camera)).GetComponent<Camera>();
        cam.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
        cam.targetTexture = rt;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.fieldOfView = 35f;

        var lines = new List<string>();
        for (int k = 0; k < list.Count; k++)
        {
            Item item = list[k];
            if (item.Source == null) continue;
            GameObject go = (GameObject)Object.Instantiate(item.Source);
            Renderer[] rs = go.GetComponentsInChildren<Renderer>();
            Bounds b = rs[0].bounds; foreach (Renderer r in rs) b.Encapsulate(r.bounds);
            lines.Add(k + " " + item.Key + " nativo=" + b.size.ToString("F2"));
            float d = b.extents.magnitude / Mathf.Tan(17f * Mathf.Deg2Rad) * 1.05f;
            for (int s = 0; s < 2; s++)
            {
                float side = s == 0 ? 1f : -1f;
                cam.backgroundColor = s == 0 ? new Color(0.22f, 0.3f, 0.38f) : new Color(0.38f, 0.26f, 0.22f);
                cam.transform.position = b.center + new Vector3(0.35f, 0.45f, side).normalized * d;
                cam.transform.LookAt(b.center);
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(cell, cell, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, cell, cell), 0, 0);
                RenderTexture.active = null;
                int ox = (k % cols) * cell * 2 + s * cell, oy = (rows - 1 - k / cols) * cell;
                sheet.SetPixels(ox, oy, cell, cell, tex.GetPixels());
                Object.DestroyImmediate(tex);
            }
            Object.DestroyImmediate(go);
        }
        sheet.Apply();
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/revisao/qa/salas"));
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, "catalogo.png"), sheet.EncodeToPNG());
        File.WriteAllLines(Path.Combine(folder, "catalogo.txt"), lines);
    }
}
