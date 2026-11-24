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
        DisableEnemy();

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

        yield return Approach(player, view, piece.GetComponent<Collider>());
        yield return Reveal(photo, view, piece);
        Check(piece.CanInteract(), "foto revela a peca (coletavel)");
        Check(piece.GetComponent<Renderer>().enabled, "peca revelada fica visivel");

        yield return QAInput.Tap(Key.C);
        yield return WaitTransition(photo);
        yield return Aim(view, piece.GetComponent<Collider>());

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
        yield return Approach(player, view, other.GetComponent<Collider>());
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
        yield return WaitTransition(photo);
        yield return Aim(view, piece.GetComponent<Collider>());
        yield return QAInput.Click();
        yield return WaitSeconds(0.8f);
    }

}
