using System.Collections;
using System.IO;
using UnityEngine;

// Capturas da sala de montagem (revisao visual) de varios pontos, na altura dos olhos do jogador.
// Saida: docs/revisao/qa/sala_montagem/*.png
public class AssemblyRoomLooksScenario : QAScenario
{
    public override string Name => "AssemblyRoomLooks";

    public override IEnumerator Run()
    {
        string folder = Path.Combine(QARunner.ResultsFolder, "sala_montagem");
        Directory.CreateDirectory(folder);
        foreach (string f in Directory.GetFiles(folder, "*.png")) File.Delete(f);

        yield return LoadScene("Principal");
        yield return WaitGameplay();
        DisableEnemy();
        yield return WaitSeconds(0.5f);

        AssemblyStation st = AssemblyStation.Instance;
        Check(st != null, "estacao na fase");
        if (st == null) yield break;
        AssemblyRoomDecor decor = st.GetComponent<AssemblyRoomDecor>();
        Check(decor != null, "decoracao montada");
        int pieces = decor != null ? decor.transform.childCount : 0;
        Debug.Log("QA TRACE sala: centro=" + st.floorCenter + " objetos=" + pieces + " kit=" + (Resources.Load<AssemblyRoomKit>("AssemblyRoomKit") != null));

        if (decor != null)
        {
            var names = new System.Collections.Generic.Dictionary<string, int>();
            foreach (Transform t in decor.transform) names[t.name] = names.TryGetValue(t.name, out int n) ? n + 1 : 1;
            var sb = new System.Text.StringBuilder("QA TRACE pecas:");
            foreach (var kv in names) sb.Append(" " + kv.Key + "x" + kv.Value);
            Debug.Log(sb.ToString());
        }
        Transform player = GameObject.FindGameObjectWithTag("Player").transform;
        CharacterController cc = player.GetComponent<CharacterController>();
        Vector3 c = st.floorCenter;
        float hx = st.size.x * 0.5f, hz = st.size.y * 0.5f;
        var views = new (string name, Vector3 pos, Vector3 look)[]
        {
            ("01_entrada", c + new Vector3(0f, 0f, -hz + 0.6f), c + new Vector3(0f, 1f, hz)),
            ("02_fundo_para_porta", c + new Vector3(0.3f, 0f, hz - 0.9f), c + new Vector3(-0.3f, 1f, -hz)),
            ("03_bancada", c + new Vector3(0.8f, 0f, -0.6f), c + new Vector3(-hx, 0.9f, -0.4f)),
            ("04_painel", c + new Vector3(-0.8f, 0f, 0.4f), c + new Vector3(hx, 1.2f, 0.6f)),
            ("05_canto", c + new Vector3(hx - 0.7f, 0f, -hz + 0.7f), c + new Vector3(-hx, 0.8f, hz)),
        };
        foreach (var v in views)
        {
            if (cc != null) cc.enabled = false;
            player.position = v.pos + Vector3.up * 0.1f;
            Vector3 flat = v.look - v.pos;
            flat.y = 0f;
            player.rotation = Quaternion.LookRotation(flat);
            if (cc != null) cc.enabled = true;
            yield return WaitSeconds(0.8f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, v.name + ".png"));
            yield return null;
        }
    }
}
