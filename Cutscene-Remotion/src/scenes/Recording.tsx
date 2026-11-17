import React from "react";
import { Easing, interpolate, useCurrentFrame } from "remotion";
import { C, Camera, clamp, rnd, talking, W, H } from "../px";
import { f, LINES } from "../timeline";
import { CrtScreen } from "./Flashback";

type Pt = [number, number];
type Pose = { at: number; l: Pt; r: Pt };

const REST_L: Pt = [258, 300], REST_R: Pt = [382, 300]; // mãos apoiadas atrás da bancada: braços caídos, sem cotovelo aberto
const SHOULDER_L: Pt = [264, 192], SHOULDER_R: Pt = [376, 192];

// poses (tempo absoluto em segundos): apresenta a câmera, aponta o esquema, aponta os medidores, desliga
const POSES: Pose[] = [
  { at: 0, l: REST_L, r: REST_R },
  { at: 42.4, l: [204, 262], r: REST_R },
  { at: 45.0, l: REST_L, r: REST_R },
  { at: 46.6, l: [168, 104], r: REST_R },
  { at: 49.6, l: REST_L, r: REST_R },
  { at: 68.3, l: REST_L, r: [474, 104] },
  { at: 70.6, l: REST_L, r: REST_R },
  { at: 107.9, l: [212, 288], r: REST_R },
];

const handAt = (abs: number, side: "l" | "r"): Pt => {
  let i = 0;
  POSES.forEach((p, k) => { if (abs >= f(p.at)) i = k; });
  const prev = POSES[Math.max(0, i - 1)][side], cur = POSES[i][side];
  const k = interpolate(abs, [f(POSES[i].at), f(POSES[i].at) + 9], [0, 1], { ...clamp, easing: Easing.out(Easing.cubic) });
  return [prev[0] + (cur[0] - prev[0]) * k, prev[1] + (cur[1] - prev[1]) * k];
};

// braço em dois segmentos (ombro, cotovelo, mão), cotovelo para fora
const Arm: React.FC<{ s: Pt; t: Pt; out: number; gesture: number }> = ({ s, t, out, gesture }) => {
  const a = 62, b = 60;
  let dx = t[0] - s[0], dy = t[1] - s[1] + gesture;
  let d = Math.hypot(dx, dy);
  if (d > a + b - 1) { dx *= (a + b - 1) / d; dy *= (a + b - 1) / d; d = a + b - 1; }
  const base = Math.atan2(dy, dx);
  const ang = Math.acos(Math.min(1, (a * a + d * d - b * b) / (2 * a * d)));
  const e1: Pt = [s[0] + a * Math.cos(base + ang), s[1] + a * Math.sin(base + ang)];
  const e2: Pt = [s[0] + a * Math.cos(base - ang), s[1] + a * Math.sin(base - ang)];
  const e = (e1[0] - s[0]) * out > (e2[0] - s[0]) * out ? e1 : e2;
  const h: Pt = [s[0] + dx, s[1] + dy];
  const cuff: Pt = [e[0] + (h[0] - e[0]) * 0.86, e[1] + (h[1] - e[1]) * 0.86];
  return (
    <g strokeLinecap="round" strokeLinejoin="round">
      <path d={`M${s[0]} ${s[1]} L${e[0]} ${e[1]}`} stroke={C.coatShade} strokeWidth={26} />
      <path d={`M${s[0]} ${s[1]} L${e[0]} ${e[1]} L${cuff[0]} ${cuff[1]}`} stroke={C.coat} strokeWidth={20} fill="none" />
      <path d={`M${e[0]} ${e[1]} L${cuff[0]} ${cuff[1]}`} stroke={C.coatShade} strokeWidth={4} fill="none" transform={`translate(${out * 6} 2)`} />
      <rect x={cuff[0] - 7} y={cuff[1] - 6} width={14} height={12} fill="#5d6b72" />
      <rect x={h[0] - 8} y={h[1] - 7} width={16} height={14} fill={C.rSkin} />
      <rect x={h[0] - 8} y={h[1] + 3} width={16} height={4} fill={C.rSkinDark} />
    </g>
  );
};

const Monitor: React.FC<{ x: number; y: number; w: number; h: number; children: React.ReactNode }> = ({ x, y, w, h, children }) => (
  <g>
    <rect x={x} y={y} width={w} height={h} fill="#3a4440" />
    <rect x={x + 6} y={y + 6} width={w - 12} height={h - 18} fill="#071009" />
    <g>{children}</g>
    <rect x={x + 6} y={y + 6} width={w - 12} height={h - 18} fill="url(#scan)" />
    <rect x={x + w - 14} y={y + h - 9} width={4} height={3} fill={C.phosDim} />
  </g>
);

