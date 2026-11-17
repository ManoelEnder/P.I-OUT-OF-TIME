# Revisão completa — Parte 1: linha de base, bugs e organização — Plano de implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ter ferramentas de teste e medição, a linha de base registrada (erros, FPS, capturas), todos os bugs do fluxo corrigidos e `Assets` organizado e limpo, com o jogo passando em todos os cenários automatizados.

**Architecture:** Testes são "cenários" que dirigem o jogo em play mode pelo Unity em modo batch, com teclado e mouse virtuais do Input System; falham em qualquer exceção, erro de log ou asserção. Uma sonda de desempenho em runtime grava o tempo de cada quadro por trecho; um benchmark roda os mesmos trechos sozinho dentro de uma build. A limpeza se apoia num mapa de referências gerado pelo editor. A Parte 2 (desempenho, visual, validação final) terá plano próprio, escrito com os números desta parte.

**Tech Stack:** Unity 6000.2.7f2, URP, Input System 1.14.2, C# (Assembly-CSharp, sem asmdef), Windows Standalone.

**Spec:** `docs/superpowers/specs/2026-11-17-revisao-completa-design.md`

## Global Constraints

- Unity: `C:/Program Files/Unity/Hub/Editor/6000.2.7f2/Editor/Unity.exe`; projeto: `C:/Program Files/GitHub/P.I-Projeto-Sasson/PROJETO TORRADEIRA`. **O editor precisa estar fechado** durante qualquer execução em batch; se o log disser "another Unity instance is running", pare e peça ao autor para fechar.
- Paleta: branco, cinzas e vermelho; vermelho só para calor e perigo; sem verde, sem laranja/amarelo.
- Commits na `main`, título curto em português, **sem descrição e sem coautoria**. Hierarquia com nomes em inglês.
- Todo efeito que pisca respeita `FlashSettings.Enabled`.
- Não mexer nas fitas de isolamento (o autor faz).
- Preservar campos do Inspector, eventos e métodos públicos; se um nome mudar, mudar os consumidores no mesmo commit.
- Mover/renomear assets só com `AssetDatabase.MoveAsset`/`RenameAsset` (preserva GUID e `.meta`).
- Ao rodar qualquer ferramenta em batch, desfazer churn não intencional antes do commit: `git checkout -- "Assets/Font" "Assets/TextMesh Pro" ProjectSettings/EditorSettings.asset` e materiais que só mudaram fim de linha (`git diff --ignore-cr-at-eol` vazio).
- Arquivos de entrega (relatórios, capturas, CSV) em `docs/revisao/` na raiz do repositório.
- Fluxo oficial: `TelaInicio` → `Menu` → `IntroCutsceneVideo` → `Principal` → `Victory TEMP` → `Menu`; `Credits` a partir do menu.

## Review Focus

1. **Jogar duas vezes seguidas** (Principal → Menu → Principal): estado estático antigo (`PauseMenu.IsPaused`, `DefeatController.IsDefeated`, `PhotoLocation.All`, missões, `Time.timeScale`, cursor) não pode vazar para a segunda partida. → teste no Task 6.
2. **ESC no mesmo quadro de uma transição** (abrindo/fechando o visor, abrindo a derrota): nada de pausa por cima de outro modal nem cursor travado. → teste no Task 6.
3. **Álbum lotado e mais uma foto**: não trava, não vaza textura (contagem de `Texture2D` vivas não cresce além do limite). → teste no Task 7.
4. **Superaquecer de novo com a câmera já perdida**: continua existindo um único `LostCameraPickup` ativo. → teste no Task 9.
5. **Primeiro lançamento da build** usa 1024x768: a build deve abrir em 1920x1080 tela cheia. → passo no Task 4.

---

### Task 1: Infraestrutura de cenários em play mode

**Files:**
- Create: `Assets/Scripts/Diagnostics/QAInput.cs` (runtime: também usado pelo benchmark na build)
- Create: `Assets/Editor/QA/QAScenario.cs`
- Create: `Assets/Editor/QA/QARunner.cs`
- Create: `Assets/Editor/QA/Scenarios/SmokeScenario.cs`

