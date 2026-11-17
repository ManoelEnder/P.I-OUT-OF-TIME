using System.Collections.Generic;
using UnityEngine;

public struct PerfSummary
{
    public string Segment;
    public int Frames;
    public float AvgFps;
    public float Low1Fps;
    public float WorstMs;
    public int FramesOver16ms;

    public string ToCsv()
    {
        return Segment + "," + Frames + "," + AvgFps.ToString("0.0") + "," + Low1Fps.ToString("0.0") + "," +
               WorstMs.ToString("0.00") + "," + FramesOver16ms;
    }

    public const string CsvHeader = "segmento,quadros,fps_medio,fps_1pct_low,pior_ms,quadros_acima_16_67ms";
}

// Resumo de um trecho a partir do tempo de cada quadro (ms).
public static class PerfStats
{
    public const float BudgetMs = 1000f / 60f;

    public static PerfSummary Compute(string segment, IReadOnlyList<float> frameMs)
    {
        var summary = new PerfSummary { Segment = segment, Frames = frameMs.Count };

        if (frameMs.Count == 0)
            return summary;

        float total = 0f;
        var sorted = new List<float>(frameMs);

        foreach (float ms in frameMs)
        {
            total += ms;
            if (ms > BudgetMs)
                summary.FramesOver16ms++;
        }

        sorted.Sort((a, b) => b.CompareTo(a));
        int slowCount = Mathf.Max(1, Mathf.CeilToInt(frameMs.Count * 0.01f));
        float slowTotal = 0f;
        for (int i = 0; i < slowCount; i++)
            slowTotal += sorted[i];

        summary.AvgFps = frameMs.Count * 1000f / total;
        summary.Low1Fps = 1000f / (slowTotal / slowCount);
        summary.WorstMs = sorted[0];
        return summary;
    }
}
