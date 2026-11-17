import React from "react";
import { Easing, interpolate, useCurrentFrame } from "remotion";

// Paleta limitada da pixel art (vídeo nativo 640x360, ampliado sem filtro na Unity).
export const C = {
  ink: "#0d0b0a", night: "#141218",
  wall: "#3f3122", wall2: "#33281c", wallLine: "#2a2016",
  amber: "#f2a84a", amberHi: "#ffd27a", amberLow: "#9a6430",
  wood: "#6b4426", woodHi: "#8c5c34", woodDark: "#43291a",
  metal: "#8a8f94", metalDark: "#4b5055", metalHi: "#c3c7c9",
  beige: "#c9b48a", beigeDark: "#8f7b58", beigeHi: "#e2d2ad",
  pcb: "#2f6b45", pcbHi: "#4f9a62", copper: "#c58a45",
  skin: "#e0a77c", skinDark: "#a86e4f", hair: "#2a1d16",
  shirt: "#b49a6a", shirtDark: "#7e6a45",
  cold: "#121d24", coldMid: "#1f3039", coldHi: "#3d5a63", coldWall: "#18262d",
  phos: "#7dffa0", phosDim: "#2c7a4c", phosDark: "#0a1a10",
  coat: "#d6dedb", coatShade: "#9aa8a4", coatDark: "#6f7d7a",
  rSkin: "#c79a7c", rSkinDark: "#946a52", rHair: "#3a3530", rHairGray: "#77716a",
  red: "#b04a3c", paper: "#e9dcc0", paperDark: "#b9ab8e", white: "#f4f0e6",
  brass: "#b58a3e", brassDark: "#6d5222",
};

export const W = 640;
export const H = 360;
export const clamp = { extrapolateLeft: "clamp", extrapolateRight: "clamp" } as const;
export const rnd = (n: number) => {
  const x = Math.sin(n * 127.1 + 311.7) * 43758.5453;
  return x - Math.floor(x);
};
export const r = Math.round;

// Recorte que desce de cima, desacelera e acomoda com um quique curto.
export const dropY = (frame: number, at: number, dist = 260, dur = 14) => {
  const t = interpolate(frame, [at, at + dur], [1, 0], { ...clamp, easing: Easing.out(Easing.cubic) });
  const after = frame - at - dur;
  const bounce = after > 0 && after < 8 ? Math.sin((after / 8) * Math.PI) * 3 * (1 - after / 8) : 0;
  return r(-t * dist + bounce);
};

export const Drop: React.FC<{ at: number; dist?: number; dur?: number; children: React.ReactNode }> = ({ at, dist = 260, dur = 14, children }) => {
  const frame = useCurrentFrame();
  if (frame < at) return null;
  return <g transform={`translate(0 ${dropY(frame, at, dist, dur)})`}>{children}</g>;
};

// Oscilação lateral lenta em torno de um pivô (recortes em destaque, cabos, papéis).
export const Sway: React.FC<{ x: number; y: number; deg: number; period?: number; phase?: number; children: React.ReactNode }> = ({ x, y, deg, period = 90, phase = 0, children }) => {
  const frame = useCurrentFrame();
  const a = Math.sin((frame / period) * Math.PI * 2 + phase) * deg;
  return <g transform={`rotate(${a.toFixed(2)} ${x} ${y})`}>{children}</g>;
};

export const Show: React.FC<{ from: number; to?: number; children: React.ReactNode }> = ({ from, to = 1e9, children }) => {
  const frame = useCurrentFrame();
  return frame >= from && frame < to ? <>{children}</> : null;
};

export const Fade: React.FC<{ from: number; dur: number; out?: number; outDur?: number; children: React.ReactNode }> = ({ from, dur, out, outDur = 8, children }) => {
  const frame = useCurrentFrame();
  if (frame < from) return null;
  let o = interpolate(frame, [from, from + dur], [0, 1], clamp);
  if (out !== undefined) o *= interpolate(frame, [out, out + outDur], [1, 0], clamp);
  if (o <= 0) return null;
  return <g opacity={o}>{children}</g>;
};

// Câmera por planos: cada plano tem corte seco no início e movimento suave até o fim.
export type Shot = { from: number; x0: number; y0: number; z0: number; x1: number; y1: number; z1: number; shake?: number };
export const Camera: React.FC<{ shots: Shot[]; end: number; children: (shotIndex: number) => React.ReactNode }> = ({ shots, end, children }) => {
  const frame = useCurrentFrame();
  let i = 0;
  shots.forEach((s, k) => { if (frame >= s.from) i = k; });
  const s = shots[i];
  const to = i + 1 < shots.length ? shots[i + 1].from : end;
  const k = interpolate(frame, [s.from, to], [0, 1], { ...clamp, easing: Easing.inOut(Easing.sin) });
  let x = s.x0 + (s.x1 - s.x0) * k, y = s.y0 + (s.y1 - s.y0) * k;
  const z = s.z0 + (s.z1 - s.z0) * k;
  if (s.shake) { x += (rnd(frame) - 0.5) * s.shake; y += (rnd(frame + 99) - 0.5) * s.shake; }
  return (
    <svg width={W} height={H} viewBox={`0 0 ${W} ${H}`} shapeRendering="crispEdges" style={{ position: "absolute" }}>
      <Dither />
      <g transform={`translate(${W / 2} ${H / 2}) scale(${z}) translate(${-x} ${-y})`}>{children(i)}</g>
    </svg>
  );
};

// Padrões de pontilhado (dithering) para luz e sombra em pixel art.
export const Dither: React.FC = () => (
  <defs>
    {[
      ["d50amber", C.amberHi], ["d50dark", C.ink], ["d50phos", C.phos], ["d50cold", C.coldHi], ["d50white", C.white], ["d50red", C.red],
    ].map(([id, col]) => (
      <pattern key={id} id={id} width={2} height={2} patternUnits="userSpaceOnUse">
        <rect width={1} height={1} fill={col} />
        <rect x={1} y={1} width={1} height={1} fill={col} />
      </pattern>
    ))}
    <pattern id="d25dark" width={2} height={2} patternUnits="userSpaceOnUse">
      <rect width={1} height={1} fill={C.ink} />
    </pattern>
    <pattern id="scan" width={2} height={2} patternUnits="userSpaceOnUse">
      <rect width={2} height={1} fill="#000" opacity={0.35} />
    </pattern>
  </defs>
);

// Boca da fala: abre e fecha enquanto há fala ativa.
export const talking = (frame: number, active: boolean) => active && rnd(Math.floor(frame / 3)) > 0.4;