**Interfaces:**
- Produces:
  - `public static class QAInput` — `static void Ensure()` (adiciona `Keyboard` e `Mouse` virtuais se não houver), `static IEnumerator Tap(Key key)` (pressiona num quadro, solta no seguinte), `static IEnumerator Click()` (botão esquerdo), `static IEnumerator Scroll(float y)`, `static void Hold(Key key, bool down)`. Implementação com `InputSystem.AddDevice<T>()` + `InputSystem.QueueStateEvent` + `KeyboardState`/`MouseState`.
  - `public abstract class QAScenario` — `public abstract string Name { get; }`, `public abstract IEnumerator Run()`, helpers protegidos `IEnumerator LoadScene(string name)` (espera carregar + 1 quadro), `IEnumerator WaitSeconds(float realtime)`, `void Check(bool condition, string message)` (registra falha, não interrompe), `T Require<T>() where T : Object` (falha se não achar, inclusive inativos).
  - `public static class QARunner` — entrada batch `public static void RunFromCommandLine()` lendo `-qaScenario <Name|All>`; menu **Ferramentas › QA › Rodar cenário…** para uso manual. Liga `EnterPlayModeOptions.DisableDomainReload` só durante a execução e restaura o valor original antes de sair. Captura `Application.logMessageReceived` (Exception/Error/Assert = falha, exceto linhas que comecem com `QA `). Timeout por cenário: 120 s realtime. Escreve `QA PASS <Name>` ou `QA FAIL <Name>: <motivo>` no log e em `docs/revisao/qa/<Name>.txt`; sai com `EditorApplication.Exit(0)` se tudo passou, `1` se não.
  - Cenários são descobertos por reflexão (todas as subclasses não abstratas de `QAScenario`).

- [ ] **Step 1: Escrever o cenário de fumaça**

`SmokeScenario` (`Name = "Smoke"`): carrega `Menu`, espera 3 s, `Check(Object.FindObjectsByType<AudioListener>(...).Length == 1, "um AudioListener no Menu")`, `yield return QAInput.Tap(Key.Space)`, espera 1 s.

- [ ] **Step 2: Rodar e ver falhar (runner ainda não existe)**

Run: `"$UNITY" -batchmode -projectPath "$PROJ" -executeMethod QARunner.RunFromCommandLine -qaScenario Smoke -logFile docs/revisao/qa/unity-smoke.log`
Expected: log com `error CS0246` (QARunner/QAScenario inexistentes) ou "executeMethod class not found".

- [ ] **Step 3: Implementar `QAInput`, `QAScenario` e `QARunner` com as interfaces acima**

- [ ] **Step 4: Rodar de novo**

Run: o mesmo comando do Step 2.
Expected: `QA PASS Smoke`, código de saída 0, `ProjectSettings/EditorSettings.asset` sem diferença no `git status`.

- [ ] **Step 5: Commit**

```bash
git add "PROJETO TORRADEIRA/Assets/Scripts/Diagnostics" "PROJETO TORRADEIRA/Assets/Editor/QA" docs/revisao/qa/Smoke.txt
git commit -m "Ferramentas de teste em play mode"
```

### Task 2: Auditoria das cenas do build

**Files:**
- Create: `Assets/Editor/QA/SceneAudit.cs`

**Interfaces:**
- Produces: `public static class SceneAudit` — `public static AuditReport Run()` e entrada batch `public static void RunFromCommandLine()` (menu **Ferramentas › QA › Auditar cenas do build**). `AuditReport` tem `List<string> MissingScripts`, `List<string> NullReferences`, `List<string> MissingPrefabs`, `int Total`. Para cada cena **habilitada** no build: objetos com script faltando (`GameObjectUtility.GetMonoBehavioursWithMissingScriptCount`), prefabs com asset ausente (`PrefabUtility.IsPrefabAssetMissing`), e campos `ObjectReference` nulos com `[SerializeField]`/públicos em componentes do Assembly-CSharp (formato: `Cena/Caminho/Do/Objeto : Componente.campo`). Escreve `docs/revisao/auditoria.md` com as três listas e os totais.

- [ ] **Step 1: Rodar a auditoria (falha: classe inexistente)**

Run: `"$UNITY" -batchmode -nographics -quit -projectPath "$PROJ" -executeMethod SceneAudit.RunFromCommandLine -logFile docs/revisao/qa/unity-audit.log`
Expected: erro de método não encontrado.

