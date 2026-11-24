# Tarefas pendentes — PROJETO TORRADEIRA

Lista do que falta fazer, com as decisões já tomadas e o que já foi levantado no código.
Projeto: `PROJETO TORRADEIRA` (Unity 6000.2.7f2, URP, Input System).

## Pedido original

- Tela inicial
- Tela de créditos: dá para fazer algo melhor
- Colocar fitas de isolamento nos lugares onde o jogador não precisa ir
- Vai ter inimigo? Se tiver, falta ele
- Adicionar câmeras de tirar foto no menu e na tela inicial

## Decisões

| Item | Decisão |
|---|---|
| Inimigo | **Ativar e completar** o TemporalEraser |
| Fitas de isolamento | **O autor do projeto faz** (não mexer) |
| Tela inicial | **Refazer no estilo do menu** (sala de laboratório 1975, CRT, câmeras com flash, "PRESSIONE QUALQUER TECLA") |
| Fotossensibilidade | **Obrigatório**: aviso de fotossensibilidade ao abrir o jogo **e** opção de desativar flashes nas configurações |
| Créditos | **Créditos rolando dentro de um CRT**, na mesma sala; a cada nome uma câmera dispara o flash e "revela" uma foto 100x100 |

Estilo a seguir: o mesmo do menu atual (cena `Menu`), montado pela ferramenta
**Ferramentas › Pixel Art › Montar menu em monitores CRT** (`Assets/Editor/ArtReview/MonitorMenuBuilder.cs`).
Sem verde, sem laranja/amarelo. Cores: branco, cinzas, vermelho. Tema: protótipo da primeira câmera digital (1975), 100x100, gravação em fita.

## 1. Flashes e fotossensibilidade — FEITO

> Feito: `FlashSettings` (PlayerPrefs `FlashEnabled`); flashes da câmera, pulso do superaquecimento e piscar do ano na intro respeitam a opção; linha FLASHES nas configurações da pausa (`PauseMenuBuilder.BuildFlashRow`); `PhotosensitivityWarning` na `TelaInicio` ([F] liga/desliga, ENTER continua, bloqueia o "pressione qualquer tecla"). Ferramenta: **Ferramentas › Pixel Art › Adicionar aviso de fotossensibilidade**.

- Criar `FlashSettings` (estático, salvo em `PlayerPrefs`, chave sugerida `FlashEnabled`, padrão ligado).
- Respeitar a opção em todo flash de tela cheia:
  - `Assets/Scripts/Camera/CameraPhotoCapture.cs` → `PlayFlash()`
  - `Assets/Scripts/Camera/CameraFlash.cs` → `Play()`
  - Conferir também efeitos piscantes em `Assets/Scripts/UI/TimeTravelIntro.cs` e `Assets/Scripts/Camera/CameraOverheat.cs`.
  - Flashes das câmeras do menu/tela inicial/créditos (novas).
- Opção nas configurações: o painel de configurações da pausa é montado por
  **Ferramentas › Pixel Art › Refazer menu de pausa** (`Assets/Editor/ArtReview/PauseMenuBuilder.cs`, método `BuildSettings`).
  Adicionar uma linha "FLASHES  [ LIGADO / DESLIGADO ]" abaixo dos sliders. A lógica dos sliders fica em `Assets/Scripts/Menu/SettingsMenu.cs`.
- Aviso de fotossensibilidade: primeira coisa que aparece na `TelaInicio`, com texto de aviso, atalho para desligar flashes ali mesmo e "ENTER para continuar".

## 2. Câmeras fotográficas com flash (menu, tela inicial e créditos) — FEITO NO MENU

> Feito no menu: `PhotoFlash` (dispara a cada 6–12 s; luz + quad emissivo + obturador sintetizado; sem flashes = só som + LED) e `MonitorMenuBuilder.BuildPhotoCameras` (3 câmeras de tripé). Falta reaproveitar na tela inicial e nos créditos (seções 3 e 4).

