using UnityEngine;

// Referencias de modelos e texturas usados para decorar a sala de montagem (carregado de Resources,
// a estacao e montada no editor por Tools > OUT OF TIME > Montar tudo na cena).
[CreateAssetMenu(menuName = "OUT OF TIME/Kit da sala de montagem")]
public class AssemblyRoomKit : ScriptableObject
{
    [Header("Texturas")]
    public Texture2D floorTiles;
    public Texture2D floorTilesNormal;
    public Texture2D metalPanel;
    public Texture2D metalPanelNormal;
    public Texture2D metalPanelMetallic;

    [Header("Bancada e computador")]
    public GameObject bench;
    public GameObject crtScreen;
    public GameObject pcTower;
    public GameObject keyboard;
    public GameObject deskLamp;
    public GameObject speaker;
    public GameObject chair;

    [Header("Itens de mesa")]
    public GameObject[] smallItems;

    [Header("Armazenamento e parede")]
    public GameObject shelf;
    public GameObject drawer;
    public GameObject safe;
    public GameObject radiator;
    public GameObject bin;
    public GameObject clock;
    public Material clockMaterial;
    public GameObject wallLight;
    public GameObject ceilingLight;
    public GameObject[] crates;
    public GameObject coil;
    public GameObject console;
}