- [ ] **Step 2: Implementar `SceneAudit`**

- [ ] **Step 3: Rodar e conferir o relatório**

Expected: `docs/revisao/auditoria.md` existe, lista as 8 cenas habilitadas do build (antes da limpeza: TelaInicio, Menu, IntroCutsceneVideo, IntroCutscene, Principal, Credits, Radio, Victory TEMP) e imprime `AUDIT TOTAL <n>` no log. Copiar o arquivo para `docs/revisao/auditoria-antes.md`.

- [ ] **Step 4: Commit**

```bash
git add "PROJETO TORRADEIRA/Assets/Editor/QA/SceneAudit.cs*" docs/revisao/auditoria-antes.md
git commit -m "Auditoria das cenas do build"
```

### Task 3: Mapa de referências

**Files:**
- Create: `Assets/Editor/QA/ReferenceMap.cs`

**Interfaces:**
- Produces: `public static class ReferenceMap` — `public static HashSet<string> UsedAssets()` e entrada batch `RunFromCommandLine()` (menu **Ferramentas › QA › Mapa de referências**). Raízes: cenas habilitadas no build; todos os assets sob qualquer pasta `Resources`; caminhos literais `"Assets/..."` encontrados em `.cs` sob `Assets/Editor` e `Assets/Cutscene/Editor` (as ferramentas de montagem carregam assets por caminho); `ProjectSettings/GraphicsSettings.asset` e `QualitySettings.asset` (pipeline, shaders sempre incluídos); arquivos `.cs` (scripts nunca entram como "não usados"). Expande com `AssetDatabase.GetDependencies(path, true)`. Escreve `docs/revisao/nao-usados.md`: assets não usados agrupados pela pasta de primeiro e segundo nível, com o tamanho em disco de cada grupo, e uma seção "carregado por nome — conferir" com cada `Resources.Load`, `Shader.Find` e `LoadScene` encontrado nos scripts.

- [ ] **Step 1: Escrever a checagem do mapa como cenário de editor**

Adicionar ao `QARunner` a checagem `ReferenceMapCheck` (não é play mode): `UsedAssets()` deve conter `Assets/Scenes/Principal.unity`, `Assets/Font/VCR_OSD_MONO SDF.asset` e `Assets/Prefabs/Enemy/Vampire A Lusth.fbx`; e **não** deve conter `Assets/_Recovery/0.unity`.

- [ ] **Step 2: Rodar e ver falhar**

Run: `... -executeMethod QARunner.RunFromCommandLine -qaScenario ReferenceMapCheck ...`
Expected: `QA FAIL ReferenceMapCheck` (ReferenceMap inexistente → erro de compilação).

- [ ] **Step 3: Implementar `ReferenceMap`**

- [ ] **Step 4: Rodar de novo**

Expected: `QA PASS ReferenceMapCheck` e `docs/revisao/nao-usados.md` gerado.

- [ ] **Step 5: Commit**

```bash
git add "PROJETO TORRADEIRA/Assets/Editor/QA" docs/revisao/nao-usados.md
git commit -m "Mapa de referencias dos assets"
```

### Task 4: Sonda de desempenho, benchmark e builds

**Files:**
- Create: `Assets/Scripts/Diagnostics/PerfStats.cs`
- Create: `Assets/Scripts/Diagnostics/PerfProbe.cs`
- Create: `Assets/Scripts/Diagnostics/PerfBenchmark.cs`
- Create: `Assets/Editor/QA/BuildTools.cs`
- Modify: `ProjectSettings/ProjectSettings.asset` (via `PlayerSettings`, dentro de `BuildTools`)

