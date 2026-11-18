using System.Collections;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;

// Leitura no escuro: toda porta tem uma luminaria guia (sem sombra, barata) e toda peca temporal
// tem a camada de brilho Retro/TemporalRim para se destacar da paleta cinza.
public class LabLightingCheck : QAScenario
{
    public override string Name => "LabLightingCheck";
    public override bool NeedsPlayMode => false;

    public override IEnumerator Run()
    {
        EditorSceneManager.OpenScene(LabLightingBuilder.ScenePath);

        Light[] guides = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(l => l.transform.parent != null && l.transform.parent.name == LabLightingBuilder.GuidesGroup)
            .ToArray();

        foreach (Door door in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Vector3 top = LabLightingBuilder.DoorTop(door);
            bool lit = guides.Any(l => Vector3.Distance(l.transform.position, top) < 2.5f);
            Check(lit, "porta " + door.name + " tem luz guia");
        }

        Check(guides.All(l => l.shadows == LightShadows.None), "luzes guia nao projetam sombra");

        foreach (TemporalObjectPickup piece in Object.FindObjectsByType<TemporalObjectPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Renderer r = piece.GetComponent<Renderer>();
            bool rim = r != null && r.sharedMaterials.Any(m => m != null && m.shader.name == "Retro/TemporalRim");
            Check(rim, "peca " + piece.name + " tem o brilho temporal");
        }

        yield break;
    }
}
