# Revisão completa — Parte 2: desempenho, gráficos e validação — Plano de implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dar ao jogo uma identidade pixel art visível (imagem do cenário pixelada, paleta branco/cinza/vermelho com pontilhado, pós-processamento coerente, luz que guia o jogador, HUD padronizado, tela final no estilo do menu) mantendo 60 FPS sustentados (média e 1% low) em 1920x1080 nesta máquina, com medições antes/depois.

**Architecture:** A pixelização vem do próprio URP: resolução de render reduzida (`renderScale`) com ampliação `Point` — a UI em overlay continua em resolução cheia e o custo de GPU cai. Por cima, um passe de tela cheia (`FullScreenPassRendererFeature` do URP) com shader próprio faz paleta (cinza + vermelho preservado), posterização e pontilhado ordenado. O Global Volume da fase é reescrito (sem motion blur/Panini/DoF que borram pixel art). Ferramentas de editor reexecutáveis aplicam tudo, como nas partes anteriores. Cada passo é medido com o `PerfBenchmark`.

**Tech Stack:** Unity 6000.2.7f2, URP 17 (Render Graph), HLSL/ShaderLab, C#, Input System.

**Spec:** `docs/superpowers/specs/2026-11-17-revisao-completa-design.md` (Etapas 3, 4 e 5). Números de partida: `docs/revisao/medicoes.md`.

## Global Constraints

- Paleta: **branco, cinzas e vermelho**; vermelho só para calor e perigo; sem verde, sem laranja/amarelo.
- Meta: **60 FPS sustentados** (média **e** 1% low ≥ 60) em todos os trechos do benchmark, build, 1920x1080 tela cheia, preset "PC", nesta máquina. Limite de FPS não conta como solução.
- HUD, mira e textos legíveis em resolução cheia (Canvas em overlay não passa pela pixelização).
- Todo efeito que pisca respeita `FlashSettings.Enabled`.
- Não mexer nas fitas de isolamento. Preservar campos do Inspector e métodos públicos.
- Commits na `main`, título curto em português, sem descrição e sem coautoria. Hierarquia em inglês.
- Testes: `QARunner -qaScenario All` com a janela do editor (sem `-batchmode`), compilando antes em batch. Medição: `BuildTools.BuildDevFromCommandLine` + `OutOfTime.exe -benchmark -perf -screen-width 1920 -screen-height 1080 -screen-fullscreen 1`.
- Desfazer churn de fontes/fim de linha depois de cada execução em batch.

## Review Focus

1. **Texto e mira legíveis com a imagem pixelada**: overlay em resolução cheia; visor e álbum também. → capturas no Task 9.
2. **Peça temporal ainda se destaca** com paleta reduzida e pontilhado. → Task 5; cenário `Piece` continua passando.
3. **Foto tirada pelo visor** (`ScreenCapture`) continua funcionando. → cenários `Camera`/`Album`.
4. **Flashes desligados** não ligam pulso/piscada nova. → Task 5.
5. **Menu, tela inicial e créditos** recebem a pixelização sem quebrar as telas CRT. → capturas no Task 9.

---

### Task 1: Benchmark confiável e referência da Parte 2

**Files:** Modify `Assets/Scripts/Diagnostics/PerfBenchmark.cs`.

- [ ] Trecho do inimigo: jogador a ~5 m num ponto do NavMesh com caminho livre (`PerfBenchmark.PlaceInFrontOf(Transform player, Transform enemy)`, mesma lógica de `QAScenario.PlaceInFrontOf`); mede até `DefeatController.IsDefeated` ou 10 s.
- [ ] Build dev + benchmark → `docs/revisao/perf-parte2-inicio/`. Esperado: `inimigo_perseguindo` com > 300 quadros.
- [ ] Commit `Benchmark do inimigo confiavel`.

### Task 2: Pixelização pelo URP

**Files:** Create `Assets/Editor/Visual/RetroRenderSetup.cs` (menu **Ferramentas › Visual › Aplicar render retro**), `Assets/Editor/QA/Scenarios/RetroRenderCheck.cs`; Modify `Assets/Settings/Pipeline/PC_RPAsset.asset` via a ferramenta.

**Interfaces:** `public static class RetroRenderSetup { public const float RenderScale = 0.5f; public static void Apply(); }` — no asset "PC": `renderScale = RenderScale`, `upscalingFilter = Point`, MSAA 1; sombras: 2 cascatas, distância 30, mapas 1024, sombra dura.

- [ ] `RetroRenderCheck` (editor): asset ativo com `renderScale ≤ 0.5` e filtro `Point` → FAIL; implementar → PASS; `All` → PASS.
- [ ] Medir; comparar capturas 0.5 vs 0.34 (640x360) pela leitura de portas, objetos e mira; registrar em `docs/revisao/visual.md`.
- [ ] Commit `Imagem pixelada pelo render do URP`.

### Task 3: Paleta e pontilhado em tela cheia

**Files:** Create `Assets/Art/Shaders/RetroPalette.shader`, `Assets/Art/Materials/RetroPalette.mat`; Modify `RetroRenderSetup` (adiciona `FullScreenPassRendererFeature` "RetroPalette" ao renderer "PC", evento `AfterRenderingPostProcessing`); estender `RetroRenderCheck`.

**Shader:** luminância Rec.709; posteriza em `_Levels` (6) com Bayer 4x4 em coordenadas da imagem de render; vermelho preservado quando `r > max(g,b) * _RedThreshold` (1.6); resto em cinza levemente frio `_ColdTint` (0.92, 0.96, 1.0); `_Strength` 0..1.

