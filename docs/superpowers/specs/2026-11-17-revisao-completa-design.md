# Revisão completa do OUT OF TIME (PROJETO TORRADEIRA)

Data: 2026-11-17 · Projeto Unity em `PROJETO TORRADEIRA/` · Branch `main`

## Intenção

Deixar o jogo da câmera temporal (terror/ficção científica num laboratório de 1975) **organizado, jogável do início ao fim, com arte pixel art consistente e 60 FPS sustentados**. O autor quer enxergar a diferença visual no Game e na build, e quer números de desempenho medidos na build, não um limite de FPS.

## Estado atual (levantado)

- Unity 6000.2.7f2, URP (perfil de qualidade ativo: "PC"), alvo Windows. Resolução padrão 1024x768 em tela cheia; vSync desligado.
- Fluxo real: `TelaInicio` → `Menu` → `IntroCutsceneVideo` (vídeo) → `Principal` → missões completas → `Victory TEMP` ("POR ENQUANTO É ISSO! OBRIGADO POR JOGAR!") → `Menu`. Do menu: `Credits` → `Menu`. Na fase: pausa (→ `Menu`), derrota do TemporalEraser (tentar de novo / `Menu`).
- No build mas fora do fluxo do menu: `IntroCutscene` (cutscene desenhada em código, leva a `Principal`) e `Radio` (leva a `Principal`). Fora do build: `CenaLabFut`, `Tutorial`, `Jullya`, `TestScene`, `Sample-CRT`, `_Recovery/*`.
- Carregamentos por nome: `SceneManager.LoadScene` com nomes vindos do Inspector (`ChangeScene`, `MissionSystem.finalSceneName`, `PauseMenu.mainMenuSceneName`, `ColectableItem.nomeDaCena`, `NPCMissao.nomeCenaMenu`, `CenaNarrativa.cenaDoJogo`, diretores da cutscene). Há uma pasta `Assets/Resources`.
- Tamanho: `ThirdParty` 518 MB (`dirty-public-toilet` 175 MB, `Leather sofa` 107 MB), `Prefabs` 109 MB, `Sounds` 95 MB, `Cutscene` 64 MB.
- Ferramentas de montagem já existentes (reexecutáveis): `MonitorMenuBuilder` (Menu/TelaInicio/Credits), `PauseMenuBuilder`, `PixelArtSceneSetup` (HUD do visor, fonte VCR), `TemporalEraserBuilder`, `IntroCutsceneBuilder`.
- Máquina de teste e alvo: Ryzen 7 5700G com Radeon integrada, 14 GB de RAM, monitor 1920x1080 a 180 Hz.

## Objetivos

1. Nenhum erro de compilação, exceção, Missing Script ou referência quebrada nas cenas do fluxo; Console sem mensagens repetidas.
2. Mecânicas verificadas: movimento e mira com o visor aberto e fechado, zoom, bordas, fade do HUD, retorno à visão normal; localizar → fotografar → coletar → missão; álbum com várias fotos (e exclusão, se existir); bateria zerada, superaquecimento, reaparecimento da câmera (uma vez só, em ponto acessível) e recuperação; pausa em transição, pular cutscene, derrota/tentar de novo, voltar ao menu — sem câmera duplicada, cursor travado, tela preta, áudio fora de hora ou missão com estado antigo.
3. `Assets` organizado por tipo e sistema, sem cópias e lixo, com GUIDs preservados e lista do que foi movido e removido.
4. **60 FPS sustentados** na build de release, em 1920x1080 tela cheia, nesta máquina: FPS médio, 1% low e picos de tempo de quadro registrados por trecho (normal, área mais pesada, visor com pós, fotos em sequência, álbum, ruptura por superaquecimento, inimigo perseguindo). Carregamentos medidos à parte.
5. Pixel art consistente em cenário, HUD, visor, álbum, pausa, derrota, final e cutscene; capturas antes/depois comparáveis.

## Não objetivos

- Fitas de isolamento nos lugares inacessíveis: **o autor faz**.
- Novas mecânicas, novas fases ou novo conteúdo narrativo.
- Refazer o menu, a tela inicial e os créditos: já estão no estilo aprovado; só recebem ajustes de consistência se algo destoar.
- Validação em outro hardware.