**Interfaces:**
- Produces:
  - `public struct PerfSummary { public string Segment; public int Frames; public float AvgFps; public float Low1Fps; public float WorstMs; public int FramesOver16ms; }`
  - `public static class PerfStats` — `public static PerfSummary Compute(string segment, IReadOnlyList<float> frameMs)`. 1% low = média dos 1% quadros mais lentos convertida em FPS (mínimo 1 quadro).
  - `public class PerfProbe : MonoBehaviour` — `public static bool Active` (true se `Debug.isDebugBuild` ou argumento `-perf`); `public static void Begin(string segment)`, `public static PerfSummary End()`; `[RuntimeInitializeOnLoadMethod]` cria uma instância `DontDestroyOnLoad` quando ativo; grava `<persistentDataPath>/perf/<data>_<segment>.csv` (um tempo por linha, `Time.unscaledDeltaTime * 1000`) e acrescenta o resumo em `perf/summary.csv`. Carregamentos de cena entram como segmento próprio `load:<cena>` (do `SceneManager.sceneLoaded` anterior ao primeiro quadro da nova cena).
  - `public class PerfBenchmark : MonoBehaviour` — roda só com o argumento `-benchmark`; usa `QAInput`. Segmentos fixos, 20 s cada, nesta ordem: `menu`, `principal_normal` (gira 360° lentamente no spawn), `principal_pesado` (pontos definidos no Task 5), `visor_aberto` (C, zoom ida e volta), `fotos_sequencia` (10 cliques espaçados pelo cooldown), `album` (F, navegar setas, F), `superaquecimento` (visor aberto até `CameraOverheat.IsCameraLost`), `inimigo_perseguindo` (teleporte a 6 m do TemporalEraser). Ao final, `Application.Quit()`.
  - `public static class BuildTools` — `BuildDev()` (Development + Profiler conectável, `Builds/Windows-Dev/OutOfTime.exe`), `BuildRelease()` (`Builds/Windows/OutOfTime.exe`), ambos com as cenas habilitadas do build; antes de buildar, define `PlayerSettings.defaultScreenWidth = 1920`, `defaultScreenHeight = 1080`, `fullScreenMode = FullScreenMode.FullScreenWindow`. Menu **Ferramentas › QA › Build de desenvolvimento / Build de release**.

- [ ] **Step 1: Escrever a checagem dos cálculos**

Checagem de editor `PerfStatsCheck` no `QARunner`: com 99 quadros de 10 ms e 1 de 40 ms → `AvgFps ≈ 97.09` (±0.1), `Low1Fps = 25`, `WorstMs = 40`, `FramesOver16ms = 1`; com lista vazia → `Frames = 0` sem exceção.

- [ ] **Step 2: Rodar e ver falhar**

Expected: `QA FAIL PerfStatsCheck` (erro de compilação).

- [ ] **Step 3: Implementar `PerfStats`, `PerfProbe`, `PerfBenchmark` e `BuildTools`**

`PerfBenchmark` entra na cena via `[RuntimeInitializeOnLoadMethod]` quando `-benchmark` está presente (nenhuma cena é alterada).

- [ ] **Step 4: Rodar a checagem e gerar a build de desenvolvimento**

Run: `-qaScenario PerfStatsCheck` → Expected: `QA PASS PerfStatsCheck`.
Run: `... -quit -executeMethod BuildTools.BuildDev ...` → Expected: `Builds/Windows-Dev/OutOfTime.exe` existe e o log contém `Build succeeded`.

- [ ] **Step 5: Commit**

```bash
git add "PROJETO TORRADEIRA/Assets/Scripts/Diagnostics" "PROJETO TORRADEIRA/Assets/Editor/QA" "PROJETO TORRADEIRA/ProjectSettings/ProjectSettings.asset"
git commit -m "Sonda de desempenho, benchmark e builds"
```

### Task 5: Linha de base registrada

**Files:**
- Create: `Assets/Editor/QA/SceneCapture.cs`
- Create: `docs/revisao/linha-de-base.md`
- Create: `docs/revisao/antes/*.png`

**Interfaces:**
- Consumes: `SceneAudit.Run()`, `BuildTools.BuildDev()`, `PerfBenchmark` (Task 4).
- Produces: `public static class SceneCapture` — `public static void Capture(string folder)` e `RunFromCommandLine()` com `-captureFolder <pasta>`. Renderiza 1920x1080 a câmera principal de cada cena do fluxo e, em `Principal`, a câmera do jogador posicionada em cada ponto da lista `CapturePoints` (const no arquivo: spawn do jogador + os pontos de `PhotoLocation.All` + 3 pontos fixos escolhidos por ter mais luzes/renderers visíveis, calculados uma vez e escritos como `Vector3`/`Quaternion` literais). Os mesmos pontos são usados para `antes/` e `depois/`; os 3 pontos mais pesados viram o segmento `principal_pesado` do benchmark.

