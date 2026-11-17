using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class PerfStatsCheck : QAScenario
{
    public override string Name => "PerfStatsCheck";
    public override bool NeedsPlayMode => false;

    public override IEnumerator Run()
    {
        var frames = new List<float>();
        for (int i = 0; i < 99; i++) frames.Add(10f);
        frames.Add(40f);

        PerfSummary s = PerfStats.Compute("teste", frames);
        Check(s.Frames == 100, "conta 100 quadros");
        Check(Mathf.Abs(s.AvgFps - 97.087f) < 0.1f, "FPS medio ~97.09 (" + s.AvgFps + ")");
        Check(Mathf.Abs(s.Low1Fps - 25f) < 0.01f, "1% low = 25 (" + s.Low1Fps + ")");
        Check(Mathf.Abs(s.WorstMs - 40f) < 0.01f, "pior quadro 40 ms");
        Check(s.FramesOver16ms == 1, "1 quadro acima de 16,67 ms");

        PerfSummary empty = PerfStats.Compute("vazio", new List<float>());
        Check(empty.Frames == 0, "lista vazia nao quebra");
        yield break;
    }
}
