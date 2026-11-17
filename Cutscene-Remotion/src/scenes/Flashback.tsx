import React from "react";
import { interpolate, useCurrentFrame, Easing } from "remotion";
import { C, Camera, clamp, rnd, W, H, Drop, Sway, Fade, Show } from "../px";
import { f, SCENES } from "../timeline";
import { Hand, LabSet, Figure } from "./LabSet";

// retrato de 0,01 megapixel no CRT: 25x18 células, 5 tons de cinza
const FACE = [
  "........:::::::::........",
  "......:::::::::::::......",
  ".....:::.........:::.....",
  "....::..#########..::....",
  "....:.#############.:....",
  "...::.##..#####..##.::...",
  "...:.##.@@.###.@@.##.:...",
  "...:.###..#####..###.:...",
  "...:.######:::######.:...",
  "...:.######:::######.:...",
  "...:..#####:::#####..:...",
  "....:.#############.:....",
  "....:..##.......##..:....",
  ".....:..###:::###..:.....",
  "......:...#####...:......",
  ".......::.......::.......",
  "........:::::::::........",
  "......:::::::::::::......",
];
const TONE: Record<string, string> = { " ": "#141614", ".": "#2a2d2a", ":": "#5a5e5a", "#": "#a8aca6", "@": "#e6e9e3" };

export const CrtScreen: React.FC<{ x: number; y: number; mode: "lines" | "portrait" | "static"; frame: number; portraitIn?: number }> = ({ x, y, mode, frame, portraitIn = 1 }) => (
  <g>
    <rect x={x} y={y} width={76} height={56} fill="#0c120d" />
    {mode === "lines" && Array.from({ length: 4 }, (_, i) => (
      <rect key={i} x={x} y={y + ((i * 15 + frame * 2) % 56)} width={76} height={3} fill={C.white} opacity={0.8} />
    ))}
    {mode === "portrait" && (
      <g opacity={portraitIn}>
        {FACE.map((row, j) => row.split("").map((ch, i) => (
          <rect key={`${i}-${j}`} x={x + 1 + i * 3} y={y + 1 + j * 3} width={3} height={3}
            fill={TONE[ch] ?? TONE[" "]} opacity={0.85 + rnd(i * 31 + j * 7 + Math.floor(frame / 3)) * 0.15} />
        )))}
      </g>
    )}
    {mode === "static" && Array.from({ length: 140 }, (_, i) => (
      <rect key={i} x={x + Math.floor(rnd(i + frame * 13) * 74)} y={y + Math.floor(rnd(i * 3 + frame * 7) * 54)} width={2} height={1} fill={C.metalHi} opacity={0.7} />
    ))}
    <rect x={x} y={y} width={76} height={56} fill="url(#scan)" />
  </g>
);

// 00:00–00:21 · 1975, laboratório: fita, montagem, primeiro teste
export const LabScene: React.FC = () => {
  const frame = useCurrentFrame();
  const abs = frame + f(SCENES.lab.from);
  return (
    <>
      <Camera end={f(21)} shots={[
        { from: 0, x0: 261, y0: 224, z0: 6.2, x1: 261, y1: 224, z1: 5.8 },
        { from: f(1.8), x0: 261, y0: 224, z0: 5.8, x1: 320, y1: 180, z1: 1 },
        { from: f(7), x0: 210, y0: 175, z0: 1.7, x1: 225, y1: 178, z1: 1.8 },
        { from: f(9.8), x0: 394, y0: 218, z0: 4.2, x1: 394, y1: 218, z1: 4.6 },
        { from: f(12), x0: 300, y0: 226, z0: 4.4, x1: 304, y1: 226, z1: 4.7 },
        { from: f(13.8), x0: 324, y0: 218, z0: 4.4, x1: 324, y1: 218, z1: 4.9 },
        { from: f(15.6), x0: 504, y0: 188, z0: 4.4, x1: 504, y1: 188, z1: 5.6 },
      ]}>
        {(shot) => (
          <>
            <LabSet abs={abs} steve />
            {shot === 3 && <Hand x={interpolate(frame, [f(9.8), f(10.4)], [430, 414], clamp)} y={224} rot={-8} />}
            {shot === 4 && <Hand x={interpolate(frame, [f(12.2), f(12.9)], [282, 290], clamp)} y={232} flip rot={6} />}
            {shot === 5 && (
              <g transform={`rotate(${Math.sin(frame / 4) * 6} 345 222)`}>
                <rect x={326} y={216} width={22} height={2} fill={C.metalHi} />
                <rect x={346} y={213} width={10} height={8} fill={C.red} />
                <Hand x={364} y={222} rot={-4} />
              </g>
            )}
            {shot === 6 && (
              <CrtScreen x={466} y={160} frame={frame} mode={frame < f(17.4) ? "lines" : "portrait"}
                portraitIn={interpolate(frame, [f(17.4), f(18.2)], [0, 1], clamp)} />
            )}
          </>
        )}
      </Camera>
      <Fade from={f(2.2)} dur={10} out={f(6.6)}>
        <svg width={W} height={H} style={{ position: "absolute" }}>
          <rect x={196} y={20} width={248} height={22} fill={C.ink} opacity={0.75} />
        </svg>
        <div style={{ position: "absolute", top: 23, width: W, textAlign: "center", fontFamily: "VCR", fontSize: 16, color: C.amberHi }}>
          1975 · LABORATÓRIOS KODAK
        </div>
      </Fade>
      <BlackIn frames={18} />
    </>
  );
};