- Câmeras dos anos 70 montadas proceduralmente (tripé, corpo, lente, unidade de flash com refletor), algumas apontadas para o jogador.
- Script novo (ex.: `PhotoFlash`): dispara a cada 6–12 s; luz forte por ~0,08 s + quad emissivo + som de obturador sintetizado.
  Com flashes desligados: só o som e um LED pequeno, sem clarão.
- Adicionar no `MonitorMenuBuilder` (menu) e reaproveitar na tela inicial e nos créditos.
- Scripts de apoio que já existem em `Assets/Scripts/Menu/`: `MenuBlinker`, `MenuSpin`, `TapeRecordingText`, `CrtCameraFeed`, `CrtNoiseScreen`, `CrtFlicker`.

## 3. Tela inicial (`Assets/Scenes/TelaInicio.unity`) — FEITO

> Feito: `MonitorMenuBuilder.Build(Mode)` com modo `Start` (**Ferramentas › Pixel Art › Montar tela inicial em monitores CRT**): mesma sala e câmeras, TV principal com "OUT OF TIME" e "PRESSIONE QUALQUER TECLA" piscando. `StartScreenCrt` espera o aviso de fotossensibilidade, e ao apertar uma tecla dispara o flash, escurece e carrega o `Menu`. UI 2D antiga, `StartScreenManager`/`StartScreenHandler` e o cenário noir com as câmeras Canon ficaram desativados (não apagados).

- Cena atual: `Canvas` com `StartPanel`, `Title`, `FadeScreen`, `GameManager`, `ChanceScene`; scripts `Assets/Scripts/UI/StartScreenManager.cs` (usa DOTween) e `StartScreenHandler.cs`.
- Refazer com a mesma sala do menu: TV principal com "OUT OF TIME" e "PRESSIONE QUALQUER TECLA" piscando; câmeras com flash; antes disso, o aviso de fotossensibilidade.
- Sugestão: generalizar o `MonitorMenuBuilder` com um modo (Menu / TelaInicial / Créditos) que muda só o conteúdo da TV principal.

## 4. Créditos (`Assets/Scenes/Credits.unity`) — FEITO

> Feito: modo `Credits` no `MonitorMenuBuilder` (**Ferramentas › Pixel Art › Montar creditos em monitores CRT**). Os créditos rolam dentro da TV principal (o `CreditsController` existente foi religado ao novo conteúdo; ESC e o fim continuam indo para o `Menu`). `CreditsSnapshots`: quando cada nome chega ao meio, uma câmera dispara e a TV do protótipo congela a imagem 100x100 (`CrtCameraFeed.Snapshot`) com "FOTO 01/02/03". Nos créditos as câmeras não disparam sozinhas. Para adicionar nomes, editar o array `Credits` no builder e rodar a ferramenta de novo.

- Script atual: `Assets/Scripts/Credits/CreditsController.cs` (rola `creditsContent`, ESC volta, evento ao terminar). Os nomes atuais estão nos textos da cena.
- Fazer os créditos rolarem dentro do canvas da TV principal (mesma sala).
- A cada nome/bloco: flash de uma câmera + a TV do protótipo congela um "snapshot" 100x100 em cinza (adicionar um `Snapshot(holdSeconds)` no `CrtCameraFeed`).

## 5. Inimigo (TemporalEraser) — ativar e completar — FEITO

> Feito com **Ferramentas › Inimigo › Montar TemporalEraser** (`Assets/Editor/Enemy/TemporalEraserBuilder.cs`): FBX em Humanoid, `Assets/Animation/Enemy/TemporalEraser.controller` (Walk), modelo no lugar da cápsula (pivô no pé, escala 1, material cinza "apagado" porque o FBX não tem texturas), `TemporalEraserPresence` (velocidade da animação segue o agente, drone e passos sintetizados, glitch que treme/some; sem flashes só treme), `AttackTrigger` com Rigidbody cinemático, `PlayerSpawn` na posição inicial do jogador, `DefeatManager` + `DefeatCanvas` ("VOCÊ FOI APAGADO DO TEMPO", TENTAR DE NOVO / MENU). TENTAR DE NOVO devolve o jogador ao `PlayerSpawn` e o inimigo ao ponto inicial. Testado em play mode (batch): perseguição → derrota → tentar de novo.
>
> Para o autor decidir: a animação anda a 0,85 m/s, mas o inimigo anda a 2 m/s (patrulha) e 4 m/s (perseguição), então as pernas ficam aceleradas (limitado a 2,2x). Baixar as velocidades no `TemporalEraserController` deixa o passo natural. Mover o `PlayerSpawn` se o respawn tiver que ser em outro lugar (ou usar `DefeatController.SetSpawnPoint` em checkpoints).