export const RecordSet: React.FC<{ abs: number; gauges?: boolean; saboteur?: boolean }> = ({ abs, gauges, saboteur }) => {
  const speaking = LINES.some((l) => abs >= f(l.at) && abs < f(l.at + l.dur));
  const mouth = talking(abs, speaking);
  const blink = rnd(Math.floor(abs / 4) + 3) > 0.93;
  const turn = interpolate(abs, [f(104.2), f(104.6)], [0, 1], { ...clamp, easing: Easing.out(Easing.cubic) });
  const g = speaking ? Math.round(Math.sin(abs / 9) * 4) : 0;
  const hx = 320 + Math.round(turn * 6), hy = 120 + Math.round(Math.sin(abs / 40));
  const wave = Array.from({ length: 34 }, (_, i) => {
    const spike = rnd(Math.floor(i / 3) + Math.floor(abs / 8) * 17) > 0.82 ? (rnd(i + abs) - 0.5) * 40 : 0;
    return `${458 + i * 4},${Math.round(70 + Math.sin(i * 0.6 + abs / 6) * 16 + spike)}`;
  }).join(" ");

  return (
    <g>
      <rect x={-200} y={-100} width={1040} height={560} fill={C.coldWall} />
      {[0, 1, 2].map((s) => (
        <g key={s}>
          <rect x={-200} y={150 + s * 70} width={1040} height={5} fill={C.coldMid} />
          {Array.from({ length: 20 }, (_, i) => <rect key={i} x={-180 + i * 52} y={150 + s * 70 - 26 - (i % 3) * 4} width={10 + (i % 4) * 3} height={26 + (i % 3) * 4} fill={i % 4 ? "#22333a" : "#2b3e45"} />)}
        </g>
      ))}
      <ellipse cx={320} cy={200} rx={260} ry={170} fill={C.phosDim} opacity={0.12} />

      <Monitor x={40} y={24} w={150} h={112}>
        {[[70, 44, 40, 26], [124, 44, 40, 26], [92, 80, 52, 30], [64, 92, 26, 18]].map(([x, y, w, h], i) => (
          <rect key={i} x={x} y={y} width={w} height={h} fill="none" stroke={C.phos} strokeWidth={1} />
        ))}
        <path d="M110 70 L118 80 M144 70 L130 80" stroke={C.phos} strokeWidth={1} strokeDasharray="2 2" />
      </Monitor>
      <Monitor x={450} y={24} w={150} h={112}>
        {gauges ? (
          <g>
            <rect x={470} y={44} width={92} height={18} fill="none" stroke={C.phos} />
            {Array.from({ length: 6 }, (_, i) => <rect key={i} x={474 + i * 14} y={48} width={10} height={10} fill={C.phos} />)}
            <rect x={470} y={78} width={110} height={14} fill="none" stroke={C.phos} />
            <rect x={473} y={81} width={76} height={8} fill={C.phos} /><rect x={549} y={81} width={28} height={8} fill={C.red} />
            <rect x={584} y={74} width={8} height={22} fill={C.red} />
          </g>
        ) : <polyline points={wave} stroke={C.phos} strokeWidth={1} fill="none" />}
      </Monitor>
      <Monitor x={396} y={4} w={92} h={72}>
        <rect x={402} y={10} width={80} height={54} fill="#3a4a40" />
        {Array.from({ length: 160 }, (_, i) => (
          <rect key={i} x={402 + Math.floor(rnd(i + abs * 11) * 78)} y={10 + Math.floor(rnd(i * 5 + abs * 3) * 48)} width={2} height={1} fill={C.metalHi} opacity={0.6} />
        ))}
        {saboteur && rnd(Math.floor(abs / 2)) > 0.2 && (
          <g fill="#020303"><rect x={432} y={20} width={16} height={16} /><polygon points="424,58 456,58 452,38 428,38" /></g>
        )}
      </Monitor>

      {/* pesquisador: jaleco, gravata, ombros caídos */}
      <polygon points="214,360 426,360 412,214 382,188 258,188 228,214" fill={C.coat} />
      <polygon points="214,360 262,360 252,208 228,214" fill={C.coatShade} />
      <polygon points="300,188 340,188 320,250" fill="#5d6b72" />
      <polygon points="316,196 324,196 326,246 320,252 314,246" fill="#2b3236" />
      <polygon points="288,188 300,188 318,250 306,256" fill={C.coatShade} />
      <polygon points="352,188 340,188 322,250 334,256" fill={C.coatShade} />
      <rect x={360} y={226} width={20} height={22} fill={C.coatShade} />
      <rect x={364} y={222} width={2} height={12} fill="#2f4f7a" /><rect x={370} y={222} width={2} height={10} fill={C.red} />

      {/* cabeça cansada, de frente para o jogador */}
      <g transform={`translate(${hx - 320} ${hy - 120}) rotate(${(-turn * 6).toFixed(1)} 320 170)`}>
        <rect x={306} y={156} width={28} height={34} fill={C.rSkinDark} />
        <rect x={290} y={82} width={60} height={80} fill={C.rSkin} />
        <rect x={290} y={146} width={60} height={16} fill={C.rSkinDark} opacity={0.25} />
        <rect x={284} y={112} width={8} height={18} fill={C.rSkinDark} /><rect x={348} y={112} width={8} height={18} fill={C.rSkinDark} />
        <polygon points="284,104 288,74 304,64 336,64 352,74 356,104 348,96 340,84 300,84 292,96" fill={C.rHair} />
        <polygon points="330,64 346,70 352,90 340,80" fill={C.rHairGray} />
        <rect x={296} y={100} width={18} height={3} fill={C.rHair} /><rect x={326} y={100} width={18} height={3} fill={C.rHair} />
        <rect x={294} y={106} width={22} height={14} fill="none" stroke={C.ink} strokeWidth={2} />
        <rect x={324} y={106} width={22} height={14} fill="none" stroke={C.ink} strokeWidth={2} />
        <rect x={316} y={111} width={8} height={2} fill={C.ink} />
        {blink ? (
          <><rect x={298} y={112} width={14} height={2} fill={C.rSkinDark} /><rect x={328} y={112} width={14} height={2} fill={C.rSkinDark} /></>
        ) : (
          <>
            <rect x={298} y={110} width={14} height={6} fill="#dfe8e4" /><rect x={328} y={110} width={14} height={6} fill="#dfe8e4" />
            <rect x={303 + Math.round(turn * 4)} y={110} width={5} height={6} fill="#1b2422" /><rect x={333 + Math.round(turn * 4)} y={110} width={5} height={6} fill="#1b2422" />
          </>
        )}
        <rect x={298} y={121} width={14} height={2} fill={C.rSkinDark} /><rect x={328} y={121} width={14} height={2} fill={C.rSkinDark} />
        <polygon points="320,114 314,136 326,136" fill={C.rSkinDark} />
        {Array.from({ length: 40 }, (_, i) => <rect key={i} x={294 + Math.floor(rnd(i) * 52)} y={138 + Math.floor(rnd(i + 40) * 22)} width={1} height={1} fill="#6e584b" />)}
        {mouth ? <rect x={312} y={144} width={16} height={8} fill="#3a1f1c" /> : <rect x={310} y={146} width={20} height={2} fill="#5a3b33" />}
      </g>

      <Arm s={SHOULDER_L} t={handAt(abs, "l")} out={-1} gesture={g} />
      <Arm s={SHOULDER_R} t={handAt(abs, "r")} out={1} gesture={-g} />

      {/* bancada com o equipamento */}
      <rect x={-200} y={282} width={1040} height={8} fill="#3d4b4f" />
      <rect x={-200} y={290} width={1040} height={200} fill="#2a3539" />
      <rect x={20} y={252} width={90} height={32} fill="#26302f" />
      {[38, 66, 94].map((x) => <g key={x}><rect x={x - 14} y={244} width={28} height={28} fill="#2b2a28" /><rect x={x - 4} y={254} width={8} height={8} fill="#9a9080" /></g>)}
      <polygon points="140,284 250,284 246,272 144,272" fill="#57615f" />
      {Array.from({ length: 10 }, (_, i) => <rect key={i} x={148 + i * 10} y={275} width={7} height={3} fill="#8b9592" />)}
      {/* câmera temporal: grafite, anéis de latão, faixa verde-fósforo */}
      <g transform={`rotate(${Math.sin(abs / 30) * 1.2} 196 268)`}>
        <rect x={170} y={250} width={44} height={32} fill="#2b2f33" />
        <rect x={172} y={252} width={30} height={3} fill={C.phos} />
        <rect x={204} y={254} width={24} height={24} fill={C.brass} />
        <rect x={207} y={257} width={18} height={18} fill={C.brassDark} />
        <rect x={210} y={260} width={12} height={12} fill="#0f2a2a" />
        <rect x={212} y={262} width={3} height={3} fill="#9ff5d0" />
        <rect x={176} y={270} width={4} height={4} fill={C.phos} opacity={rnd(Math.floor(abs / 10)) > 0.3 ? 1 : 0.3} />
      </g>
      <rect x={506} y={222} width={92} height={62} fill="#4d5652" />
      <rect x={512} y={228} width={54} height={44} fill="#061209" />
      <polyline points={Array.from({ length: 13 }, (_, i) => `${514 + i * 4},${250 + Math.round(Math.sin(i * 0.8 + abs / 5) * 10)}`).join(" ")} stroke={C.phos} fill="none" />
      {[0, 1, 2].map((i) => <rect key={i} x={574} y={232 + i * 14} width={10} height={8} fill="#262c2a" />)}
      <path d="M420 300 Q470 250 520 284" stroke={C.red} strokeWidth={3} fill="none" />
      <path d="M430 300 Q480 270 540 290" stroke="#25425c" strokeWidth={3} fill="none" />
      <g transform={`rotate(-6 450 300) translate(0 ${Math.round(Math.sin(abs / 60))})`}>
        <rect x={420} y={288} width={64} height={40} fill="#c4d0d3" />
        <rect x={440} y={298} width={20} height={14} fill="none" stroke="#2c4f7a" />
        <rect x={426} y={294} width={10} height={8} fill="none" stroke="#2c4f7a" />
      </g>

      {/* gravação: verde-fósforo e linhas de varredura */}
      <rect x={-200} y={-100} width={1040} height={560} fill="#0f3a22" opacity={0.16} />
    </g>
  );
};

