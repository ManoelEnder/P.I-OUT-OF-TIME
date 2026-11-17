# Cutscene de abertura (IntroCutscene)

> **Situação (2026-11-17):** a cutscene oficial do jogo é a de vídeo (`IntroCutsceneVideo`, feita no projeto `Cutscene-Remotion/`): o fluxo é **Menu → Jogar → `IntroCutsceneVideo` → `Principal`**. Esta versão desenhada em código (`IntroCutscene`) continua no projeto como alternativa, **fora do build**. Para voltar a usá-la, habilite a cena em Build Settings e troque o destino do botão Jogar no menu.

Fluxo original desta versão: **Menu → Jogar → `IntroCutscene` → `Principal`** (antes era `Radio`). Duração configurada: **112 s** (1:52), já com o fade de saída de 0,4 s.
Tudo é desenhado em arte própria e renderizado em 640x360 (a mesma resolução pixelada do jogo), ampliado sem filtro; os textos usam a fonte VCR_OSD_MONO do projeto.

## Onde editar
- **Linha do tempo** (planos/cortes, falas, eventos de áudio, curvas de trilha e ambiente, cena seguinte): objeto `Diretor` na cena `Assets/Scenes/IntroCutscene.unity`, componente `IntroCutsceneDirector`.
- **Camadas**: cada recorte tem um `CutoutLayer`, com âncora (id do plano), entrada, saída, movimento e “vida” (oscilação, flutuação, piscar, boca).
- **Regerar tudo**: menu `Ferramentas > Cutscene de Abertura > Gerar arte, áudio e cena`. A cena é recriada com os tempos padrão do `IntroCutsceneBuilder`, então ajustes feitos no Inspector se perdem. Os PNGs e WAVs que já existem não são sobrescritos: para trocar uma arte, substitua o arquivo mantendo o mesmo nome.
- **Prévia**: `Ferramentas > Cutscene de Abertura > Exportar prévia`. Os quadros e o `mix.wav` vão para `CutscenePreview/`. O comando do ffmpeg aparece no Console.

## Voz
Não há narração no projeto: por enquanto a cutscene mostra só as legendas, com o tempo de cada fala reservado. O texto para a dublagem (ElevenLabs) está em `Roteiro_Dublagem.txt`. Salve cada fala como `Assets/Cutscene/Audio/Voz/voz_NN.wav` ou `.mp3` e rode “Gerar”. Depois confira os tempos de início no Inspector (`Lines`): a leitura humana costuma ser mais lenta.

| # | Início | Plano | Texto |
|---|---|---|---|
| 01 | 0:01.0 | voz off, montagem | Em mil novecentos e setenta e cinco, Steve Sasson montou, num laboratório da Kodak, um protótipo capaz de gravar uma foto em fita magnética. |
| 02 | 0:18.0 | retrato no CRT | Era o começo da fotografia digital. |
| 03 | 0:21.8 | bancada vazia | Então alguém voltou àquela noite. Destruiu o protótipo, |
| 04 | 0:29.0 | após o impacto | e o futuro perdeu décadas de avanços. |
| 05 | 0:33.3 | peça suspensa | Algumas peças ficaram presas no tempo. |
| 06 | 0:38.4 | olhando para o jogador | Se você está ouvindo esta gravação, a câmera que deixei no laboratório chegou até você. |
| 07 | 0:44.4 | aponta o esquema | Use-a para encontrar essas peças e reconstruir o protótipo. |
| 08 | 0:51.6 | visor abre | Abra o visor e procure onde a imagem falha. |
| 09 | 0:55.0 | mira | Quando enxergar uma peça, mire e fotografe. *(pausa: obturador em 0:58.3)* |
| 10 | 0:59.6 | coleta | Ela vai continuar visível por um tempo. Chegue perto e recolha antes que desapareça. |
| 11 | 1:08.8 | medidores | O visor gasta bateria e esquenta enquanto está aberto. |
| 12 | 1:13.4 | calor recua | Feche entre uma busca e outra para economizar carga e deixar a câmera esfriar. |
| 13 | 1:22.6 | superaquecendo | Se ela superaquecer, vai sumir. *(pausa: ruptura em 1:26.0)* |
| 14 | 1:28.2 | visor falha | Uma ruptura pode levá-la para outro ponto do laboratório. |
| 15 | 1:35.0 | recuperação | Encontre a câmera e recolha-a para continuar. |
| 16 | 1:40.2 | gravação | Traga as peças de volta. |
| 17 | 1:41.9 | sabotador no monitor | E, se vir alguém nas gravações, preste atenção. |
| 18 | 1:45.5 | fim da mensagem | Eu ainda não sei se aquela pessoa foi embora. |

Interpretação: voz natural, firme e um pouco cansada. Use a mesma voz do começo ao fim; do 01 ao 05 ela entra em off.

## Como validar no Editor
1. Abra `TelaInicio` (ou `Menu`), dê Play e clique em **Jogar**. A cutscene deve rodar até o fim, por cerca de 1:52, e carregar a `Principal` uma única vez.
2. Na `Principal`, depois da intro 2026→1975 que já existia: confira se dá para mover, olhar e abrir o visor com **C**.
3. Dê Play de novo e, no meio da cutscene, aperte **Esc/Espaço/Enter**. Aparece “Pressione novamente para pular”; um segundo toque em até 3 s pula para a mesma saída.