// 00:21–00:30 · alguém voltou: bancada vazia, relógio recua, silhueta, golpe, corte seco
export const SabotageScene: React.FC = () => {
  const frame = useCurrentFrame();
  const abs = frame + f(SCENES.sabotage.from);
  const cut = f(6.4);
  if (frame >= cut) return <FloorScatter t={frame - cut} />;
  const push = frame >= f(5.6);
  return (
    <Camera end={cut} shots={[
      { from: 0, x0: 320, y0: 170, z0: 1.15, x1: 320, y1: 170, z1: 1.2 },
      { from: f(3.2), x0: 200, y0: 170, z0: 1.4, x1: 190, y1: 168, z1: 1.55 },
      { from: f(5.6), x0: 290, y0: 190, z0: 2.1, x1: 300, y1: 196, z1: 2.4, shake: 3 },
    ]}>
      {() => (
        <LabSet abs={abs} steve={false} cold rewind={frame < f(3)} silhouette={push ? "push" : frame >= f(3.2) ? "shadow" : "none"}
          tilt={interpolate(abs, [f(26.9), f(27.35)], [0, 1], { ...clamp, easing: Easing.in(Easing.quad) })} />
      )}
    </Camera>
  );
};

const FloorScatter: React.FC<{ t: number }> = ({ t }) => {
  const k = interpolate(t, [0, 20], [0, 1], { ...clamp, easing: Easing.out(Easing.cubic) });
  const shake = t < 12 ? (rnd(t) - 0.5) * 8 : 0;
  const part = (dx: number, dy: number, rot: number, el: React.ReactNode, key: number) => (
    <g key={key} transform={`translate(${Math.round(320 + dx * k)} ${Math.round(170 + dy * k)}) rotate(${rot * k})`}>{el}</g>
  );
  return (
    <svg width={W} height={H} shapeRendering="crispEdges" style={{ position: "absolute" }}>
      <g transform={`translate(${shake} ${shake / 2})`}>
        <rect width={W} height={H} fill="#231e19" />
        {Array.from({ length: 12 }, (_, i) => <rect key={i} y={i * 32} width={W} height={2} fill="#15110e" />)}
        {Array.from({ length: 30 }, (_, i) => <rect key={i} x={Math.floor(rnd(i) * W)} y={(i % 12) * 32} width={2} height={32} fill="#15110e" />)}
        <ellipse cx={320} cy={180} rx={260} ry={150} fill="#9fb4c4" opacity={0.08} />
        {part(-150, -40, -110, <g><rect x={-40} y={-22} width={80} height={44} fill={C.metal} /><rect x={-36} y={-14} width={34} height={28} fill="#1e2120" /><path d="M-5 -22 L5 0 L-2 22" stroke={C.ink} strokeWidth={2} fill="none" /></g>, 1)}
        {part(150, -70, 200, <g><rect x={-16} y={-13} width={32} height={26} fill={C.pcb} /><rect x={-10} y={-6} width={10} height={8} fill={C.ink} /></g>, 2)}
        {part(210, 90, 540, <g><rect x={-16} y={-16} width={32} height={32} fill={C.metalDark} /><rect x={-10} y={-10} width={20} height={20} fill={C.metalHi} /><rect x={-7} y={-7} width={14} height={14} fill="#22303a" /></g>, 3)}
        {part(-230, 60, -60, <g><rect x={-29} y={-16} width={58} height={32} fill="#9a9282" /><rect x={-25} y={-12} width={46} height={18} fill="#1c1a18" /></g>, 4)}
        {[[-90, 120, 900], [70, 140, -700], [120, -130, 400], [-170, -120, -300], [40, -40, 250]].map(([dx, dy, rot], i) =>
          part(dx, dy, rot, <g><rect x={-4} y={-4} width={8} height={8} fill="#3c3127" /><rect x={-1} y={-5} width={2} height={10} fill={C.white} /></g>, 10 + i))}
        <g opacity={interpolate(t, [0, 40], [0.8, 0], clamp)}>
          {Array.from({ length: 10 }, (_, i) => <rect key={i} x={320 + Math.round((rnd(i) - 0.5) * 120 * (1 + t / 20))} y={170 + Math.round((rnd(i + 5) - 0.5) * 70 * (1 + t / 30)) - t} width={14} height={10} fill="#8a857a" />)}
        </g>
        {t < 3 && <rect width={W} height={H} fill={C.white} opacity={1 - t / 3} />}
      </g>
    </svg>
  );
};