## Anti-objetivos (falha mesmo que "funcione")

- Quebrar uma referência (GUID ou carregamento por nome) ao mover ou apagar.
- Apagar algo que é carregado em tempo de execução ou que outra pessoa da equipe usa sem confirmar.
- Bater 60 FPS só com limite de quadros, ou degradando efeitos que informam o jogador (calor, bateria, peça temporal, flash).
- Mudar a paleta aprovada ou desfazer o visual já aprovado.
- Pixelar a mira e os textos a ponto de ficarem ilegíveis.

## Restrições e decisões do autor

- **Paleta: branco, cinzas e vermelho**, com o vermelho reservado para calor e perigo. Sem verde e sem laranja/amarelo. Para separar a peça temporal do cenário, usar contraste, brilho e movimento (glitch, partículas), não cor nova.
- **Caminho A:** trabalho em etapas, com testes automatizados executados pelo Unity em modo batch (o editor precisa estar fechado durante essas execuções) e medição numa build.
- **Alvo de desempenho:** 1080p tela cheia nesta máquina.
- Commits na `main`, título curto em português, sem descrição e sem coautoria. Hierarquia com nomes em inglês.
- Acessibilidade de flashes (`FlashSettings`) continua valendo para todo efeito novo que pisque.

## Abordagens consideradas

| | Como | Troca | Decisão |
|---|---|---|---|
| **A** | Etapas: bugs → organização → desempenho → visual → validação; testes de cenário em play mode via batch; build de desenvolvimento para medir e build de release para validar | Exige o Unity fechado durante as execuções; o "teste jogando de verdade" fica para o autor com roteiro | **Escolhida** pelo autor: já funcionou nas seções anteriores, não depende de instalar nada e segue a prioridade pedida |
| B | Servidor MCP da comunidade para controlar o editor aberto | Interação ao vivo, mas instalação e confiabilidade incertas | Descartada |
| C | Visual primeiro | Diferença visível mais cedo, mas otimiza e organiza em cima de cenas que vão mudar | Descartada (vai contra a prioridade) |

## Como será feito

### Etapa 0 — Linha de base
- Ferramentas de QA em `Assets/Editor/QA/` (versionadas, no menu **Ferramentas › QA**):
  - **Auditoria:** abre cada cena do build e lista Missing Scripts, referências nulas em campos serializados de scripts do projeto, objetos inativos suspeitos e avisos de importação.
  - **Cenários em play mode:** roteiros que dirigem o jogo sem janela (teleporte, input simulado pelo Input System, chamadas diretas onde o input não for simulável) e registram exceções e asserções. Mesmo mecanismo já usado para testar o inimigo.
  - **Mapa de referências:** dependências de cada cena do build, prefabs, `Resources` e strings de carregamento por nome, usado para decidir o que pode ser removido.
- Componente de medição `PerfProbe` (só em build de desenvolvimento ou com o argumento `-perf`): registra o tempo de cada quadro por trecho nomeado em CSV e resume média, 1% low, pior quadro e quadros acima de 16,67 ms. Um roteiro de benchmark percorre os trechos sozinho.
- Capturas "antes" de cada cena do fluxo, no mesmo ângulo, guardadas fora do projeto (pasta de entrega na raiz do repositório: `docs/revisao/antes/`).

### Etapa 1 — Bugs
- Corrigir tudo o que a auditoria e os cenários acusarem, preservando campos do Inspector, eventos e métodos usados por outros sistemas. Se um nome mudar, os consumidores mudam no mesmo commit.
- Fluxo: confirmar com o autor qual cutscene é a oficial (hoje o menu chama a de vídeo); `PauseMenu` e derrota durante transições; cursor e `timeScale` sempre restaurados ao trocar de cena.

