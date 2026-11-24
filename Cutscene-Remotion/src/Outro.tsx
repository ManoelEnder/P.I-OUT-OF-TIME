import React from "react";
import { AbsoluteFill, Easing, Img, interpolate, Sequence, staticFile, useCurrentFrame, useVideoConfig } from "remotion";
import { Audio } from "@remotion/media";
import { loadFont } from "@remotion/fonts";
import { C, Camera, clamp, Drop, Show, W, H } from "./px";
import { BlackIn, FloorScatter, LabScene } from "./scenes/Flashback";
import { LabSet } from "./scenes/LabSet";

loadFont({ family: "VCR", url: staticFile("VCR_OSD_MONO.ttf") });

// Cutscene final: as peças entregues voltam a 1975, o cientista monta o protótipo
// (mesma montagem da abertura) e a linha do tempo se restaura.
export const OUTRO_S = 50;
const FPS = 30;
const f = (s: number) => Math.round(s * FPS);

const PART = {
  back: { from: 0, to: 6 },
  lab: { from: 6, to: 27 },
  restored: { from: 27, to: 39 },
  end: { from: 39, to: OUTRO_S },
} as const;

const LINES: { at: number; dur: number; text: string }[] = [
  { at: 0.6, dur: 4.6, text: "As peças atravessaram o tempo de volta para aquela noite." },
  { at: 7.5, dur: 5.5, text: "Em 1975, num laboratório da Kodak, o protótipo foi montado outra vez." },
  { at: 14.0, dur: 4.5, text: "Cada peça no lugar. Nenhuma sombra na porta." },
  { at: 19.0, dur: 4.5, text: "A primeira foto digital ficou gravada na fita." },
  { at: 28.0, dur: 3.6, text: "E o futuro começou a voltar." },
  { at: 32.5, dur: 5.0, text: "As fotos voltaram ao arquivo, as câmeras voltaram a ser digitais." },
  { at: 40.0, dur: 4.0, text: "Obrigado. A gravação termina aqui." },
];

const CUES: { at: number; sfx: string; vol: number }[] = [
  { at: 0.1, sfx: "rewind", vol: 0.7 }, { at: 3.2, sfx: "sfx_whoosh", vol: 0.4 }, { at: 5.2, sfx: "sfx_reveal", vol: 0.7 },
  { at: 6.15, sfx: "sfx_tape_click", vol: 0.9 },
  { at: 7.9, sfx: "sfx_whoosh", vol: 0.3 }, { at: 8.4, sfx: "sfx_tool_click", vol: 0.55 }, { at: 11.9, sfx: "sfx_whoosh", vol: 0.35 },
  { at: 16.6, sfx: "sfx_tool_click", vol: 0.6 }, { at: 18.6, sfx: "sfx_tool_click", vol: 0.7 },
  { at: 21.65, sfx: "sfx_crt_on", vol: 0.6 }, { at: 23.6, sfx: "sfx_discovery", vol: 0.8 },
  { at: 27.1, sfx: "sfx_whoosh", vol: 0.35 }, { at: 28.3, sfx: "sfx_whoosh", vol: 0.35 }, { at: 29.5, sfx: "sfx_whoosh", vol: 0.35 },
  { at: 30.4, sfx: "sfx_reveal", vol: 0.5 }, { at: 32.0, sfx: "confirm", vol: 0.5 }, { at: 34.0, sfx: "shutter", vol: 0.8 },
  { at: 36.2, sfx: "sfx_collect", vol: 0.7 },
  { at: 39.2, sfx: "sfx_discovery", vol: 0.6 }, { at: 47.6, sfx: "sfx_crt_off", vol: 0.8 },
];

const MUSIC_KEYS: [number, number][] = [[0, 0], [1, 0.3], [5.6, 0.35], [6, 0.42], [26, 0.42], [27, 0.5], [46, 0.5], [49.6, 0], [OUTRO_S, 0]];

const curve = (keys: [number, number][]) => (frame: number) =>
  interpolate(frame / FPS, keys.map((k) => k[0]), keys.map((k) => k[1]), clamp);

