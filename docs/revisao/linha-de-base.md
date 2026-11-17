# Linha de base (antes das correções)

Data: 2026-11-17 · Build de desenvolvimento (`BuildTools.BuildDev`) · commit `2a484e3` + ferramentas do Task 5

## Condições

- Hardware: AMD Ryzen 7 5700G, gráfico integrado Radeon (Vega 8), 14 GB de RAM, Windows 11
- Resolução: 1920x1080 tela cheia (`-screen-width 1920 -screen-height 1080 -screen-fullscreen 1`)
- Preset gráfico: "PC" (URP `PC_RPAsset`), vSync desligado, sem limite de FPS
- Roteiro: `PerfBenchmark` (argumento `-benchmark`), input virtual, mesmos trechos e pontos em todas as medições
- Comando: `Builds/Windows-Dev/OutOfTime.exe -benchmark -perf -screen-width 1920 -screen-height 1080 -screen-fullscreen 1`

## Resultado por trecho

Primeiro quadro de cada trecho descartado (é o quadro da captura de tela, artefato da própria medição).
Dados brutos em `perf-antes/`, capturas em `antes/`.

| Trecho | Quadros | FPS médio | 1% low | Pior quadro (ms) | Quadros > 16,67 ms |
|---|---:|---:|---:|---:|---:|
| album | 1199 | 121.1 | 110.0 | 10.7 | 0 |
| cutscene | 6682 | 447.1 | 384.0 | 3.9 | 0 |
| fotos_sequencia | 1554 | 125.4 | 116.6 | 10.1 | 0 |
| inimigo_perseguindo | 1222 | 123.6 | 111.9 | 10.4 | 0 |
| menu | 1353 | 67.7 | 63.1 | 17.3 | 1 |
| principal_normal | 2236 | 112.4 | 85.3 | 43.1 | 1 |
| principal_pesado_p1 | 1176 | 118.6 | 104.4 | 11.5 | 0 |
| principal_pesado_p2 | 2717 | 274.7 | 176.8 | 43.7 | 1 |
| principal_pesado_p3 | 1219 | 123.2 | 109.1 | 10.1 | 0 |
| superaquecimento | 7509 | 125.4 | 117.2 | 10.5 | 0 |
| visor_aberto | 1412 | 70.9 | 55.8 | 46.5 | 2 |

## Leitura

- **Tudo acima de 60 FPS na média.** A fase (`principal_*`) fica entre 112 e 275 FPS, com o álbum, as fotos em sequência e o superaquecimento acima de 120 FPS.
- **Visor aberto é o trecho crítico**: 70,9 FPS médios e **1% low de 55,8 FPS**, abaixo da meta. O visor liga a segunda câmera e o pós-processamento.
- **Menu no limite**: 67,7 FPS médios, 1% low de 63,1. A sala do menu tem várias luzes em tempo real, a câmera do protótipo (feed) e 3 câmeras-prop com luz.
- **Engasgos isolados** de 43–47 ms na fase (1 a cada 8–14 s), a investigar (candidatos: coleta de lixo, `Find` ou carregamento sob demanda).
- **Carregamentos**: a medida desta rodada não pegou o tempo de carregamento (o travamento acontece antes do evento usado); corrigido na sonda e remedido no fim da Parte 1. Primeira cena (`TelaInicio`) levou 2,8 s para o primeiro quadro.

## Erros vistos na build

- `Not allowed to access vertices/normals/uv4 on mesh 'Combined Mesh (root: scene) 3'` (3 erros ao entrar na fase): algum script lê a malha de um objeto com static batching. Candidato: `QuickOutline/Outline.cs` em objeto estático.
- Auditoria: 1 Missing Script em `Credits/Canvas/Text (TMP)` (canvas antigo, desativado) e 448 referências vazias (ver `auditoria-antes.md`).
- Bug de build já corrigido: `AutomaticLODTools` impedia qualquer build (ver `bugs.md`).

## Observações visuais (para a Parte 2)

- Texto de missão grande, sem moldura, encostado no canto; estilos diferentes entre missão, visor, bateria e contador de fotos.
- O jogo já sai em tons de cinza (pós-processamento), coerente com a paleta branco/cinza/vermelho.
