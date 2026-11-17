using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

// Executa os cenarios de teste (Ferramentas > QA ou em batch com -qaScenario <Nome|All>).
// Cada cenario de play mode entra no Play do zero (com recarga de codigo, como o jogo real);
// o estado do runner atravessa a recarga pelo SessionState.
[InitializeOnLoad]
public static class QARunner
{
    private const string KeyActive = "QA_Active";
    private const string KeyQueue = "QA_Queue";
    private const string KeyCurrent = "QA_Current";
    private const string KeyStart = "QA_Start";
    private const string KeyResults = "QA_Results";
    private const string KeyExit = "QA_Exit";
    private const double Timeout = 120.0;

    private static QAScenario running;
    private static readonly List<string> logErrors = new List<string>();
    private static InputSettings originalInputSettings;

    public static string ResultsFolder =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/revisao/qa"));

    static QARunner()
    {
        if (!SessionState.GetBool(KeyActive, false))
            return;

        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
        EditorApplication.update -= Pump;
        EditorApplication.update += Pump;
    }

    public static void RunFromCommandLine()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-qaScenario");
        string which = index >= 0 && index + 1 < args.Length ? args[index + 1] : "All";
        Begin(which, true);
    }

    [MenuItem("Ferramentas/QA/Rodar todos os cenarios")]
    private static void RunAllFromMenu()
    {
        Begin("All", false);
    }

    private static void Begin(string which, bool exitWhenDone)
    {
        List<QAScenario> selected = CreateAll()
            .Where(s => which == "All" || string.Equals(s.Name, which, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.NeedsPlayMode)
            .ToList();

        if (selected.Count == 0)
        {
            Debug.LogError("QA: nenhum cenario chamado " + which);
            if (exitWhenDone)
                EditorApplication.Exit(1);
            return;
        }

        SessionState.SetString(KeyQueue, string.Join(",", selected.Select(s => s.Name)));
        SessionState.SetString(KeyCurrent, "");
        SessionState.SetString(KeyResults, "");
        SessionState.SetBool(KeyExit, exitWhenDone);
        SessionState.SetBool(KeyActive, true);

        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
        EditorApplication.update -= Pump;
        EditorApplication.update += Pump;
    }

    private static IEnumerable<QAScenario> CreateAll()
    {
        foreach (Type type in TypeCache.GetTypesDerivedFrom<QAScenario>())
        {
            if (!type.IsAbstract)
                yield return (QAScenario)Activator.CreateInstance(type);
        }
    }

    private static QAScenario Create(string name)
    {
        return CreateAll().First(s => s.Name == name);
    }

    private static void Pump()
    {
        string current = SessionState.GetString(KeyCurrent, "");

        if (current.Length == 0)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (EditorApplication.isPlaying)
                    EditorApplication.ExitPlaymode();
                return;
            }

            StartNext();
            return;
        }

        if (EditorApplication.timeSinceStartup - double.Parse(SessionState.GetString(KeyStart, "0")) > Timeout)
        {
            QAScenario timedOut = running ?? Create(current);
            timedOut.Failures.Add("tempo esgotado (" + Timeout + " s)");
            Finish(timedOut);
            return;
        }

        if (!EditorApplication.isPlaying)
            return;

        if (running == null)
        {
            running = Create(current);
            ConfigureInput();
            QACoroutineHost.Get().Run(running.Run());
            return;
        }

        if (!QACoroutineHost.Get().Running)
            Finish(running);
    }

    private static void StartNext()
    {
        List<string> queue = SessionState.GetString(KeyQueue, "")
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

        if (queue.Count == 0)
        {
            Complete();
            return;
        }

        string name = queue[0];
        queue.RemoveAt(0);
        SessionState.SetString(KeyQueue, string.Join(",", queue));
        SessionState.SetString(KeyCurrent, name);
        SessionState.SetString(KeyStart, EditorApplication.timeSinceStartup.ToString("R"));
        logErrors.Clear();

        QAScenario scenario = Create(name);

        if (!scenario.NeedsPlayMode)
        {
            try
            {
                IEnumerator routine = scenario.Run();
                while (routine.MoveNext()) { }
            }
            catch (Exception e)
            {
                scenario.Failures.Add("excecao: " + e.Message);
            }

            Finish(scenario);
            return;
        }

        Debug.Log("QA iniciando " + name);
        EditorApplication.EnterPlaymode();
    }

    private static void Finish(QAScenario scenario)
    {
        scenario.Failures.AddRange(logErrors);
        logErrors.Clear();

        bool pass = scenario.Failures.Count == 0;
        string line = pass
            ? "QA PASS " + scenario.Name
            : "QA FAIL " + scenario.Name + ": " + string.Join(" | ", scenario.Failures);

        Debug.Log(line);
        Directory.CreateDirectory(ResultsFolder);
        File.WriteAllText(Path.Combine(ResultsFolder, scenario.Name + ".txt"),
            line + "\n" + string.Join("\n", scenario.Failures.Select(f => "- " + f)) + "\n");

        SessionState.SetString(KeyResults, SessionState.GetString(KeyResults, "") + line + "\n");
        SessionState.SetString(KeyCurrent, "");

        running = null;
        RestoreInput();

        if (EditorApplication.isPlaying)
            EditorApplication.ExitPlaymode();
    }

    private static void Complete()
    {
        string results = SessionState.GetString(KeyResults, "");
        bool anyFail = results.Contains("QA FAIL");
        bool exit = SessionState.GetBool(KeyExit, false);

        SessionState.SetBool(KeyActive, false);
        EditorApplication.update -= Pump;
        Application.logMessageReceived -= OnLog;

        Debug.Log("QA RESUMO\n" + results);

        if (exit)
            EditorApplication.Exit(anyFail ? 1 : 0);
    }

    // Erros e excecoes do jogo durante o cenario contam como falha.
    private static void OnLog(string message, string stackTrace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            return;

        if (message.StartsWith("QA "))
            return;

        string first = message.Split('\n')[0];
        string where = stackTrace.Split('\n').FirstOrDefault(l => l.Contains("Assets/")) ?? "";
        logErrors.Add(type + ": " + first + (where.Length > 0 ? " @ " + where.Trim() : ""));
    }

    // Sem janela do Game em foco: o input virtual precisa chegar mesmo assim.
    private static void ConfigureInput()
    {
        originalInputSettings = InputSystem.settings;
        InputSettings copy = UnityEngine.Object.Instantiate(originalInputSettings);
        copy.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        copy.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings = copy;
        QAInput.Ensure();
    }

    private static void RestoreInput()
    {
        if (originalInputSettings != null)
            InputSystem.settings = originalInputSettings;

        originalInputSettings = null;
    }
}