const RecOverlay: React.FC<{ abs: number }> = ({ abs }) => (
  <div style={{ position: "absolute", left: 12, top: 8, fontFamily: "VCR", fontSize: 14, color: C.phos }}>
    <span style={{ opacity: Math.floor(abs / 15) % 2 ? 1 : 0.2 }}>●</span> REC 1984-11-16 03:12
  </div>
);

const Scan: React.FC = () => (
  <svg width={W} height={H} style={{ position: "absolute" }}>
    <defs><pattern id="scan2" width={2} height={3} patternUnits="userSpaceOnUse"><rect width={2} height={1} fill="#000" opacity={0.25} /></pattern></defs>
    <rect width={W} height={H} fill="url(#scan2)" />
  </svg>
);

// 00:37–00:51 · do CRT de 1975 para o CRT da gravação; o pesquisador fala com o jogador
export const RecordScene: React.FC = () => {
  const frame = useCurrentFrame();
  const abs = frame + f(37);
  return (
    <>
      <Camera end={f(14)} shots={[
        { from: 0, x0: 115, y0: 74, z0: 4.4, x1: 320, y1: 180, z1: 1 },
        { from: f(3), x0: 320, y0: 180, z0: 1, x1: 320, y1: 168, z1: 1.12 },
      ]}>
        {() => <RecordSet abs={abs} />}
      </Camera>
      <Scan />
      <RecOverlay abs={abs} />
    </>
  );
};