Levantado na cena `Principal`:
- GameObject `TemporalEraser` existe mas está **inativo**; é só uma cápsula (MeshFilter/MeshRenderer/CapsuleCollider), com `NavMeshAgent`, `TemporalEraserController` e `TemporalEraserVision` (referências de `player` e `vision` já preenchidas).
- NavMesh já existe (`Assets/Scenes/Principal/NavMesh-*.asset`).
- `DefeatController` (`Assets/Scripts/Enemy/DefeatController.cs`) **não está em nenhuma cena**: falta painel de derrota, spawn point e ligação.
- `TemporalEraserAttack` precisa de um trigger e da referência ao `DefeatController`.

A fazer:
- Trocar a cápsula pelo modelo `Assets/Prefabs/Enemy/Vampire A Lusth.fbx` com a animação de `Walking (2).fbx` (os dois em Humanoid), com um AnimatorController cuja velocidade siga o `NavMeshAgent`.
- Som (drone/passos) e algum efeito de "apagado do tempo" (glitch).
- Criar a UI de derrota (ex.: "VOCÊ FOI APAGADO DO TEMPO", botões TENTAR DE NOVO e MENU) no estilo da pausa (texto limpo, sem caixa), ligar o `DefeatController` (painel, player, spawn point) e o trigger de ataque.
- Ativar o GameObject e testar patrulha → perseguição → busca → derrota → retry.

## Preferências do autor

- Commits na branch `main`, título curto e direto em português, sem descrição e sem coautoria do Claude.
- Hierarquia com nomes em inglês e organizada.

## Revisão completa (2026-11-17)

Design: `docs/superpowers/specs/2026-11-17-revisao-completa-design.md` · Plano da Parte 1: `docs/superpowers/plans/2026-11-17-revisao-parte1-base-bugs-organizacao.md` · Entregas: `docs/revisao/`.

**Parte 1 — linha de base, bugs e organização: FEITA**
- Testes automáticos: **Ferramentas › QA** (`Assets/Editor/QA/`). 13 cenários/checagens (fluxo completo duas vezes, pausa, visor, álbum, peças, bateria/superaquecimento, inimigo, auditoria, mapa de referências, contorno, caminhos das ferramentas). Rodar todos: `Unity.exe -projectPath "<projeto>" -executeMethod QARunner.RunFromCommandLine -qaScenario All` (com a janela do editor; o Unity precisa estar fechado antes).
- Medição: build de desenvolvimento (`Ferramentas › QA › Build de desenvolvimento`) + `OutOfTime.exe -benchmark -perf` → `%USERPROFILE%/AppData/LocalLow/<empresa>/OUT OF TIME/perf/`.
- 5 bugs corrigidos (`docs/revisao/bugs.md`), 353 MB removidos e pastas reorganizadas (`docs/revisao/limpeza.md`), linha de base em `docs/revisao/linha-de-base.md`.