- [ ] **Step 1: Implementar `SceneCapture` e gerar `docs/revisao/antes/`**

Run: `... -executeMethod SceneCapture.RunFromCommandLine -captureFolder ../docs/revisao/antes ...` (sem `-nographics`)
Expected: um PNG por cena do fluxo e um por ponto de `Principal`, nomes `<cena>_<ponto>.png`.

- [ ] **Step 2: Rodar o benchmark na build de desenvolvimento**

Run: `Builds/Windows-Dev/OutOfTime.exe -benchmark -perf -screen-width 1920 -screen-height 1080 -screen-fullscreen 1 -logFile docs/revisao/qa/bench-antes.log`
Expected: `%USERPROFILE%/AppData/LocalLow/<empresa>/<produto>/perf/summary.csv` com uma linha por segmento e por carregamento.

- [ ] **Step 3: Escrever `docs/revisao/linha-de-base.md`**

Hardware (Ryzen 7 5700G, Radeon integrada, 14 GB, 1920x1080), preset "PC", tabela por segmento (FPS médio, 1% low, pior quadro, quadros > 16,67 ms), carregamentos, total da auditoria, e a lista de erros/exceções vistos no log do benchmark. Copiar os CSV para `docs/revisao/perf-antes/`.

- [ ] **Step 4: Commit**

```bash
git add "PROJETO TORRADEIRA/Assets/Editor/QA/SceneCapture.cs*" docs/revisao
git commit -m "Linha de base de erros, FPS e capturas"
```

### Task 6: Cenários de fluxo, pausa e repetição — e correções

**Files:**
- Create: `Assets/Editor/QA/Scenarios/FlowScenario.cs`, `PauseScenario.cs`
- Modify: os scripts que os cenários acusarem (correção na origem)
- Create/Modify: `docs/revisao/bugs.md` (uma linha por bug: sintoma, causa, arquivo:linha, correção)

**Interfaces:**
- Consumes: `QAScenario`, `QAInput` (Task 1).

- [ ] **Step 1: Escrever `FlowScenario` (`Name = "Flow"`)**

`TelaInicio`: espera 1 s, `Tap(Enter)` (fecha o aviso), `Check(!PhotosensitivityWarning.IsShowing)`, `Tap(Space)` → espera carregar `Menu`. No `Menu`, invocar `onClick` do botão `PlayButton` → `IntroCutsceneVideo` → `Tap(Escape)` ou a tecla de pular definida em `IntroVideoCutscene` → `Principal`. Em `Principal` (depois de pular a `TimeTravelIntro` se ela bloquear): `Check` de 1 `AudioListener`, 1 câmera com tag `MainCamera` ativa, `Time.timeScale == 1`, `Cursor.lockState == Locked`. Completar as missões via `MissionSystem` (chamar `AddFoto`/`AddPeca` até cumprir os requisitos) → espera `Victory TEMP` → invoca o botão de menu → `Menu` com `Cursor.visible`. **Repetir o caminho Menu → Principal uma segunda vez** e refazer os `Check` (Review Focus 1), mais `!PauseMenu.IsPaused`, `!DefeatController.IsDefeated` e contagem de missões zerada. Ao voltar ao `Menu`, `Check` que nenhum `AudioSource` fora do `MusicManager` está tocando com clipe da fase (áudio fora de hora).

- [ ] **Step 2: Escrever `PauseScenario` (`Name = "Pause"`)**

Em `Principal`: `Tap(C)` e no mesmo quadro `Tap(Escape)` → depois de 1 s, `Check` que não há pausa aberta por cima do visor fechando e que o cursor está coerente; `Tap(Escape)` → `PauseMenu.IsPaused`, `timeScale 0`, cursor visível; `Tap(Escape)` → retoma. Derrota: teleporte na frente do TemporalEraser, esperar `DefeatController.IsDefeated`, `Tap(Escape)` → `Check(!PauseMenu.IsPaused)` (Review Focus 2); botão MENU da derrota → `Menu`, `timeScale 1`, cursor visível, nenhum objeto `DontDestroyOnLoad` duplicado (contar raízes da cena `DontDestroyOnLoad` por nome antes/depois).