// 01:08–01:10.6 · aponta os medidores (o restante do bloco é a demonstração)
export const MetersRec: React.FC = () => {
  const frame = useCurrentFrame();
  const abs = frame + f(68);
  return (
    <>
      <Camera end={f(2.6)} shots={[{ from: 0, x0: 370, y0: 150, z0: 1.25, x1: 384, y1: 146, z1: 1.32 }]}>
        {() => <RecordSet abs={abs} gauges />}
      </Camera>
      <Scan />
      <RecOverlay abs={abs} />
    </>
  );
};

// 01:40–01:52 · sabotador no monitor, fim da mensagem, CRT desliga, objetivo
export const EndingScene: React.FC = () => {
  const frame = useCurrentFrame();
  const abs = frame + f(100);
  const off = f(8.4);
  if (frame < off) {
    return (
      <>
        <Camera end={off} shots={[{ from: 0, x0: 320, y0: 168, z0: 1.12, x1: 336, y1: 140, z1: 1.38 }]}>
          {() => <RecordSet abs={abs} saboteur={abs >= f(103.2) && abs < f(104.1)} />}
        </Camera>
        <Scan />
        <RecOverlay abs={abs} />
      </>
    );
  }
  const t = frame - off;
  const line = interpolate(t, [3, 14], [1, 0], clamp);
  const objective = interpolate(frame, [f(9.4), f(9.9)], [0, 1], clamp) * interpolate(frame, [f(11.4), f(12)], [1, 0], clamp);
  return (
    <>
      <svg width={W} height={H} shapeRendering="crispEdges" style={{ position: "absolute" }}>
        <rect width={W} height={H} fill="#000" />
        {t < 3 && <rect y={H / 2 - Math.round(180 * (1 - t / 3))} width={W} height={Math.max(2, Math.round(360 * (1 - t / 3)))} fill={C.coldHi} />}
        {t >= 3 && line > 0 && <rect x={W / 2 - Math.round(300 * line)} y={H / 2 - 1} width={Math.max(2, Math.round(600 * line))} height={2} fill={C.white} />}
        {t >= 14 && t < 20 && <rect x={W / 2 - 1} y={H / 2 - 1} width={2} height={2} fill={C.white} />}
      </svg>
      <div style={{ position: "absolute", top: 160, width: W, textAlign: "center", fontFamily: "VCR", fontSize: 20, color: C.paper, opacity: objective }}>
        Recupere as peças e reconstrua o protótipo
      </div>
    </>
  );
};

export { CrtScreen };
