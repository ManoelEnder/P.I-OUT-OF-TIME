# Medições

Mesmas condições em todas as rodadas: Ryzen 7 5700G (Radeon integrada), 14 GB, 1920x1080 tela cheia, preset "PC", vSync desligado, build de desenvolvimento, roteiro `PerfBenchmark`. Valores: FPS médio / 1% low / pior quadro (ms).

| Trecho | Antes (linha de base) | Fim da Parte 1 |
|---|---|---|
| menu | 67,7 / 63,1 / 17,3 | 67,6 / 63,2 / 16,1 |
| cutscene | 447 / 384 / 3,9 | 448 / 382 / 2,8 |
| principal_normal | 112 / 85 / 43,1 | 113 / 75 / 87,6 (1 quadro) |
| principal_pesado_p1 | 119 / 104 / 11,5 | 119 / 109 / 9,3 |
| principal_pesado_p2 | 275 / 177 / 43,7 | 279 / 250 / 4,4 |
| principal_pesado_p3 | 123 / 109 / 10,1 | 123 / 111 / 10,9 |
| visor_aberto | 70,9 / **55,8** / 46,5 | 71,6 / **62,4** / 16,3 |
| fotos_sequencia | 125 / 117 / 10,1 | 125 / 120 / 8,6 |
| album | (câmera perdida, álbum não abriu) | **65,3 / 62,6** / 16,2 |
| superaquecimento | (câmera perdida) | 125 / 102 / 16,0 |
| inimigo_perseguindo | 124 / 112 / 10,4 | (derrota em 9 quadros: roteiro a corrigir) |

Carregamentos (fim da Parte 1, do descarregamento da cena anterior ao 1º quadro): TelaInicio 588 ms (primeira cena), Principal 92 ms, IntroCutsceneVideo 14 ms, Menu 9 ms (este último provavelmente subestimado; conferir na Parte 2).

Leitura: todos os trechos ficam acima de 60 FPS na média e no 1% low. Os mais apertados são **menu, visor aberto e álbum (~62–63 no 1% low)**: margem pequena para a melhoria gráfica da Parte 2, que precisa medir o custo de cada efeito. Os erros de malha da build sumiram. A build caiu de 584 MB para 441 MB.

Brutos: `perf-antes/`, `perf-parte1/`. Capturas: `antes/`, `depois-parte1/`.
