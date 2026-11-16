using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Teste em Play Mode pelo botão Jogar. Modo "full" deixa a cutscene terminar; "skip" pula no meio.
// Menu: Ferramentas > Cutscene de Abertura > Testar (completo | pular). Resultado em Temp/IntroCutscene.log.
[InitializeOnLoad]
public static class IntroCutscenePlayTest
{
    const string ModeKey = "IntroCutscene.TestMode";
    const string LogPath = "Temp/IntroCutscene.log";
    static double phaseStart, cutsceneStart;
    static int step;

    static IntroCutscenePlayTest()
    {
        EditorApplication.playModeStateChanged += s =>
        {
            if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetString(ModeKey, "") != "")
            {
                step = 0; phaseStart = EditorApplication.timeSinceStartup;
                SceneManager.activeSceneChanged += (a, b) => Log($"cena ativa: {b.name} em {EditorApplication.timeSinceStartup - phaseStart:0.0}s");
                EditorApplication.update += Tick;
            }
            if (s == PlayModeStateChange.EnteredEditMode) SessionState.EraseString(ModeKey);
        };
        EditorApplication.update += PollRequest;
    }

    static void PollRequest()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists("Temp/IntroCutscene.test")) return;
        string mode = File.ReadAllText("Temp/IntroCutscene.test").Trim();
        File.Delete("Temp/IntroCutscene.test");
        Start(mode);
    }

    [MenuItem("Ferramentas/Cutscene de Abertura/Testar (deixar terminar)")] static void Full() => Start("full");
    [MenuItem("Ferramentas/Cutscene de Abertura/Testar (pular no meio)")] static void Skip() => Start("skip");

    static void Start(string mode)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(mode == "baseline" ? "Assets/Scenes/Principal.unity" : "Assets/Scenes/Menu.unity", OpenSceneMode.Single);
        SessionState.SetString(ModeKey, mode);
        Log("teste em Play Mode: " + mode);
        EditorApplication.EnterPlaymode();
    }

    static void Log(string msg)
    {
        Debug.Log("[IntroCutscene teste] " + msg);
        File.AppendAllText(LogPath, System.DateTime.Now.ToString("HH:mm:ss ") + "TESTE " + msg + "\n");
    }

    static void Down(Key key) => InputSystem.QueueStateEvent(Keyboard.current ?? InputSystem.AddDevice<Keyboard>(), new KeyboardState(key));
    static void Up() => InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());

    static void Tick()
    {
        if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
        double now = EditorApplication.timeSinceStartup;
        string scene = SceneManager.GetActiveScene().name;
        string mode = SessionState.GetString(ModeKey, "full");

        switch (step)
        {
            case 0 when mode == "baseline":
                phaseStart = now; step = 5;
                break;
            case 0 when mode == "menu" && now - phaseStart > 2.0:
                Log("controle: Menu -> Principal sem cutscene");
                SceneManager.LoadScene("Principal");
                cutsceneStart = now; step = 4;
                break;
            case 0 when now - phaseStart > 2.0:
                var play = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(b => new SerializedObject(b).FindProperty("m_OnClick.m_PersistentCalls.m_Calls") is SerializedProperty calls &&
                        Enumerable.Range(0, calls.arraySize).Any(i => calls.GetArrayElementAtIndex(i).FindPropertyRelative("m_Arguments.m_StringArgument").stringValue == "IntroCutscene"));
                if (play == null) { Log("FALHA: botão Jogar não encontrado"); Stop(); return; }
                Log("clicando em: " + play.name);
                play.onClick.Invoke();
                step = 1;
                break;
            case 1 when scene == "IntroCutscene":
                cutsceneStart = now;
                var loads = 0;
                SceneManager.sceneLoaded += (s, m) => { if (s.name == "Principal") Log($"Principal carregada (carga nº {++loads})"); };
                step = 2;
                break;
            case 2 when mode == "skip" && now - cutsceneStart > 45:
                Down(Key.Escape);
                step = 3;
                phaseStart = now;
                break;
            case 3 when now - phaseStart > 0.6:
                Up();
                step = 30; phaseStart = now;
                break;
            case 30 when now - phaseStart > 0.6:
                Down(Key.Escape);
                step = 31; phaseStart = now;
                break;
            case 31 when now - phaseStart > 0.3:
                Up();
                Log("pulo pedido em " + (now - cutsceneStart).ToString("0.0") + "s");
                step = 4;
                break;
            case 2:
            case 4:
                if (scene == "Principal")
                {
                    Log($"duração da cutscene (relógio do Editor): {now - cutsceneStart:0.0}s");
                    phaseStart = now; step = 5;
                }
                else if (now - cutsceneStart > 130) { Log("FALHA: cutscene não terminou em 130 s"); Stop(); }
                break;
            case 5 when now - phaseStart > 9:
                var photo = Object.FindFirstObjectByType<PhotoCamera>();
                var move = Object.FindFirstObjectByType<PlayerMovement>();
                var look = Object.FindFirstObjectByType<MouseLook>();
                Log($"retomada: timeScale={Time.timeScale} cursor={Cursor.lockState}/{(Cursor.visible ? "visível" : "oculto")} " +
                    $"PlayerMovement={(move != null && move.isActiveAndEnabled)} MouseLook={(look != null ? look.isActiveAndEnabled.ToString() : "n/a")} " +
                    $"PhotoCamera={(photo != null && photo.isActiveAndEnabled)} câmera principal={(Camera.main != null ? Camera.main.name : "nenhuma")}");
                Down(Key.C);
                Log("C pressionado? " + (Keyboard.current != null) );
                phaseStart = now; step = 50;
                break;
            case 50 when now - phaseStart > 0.3:
                Up();
                phaseStart = now; step = 6;
                break;
            case 6 when now - phaseStart > 1.5:
                foreach (var p in Object.FindObjectsByType<PhotoCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    Log($"PhotoCamera {p.name}: ativo={p.isActiveAndEnabled} temCamera={p.HasCamera} modo={p.IsCameraMode}");
                var pc = Object.FindFirstObjectByType<PhotoCamera>();
                Log("visor abre com C: " + (pc != null && pc.IsCameraMode ? "SIM" : "NÃO"));
                Stop();
                break;
        }
    }

    static void Stop()
    {
        EditorApplication.update -= Tick;
        Log("fim do teste");
        EditorApplication.ExitPlaymode();
    }
}