// 00:30–00:37 · linha do tempo alterada (motion comic) + peça presa entre dois instantes
export const AlteredScene: React.FC = () => {
  const frame = useCurrentFrame();
  if (frame >= f(6.3)) {
    return (
      <Camera end={f(7)} shots={[{ from: 0, x0: 504, y0: 188, z0: 5.6, x1: 504, y1: 188, z1: 5.6 }]}>
        {() => <><LabSet abs={f(30)} steve={false} cold /><CrtScreen x={466} y={160} mode="static" frame={frame} /></>}
      </Camera>
    );
  }
  const piece = frame >= f(5);
  const fadeOut = interpolate(frame, [f(4.3), f(5)], [1, 0], clamp);
  return (
    <svg width={W} height={H} shapeRendering="crispEdges" style={{ position: "absolute" }}>
      <rect width={W} height={H} fill="#211b15" />
      {!piece && (
        <g opacity={fadeOut}>
          {[0, 1, 2].map((i) => (
            <Drop key={i} at={f(i * 1.2)} dist={300}>
              <rect x={28 + i * 200} y={50} width={184} height={250} fill={C.paper} />
              <rect x={34 + i * 200} y={56} width={172} height={238} fill="#2e261e" />
            </Drop>
          ))}
          {/* 1: fotos digitais somem do arquivo */}
          <Drop at={f(0.2)}>
            <rect x={62} y={210} width={116} height={70} fill="#5a5d57" /><rect x={58} y={200} width={124} height={14} fill="#6d706a" /><rect x={110} y={230} width={20} height={4} fill={C.metalHi} />
          </Drop>
          {[0, 1, 2].map((i) => (
            <g key={i} opacity={interpolate(frame, [f(0.9 + i * 0.25), f(2 + i * 0.25)], [1, 0], clamp) * (frame < f(0.35 + i * 0.1) ? 0 : 1) * (rnd(frame + i) > 0.25 ? 1 : 0.3)}>
              <rect x={76 + i * 26} y={130 + (i % 2) * 18} width={40} height={30} fill={C.white} />
              <rect x={79 + i * 26} y={133 + (i % 2) * 18} width={34} height={22} fill="#6f7c86" />
              <polygon points={`${79 + i * 26},${155 + (i % 2) * 18} ${95 + i * 26},${140 + (i % 2) * 18} ${113 + i * 26},${155 + (i % 2) * 18}`} fill="#3c464d" />
            </g>
          ))}
          {/* 2: câmera digital vira aparelho analógico */}
          <Show from={f(1.3)}>
            <g opacity={interpolate(frame, [f(2), f(2.8)], [1, 0], clamp)}>
              <rect x={280} y={140} width={80} height={50} fill="#8e9296" /><rect x={322} y={150} width={30} height={30} fill={C.metalDark} /><rect x={328} y={156} width={18} height={18} fill="#22303a" /><rect x={288} y={150} width={24} height={20} fill="#3a5566" />
            </g>
            <g opacity={interpolate(frame, [f(2.1), f(2.7)], [0, 1], clamp)}>
              <rect x={278} y={150} width={84} height={42} fill="#2a2826" /><rect x={278} y={150} width={84} height={10} fill="#b8b2a6" /><rect x={302} y={160} width={36} height={30} fill={C.metalDark} /><rect x={310} y={168} width={20} height={16} fill="#1e2830" /><rect x={340} y={140} width={16} height={10} fill="#b8b2a6" />
            </g>
          </Show>
          {/* 3: marco da tecnologia salta décadas */}
          <Drop at={f(2.5)}>
            <rect x={446} y={190} width={150} height={3} fill={C.paper} />
            {Array.from({ length: 6 }, (_, i) => <rect key={i} x={448 + i * 29} y={184} width={2} height={14} fill={C.paper} />)}
          </Drop>
          <Show from={f(2.8)}>
            <g transform={`translate(${Math.round(interpolate(frame, [f(3.1), f(4.1)], [0, 116], { ...clamp, easing: Easing.inOut(Easing.cubic) }))} 0)`}>
              <rect x={432} y={150} width={34} height={22} fill="#d9c48f" /><polygon points="445,172 453,172 449,180" fill="#d9c48f" />
              <rect x={437} y={158} width={24} height={3} fill={C.ink} /><rect x={437} y={164} width={18} height={3} fill={C.ink} />
            </g>
          </Show>
        </g>
      )}
      {piece && <StuckPiece t={frame - f(5)} />}
    </svg>
  );
};