- [ ] Teste: renderer "PC" com a feature ativa e o material → FAIL; implementar → PASS; `All` → PASS.
- [ ] Custo do passe (Δ ms em `principal_normal`) em `visual.md`.
- [ ] Commit `Paleta e pontilhado retro em tela cheia`.

### Task 4: Global Volume da fase

**Files:** Create `Assets/Settings/Volumes/GameplayVolume.asset` (gerado); Modify `RetroRenderSetup` (`ApplyGameplayVolume()`), `Assets/Scenes/Principal.unity`.

**Valores:** Tonemapping Neutral; ColorAdjustments exposição +0.2, contraste +18, saturação 0; Bloom limiar 0.9, intensidade 0.8, scatter 0.6; Vignette 0.35; FilmGrain Medium1 0.25; ChromaticAberration 0.08; LensDistortion −0.05. **Desligados:** MotionBlur, PaniniProjection, DepthOfField. O glitch do superaquecimento continua por cima.

- [ ] `GameplayVolumeCheck` (editor): volume global ativo de `Principal` sem MotionBlur/Panini/DoF e com Tonemapping → FAIL; implementar → PASS; `All` → PASS.
- [ ] Medir e capturar.
- [ ] Commit `Pos-processamento da fase para pixel art`.

### Task 5: Luzes guia e peça temporal em destaque

**Files:** Create `Assets/Art/Shaders/TemporalGlow.shader` + material, `Assets/Editor/Visual/LabLightingBuilder.cs` (menu **Ferramentas › Visual › Iluminar laboratorio**); Modify `Assets/Scenes/Principal.unity` via a ferramenta.

**Decisões:** em cada `Door`, luminária pequena acima do batente (quad emissivo branco frio + luz pontual sem sombra, alcance 4); brilho fraco em `BatteryPickup` e `PhotoLocation`; nenhuma luz nova com sombra; luzes sob `Lighting/Guides`. Peça revelada usa `TemporalGlow` (emissão branca com pulso lento e linhas de glitch; sem pulso quando `FlashSettings.Enabled == false`).

- [ ] `LabLightingCheck` (editor): toda `Door` com luz guia, nenhuma com sombra → FAIL; implementar → PASS; `Piece` e `All` → PASS.
- [ ] Medir e capturar portas e peças.
- [ ] Commit `Luzes guia e brilho das pecas temporais`.

### Task 6: Visor sem renderizar a cena duas vezes

**Files:** Modify `Assets/Scripts/Camera/PhotoCamera.cs`.

**Achado:** com o visor aberto `PhotoCamera` e `CameraPlayer` ficam ligadas (cena desenhada duas vezes). Investigar (systematic-debugging) se a `photoCam` precisa renderizar todo quadro (a foto usa `ScreenCapture`); se não, manter desligada e só sincronizar, ou ligar só no quadro da captura.

- [ ] Cenário `Camera`: `Check(!photo.photoCam.enabled || photo.photoCam == photo.playerCam, "visor nao renderiza a cena duas vezes")` com o visor aberto → FAIL; corrigir preservando zoom, bordas, HUD e foto; `Camera`, `Album`, `Piece`, `All` → PASS.
- [ ] Medir `visor_aberto`.
- [ ] Commit `Visor sem renderizar a cena duas vezes`.

### Task 7: HUD padronizado

**Files:** Modify `Assets/Editor/ArtReview/PixelArtSceneSetup.cs` (rodar em `Principal`).

**Decisões:** missão em 22 px com moldura `ui_panel` no canto superior esquerdo; bateria = pilha segmentada **branca**; calor = termômetro **vermelho** vertical com marca de perigo; contador de fotos e "[CAM] MODO TEMPORAL" na fonte VCR, mesmo tamanho e margem de 32 px; álbum e mensagens de rádio no mesmo estilo.

- [ ] `HudStyleCheck` (play mode): textos do HUD na fonte VCR e tamanhos de uma lista fixa; bateria e calor com cores diferentes → FAIL; implementar → PASS; `All` → PASS.
- [ ] Capturas do HUD, visor, álbum e pausa.
- [ ] Commit `HUD padronizado em pixel art`.

### Task 8: Tela final no estilo do menu

**Files:** Modify `Assets/Editor/ArtReview/MonitorMenuBuilder.cs` (modo `Final`); `Assets/Scenes/Victory TEMP.unity` via a ferramenta.

**Decisões:** mesma sala; TV principal com "FITA RECUPERADA", "O PROTÓTIPO FOI RECONSTRUÍDO", "OBRIGADO POR JOGAR" e botões MENU / CRÉDITOS; `FrameLikeMenu`; câmeras com flash; o `Flow` continua achando o botão "Menu".

- [ ] `Flow` → PASS; captura.
- [ ] Commit `Tela final no estilo do menu`.

### Task 9: Validação final e entrega

- [ ] Suite `All` → todos PASS.
- [ ] Build de release + benchmark → `docs/revisao/perf-final/`.
- [ ] `docs/revisao/medicoes.md` com a coluna "Final" (FPS médio, 1% low, pior quadro, carregamentos) e onde algo fica abaixo de 60 e por quê.
- [ ] Capturas `docs/revisao/depois/`; `docs/revisao/visual.md` com antes/depois e custo de cada efeito.
- [ ] Roteiro manual em `docs/revisao/teste-manual.md` (sensação do mouse, leitura em movimento, cursor travado na build, flashes desligados).
- [ ] Atualizar `TAREFAS_PENDENTES.md`; commit `Validacao final da revisao`; `git push origin main`.
