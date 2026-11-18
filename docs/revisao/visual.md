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
