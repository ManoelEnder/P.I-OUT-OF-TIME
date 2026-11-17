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
