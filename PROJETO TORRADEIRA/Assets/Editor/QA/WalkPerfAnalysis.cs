using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

// Compara as gravacoes do WalkPerfScenario: tempo proprio medio por marcador (ms/quadro)
// em cada situacao e o que mais aumenta ao andar.
//   Unity.exe -batchmode -projectPath <projeto> -executeMethod WalkPerfAnalysis.Analyze -quit
public static class WalkPerfAnalysis
{
    private static readonly string[] Labels = { "parado", "girando", "andando", "correndo" };

    [MenuItem("Ferramentas/QA/Analisar medicao de andar")]
    public static void Analyze()
    {
        var results = new Dictionary<string, Dictionary<string, float>>();

        foreach (string label in Labels)
        {
            string path = Path.Combine(WalkPerfScenario.Folder, "walkperf_" + label + ".raw");
            if (File.Exists(path))
                results[label] = Load(path);
        }

        var report = new StringBuilder();
        report.AppendLine("Tempo proprio medio por quadro (ms), threads principal e de render.");
        report.AppendLine();

        if (!results.ContainsKey("parado") || !results.ContainsKey("andando"))
        {
            report.AppendLine("Gravacoes faltando.");
        }
        else
        {
            var baseline = results["parado"];
            var walking = results["andando"];
            var keys = walking.Keys.Union(baseline.Keys)
                .OrderByDescending(k => Get(walking, k) - Get(baseline, k))
                .Take(40);

            report.AppendLine(string.Format("{0,-70} {1,8} {2,8} {3,8} {4,8} {5,8}", "marcador", "parado", "girando", "andando", "correndo", "delta"));
            foreach (string key in keys)
            {
                report.AppendLine(string.Format("{0,-70} {1,8:0.000} {2,8:0.000} {3,8:0.000} {4,8:0.000} {5,8:+0.000;-0.000}",
                    key.Length > 70 ? key.Substring(0, 70) : key,
                    Get(baseline, key),
                    results.ContainsKey("girando") ? Get(results["girando"], key) : 0f,
                    Get(walking, key),
                    results.ContainsKey("correndo") ? Get(results["correndo"], key) : 0f,
                    Get(walking, key) - Get(baseline, key)));
            }
        }

        string output = Path.Combine(WalkPerfScenario.Folder, "walkperf_analise.txt");
        File.WriteAllText(output, report.ToString());
        Debug.Log("WalkPerfAnalysis: " + output + "\n" + report);
    }

    private static float Get(Dictionary<string, float> map, string key)
    {
        return map.TryGetValue(key, out float v) ? v : 0f;
    }

    // Soma o tempo proprio de cada marcador (por thread) e divide pelo numero de quadros.
    private static Dictionary<string, float> Load(string path)
    {
        var totals = new Dictionary<string, float>();
        ProfilerDriver.ClearAllFrames();

        if (!ProfilerDriver.LoadProfile(path, false))
        {
            Debug.LogError("WalkPerfAnalysis: nao abriu " + path);
            return totals;
        }

        int first = ProfilerDriver.firstFrameIndex;
        int last = ProfilerDriver.lastFrameIndex;
        int frames = 0;
        var children = new List<int>();

        // Pula os primeiros quadros (gravacao comecando).
        for (int frame = first + 3; frame <= last; frame++)
        {
            frames++;

            for (int thread = 0; thread < 4; thread++)
            {
                using (HierarchyFrameDataView view = ProfilerDriver.GetHierarchyFrameDataView(frame, thread, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName, HierarchyFrameDataView.columnSelfTime, false))
                {
                    if (view == null || !view.valid)
                        continue;

                    string threadName = view.threadName;
                    if (thread > 0 && !threadName.Contains("Render"))
                        continue;

                    var stack = new Stack<int>();
                    stack.Push(view.GetRootItemID());

                    while (stack.Count > 0)
                    {
                        int id = stack.Pop();
                        children.Clear();
                        view.GetItemChildren(id, children);
                        foreach (int child in children)
                            stack.Push(child);

                        if (id == view.GetRootItemID())
                            continue;

                        string name = (thread == 0 ? "" : "[render] ") + view.GetItemName(id);
                        float self = view.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnSelfTime);
                        totals[name] = (totals.TryGetValue(name, out float t) ? t : 0f) + self;
                    }
                }
            }
        }

        var averages = new Dictionary<string, float>();
        foreach (var pair in totals)
            averages[pair.Key] = pair.Value / Mathf.Max(1, frames);

        averages["(quadros analisados)"] = frames;
        return averages;
    }
}
