using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

// Peca temporal: invisivel/nao coletavel -> fotografar com o visor revela -> E coleta -> missao conta.
// Uma segunda peca confere que a revelacao expira.
public class PieceScenario : QAScenario
{
    public override string Name => "Piece";

    public override IEnumerator Run()
    {
        yield return LoadScene("Principal");
        yield return WaitGameplay();

        PhotoCamera photo = Require<PhotoCamera>();
        MissionSystem missions = Require<MissionSystem>();
        TemporalObjectPickup[] pieces = Object.FindObjectsByType<TemporalObjectPickup>(FindObjectsSortMode.None);
        Check(pieces.Length >= 2, "fase tem pelo menos 2 pecas (" + pieces.Length + ")");
        if (photo == null || missions == null || pieces.Length < 2)
            yield break;

        Transform player = GameObject.FindGameObjectWithTag("Player").transform;
        Camera view = photo.playerCam;
        TemporalObjectPickup piece = pieces[0];

        Check(!piece.CanInteract(), "peca nao revelada nao e coletavel");

        yield return Approach(player, view, piece);
        yield return Reveal(photo, view, piece);
        Check(piece.CanInteract(), "foto revela a peca (coletavel)");
        Check(piece.GetComponent<Renderer>().enabled, "peca revelada fica visivel");

        yield return QAInput.Tap(Key.C);
        yield return WaitSeconds(photo.enterTransitionTime + 0.6f);
        yield return Aim(view, piece);

        int before = missions.GetPieceCount();
        yield return QAInput.Tap(Key.E);
        yield return WaitSeconds(0.3f);
        Check(!piece.gameObject.activeSelf, "E coleta a peca");
        Check(missions.GetPieceCount() == before + 1, "missao conta a peca (" + before + " -> " + missions.GetPieceCount() + ")");

        TMP_Text text = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(t => t.text.Contains("pe") && t.text.Contains("/"));
        Check(text != null && text.text.Contains("[" + missions.GetPieceCount() + "/"), "texto da missao atualizado (" + (text != null ? text.text.Replace("\n", " ") : "-") + ")");

        // Segunda peca: a revelacao acaba sozinha.
        TemporalObjectPickup other = pieces[1];
        yield return Approach(player, view, other);
        yield return Reveal(photo, view, other);
        Check(other.CanInteract(), "segunda peca revelada");
        yield return QAInput.Tap(Key.C);

        float end = Time.realtimeSinceStartup + 40f;
        while (other.CanInteract() && Time.realtimeSinceStartup < end)
            yield return null;
        Check(!other.CanInteract(), "revelacao expira com o tempo");
    }

    private IEnumerator Reveal(PhotoCamera photo, Camera view, TemporalObjectPickup piece)
    {
        photo.AddBattery(100);
        yield return QAInput.Tap(Key.C);
        yield return WaitSeconds(photo.enterTransitionTime + 0.5f);
        yield return Aim(view, piece);
        yield return QAInput.Click();
        yield return WaitSeconds(0.8f);
    }

    // Coloca o jogador a ~2 m da peca, sobre um piso (as pecas ficam em andares sem NavMesh),
    // com espaco para o corpo e linha de visao.
    private IEnumerator Approach(Transform player, Camera view, TemporalObjectPickup piece)
    {
        Vector3 target = piece.GetComponent<Collider>().bounds.center;
        float eye = view.transform.position.y - player.position.y;
        CharacterController body = player.GetComponent<CharacterController>();
        float radius = body != null ? body.radius : 0.4f;

        foreach (float distance in new[] { 2f, 1.6f, 2.4f })
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
                Vector3 above = target + dir * distance + Vector3.up * 1.5f;

                if (!Physics.Raycast(above, Vector3.down, out RaycastHit floor, 4f, ~0, QueryTriggerInteraction.Ignore))
                    continue;

                Vector3 feet = floor.point;
                if (Physics.CheckCapsule(feet + Vector3.up * (radius + 0.05f), feet + Vector3.up * (eye - 0.1f), radius, ~0, QueryTriggerInteraction.Ignore))
                    continue;

                Vector3 eyePos = feet + Vector3.up * eye;
                if (Physics.Linecast(eyePos, target, out RaycastHit block, ~0, QueryTriggerInteraction.Ignore) && block.collider.gameObject != piece.gameObject)
                    continue;

                Vector3 flat = target - feet;
                flat.y = 0f;
                PerfBenchmark.Teleport(player, feet + Vector3.up * 0.05f, Quaternion.LookRotation(flat).eulerAngles.y);
                yield return WaitSeconds(0.5f);
                yield break;
            }
        }

        Check(false, "achou ponto com visao para " + piece.name);
    }

    // Ajusta a mira com o mouse virtual ate o centro da tela cair sobre a peca.
    private IEnumerator Aim(Camera view, TemporalObjectPickup piece)
    {
        Collider target = piece.GetComponent<Collider>();

        for (int i = 0; i < 120; i++)
        {
            Ray ray = view.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, 5f) && hit.collider == target)
                yield break;

            Vector3 vp = view.WorldToViewportPoint(target.bounds.center);
            Vector2 delta = new Vector2((vp.x - 0.5f) * 40f, (vp.y - 0.5f) * 40f);
            yield return QAInput.Look(delta, 1);
        }

        Check(false, "mira chegou em " + piece.name);
    }
}
