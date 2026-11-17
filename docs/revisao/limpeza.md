# Limpeza do projeto

## Cenas do build

- `Assets/Scenes/TelaInicio.unity`
- `Assets/Scenes/Menu.unity`
- `Assets/Scenes/IntroCutsceneVideo.unity`
- `Assets/Scenes/Principal.unity`
- `Assets/Scenes/Victory TEMP.unity`
- `Assets/Scenes/Credits.unity`
- (fora do build) `Assets/Scenes/IntroCutscene.unity`

## Removidos

| Caminho | Tamanho | Motivo |
|---|---:|---|
| `Assets/_Recovery/` (3 cenas) | 4,3 MB | Cópias de recuperação automática do Unity, sem referência |
| `Assets/Scenes/CenaLabFut/` | 0,2 MB | Restos (iluminação/reflexo) de cena de rascunho já apagada |
| `Assets/Scenes/TestScene.unity` | 0,1 MB | Cena de teste fora do build (autorizado) |
| `Assets/Scenes/Radio.unity` + `Assets/Scenes/Radio/` | 0,1 MB | Cena de rádio fora do fluxo (autorizado) |
| `Assets/ThirdParty/dirty-public-toilet/` | 174,6 MB | Pacote sem nenhum arquivo usado |
| `Assets/ThirdParty/Leather sofa/` | 106,2 MB | Pacote sem nenhum arquivo usado |
| `Assets/ThirdParty/ArmsModel/` | 52,8 MB | Pacote sem nenhum arquivo usado |
| `Assets/ThirdParty/sink/` | 14,8 MB | Pacote sem nenhum arquivo usado |
| `Assets/ThirdParty/window-blinds/` | 0,0 MB | Pacote vazio |
| Entradas mortas do build (`Jullya`, `Tutorial`, `CenaLabFut`, `Sample-CRT`) | — | Arquivos já não existiam; só a lista do build apontava para eles |

**Total liberado: 353,1 MB.** Tudo recuperável pelo histórico do Git.

## Preservados

- `Assets/ThirdParty/CautionTape/`: sem uso hoje, mas é para as fitas de isolamento que o autor vai colocar.
- `Assets/Scenes/IntroCutscene.unity` e a arte dela em `Assets/Cutscene/`: cutscene alternativa, fora do build (decisão do autor).

## Sem uso, mas preservados por dúvida

Não aparecem em nenhuma cena do build, mas podem ser material da equipe para a fase ou para o futuro. Ficaram; lista completa em `nao-usados.md`.

| Pasta | Tamanho | Por que ficou |
|---|---:|---|
| `Assets/Sounds/Casual & Relaxing Game Music/` | 63,4 MB | Músicas; uma é citada pelo builder da cutscene alternativa |
| `Assets/Prefabs/Maps/` | 48,7 MB | Prefabs de mapa da equipe, possivelmente versões de trabalho |
| `Assets/ThirdParty/Abandoned Office/` | 39,0 MB | Pacote com scripts (não é removido inteiro com scripts) |
| `Assets/ThirdParty/QuickOutline/` (exemplos) | 30,7 MB | O script `Outline` é usado; só os exemplos do pacote sobram |
| `Assets/Prefabs/Mat`, `Lamps`, `CenterTable`, `UI`, `Radar` | 15,4 MB | Prefabs da equipe sem uso hoje |
| `Assets/ThirdParty/bathroom-cleaning-props-set-low-poly/`, `kenney_fantasy-ui-borders (1)/`, `Easy Collider/` | 3,6 MB | Pacotes com scripts ou ferramenta de editor |
| `Assets/Sounds/Ambient`, `Sounds/Radio`, `Animation`, `Textures` | 7,2 MB | Sons e animações soltos sem uso hoje |

## Ferramentas

- `Ferramentas › QA › Limpar projeto` (`Assets/Editor/QA/ProjectCleanup.cs`): refaz a lista do build e só apaga o que o mapa de referências confirma sem uso.
- `Ferramentas › QA › Mapa de referencias`: gera `nao-usados.md`.

## Movidos (reorganização de pastas)

Feito com `Ferramentas › QA › Reorganizar pastas` (`AssetDatabase.MoveAsset`: GUID e `.meta` preservados, cenas e prefabs continuam ligados). Os caminhos citados nas ferramentas de montagem foram atualizados no mesmo commit; o teste `ToolPathsCheck` confere que todos existem.

| De | Para |
|---|---|
| `Assets/Sounds/` | `Assets/Audio/` |
| `Assets/Font/` | `Assets/Fonts/` |
| `Assets/Materials/` | `Assets/Art/Materials/` |
| `Assets/Textures/` | `Assets/Art/Textures/` |
| `Assets/UI/` | `Assets/Art/UI/` |
| `Assets/Shaders/` | `Assets/Art/Shaders/` |
| `Assets/Scripts/Settings/` (pipeline e volumes) | `Assets/Settings/Pipeline/` |
| `Assets/New Universal Render Pipeline Asset_Renderer.asset` | `Assets/Settings/Pipeline/` |
| `Assets/Rendering/UI_Renderer.asset` | `Assets/Settings/Rendering/` |
| `Assets/DefaultVolumeProfile.asset` | `Assets/Settings/` |
| `Assets/*.inputactions` (3 arquivos) | `Assets/Settings/Input/` |
| `Assets/Scripts/Optimization/AutomaticLODTools.cs` (ferramenta de editor) | `Assets/Editor/Optimization/` |

Intocados de propósito: `ThirdParty/*` por dentro, `Plugins`, `TextMesh Pro`, `Resources`, `Ilumisoft`, `Cutscene` (já organizada por tipo), `Animation`, `Prefabs`, `Scenes`, `Scripts/<Sistema>`.

### Estrutura final de `Assets/`

```
Art/          Materials, Textures, UI (PixelArt), Shaders
Audio/        sons e músicas
Animation/    controladores e clipes
Cutscene/     arte, áudio, vídeo e scripts da abertura
Editor/       ferramentas de montagem (ArtReview, Enemy, Optimization) e QA
Fonts/        VCR OSD Mono
Prefabs/      prefabs por sistema
Resources/    (carregados em tempo de execução)
Scenes/       cenas do jogo e dados de cada uma
Scripts/      código do jogo por sistema (Camera, Enemy, Menu, Missions, UI, Diagnostics...)
Settings/     Pipeline (URP), Rendering, Input, volume padrão
ThirdParty/   pacotes de terceiros
```