// 00–06 · as peças sobem do chão e se juntam (o golpe da abertura ao contrário)
const BackScene: React.FC = () => {
  const frame = useCurrentFrame();
  const t = Math.round(interpolate(frame, [f(0.4), f(5.2)], [40, 0], { ...clamp, easing: Easing.inOut(Easing.cubic) }));
  const flash = interpolate(frame, [f(5.2), f(5.5), f(6)], [0, 1, 1], clamp);
  return (
    <>
      <FloorScatter t={Math.max(3, t)} />
      <AbsoluteFill style={{ background: "#14303c", opacity: 0.35 }} />
      <AbsoluteFill style={{ background: C.white, opacity: flash }} />
      <BlackIn frames={14} />
    </>
  );
};

// 27–39 · linha do tempo restaurada (a sequência alterada da abertura, invertida)
const RestoredScene: React.FC = () => {
  const frame = useCurrentFrame();
  const fadeOut = interpolate(frame, [f(11.2), f(12)], [1, 0], clamp);
  return (
    <svg width={W} height={H} shapeRendering="crispEdges" style={{ position: "absolute" }}>
      <rect width={W} height={H} fill="#211b15" />
      <g opacity={fadeOut}>
        {[0, 1, 2].map((i) => (
          <Drop key={i} at={f(i * 1.2)} dist={300}>
            <rect x={28 + i * 200} y={50} width={184} height={250} fill={C.paper} />
            <rect x={34 + i * 200} y={56} width={172} height={238} fill="#2e261e" />
          </Drop>
        ))}
        {/* 1: as fotos digitais voltam ao arquivo */}
        <Drop at={f(0.2)}>
          <rect x={62} y={210} width={116} height={70} fill="#5a5d57" /><rect x={58} y={200} width={124} height={14} fill="#6d706a" /><rect x={110} y={230} width={20} height={4} fill={C.metalHi} />
        </Drop>
        {[0, 1, 2].map((i) => (
          <g key={i} opacity={interpolate(frame, [f(3.6 + i * 0.4), f(4.2 + i * 0.4)], [0, 1], clamp)}>
            <rect x={76 + i * 26} y={130 + (i % 2) * 18} width={40} height={30} fill={C.white} />
            <rect x={79 + i * 26} y={133 + (i % 2) * 18} width={34} height={22} fill="#6f7c86" />
            <polygon points={`${79 + i * 26},${155 + (i % 2) * 18} ${95 + i * 26},${140 + (i % 2) * 18} ${113 + i * 26},${155 + (i % 2) * 18}`} fill="#3c464d" />
          </g>
        ))}
        {/* 2: o aparelho analógico vira câmera digital */}
        <Show from={f(1.3)}>
          <g opacity={interpolate(frame, [f(5.6), f(6.4)], [1, 0], clamp)}>
            <rect x={278} y={150} width={84} height={42} fill="#2a2826" /><rect x={278} y={150} width={84} height={10} fill="#b8b2a6" /><rect x={302} y={160} width={36} height={30} fill={C.metalDark} /><rect x={310} y={168} width={20} height={16} fill="#1e2830" /><rect x={340} y={140} width={16} height={10} fill="#b8b2a6" />
          </g>
          <g opacity={interpolate(frame, [f(5.9), f(6.6)], [0, 1], clamp)}>
            <rect x={280} y={140} width={80} height={50} fill="#8e9296" /><rect x={322} y={150} width={30} height={30} fill={C.metalDark} /><rect x={328} y={156} width={18} height={18} fill="#22303a" /><rect x={288} y={150} width={24} height={20} fill="#3a5566" />
            <rect x={291} y={153} width={18} height={14} fill="#6f7c86" />
          </g>
        </Show>
        {/* 3: o marco da tecnologia volta para o lugar */}
        <Drop at={f(2.5)}>
          <rect x={446} y={190} width={150} height={3} fill={C.paper} />
          {Array.from({ length: 6 }, (_, i) => <rect key={i} x={448 + i * 29} y={184} width={2} height={14} fill={C.paper} />)}
        </Drop>
        <Show from={f(2.8)}>
          <g transform={`translate(${Math.round(interpolate(frame, [f(7.4), f(8.6)], [116, 0], { ...clamp, easing: Easing.inOut(Easing.cubic) }))} 0)`}>
            <rect x={432} y={150} width={34} height={22} fill="#d9c48f" /><polygon points="445,172 453,172 449,180" fill="#d9c48f" />
            <rect x={437} y={158} width={24} height={3} fill={C.ink} /><rect x={437} y={164} width={18} height={3} fill={C.ink} />
          </g>
        </Show>
      </g>
    </svg>
  );
};

