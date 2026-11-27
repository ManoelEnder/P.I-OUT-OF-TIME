using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Porta abre para o lado oposto ao jogador (testa dos dois lados) e a antena do radio fica escondida.
public class DoorScenario : QAScenario
{
    public override string Name => "Door";

    public override IEnumerator Run()
    {
        yield return LoadScene("Principal");
        yield return WaitGameplay();
        DisableEnemy();

        PlayerMovement player = Require<PlayerMovement>();
        Door door = Object.FindFirstObjectByType<Door>();
        Check(door != null, "tem porta na cena");
        if (player == null || door == null) yield break;

        // Antena do radio escondida enquanto nao ha mensagem.
        foreach (Image img in Object.FindObjectsByType<Image>(FindObjectsSortMode.None))
            if (img.name == "Antena")
                Check(img.color.a < 0.01f, "antena do radio escondida sem mensagem");

        Vector3 hinge = door.transform.position;
        Vector3 leafStart = LeafCenter(door);
        Vector3 normal = Vector3.ProjectOnPlane(door.transform.rotation * Vector3.forward, Vector3.up).normalized;
        // Garante que a "normal" e perpendicular a folha.
        Vector3 leafDir = Vector3.ProjectOnPlane(leafStart - hinge, Vector3.up).normalized;
        normal = Vector3.Cross(Vector3.up, leafDir).normalized;

        foreach (float s in new[] { 1f, -1f })
        {
            Vector3 spot = leafStart + normal * (1.6f * s);
            spot.y = player.transform.position.y;
            Teleport(player, spot);
            yield return WaitSeconds(0.3f);

            door.Interact();
            yield return WaitSeconds(1.2f);
            Vector3 leafOpen = LeafCenter(door);
            float side = Vector3.Dot(leafOpen - leafStart, normal) * s;
            Debug.Log("QA porta lado " + s + ": folha andou " + side.ToString("F2") + " m (negativo = para longe do jogador)");
            Check(side < -0.2f, "porta abre para longe do jogador (lado " + s + ")");

            door.Interact();
            yield return WaitSeconds(1.5f);
            Check(Vector3.Distance(LeafCenter(door), leafStart) < 0.05f, "porta volta ao batente (lado " + s + ")");
        }
    }

    private static Vector3 LeafCenter(Door door)
    {
        Renderer[] rs = door.GetComponentsInChildren<Renderer>();
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b.center;
    }

    private static void Teleport(PlayerMovement player, Vector3 pos)
    {
        CharacterController cc = player.controller;
        if (cc != null) cc.enabled = false;
        player.transform.position = pos;
        if (cc != null) cc.enabled = true;
    }
}
