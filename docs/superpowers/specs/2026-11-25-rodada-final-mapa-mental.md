# Rodada final: mapa mental (brainstorm)

Pedido de 2026-11-25. Nada foi implementado ainda: isto é o mapa para começar quando os tokens voltarem.

```
                                OUT OF TIME, rodada final
                                          │
     ┌──────────────┬─────────────────────┼─────────────────────┬──────────────────┐
  1 CORRIDA      2 STAMINA          3 LANTERNA            4 SEM MOBÍLIA      5 FINAL DO JOGO
 (Cinemachine)                       (na câmera)                              │
     │              │                   │                     │      ┌────────┼────────────┐
 inclina p/ frente  barra no HUD     F liga/desliga         apagar grupo  sala da   cutscene   cena de
 + p/ os lados      desce correndo   luz spot fria          "Mobilia"     garra     montagem   agradecimento
 (curva de virada)  recupera parado  gasta bateria + rápido  + ferramentas mecânica  + futuro    (créditos)
 noise ao correr    vazia = anda     some com a câmera      de mobília    5 encaixes restaurado
 FOV abre um pouco  sopro/ofegar     perdida / sem bateria  occlusion     braço pega (Remotion,
                    estilo do HUD    aviso piscando         refeito       as peças   já existe,
                                                                          e monta    refinar)
```

## 1. Corrida no Cinemachine

- **Hoje:** não tem Cinemachine no projeto. O balanço da câmera é feito à mão no `PlayerMovement.cs` (head bob, roll, aterrissagem), e o `PhotoCamera.cs` mexe direto no FOV da câmera do jogador.
- **Plano:**
  - Instalar o pacote Cinemachine 3 e criar uma `CinemachineCamera` presa à cabeça do jogador.
  - Correndo, a câmera inclina para a frente (pitch +4 a 6°) e para os lados conforme a direção (roll de até 5° na curva e no passo).
  - Somar um `CinemachineBasicMultiChannelPerlin` mais forte ao correr e abrir um pouco o FOV.
- **Cuidado:** o zoom do visor (`PhotoCamera`) precisa passar a mexer no FOV da CinemachineCamera, não na Camera. O head bob atual sai do script para não somar duas vezes.
- **Pronto quando:** correndo para a frente inclina para a frente, virando inclina para o lado, e parado volta suave. O visor e o jumpscare continuam funcionando.

## 2. Barra de stamina

- **Uso:** Shift gasta stamina; parado ou andando, ela recupera, mais rápido parado.
- **Vazia:** não dá para correr até recuperar 30%, e toca um som de ofegar (sintetizado, como os outros).
- **Visual:** barra discreta no HUD, no mesmo estilo pixel e preto e branco, que some quando está cheia.
- **Liga com:** a corrida (Cinemachine só inclina se há stamina) e com o inimigo, que já ouve corrida.

## 3. Lanterna na câmera

- **Controle:** tecla para ligar e desligar (proposta: F; hoje o F abre o álbum, então a lanterna fica no **L**).
- **Luz:** spot de luz fria saindo da câmera do jogador, com clique ao ligar e desligar e um leve tremor quando a bateria está baixa.
- **Bateria:** ligada, a bateria (hoje 10 pontos, 1 a cada 3 s com o visor aberto) também gasta, por exemplo 1 a cada 6 s, e gasta mais rápido ainda junto com o visor.
- **Desliga sozinha:** sem bateria, ou quando a câmera some no superaquecimento.
- **Tela:** ícone pequeno no HUD e a descrição nos controles da pausa.

## 4. Tirar toda a mobília

- Apagar o grupo `Mobilia` da cena Principal e refazer o occlusion culling.
- Remover ou desativar as ferramentas de mobília (`RoomFurnisher`, `FurnitureCatalog`, materiais e LODs gerados) para ninguém regerar sem querer.
- O `FinalRoundBuilder` não pode mais chamar a mobília.

## 5. Final do jogo

### 5a. Sala da garra mecânica (substitui a bancada simples)

- Uma sala própria de montagem, com o protótipo de 1975 no centro sob uma garra mecânica de laboratório.
- **Encaixes:** 4 ou 5 numa base, um por peça, com luz que acende.
- **Montagem:** o jogador aperta E em cada encaixe para colocar a peça. Com tudo encaixado, a garra desce, pega e monta, com som de servo e faíscas.
- **Fim:** a tela vira estática e vai para a cutscene.
- **Radar e missões:** a sala aparece no radar quando a missão libera.
- **Modelo:** garra feita em Blender (já usamos para o inimigo) ou montada com peças simples na Unity.

### 5b. Cutscene da montagem e do futuro restaurado

- Já existe a `OutroCutscene` no Remotion: peças voltando, cientista montando e linha do tempo restaurada.
- O plano é refinar e ligar à sala da garra: abrir com a garra soltando a última peça e terminar no futuro restaurado.

### 5c. Cena de agradecimento e créditos

- Nova cena depois da cutscene, no estilo dos créditos em CRT que já temos.
- **Agradecimento:** a equipe e o projeto.
- **Assets:** créditos reais dos pacotes usados, como PSX Mega Pack, PSXNostalgia, Low Poly Furniture, JustPlay, Abandoned World e as fontes.
- **Sons:** sintetizados no próprio jogo e a trilha usada.
- **Ferramentas:** Unity, Blender e Remotion.
- **Não entra:** nenhuma IA nos créditos.

## Ordem sugerida

1. Tirar a mobília (rápido e limpa o mapa para a sala da garra).
2. Stamina.
3. Lanterna.
4. Corrida no Cinemachine (a mais arriscada, porque mexe na câmera).
5. Sala da garra.
6. Cutscene.
7. Agradecimento.

## Skills e plugins

- `superpowers:writing-plans`: plano de execução.
- `remotion:*`: cutscene.
- `superpowers:systematic-debugging`: bugs.
- `code-review`: revisão antes do commit.
- Blender MCP: modelo da garra.
- Testes de QA com capturas no jogo, como já fazemos.

## Pontos abertos

- **Nomes da equipe nos créditos:** o pedido foi "pode inventar". Hoje o git mostra Jullya2 e GPamplonaSP; falta confirmar os nomes reais e as funções.
- **Lugar da sala da garra:** criar uma sala nova anexa ao mapa ou usar uma sala existente perto do início.
