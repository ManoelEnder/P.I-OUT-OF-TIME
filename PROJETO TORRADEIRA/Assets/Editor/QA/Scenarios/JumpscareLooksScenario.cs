using System.Collections;
using System.IO;
using UnityEngine;

// Capturas do jumpscare (revisao visual): o inimigo alcanca o jogador e a sequencia e fotografada
// em cada fase (encarada, bote, ataque, circulo fechando, tela de derrota).
// Saida: docs/revisao/qa/jumpscare/*.png
public class JumpscareLooksScenario : QAScenario
{
    public override string Name => "JumpscareLooks";

    private static string Folder => Path.Combine(QARunner.ResultsFolder, "jumpscare");

    public override IEnumerator Run()
    {
        Directory.CreateDirectory(Folder);
        foreach (string f in Directory.GetFiles(Folder, "*.png")) File.Delete(f);

        yield return LoadScene("Principal");
        yield return WaitGameplay();

        TemporalEraserController enemy = Require<TemporalEraserController>();
        if (enemy == null)
            yield break;
        Transform player = GameObject.FindGameObjectWithTag("Player").transform;
        enemy.ActivateNow();
        PlaceInFrontOf(player, enemy.transform);

        float end = Time.realtimeSinceStartup + 15f;
        while (!DefeatController.IsDefeated && Time.realtimeSinceStartup < end)
            yield return null;
        Check(DefeatController.IsDefeated, "inimigo pega o jogador");
        if (!DefeatController.IsDefeated)
            yield break;

        float start = Time.realtimeSinceStartup;
        float[] marks = { 0.08f, 0.2f, 0.32f, 0.45f, 0.7f, 1.0f, 1.45f, 1.7f, 1.95f, 2.15f, 2.6f, 3.3f };
        int i = 0;
        while (i < marks.Length)
        {
            if (Time.realtimeSinceStartup - start >= marks[i])
            {
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(Folder, i.ToString("00") + "_" + marks[i].ToString("0.00").Replace(',', '.') + "s.png"));
                i++;
            }
            yield return null;
        }
        yield return WaitSeconds(0.5f);
        Check(Time.timeScale == 0f, "tela de derrota aparece depois do jumpscare");
    }
}
