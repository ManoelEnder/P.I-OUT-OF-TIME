// Linha do tempo central da cutscene (segundos). Cenas, falas e efeitos sonoros ficam aqui.
export const FPS = 30;
export const DURATION_S = 112;
export const f = (s: number) => Math.round(s * FPS);

export const SCENES = {
  lab: { from: 0, to: 21 },          // 1975: fita, montagem, primeiro teste
  sabotage: { from: 21, to: 30 },    // bancada vazia, silhueta, impacto
  altered: { from: 30, to: 37 },     // linha do tempo alterada + peça presa
  record: { from: 37, to: 51 },      // pesquisador fala com o jogador
  find: { from: 51, to: 68 },        // localize, fotografe, colete
  meters: { from: 68, to: 82 },      // bateria e calor
  rupture: { from: 82, to: 100 },    // superaquecimento e câmera perdida
  ending: { from: 100, to: 112 },    // sabotador no monitor, fim da mensagem
} as const;

// Dublagem: salve public/voz/voz_01.mp3 ... voz_18.mp3 (ElevenLabs). Sem arquivo, aparece só a legenda.
export const LINES: { at: number; dur: number; text: string }[] = [
  { at: 1.0, dur: 9.4, text: "Em 1975, Steve Sasson montou, num laboratório da Kodak, um protótipo capaz de gravar uma foto em fita magnética." },
  { at: 18.0, dur: 2.6, text: "Era o começo da fotografia digital." },
  { at: 21.8, dur: 4.2, text: "Então alguém voltou àquela noite. Destruiu o protótipo," },
  { at: 29.0, dur: 2.6, text: "e o futuro perdeu décadas de avanços." },
  { at: 33.3, dur: 2.7, text: "Algumas peças ficaram presas no tempo." },
  { at: 38.4, dur: 5.4, text: "Se você está ouvindo esta gravação, a câmera que deixei no laboratório chegou até você." },
  { at: 44.4, dur: 3.9, text: "Use-a para encontrar essas peças e reconstruir o protótipo." },
  { at: 51.6, dur: 2.8, text: "Abra o visor e procure onde a imagem falha." },
  { at: 55.0, dur: 3.0, text: "Quando enxergar uma peça, mire e fotografe." },
  { at: 59.6, dur: 5.9, text: "Ela vai continuar visível por um tempo. Chegue perto e recolha antes que desapareça." },
  { at: 68.8, dur: 3.4, text: "O visor gasta bateria e esquenta enquanto está aberto." },
  { at: 73.4, dur: 4.8, text: "Feche entre uma busca e outra para economizar carga e deixar a câmera esfriar." },
  { at: 82.6, dur: 2.4, text: "Se ela superaquecer, vai sumir." },
  { at: 88.2, dur: 3.5, text: "Uma ruptura pode levá-la para outro ponto do laboratório." },
  { at: 95.0, dur: 2.9, text: "Encontre a câmera e recolha-a para continuar." },
  { at: 100.2, dur: 1.7, text: "Traga as peças de volta." },
  { at: 101.9, dur: 3.4, text: "E, se vir alguém nas gravações, preste atenção." },
  { at: 105.5, dur: 2.7, text: "Eu ainda não sei se aquela pessoa foi embora." },
];

export const CUES: { at: number; sfx: string; vol: number }[] = [
  { at: 0.15, sfx: "sfx_tape_click", vol: 0.9 },
  { at: 1.9, sfx: "sfx_whoosh", vol: 0.3 }, { at: 2.4, sfx: "sfx_tool_click", vol: 0.55 },
  { at: 2.7, sfx: "sfx_whoosh", vol: 0.22 }, { at: 3.2, sfx: "sfx_whoosh", vol: 0.25 },
  { at: 4.6, sfx: "sfx_whoosh", vol: 0.3 }, { at: 5.9, sfx: "sfx_whoosh", vol: 0.35 }, { at: 6.3, sfx: "sfx_tool_click", vol: 0.6 },
  { at: 10.6, sfx: "sfx_tool_click", vol: 0.6 }, { at: 11.2, sfx: "sfx_tool_click", vol: 0.5 },
  { at: 12.6, sfx: "sfx_tool_click", vol: 0.7 }, { at: 14.3, sfx: "sfx_tool_click", vol: 0.5 }, { at: 14.9, sfx: "sfx_tool_click", vol: 0.5 },
  { at: 15.65, sfx: "sfx_crt_on", vol: 0.6 }, { at: 17.6, sfx: "sfx_discovery", vol: 0.75 },
  { at: 21.1, sfx: "rewind", vol: 0.7 }, { at: 24.4, sfx: "glitch", vol: 0.55 },
  { at: 26.7, sfx: "sfx_whoosh", vol: 0.5 }, { at: 27.4, sfx: "sfx_impact", vol: 1 },
  { at: 30.05, sfx: "sfx_whoosh", vol: 0.35 }, { at: 31.25, sfx: "sfx_whoosh", vol: 0.35 }, { at: 32.45, sfx: "sfx_whoosh", vol: 0.35 },
  { at: 35.1, sfx: "sfx_reveal", vol: 0.5 }, { at: 36.3, sfx: "glitch", vol: 0.3 }, { at: 37.0, sfx: "sfx_crt_on", vol: 0.5 },
  { at: 52.3, sfx: "sfx_tool_click", vol: 0.5 }, { at: 52.8, sfx: "sfx_whoosh", vol: 0.2 }, { at: 53.6, sfx: "sfx_reveal", vol: 0.6 },
  { at: 58.3, sfx: "shutter", vol: 1 }, { at: 59.5, sfx: "sfx_tool_click", vol: 0.5 },
  { at: 64.0, sfx: "sfx_collect", vol: 0.8 }, { at: 64.6, sfx: "confirm", vol: 0.45 },
  { at: 71.0, sfx: "sfx_tool_click", vol: 0.5 }, { at: 74.2, sfx: "sfx_heat_warning", vol: 0.55 }, { at: 75.0, sfx: "sfx_tool_click", vol: 0.5 },
  { at: 78.0, sfx: "sfx_reveal", vol: 0.35 },
  { at: 82.05, sfx: "sfx_tool_click", vol: 0.4 }, { at: 84.2, sfx: "sfx_heat_warning", vol: 0.6 }, { at: 85.2, sfx: "sfx_heat_warning", vol: 0.7 },
  { at: 86.0, sfx: "sfx_rupture", vol: 1 }, { at: 86.4, sfx: "shutter", vol: 0.6 },
  { at: 88.7, sfx: "sfx_denied", vol: 0.6 }, { at: 90.6, sfx: "sfx_whoosh", vol: 0.3 }, { at: 96.5, sfx: "sfx_collect", vol: 0.8 },
  { at: 103.2, sfx: "glitch", vol: 0.55 }, { at: 108.4, sfx: "sfx_crt_off", vol: 0.8 }, { at: 109.3, sfx: "sfx_reveal", vol: 0.35 },
];

// trilha: abaixa no impacto e durante as instruções
export const MUSIC_KEYS: [number, number][] = [
  [0, 0], [1.5, 0.42], [20.5, 0.42], [26.9, 0.5], [27.35, 0], [29.2, 0.3], [37, 0.3], [38.6, 0.16],
  [99.5, 0.16], [102.5, 0.28], [108.3, 0.28], [108.5, 0], [109.3, 0.3], [112, 0],
];
export const AMBIENCE_KEYS: [number, number][] = [
  [0, 0.35], [21, 0.35], [27.4, 0.15], [37, 0.3], [51, 0.15], [100, 0.3], [108.4, 0], [109.2, 0.2], [112, 0],
];
