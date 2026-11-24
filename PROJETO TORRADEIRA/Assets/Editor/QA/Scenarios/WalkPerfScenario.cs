using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;

// Mede o custo de andar: grava o profiler com o jogador parado, girando a camera e andando.
// A comparacao por marcador e feita depois por WalkPerfAnalysis.Analyze (editor).
// Saida: docs/revisao/qa/walkperf_*.raw e walkperf_tempos.txt
public class WalkPerfScenario : QAScenario
{
    public override string Name => "WalkPerf";

    public static string Folder => QARunner.ResultsFolder;

    public override IEnumerator Run()
    {
        yield return LoadScene("Principal");
        yield return WaitGameplay();
        DisableEnemy();
        yield return WaitSeconds(2f);

        var report = new StringBuilder();

        yield return Record("parado", 3f, null, report);
        yield return Record("girando", 3f, () => QAInput.Look(new Vector2(6f, 0f), 1), report);

        QAInput.Hold(Key.W, true);
        yield return Record("andando", 3f, () => QAInput.Look(new Vector2(1.5f, 0f), 1), report);
        QAInput.Hold(Key.W, false);

        QAInput.Hold(Key.W, true);
        QAInput.Hold(Key.LeftShift, true);
        yield return Record("correndo", 3f, () => QAInput.Look(new Vector2(-1.5f, 0f), 1), report);
        QAInput.Hold(Key.LeftShift, false);
        QAInput.Hold(Key.W, false);

        File.WriteAllText(Path.Combine(Folder, "walkperf_tempos.txt"), report.ToString());
        Debug.Log("QA WALKPERF\n" + report);
    }

    private IEnumerator Record(string label, float seconds, System.Func<IEnumerator> perFrame, StringBuilder report)
    {
        string path = Path.Combine(Folder, "walkperf_" + label + ".raw");
        if (File.Exists(path))
            File.Delete(path);

        Profiler.logFile = Path.ChangeExtension(path, null);
        Profiler.enableBinaryLog = true;
        Profiler.enabled = true;

        int frames = 0;
        float total = 0f, worst = 0f;
        float end = Time.realtimeSinceStartup + seconds;

        while (Time.realtimeSinceStartup < end)
        {
            if (perFrame != null)
                yield return perFrame();
            else
                yield return null;

            float dt = Time.unscaledDeltaTime;
            total += dt;
            worst = Mathf.Max(worst, dt);
            frames++;
        }

        Profiler.enabled = false;
        Profiler.enableBinaryLog = false;
        Profiler.logFile = "";

        float avgMs = total / Mathf.Max(1, frames) * 1000f;
        report.AppendLine(label + ": " + frames + " quadros, media " + avgMs.ToString("0.00") + " ms (" + (1000f / avgMs).ToString("0") + " FPS), pior " + (worst * 1000f).ToString("0.0") + " ms");
    }
}