const StuckPiece: React.FC<{ t: number }> = ({ t }) => {
  const bob = Math.round(Math.sin(t / 12) * 4);
  const room = (x: number, tint: string, dust: boolean) => (
    <g>
      <rect x={x} width={320} height={H} fill={tint} />
      <rect x={x} y={230} width={320} height={130} fill="#00000055" />
      <rect x={x} y={226} width={320} height={6} fill="#00000066" />
      <rect x={x + 120} y={190} width={80} height={36} fill="#00000080" />
      <rect x={x + 210} y={198} width={20} height={20} fill="#000000a0" />
      <polygon points={`${x + 60},226 ${x + 66},226 ${x + 80},150 ${x + 76},150`} fill="#00000080" />
      {dust && Array.from({ length: 20 }, (_, i) => <rect key={i} x={x + Math.floor(rnd(i) * 320)} y={Math.floor((rnd(i + 9) * 360 + t * 0.4) % 360)} width={1} height={1} fill={C.metalHi} />)}
    </g>
  );
  return (
    <g>
      {room(0, "#7a5a34", false)}
      {room(320, "#2f4a56", true)}
      <polygon points="318,0 324,40 316,90 325,140 317,200 324,260 318,360 330,360 323,260 331,200 322,140 330,90 322,40 328,0" fill={C.paper} />
      <g transform={`translate(0 ${bob})`}>
        <rect x={290} y={150} width={60} height={46} fill={C.phos} opacity={0.18 + (rnd(Math.floor(t / 2)) > 0.6 ? 0.1 : 0)} />
        <rect x={296} y={156} width={48} height={34} fill="#30443a" />
        <rect x={299} y={159} width={42} height={28} fill={C.pcb} />
        <rect x={310} y={164} width={20} height={16} fill={C.ink} />
        {[0, 1, 2, 3].map((i) => <rect key={i} x={312 + i * 5} y={161} width={2} height={3} fill={C.metalHi} />)}
        <rect x={296} y={156} width={48} height={2} fill={C.phos} /><rect x={296} y={188} width={48} height={2} fill={C.phos} />
      </g>
    </g>
  );
};

export const BlackIn: React.FC<{ frames: number }> = ({ frames }) => {
  const frame = useCurrentFrame();
  const o = interpolate(frame, [0, frames], [1, 0], clamp);
  return o > 0 ? <div style={{ position: "absolute", inset: 0, background: "#000", opacity: o }} /> : null;
};

export { Sway, Figure };