- [ ] **Step 3: Rodar os dois cenários**

Run: `... -qaScenario Flow` e `... -qaScenario Pause`
Expected na primeira execução: provavelmente `QA FAIL`; registrar cada falha em `docs/revisao/bugs.md`.

- [ ] **Step 4: Corrigir cada falha na origem**

Usar superpowers:systematic-debugging em cada uma. Corrigir também, no caminho, mensagens repetidas do Console vistas nos logs.

- [ ] **Step 5: Rodar até passar**

Expected: `QA PASS Flow` e `QA PASS Pause`, sem exceções no log.

- [ ] **Step 6: Commit**

```bash
git add "PROJETO TORRADEIRA/Assets" docs/revisao/bugs.md docs/revisao/qa
git commit -m "Corrige fluxo, pausa e segunda partida"
```

### Task 7: Cenários de câmera e álbum — e correções

**Files:**
- Create: `Assets/Editor/QA/Scenarios/CameraScenario.cs`, `AlbumScenario.cs`
- Modify: scripts acusados; `docs/revisao/bugs.md`

- [ ] **Step 1: Escrever `CameraScenario` (`Name = "Camera"`)**

Em `Principal`: girar a visão nas quatro direções (mouse virtual) com o visor fechado e aberto e `Check` que a rotação mudou; `Tap(C)` → esperar `PhotoCamera.enterTransitionTime + 0.2 s` → `Check(cameraHUD.activeSelf && !crosshair.activeSelf)`; `Scroll(+120)` e `Scroll(-120)` → `photoCam.fieldOfView` muda e fica em `[minZoomFOV, maxZoomFOV]`; `Click()` → `CameraPhotoData.PhotoCount` +1; `Tap(C)` → HUD some, `crosshair` volta, `playerCam.fieldOfView == normalFOV`, `PlayerMovement` responde a `W` (posição muda).

- [ ] **Step 2: Escrever `AlbumScenario` (`Name = "Album"`)**

Tirar fotos (respeitando `CameraPhotoData.Cooldown`) até `AlbumController.IsFull`; contar `Resources.FindObjectsOfTypeAll<Texture2D>().Length` → tirar mais uma foto → a contagem não cresce mais que 1 (Review Focus 3); `Tap(F)` → `IsAlbumOpen`; `RemovePhoto(0)` → `Photos.Count` -1 e, após 1 quadro, a textura removida foi destruída (`== null`); `Tap(F)` → fechado, `timeScale 1`.

- [ ] **Step 3: Rodar, registrar falhas, corrigir na origem, rodar até `QA PASS Camera` e `QA PASS Album`**

- [ ] **Step 4: Commit**

```bash
git add "PROJETO TORRADEIRA/Assets" docs/revisao
git commit -m "Corrige visor, fotos e album"
```

### Task 8: Cenário de peça temporal e missão — e correções

**Files:**
- Create: `Assets/Editor/QA/Scenarios/PieceScenario.cs`
- Modify: scripts acusados; `docs/revisao/bugs.md`

- [ ] **Step 1: Escrever `PieceScenario` (`Name = "Piece"`)**

Para a primeira `TemporalObjectPickup` da cena: teleportar o jogador a 2,5 m olhando para ela; `Check(!pickup.CanInteract())`; `Tap(C)`, mirar, `Click()` → `Check(pickup.CanInteract())` e o renderer visível; `Tap(C)`; `Tap(E)` (interação do `InteractionDetector`) → pickup coletado (inativo/destruído) e `MissionSystem.GetPieceCount()` +1; texto da missão (`MissionUIController`) atualizado. Repetir com a revelação expirando: fotografar, esperar o tempo de visibilidade + 1 s → `!CanInteract()`.

- [ ] **Step 2: Rodar, registrar, corrigir, rodar até `QA PASS Piece`**

- [ ] **Step 3: Commit**

```bash
git add "PROJETO TORRADEIRA/Assets" docs/revisao
git commit -m "Corrige ciclo de pecas e missao"
```

### Task 9: Cenário de bateria, superaquecimento e recuperação — e correções

