# Medições

Mesma máquina e roteiro em todas as rodadas: Ryzen 7 5700G (gráfico integrado Radeon), 14 GB, Windows 11, **1920x1080 tela cheia**, preset "PC", vSync desligado, sem limite de FPS, roteiro automático `PerfBenchmark` (`OutOfTime.exe -benchmark -perf -screen-width 1920 -screen-height 1080 -screen-fullscreen 1`).
Valores: **FPS médio / 1% low / pior quadro (ms)**. Antes e Parte 1: build de desenvolvimento. **Final: build de release.**

| Trecho | Antes | Fim da Parte 1 | **Final (release)** |
|---|---|---|---|
| menu | 67,7 / 63,1 / 17,3 | 67,6 / 63,2 / 16,1 | **247 / 225 / 5,3** |
| cutscene | 447 / 384 / 3,9 | 448 / 382 / 2,8 | **917 / 746 / 1,6** |
| principal_normal | 112 / 85 / 43,1 | 113 / 75 / 87,6 | **315 / 227 / 52,5** |
| principal_pesado_p1 | 119 / 104 / 11,5 | 119 / 109 / 9,3 | **359 / 125 / 99,2** |
| principal_pesado_p2 | 275 / 177 / 43,7 | 279 / 250 / 4,4 | **730 / 595 / 8,7** |
| principal_pesado_p3 | 123 / 109 / 10,1 | 123 / 111 / 10,9 | **385 / 358 / 2,9** |
| visor_aberto | 70,9 / **55,8** / 46,5 | 71,6 / 62,4 / 16,3 | **373 / 341 / 5,8** |
| fotos_sequencia | 125 / 117 / 10,1 | 125 / 120 / 8,6 | **375 / 351 / 3,0** |
| album | (câmera perdida) | 65,3 / 62,6 / 16,2 | **400 / 375 / 2,9** |
| superaquecimento | (câmera perdida) | 125 / 102 / 16,0 | **375 / 345 / 4,1** |
| inimigo_perseguindo | 124 / 112 / 10,4 | (roteiro com erro) | **405 / 358 / 4,8** |

Carregamentos (final, da descarga da cena anterior ao 1º quadro da nova): TelaInicio 577 ms (primeira cena do jogo), Principal 88 ms, IntroCutsceneVideo 12 ms, Menu 8 ms (provavelmente subestimado: a troca a partir da tela inicial é rápida, mas a medida pode não pegar o carregamento todo).

## Leitura

- **Meta atingida:** todos os trechos ficam bem acima de 60 FPS **na média e no 1% low** (o menor 1% low é 125 FPS, no ponto pesado p1). O pior trecho antes (visor aberto, 1% low 55,8) agora tem 1% low de 341.
- **De onde veio o ganho:** o jogo estava limitado pela GPU. (1) render em meia resolução com ampliação Point (o visual pixelado), (2) visor sem desenhar a cena duas vezes, (3) pós-processamento sem motion blur/Panini/DoF e sombras mais simples.
- **O que ainda aparece:** engasgos isolados de 1–2 quadros (52 ms em principal_normal, 99 ms em p1) logo no começo do trecho, quando uma área nova entra em cena pela primeira vez — compatível com compilação de shader/carregamento sob demanda. Não se repetem no trecho. Possível melhoria futura: pré-aquecer shaders (ShaderVariantCollection) na carga da fase.
- Build de release: 378 MB (antes da revisão: 584 MB).

Brutos: `perf-antes/`, `perf-parte1/`, `perf-parte2-inicio/`, `perf-t2-pixelizacao/`, `perf-t4-volume/`, `perf-t6-visor/`, `perf-final/`. Capturas: `antes/`, `depois-parte1/`, `capturas-t2..t6/`, `depois/`.