// 39–50 · laboratório aquecido, sem sombra; título final
const EndScene: React.FC = () => {
  const frame = useCurrentFrame();
  const title = interpolate(frame, [f(1.2), f(2.2)], [0, 1], clamp) * interpolate(frame, [f(8.2), f(8.8)], [1, 0], clamp);
  const sub = interpolate(frame, [f(3), f(4)], [0, 1], clamp) * interpolate(frame, [f(8.2), f(8.8)], [1, 0], clamp);
  const off = f(8.6);
  if (frame >= off) {
    const t = frame - off;
    return (
      <svg width={W} height={H} shapeRendering="crispEdges" style={{ position: "absolute" }}>
        <rect width={W} height={H} fill="#000" />
        {t < 3 && <rect y={H / 2 - Math.round(180 * (1 - t / 3))} width={W} height={Math.max(2, Math.round(360 * (1 - t / 3)))} fill={C.amberLow} />}
        {t >= 3 && t < 16 && <rect x={W / 2 - Math.round(300 * interpolate(t, [3, 14], [1, 0], clamp))} y={H / 2 - 1} width={Math.max(2, Math.round(600 * interpolate(t, [3, 14], [1, 0], clamp)))} height={2} fill={C.white} />}
      </svg>
    );
  }
  return (
    <>
      <Camera end={off} shots={[{ from: 0, x0: 320, y0: 180, z0: 1.05, x1: 300, y1: 196, z1: 1.35 }]}>
        {() => <LabSet abs={f(19) + frame} steve />}
      </Camera>
      <AbsoluteFill style={{ background: "#000", opacity: 0.35 * title }} />
      <div style={{ position: "absolute", top: 120, width: W, textAlign: "center", fontFamily: "VCR", fontSize: 26, color: C.amberHi, opacity: title }}>
        LINHA DO TEMPO RESTAURADA
      </div>
      <div style={{ position: "absolute", top: 160, width: W, textAlign: "center", fontFamily: "VCR", fontSize: 16, color: C.paper, opacity: sub }}>
        OUT OF TIME
      </div>
    </>
  );
};

const Subtitles: React.FC = () => {
  const frame = useCurrentFrame();
  const line = LINES.find((l) => frame >= f(l.at) && frame < f(l.at + l.dur + 0.3));
  if (!line) return null;
  return (
    <div style={{ position: "absolute", left: 0, right: 0, bottom: 10, display: "flex", justifyContent: "center" }}>
      <div style={{ maxWidth: 560, background: "#000000b0", padding: "4px 10px", fontFamily: "VCR", fontSize: 15, lineHeight: "18px", color: C.paper, textAlign: "center" }}>
        {line.text}
      </div>
    </div>
  );
};

const Grain: React.FC = () => {
  const frame = useCurrentFrame();
  return <Img src={staticFile(`grain_${Math.floor(frame / 3) % 4}.png`)} style={{ position: "absolute", width: W, height: H, imageRendering: "pixelated" }} />;
};

export const Outro: React.FC = () => {
  const { fps } = useVideoConfig();
  const part = (name: keyof typeof PART, el: React.ReactNode) => (
    <Sequence name={name} from={PART[name].from * fps} durationInFrames={(PART[name].to - PART[name].from) * fps}>{el}</Sequence>
  );
  return (
    <AbsoluteFill style={{ background: "#000", overflow: "hidden" }}>
      {part("back", <BackScene />)}
      {/* A montagem é a mesma da abertura; a legenda da abertura fica de fora. */}
      {part("lab", <LabScene />)}
      {part("restored", <RestoredScene />)}
      {part("end", <EndScene />)}

      <Grain />
      <div style={{ position: "absolute", inset: 0, background: "radial-gradient(ellipse at center, transparent 55%, rgba(0,0,0,0.55) 100%)" }} />
      <Subtitles />

      <Audio src={staticFile("music/darkness.mp3")} loop volume={curve(MUSIC_KEYS)} />
      <Audio src={staticFile("sfx/amb_lab.wav")} loop volume={0.25} />
      {CUES.map((c, i) => (
        <Sequence key={i} name={`sfx ${c.sfx}`} from={f(c.at)} layout="none">
          <Audio src={staticFile(`sfx/${c.sfx}.wav`)} volume={c.vol} />
        </Sequence>
      ))}
    </AbsoluteFill>
  );
};