**Files:**
- Create: `Assets/Editor/QA/Scenarios/BatteryHeatScenario.cs`
- Modify: scripts acusados; `docs/revisao/bugs.md`

- [ ] **Step 1: Escrever `BatteryHeatScenario` (`Name = "BatteryHeat"`)**

Bateria: `CameraBattery.Use(Current)` → `IsEmpty`; `Tap(C)` → o visor não abre (ou fecha), sem exceção; `BatteryPickup` mais próximo coletado com `E` → `!IsEmpty`. Calor: com o visor aberto, chamar `CameraOverheat.RegisterPhotoHeat()` até `IsCameraLost` → visor fechado e controles restaurados; exatamente **um** `LostCameraPickup` ativo, com `NavMesh.SamplePosition(pos, 1f)` verdadeiro e caminho completo (`NavMesh.CalculatePath` do jogador até ele = `PathComplete`); chamar `RegisterPhotoHeat()` mais 5 vezes → ainda um único pickup (Review Focus 4); teleporte até ele, `Tap(E)` → `!IsCameraLost`, `Tap(C)` abre o visor de novo.

- [ ] **Step 2: Rodar, registrar, corrigir, rodar até `QA PASS BatteryHeat`**

- [ ] **Step 3: Commit**

```bash
git add "PROJETO TORRADEIRA/Assets" docs/revisao
git commit -m "Corrige bateria, superaquecimento e recuperacao"
```

### Task 10: Inimigo e auditoria limpa

**Files:**
- Create: `Assets/Editor/QA/Scenarios/EnemyScenario.cs`
- Modify: o que a auditoria acusar

- [ ] **Step 1: Escrever `EnemyScenario` (`Name = "Enemy"`)**

Mesmo roteiro já validado: teleporte a 6 m na frente do TemporalEraser → `IsChasing` → `DefeatController.IsDefeated` em até 10 s → painel ativo, `timeScale 0` → `Retry()` → jogador a < 0,5 m do `PlayerSpawn`, inimigo a < 1 m do início, `!IsChasing`, `timeScale 1`.

- [ ] **Step 2: Rodar `SceneAudit` e zerar Missing Scripts e prefabs ausentes**

Referências nulas: corrigir as que causam erro em runtime (vistas nos cenários) e listar as opcionais restantes em `docs/revisao/auditoria.md` com o motivo.

- [ ] **Step 3: Rodar `-qaScenario All`**

Expected: `QA PASS` para Smoke, Flow, Pause, Camera, Album, Piece, BatteryHeat, Enemy, ReferenceMapCheck, PerfStatsCheck; auditoria com `MissingScripts = 0` e `MissingPrefabs = 0`.

- [ ] **Step 4: Commit**

```bash
git add "PROJETO TORRADEIRA/Assets" docs/revisao
git commit -m "Teste do inimigo e auditoria sem erros"
```

### Task 11: Limpeza (remoções)

**Files:**
- Modify: `ProjectSettings/EditorBuildSettings.asset` (via `EditorBuildSettings.scenes`)
- Delete: ver lista abaixo
- Create: `Assets/Editor/QA/ProjectCleanup.cs` (menu **Ferramentas › QA › Limpar projeto**, idempotente)
- Modify: `Assets/Cutscene/LEIAME.md`

- [ ] **Step 1: Ajustar o build**

Cenas habilitadas, nesta ordem: `TelaInicio`, `Menu`, `IntroCutsceneVideo`, `Principal`, `Victory TEMP`, `Credits`. `IntroCutscene` sai do build (arquivo preservado). LEIAME da cutscene: a oficial é a de vídeo; a desenhada em código fica como alternativa fora do build.

- [ ] **Step 2: Remover com `AssetDatabase.DeleteAsset` (pasta inteira quando indicado)**

Autorizados: `Assets/_Recovery/`; `Assets/Jullya/`; `Assets/Scenes/CenaLabFut.unity` + pasta `CenaLabFut/`; `Assets/Scenes/Tutorial.unity`; `Assets/Scenes/TestScene.unity`; `Assets/Scenes/Radio.unity` + pasta `Radio/`; `Assets/ThirdParty/CRT-Free/Scenes/`. Em seguida regenerar `ReferenceMap` e remover os pacotes de `ThirdParty` **inteiros** que ficarem 100% não usados e sem nenhum caminho literal nas ferramentas de editor; scripts que só as cenas removidas usavam (`Scripts/Radio/*` se o mapa confirmar, `NPCMissao` idem) saem junto, conferindo com `grep` que nenhum outro script os referencia. Qualquer coisa com uso incerto fica e vai para a seção "Preservados (dúvida)" de `docs/revisao/limpeza.md`.

