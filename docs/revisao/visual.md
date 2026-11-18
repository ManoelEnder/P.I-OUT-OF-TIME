# Mudanças visuais (Parte 2) e custo de cada uma

Medições: build de desenvolvimento, 1920x1080 tela cheia, mesma máquina e roteiro. FPS médio / 1% low.

## 1. Imagem pixelada pelo render do URP (`Ferramentas › Visual › Aplicar render retro`)

- A câmera renderiza a 50% (960x540 em 1080p) e o URP amplia com filtro **Point** (pixel 2x2 nítido). HUD, mira e textos do Canvas em overlay continuam em resolução cheia.
- Sombras duras (combinam com pixel art), 2 cascatas, distância 30 m, mapas 1024.
- Escala escolhida: **0,5**. 0,34 (640x360, igual à cutscene) pixelaria mais, mas os textos que estão *no mundo* (TV do menu, créditos, tela final) perderiam leitura; o visual retro se reforça com a paleta e o pontilhado (item 2).

| Trecho | Antes | Pixelado |
|---|---|---|
| menu | 66,8 / 60,0 | **251 / 208** |
| principal_normal | 112 / 85 | **313 / 198** |
| visor_aberto | 71,6 / 61,2 | **211 / 165** |
| album | 65,5 / 63,4 | **182 / 153** |
| superaquecimento | 124 / 90 | **341 / 240** |

O jogo estava limitado pela GPU (resolução cheia + pós-processamento pesado + cena desenhada duas vezes no visor); a resolução de render reduzida dá folga para a melhoria gráfica.

## 2. Paleta e pontilhado (`Assets/Art/Shaders/RetroPalette.shader`, `Ferramentas › Visual › Aplicar paleta retro`)

- Passe de tela cheia do URP (`FullScreenPassRendererFeature` "RetroPalette" no `PC_Renderer`, depois do pós-processamento): cinzas levemente frios, **vermelho preservado** (LEDs, calor, perigo), 8 tons por canal com pontilhado Bayer 4x4 no pixel da imagem de render.
- Quantização em espaço perceptual (gama): na primeira versão (linear, 6 tons) o laboratório escuro virava preto — capturas `capturas-t3/` mostram a versão corrigida.
- Custo medido: ~0,06 ms por quadro (principal_normal 313 → 307 FPS; menu 251 → 244).
- Parâmetros no material `Assets/Art/Materials/RetroPalette.mat`: tons, limiar do vermelho, tom dos cinzas, força do pontilhado e intensidade (0 desliga).

## 3. Pós-processamento da fase (`Ferramentas › Visual › Aplicar volume da fase`, perfil `Assets/Settings/Volumes/GameplayVolume.asset`)

- **Fora:** motion blur (qualidade alta), projeção Panini e desfoque de profundidade — borravam a pixel art e custavam GPU.
- **Dentro:** tonemapping Neutral, exposição +0,2, contraste +18, bloom para as luzes emissivas, vinheta 0,35, grão leve.
- **Saturação 0 (antes −100):** o volume tirava o vermelho antes da paleta; agora a paleta faz os cinzas e o vermelho de perigo aparece.
- Aberração cromática (0,08) e distorção (−0,05) continuam porque o superaquecimento e a pausa animam as duas.
- Custo: dentro da variação da medição (os ganhos grandes vieram da pixelização e do visor).

## 4. Luzes guia e brilho das peças (`Ferramentas › Visual › Iluminar laboratorio`)

- Sobre cada porta: placa emissiva branca fria (brilha com o bloom) + luz pontual sem sombra, alcance 4 m (`Lighting/Guides`).
- Peças temporais: segunda camada de material `Retro/TemporalRim` (`Assets/Art/Shaders/TemporalRim.shader`) — borda branca brilhante, linhas de glitch subindo e pulso lento. **Sem pulso quando os flashes estão desligados** (`FlashSettings` publica o global `_RetroFlashes`).
- Custo: sem diferença mensurável (luzes sem sombra, 4 portas, 4 peças).

## 5. Visor sem renderizar a cena duas vezes

- Com o visor aberto a imagem na tela é a da `photoCam` (confirmado por comparação de capturas: diferença 0,0012 x 0,0048); a câmera do jogador agora desliga enquanto o visor está aberto.
- visor_aberto: **217 → 373 FPS** médios, 1% low **183 → 306**; álbum 192 → 371.

## 6. HUD padronizado (`Ferramentas › Visual › Padronizar HUD da fase`)

- Escala única no `HUDCanvas`: 30 (rótulo do visor, contador de fotos), 26 (rádio, mensagens do álbum), 24 (missão, dicas, interação, aviso de calor), 20 (salvamento); fonte VCR em todos.
- Missão com moldura escura em pixel art (leitura em áreas claras).
- **Bateria branca/cinza** (pilha segmentada) x **calor vermelho** (barra contínua; branco-quente no crítico): diferentes na forma e na cor.

## 7. Tela final no estilo do menu (`Ferramentas › Pixel Art › Montar tela final em monitores CRT`)

- A cena `Victory TEMP` agora é a mesma sala do laboratório, com a TV principal: "FITA RECUPERADA", "O PROTÓTIPO FOI RECONSTRUÍDO", "OBRIGADO POR JOGAR" e os botões MENU / CRÉDITOS.