**Parte 2 — desempenho, gráficos e validação: FEITA** (plano `docs/superpowers/plans/2026-11-17-revisao-parte2-desempenho-visual.md`).
- Visual: imagem pixelada pelo URP, shader de paleta (cinzas + vermelho) com pontilhado, volume da fase sem efeitos que borram, luzes guia nas portas, brilho `Retro/TemporalRim` nas peças, HUD padronizado, tela final na sala do laboratório (`docs/revisao/visual.md`; ferramentas em **Ferramentas › Visual**).
- Desempenho (build de release, 1080p): todos os trechos acima de 60 FPS na média e no 1% low; o menor 1% low é 125 (`docs/revisao/medicoes.md`).
- 2 bugs a mais corrigidos (inimigo invisível sem o visor; visor desenhando a cena duas vezes).
- Teste manual sugerido: `docs/revisao/teste-manual.md`.
- Fitas de isolamento continuam com o autor (pacote `ThirdParty/CautionTape` preservado para isso).

## Próxima rodada (pedido de 2026-11-24)

Nada disto foi feito ainda. Onde mexer em cada item:

1. **Nocautear o inimigo com o flash** — ao tirar foto (`PhotoCamera.TakePhoto`), se o inimigo estiver no centro do visor e a menos de ~6 m (raio do centro da tela sem parede no meio), chamar um `Stun(segundos)` novo em `Scripts/Enemy/TemporalEraserController.cs`: para o agente, toca glitch forte (`TemporalEraserPresence`) e depois volta a procurar. Respeitar `FlashSettings` só no visual (o efeito de jogo vale sempre).
2. **Aviso de inimigo chegando** — além do LED do `RadarDevice` (já pisca mais rápido): batida de coração/estática que aumenta com a distância (< 12 m) e leve vinheta escura nas bordas; pode reaproveitar a borda do `CameraOverheat.CreateHeatGlow`.
3. **Animação diferente quando a câmera superaquece** — hoje `CameraOverheat.LoseCamera` usa o fechamento normal. Criar `CameraBlink.PlayOverheat()`: lâminas travando/tremendo, tela vermelha com chiado e a câmera em pixel art caindo das mãos (sprite `CameraInHands` descendo girando) em vez de descer normal.
4. **Câmera perdida aparece no radar (azul)** — em `Scripts/Detector/RadarDevice.cs`, incluir o objeto da câmera perdida (`CameraOverheat.lostCameraInstance` / `LostCameraPickup`) como alvo com cor azul (ex.: `new Color32(70,140,255,255)`), pulsando como as peças. (Exceção à regra de "sem verde/laranja": azul é permitido aqui a pedido.)
5. **Colocar cômodos** — mobiliar salas vazias: estender `Editor/Visual/PropScatterBuilder.cs` com conjuntos por cômodo (escritório: mesa+cadeira+PC; depósito: prateleiras+caixas), usando os modelos de `ThirdParty/PSXNostalgia-FBX` (Desk, Chair, PcScreen, PcTower) e do PSX Mega Pack.
6. **Arrumar os relógios** — conferir as fotos `docs/revisao/qa/props/clock_*.png`; o alinhamento em `PropScatterBuilder.AlignToWall` escolhe o eixo mais fino e pode deixar o mostrador virado para a parede ou torto. Fixar a orientação do `clock_2.glb` (frente conhecida) e rodar **Ferramentas › Visual › Espalhar props PSX** de novo.
7. **Peças aparecem antes da câmera abrir por inteiro** — `PhotoCamera.OpenCamera` chama `temporalController.SetCameraMode(true)` no início; mover para o fim de `PlayTransition` (depois que `cameraBlink.Play()` termina, quando a lente já abriu).
8. **Marcação no PC** — destacar o computador/terminal de missão (qual objeto? confirmar com o autor) com ícone no HUD apontando a direção e/ou marcador no radar.
9. **Eventos aleatórios** — script `RandomEvents` na Principal: a cada 25–60 s, sorteia um evento perto (8–20 m) e fora da visão: luz piscando (luzes da fase), porta abrindo/fechando sozinha (`Door.Interact`), barulho de passos. Respeitar `FlashSettings` para o piscar.
10. **Mão do player em pixel art** — sprite em preto e branco (como `CameraInHands` em `CameraBlink.BuildCameraSprite`) segurando o radar e visível embaixo à direita; integrar no `RadarDevice` (desenhar a mão no corpo do aparelho).