- [ ] **Step 3: Validar**

Run: `SceneAudit` + `-qaScenario All`
Expected: tudo `QA PASS`, auditoria sem novos itens, compilação sem erros.

- [ ] **Step 4: Escrever `docs/revisao/limpeza.md`** (removidos com tamanho, preservados com motivo, total liberado)

- [ ] **Step 5: Commit**

```bash
git add -A "PROJETO TORRADEIRA/Assets" "PROJETO TORRADEIRA/ProjectSettings/EditorBuildSettings.asset" docs/revisao
git commit -m "Remove cenas de rascunho e assets sem uso"
```

### Task 12: Reorganização de pastas

**Files:**
- Create: `Assets/Editor/QA/AssetReorganizer.cs` (tabela `origem → destino`, idempotente, menu **Ferramentas › QA › Reorganizar pastas**)
- Modify: constantes de caminho nas ferramentas de editor que apontarem para pastas movidas (`MonitorMenuBuilder`, `PauseMenuBuilder`, `PixelArtSceneSetup`, `PhotosensitivityWarningSetup`, `TemporalEraserBuilder`, `IntroCutsceneBuilder`, `ReferenceMap`)

**Interfaces:**
- Produces: estrutura final de `Assets/` (a tabela concreta sai do `ReferenceMap` atual; regras fixas):
  - `Scenes/` só com `.unity` e as pastas de dados de cada cena; `Art/Models`, `Art/Textures`, `Art/Materials`, `Art/UI` (hoje `UI/PixelArt`, `Textures`, `Materials`, modelos soltos em `Prefabs`); `Audio/` (hoje `Sounds`); `Fonts/` (hoje `Font`); `Animation/` mantém o nome; `Scripts/<Sistema>/` com `Diagnostics` novo; `Settings/` recebe os assets de pipeline e volume soltos na raiz (`DefaultVolumeProfile.asset`, `New Universal Render Pipeline Asset_Renderer.asset`) e os de `Scripts/Settings`.
  - Intocados: `ThirdParty/*` por dentro, `Plugins`, `TextMesh Pro`, `Resources`, `Editor`, `Cutscene` (já organizada por tipo), `Ilumisoft` (pacote).

- [ ] **Step 1: Gerar a tabela a partir do mapa e revisar à mão**

Escrever a tabela em `AssetReorganizer` e também em `docs/revisao/limpeza.md` (seção "Movidos").

- [ ] **Step 2: Aplicar com `AssetDatabase.MoveAsset`, atualizar as constantes de caminho das ferramentas no mesmo passo**

- [ ] **Step 3: Validar**

Run: `SceneAudit`, `ReferenceMapCheck` (com os caminhos novos) e `-qaScenario All`; rodar cada ferramenta de montagem em modo "só checar caminhos" (`AssetDatabase.LoadAssetAtPath` de cada constante ≠ null).
Expected: tudo `QA PASS`; `git status` mostra os arquivos como renomeados (`R`), não como apagados e criados.

- [ ] **Step 4: Commit**

```bash
git add -A "PROJETO TORRADEIRA/Assets" docs/revisao/limpeza.md
git commit -m "Reorganiza pastas do projeto"
```

### Task 13: Fechamento da Parte 1

- [ ] **Step 1: Gerar a build de desenvolvimento e rodar o benchmark de novo** (mesmos argumentos do Task 5), salvar em `docs/revisao/perf-parte1/`.
- [ ] **Step 2: Atualizar `TAREFAS_PENDENTES.md`** com o estado e apontar para `docs/revisao/`.
- [ ] **Step 3: Escrever o plano da Parte 2** (desempenho, visual, validação) com os gargalos medidos, usando superpowers:writing-plans.
- [ ] **Step 4: Commit**

```bash
git add TAREFAS_PENDENTES.md docs
git commit -m "Fecha parte 1 da revisao"
```
