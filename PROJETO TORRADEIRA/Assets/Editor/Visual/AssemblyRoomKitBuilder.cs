using System.Linq;
using UnityEditor;
using UnityEngine;

// Cria/atualiza Assets/Resources/AssemblyRoomKit.asset com os modelos e texturas da sala de montagem.
public static class AssemblyRoomKitBuilder
{
    private const string H = "Assets/Comodos/Low Poly Household Items/Low Poly Furniture/";
    private const string P = "Assets/ThirdParty/PSXNostalgia-FBX/PSXNostalgia-FBX/";
    private const string T = "Assets/ThirdParty/Facility-7 Genetics/Level Textures/";
    private const string A = "Assets/Comodos/Abandoned World/Wood Box Free/Prefabs/";
    private const string M = "Assets/Art/Props/PSXMegaPack/";

    [MenuItem("Tools/OUT OF TIME/Atualizar kit da sala de montagem")]
    public static void Build()
    {
        const string path = "Assets/Resources/AssemblyRoomKit.asset";
        var kit = AssetDatabase.LoadAssetAtPath<AssemblyRoomKit>(path);
        if (kit == null)
        {
            kit = ScriptableObject.CreateInstance<AssemblyRoomKit>();
            AssetDatabase.CreateAsset(kit, path);
        }

        kit.floorTiles = Tex(T + "SterileTiles_Base_color.png");
        kit.floorTilesNormal = Tex(T + "SterileTiles_Normal_DirectX.png", true);
        kit.metalPanel = Tex(T + "VentMetal_Base_color.png");
        kit.metalPanelNormal = Tex(T + "VentMetal_Normal_DirectX.png", true);
        kit.metalPanelMetallic = Tex(T + "VentMetal_Metallic.png");

        kit.bench = Go("Assets/Comodos/Furniture/Prefabs/MetalTable.prefab");
        kit.crtScreen = Go(P + "ComputerSet/PcScreen.fbx");
        kit.pcTower = Go(P + "ComputerSet/PcTower.fbx");
        kit.keyboard = Go(P + "ComputerSet/Keyboard.fbx");
        kit.deskLamp = Go(P + "Accessories/Lamp.fbx");
        kit.speaker = Go(P + "ComputerSet/Speaker.fbx");
        kit.chair = Go(P + "ComputerSet/Chair.fbx");
        kit.smallItems = new[]
        {
            Go(P + "Accessories/Mugs/MugRed.fbx"), Go(P + "Accessories/Books/BookProgramming.fbx"),
            Go(P + "Accessories/Books/GenericBook2.fbx"), Go(P + "Accessories/StickyNotes.fbx"),
            Go(P + "Accessories/Cigarettes/AshtrayFull.fbx"), Go(M + "Models/floppy_disc_1.glb"),
            Go(M + "Models/notebook_1.glb"), Go(M + "Models/flashlight_1.glb"),
        }.Where(g => g != null).ToArray();

        kit.shelf = Go(H + "Shelves/Shelf C.fbx");
        kit.drawer = Go(H + "Drawers/Drawer A.fbx");
        kit.safe = Go(H + "Miscellaneous/Safe B.fbx");
        kit.radiator = Go(H + "Miscellaneous/Radiator A.fbx");
        kit.bin = Go(H + "Miscellaneous/Bin.fbx");
        kit.clock = Go(M + "Models/clock_2.glb");
        kit.clockMaterial = AssetDatabase.LoadAssetAtPath<Material>(M + "Materials/clock_2.mat");
        kit.wallLight = Go(H + "Lights/Wall Light.fbx");
        kit.ceilingLight = Go(H + "Lights/Ceiling Light.fbx");
        kit.crates = new[] { Go(A + "SM_WoodBox_1.prefab"), Go(A + "SM_WoodBox_3.prefab"), Go(A + "SM_CardboardBox_1.prefab"), Go(A + "SM_CardboardBox_3.prefab") }.Where(g => g != null).ToArray();
        kit.coil = Go(A + "SM_Coil.prefab");
        kit.console = Go(H + "Electronics/Console B.fbx");

        EditorUtility.SetDirty(kit);
        AssetDatabase.SaveAssets();
        Debug.Log("AssemblyRoomKit atualizado");
    }

    private static GameObject Go(string p)
    {
        var g = AssetDatabase.LoadAssetAtPath<GameObject>(p);
        if (g == null) Debug.LogWarning("Kit: nao achou " + p);
        return g;
    }

    private static Texture2D Tex(string p, bool normal = false)
    {
        var imp = AssetImporter.GetAtPath(p) as TextureImporter;
        if (imp != null)
        {
            bool changed = false;
            if (normal && imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; changed = true; }
            if (changed) imp.SaveAndReimport();
        }
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        if (t == null) Debug.LogWarning("Kit: nao achou " + p);
        return t;
    }
}
