using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Mede o tempo de cada quadro por trecho nomeado e grava CSV em <persistentDataPath>/perf/.
// Ativo em build de desenvolvimento, ou em qualquer build/editor com -perf ou -benchmark.
public class PerfProbe : MonoBehaviour
{
    private static PerfProbe instance;

    private readonly List<float> frames = new List<float>();
    private string segment;
    private bool skipFirstFrame;
    private string pendingLoad;
    private float unloadStart = -1f;
    private string folder;
    private string stamp;

    public static bool Active =>
        HasArg("-perf") || HasArg("-benchmark") || (!Application.isEditor && Debug.isDebugBuild);

    public static string Folder => instance != null ? instance.folder : null;

    public static bool HasArg(string arg)
    {
        return Array.IndexOf(Environment.GetCommandLineArgs(), arg) >= 0;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        if (!Active || instance != null)
            return;

        GameObject go = new GameObject("PerfProbe");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PerfProbe>();
    }

    private void Awake()
    {
        folder = Path.Combine(Application.persistentDataPath, "perf");
        stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        Directory.CreateDirectory(folder);
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        Debug.Log("PerfProbe ativo: " + folder);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    public static void Begin(string name)
    {
        if (instance == null)
            return;

        End();
        instance.segment = name;
        instance.frames.Clear();
        instance.skipFirstFrame = true;
    }

    public static PerfSummary End()
    {
        if (instance == null || instance.segment == null)
            return default;

        PerfSummary summary = PerfStats.Compute(instance.segment, instance.frames);
        instance.Save(summary, instance.frames);
        instance.segment = null;
        return summary;
    }

    // Carregamento = da descarga da cena anterior ate o primeiro quadro da nova (Awake/Start inclusos).
    private void OnSceneUnloaded(Scene scene)
    {
        unloadStart = Time.realtimeSinceStartup;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        pendingLoad = "load:" + scene.name;
        if (unloadStart < 0f)
            unloadStart = Time.realtimeSinceStartup - Time.unscaledDeltaTime;
    }

    private void Update()
    {
        float ms = Time.unscaledDeltaTime * 1000f;

        if (pendingLoad != null)
        {
            float loadMs = (Time.realtimeSinceStartup - unloadStart) * 1000f;
            Save(PerfStats.Compute(pendingLoad, new[] { loadMs }), null);
            pendingLoad = null;
            unloadStart = -1f;
            return;
        }

        if (segment == null)
            return;

        if (skipFirstFrame)
        {
            skipFirstFrame = false;
            return;
        }

        frames.Add(ms);
    }

    private void Save(PerfSummary summary, List<float> raw)
    {
        string summaryPath = Path.Combine(folder, stamp + "_summary.csv");
        if (!File.Exists(summaryPath))
            File.WriteAllText(summaryPath, PerfSummary.CsvHeader + "\n");

        File.AppendAllText(summaryPath, summary.ToCsv() + "\n");
        Debug.Log("PERF " + summary.ToCsv());

        if (raw == null)
            return;

        var lines = new List<string>(raw.Count);
        foreach (float ms in raw)
            lines.Add(ms.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture));

        File.WriteAllLines(Path.Combine(folder, stamp + "_" + summary.Segment.Replace(':', '_') + ".csv"), lines);
    }
}