### Etapa 2 — Organização e limpeza
- Estrutura alvo em `Assets/`: `Scenes/`, `Scripts/<Sistema>/` (Camera, Enemy, Missions, UI, Menu, Player, Album...), `Prefabs/`, `Art/` (Models, Textures, Materials, Sprites/UI), `Audio/`, `Cutscene/`, `Fonts/`, `Settings/` (pipeline e volumes), `ThirdParty/` (pacotes intactos por dentro), `Editor/`, `Resources/`.
- Mover só com `AssetDatabase.MoveAsset` (preserva GUID e `.meta`); revalidar com a auditoria depois de cada lote.
- Remover apenas o que o mapa de referências mostrar sem uso **e** não for carregado por nome: candidatos já vistos são `_Recovery/`, a cena `Sample-CRT` do pacote CRT, pacotes de terceiros sem nenhuma dependência (por exemplo os de banheiro, se confirmado) e as cenas fora do build. Cenas pessoais (`Jullya`, `CenaLabFut`, `Tutorial`, `TestScene`) e a cutscene que não estiver no fluxo ficam preservadas e listadas como dúvida, a menos que o autor autorize.

### Etapa 3 — Desempenho
- Medir a linha de base na build de desenvolvimento com o Profiler (CPU, GPU, memória, carregamentos) e identificar o gargalo de cada trecho.
- Atacar o que for medido: câmeras simultâneas (feed do visor, câmera de foto), sombras e luzes em tempo real, pós-processamento, transparências, reconstrução de Canvas, alocações por quadro, `Find`/`GetComponent` repetidos, raycasts redundantes, instanciação frequente.
- Memória das fotos: textura capturada, prévias e buffers do álbum liberados ao excluir, ao lotar e ao reiniciar/trocar de cena.
- Comparar o mesmo trecho com as mesmas configurações a cada ajuste. Validação final numa build de release sem Profiler.

### Etapa 4 — Visual
- Texturas: filtro Point, sem mipmaps quando ficarem borradas, compressão que não cria artefatos na arte pixelada, densidade de pixels consistente (texels por metro) no laboratório, corredores e bancadas; atenção especial a CRTs, teclados, cabos e peças eletrônicas.
- Imagem do cenário: pixelização em baixa resolução (como os 640x360 da cutscene), mantendo mira, HUD e textos em resolução cheia. A densidade final será definida pelo builder comparando capturas, sem perder a leitura de portas e caminhos.
- Iluminação e contraste: portas, caminhos e objetos interativos legíveis no escuro; a peça temporal destacada por brilho, contraste e glitch.
- Interface: molduras, ícones, fonte VCR, espaçamento e cores padronizados em visor, álbum, pausa, derrota e final; bateria e calor diferentes **em formato e cor** (calor em vermelho, bateria em branco/cinza com forma própria).
- Final: a tela `Victory TEMP` ganha o mesmo tratamento das telas de menu (proposta: modo "Final" no `MonitorMenuBuilder`).
- Cutscene: a de vídeo vem pronta (feita no projeto Remotion em `Cutscene-Remotion/`); só é refeita se destoar e o autor quiser. A desenhada em código já está em 640x360 com camadas e é preservada.
- Distorções temporais apenas na revelação da peça e no superaquecimento, com custo medido.

### Etapa 5 — Validação e entrega
- Cenário automatizado completo (menu → cutscene → fase → final) e outro com pausa, derrota/tentar de novo e volta ao menu, depois da limpeza e da otimização.
- Build Windows de release: desempenho por trecho, conferência visual por capturas e controles.
- Entrega: resumo com pastas movidas, arquivos removidos, bugs corrigidos, artes substituídas, medições antes/depois, testes executados e capturas antes/depois. Cada verificação pendente vem com o motivo e o passo exato para reproduzir.

## O que fica para o builder decidir

- A lista final de remoções (a partir do mapa de referências), com a regra de preservar o que for incerto.
- A resolução da pixelização e os parâmetros finais de luz e pós, escolhidos por comparação de capturas e custo medido.
- Quais otimizações aplicar, conforme os gargalos medidos.

## Perguntas em aberto para o autor

1. Qual cutscene é a oficial: a de vídeo (`IntroCutsceneVideo`, que o menu chama hoje) ou a desenhada em código (`IntroCutscene`, descrita no LEIAME)? A outra seria preservada fora do fluxo.
2. As cenas pessoais e de rascunho (`Jullya`, `CenaLabFut`, `Tutorial`, `TestScene`, `Radio`) podem ser removidas, ou ficam?
