import React from "react";
import { Easing, interpolate, useCurrentFrame } from "remotion";
import { C, clamp, rnd, W, H, Dither } from "../px";
import { f } from "../timeline";
import { MetersRec } from "./Recording";

const PIECE: [number, number] = [320, 214];
const LOSTCAM: [number, number] = [322, 292];

// ---------- visão do jogador (ilustrada) ----------
const PovBench: React.FC = () => (
  <g>
    <rect width={W} height={H} fill="#1d282d" />
    {Array.from({ length: 16 }, (_, i) => <rect key={i} x={i * 40} y={0} width={2} height={200} fill="#172025" />)}
    <rect x={430} y={36} width={150} height={110} fill="#5d7a87" />
    {Array.from({ length: 10 }, (_, i) => <rect key={i} x={430} y={38 + i * 11} width={150} height={5} fill="#1a2428" />)}
    <rect x={426} y={32} width={158} height={4} fill="#11181b" /><rect x={426} y={146} width={158} height={4} fill="#11181b" />
    {[60, 120].map((y) => (
      <g key={y}>
        <rect x={24} y={y} width={190} height={5} fill={C.woodDark} />
        {Array.from({ length: 8 }, (_, i) => <rect key={i} x={30 + i * 22} y={y - 22 - (i % 3) * 5} width={16} height={22 + (i % 3) * 5} fill={i % 3 ? "#3a3f3c" : "#4d5a52"} />)}
      </g>
    ))}
    <rect x={240} y={60} width={150} height={92} fill="#4e4433" />
    {Array.from({ length: 13 }, (_, i) => Array.from({ length: 7 }, (_, j) => <rect key={`${i}-${j}`} x={246 + i * 11} y={66 + j * 12} width={2} height={2} fill="#2e281e" />))}
    <rect x={262} y={74} width={6} height={56} fill="#2b2f33" /><rect x={290} y={78} width={4} height={50} fill="#7a3b2c" />
    {/* bancada em perspectiva */}
    <polygon points="0,260 640,260 600,200 40,200" fill="#5c4430" />
    <rect x={0} y={198} width={640} height={3} fill="#2c2017" />
    <rect x={0} y={260} width={640} height={100} fill="#3e2c1e" />
    <rect x={0} y={258} width={640} height={4} fill="#2c2017" />
    <rect x={70} y={150} width={104} height={64} fill="#3c4440" /><rect x={78} y={160} width={60} height={44} fill="#0b140e" />
    {[0, 1, 2].map((i) => <rect key={i} x={148} y={164 + i * 14} width={12} height={8} fill="#232826" />)}
    <polygon points="440,224 512,220 518,236 446,240" fill={C.paper} /><polygon points="452,232 524,236 520,250 448,246" fill="#cfc5ad" />
    <rect x={536} y={150} width={4} height={56} fill="#3b3f3a" /><polygon points="520,150 560,146 556,136 528,138" fill="#3b4a3f" />
    <rect x={190} y={214} width={52} height={24} fill="#5a4a36" /><rect x={370} y={210} width={60} height={14} fill="#3a3a3e" />
    <ellipse cx={320} cy={200} rx={250} ry={120} fill="#9fb4c4" opacity={0.08} />
  </g>
);

const PovCorner: React.FC = () => (
  <g>
    <rect width={W} height={H} fill="#1d282d" />
    <rect y={180} width={W} height={180} fill="#2e2a25" />
    {Array.from({ length: 17 }, (_, i) => <line key={i} x1={320 + (i - 8) * 120} y1={360} x2={320 + (i - 8) * 26} y2={180} stroke="#1e1b18" strokeWidth={2} />)}
    {[196, 216, 242, 276, 320].map((y) => <rect key={y} y={y} width={W} height={2} fill="#1e1b18" />)}
    <rect y={176} width={W} height={6} fill="#15181a" />
    <rect x={50} y={40} width={170} height={140} fill="#4a5250" />
    {[0, 1, 2, 3].map((i) => <g key={i}><rect x={58} y={46 + i * 33} width={154} height={28} fill="#3f4645" /><rect x={126} y={57 + i * 33} width={18} height={4} fill="#8b9592" /></g>)}
    <rect x={420} y={10} width={130} height={170} fill="#3c3228" /><rect x={430} y={20} width={110} height={160} fill="#4a3e31" />
    <rect x={526} y={100} width={6} height={6} fill={C.brass} />
    <rect x={590} y={0} width={10} height={180} fill="#2f3537" />
    <rect x={250} y={120} width={90} height={60} fill="#5a4a36" /><rect x={262} y={84} width={64} height={38} fill="#6b5840" />
    <ellipse cx={320} cy={290} rx={240} ry={70} fill="#a9bdc8" opacity={0.08} />
  </g>
);

const Piece: React.FC<{ x: number; y: number; glitch: boolean; frame: number }> = ({ x, y, glitch, frame }) => {
  if (glitch && rnd(Math.floor(frame / 2)) < 0.35) return null;
  const dx = glitch ? Math.round((rnd(frame) - 0.5) * 4) : 0;
  return (
    <g transform={`translate(${x - 16 + dx} ${y - 12})`}>
      <rect x={-3} y={-3} width={38} height={30} fill={C.phos} opacity={0.18} />
      <rect width={32} height={24} fill="#30443a" /><rect x={2} y={2} width={28} height={20} fill={C.pcb} />
      <rect x={10} y={6} width={12} height={12} fill={C.ink} />
      <rect width={32} height={2} fill={C.phos} /><rect y={22} width={32} height={2} fill={C.phos} />
    </g>
  );
};

const LostCam: React.FC<{ x: number; y: number }> = ({ x, y }) => (
  <g transform={`translate(${x - 22} ${y - 12}) rotate(10 22 12)`}>
    <rect x={-6} y={-6} width={60} height={38} fill={C.phos} opacity={0.15} />
    <rect width={34} height={24} fill="#2b2f33" /><rect x={2} y={2} width={22} height={2} fill={C.phos} />
    <rect x={26} y={2} width={20} height={20} fill={C.brass} /><rect x={29} y={5} width={14} height={14} fill={C.brassDark} /><rect x={32} y={8} width={8} height={8} fill="#0f2a2a" />
  </g>
);

const Bracket: React.FC<{ x: number; y: number; s: number; frame: number }> = ({ x, y, s, frame }) => (
  <g fill={C.phos} opacity={rnd(Math.floor(frame / 3)) > 0.15 ? 1 : 0.4}>
    {[[-1, -1], [1, -1], [-1, 1], [1, 1]].map(([sx, sy]) => (
      <g key={`${sx}${sy}`}>
        <rect x={x + sx * s - (sx > 0 ? 8 : 0)} y={y + sy * s - (sy > 0 ? 2 : 0)} width={8} height={2} />
        <rect x={x + sx * s - (sx > 0 ? 2 : 0)} y={y + sy * s - (sy > 0 ? 8 : 0)} width={2} height={8} />
      </g>
    ))}
  </g>
);

// ---------- HUD do visor (reprodução do visor do jogo) ----------
const Hud: React.FC<{ open: number; battery: number; heat: number; showHeat: boolean; frame: number }> = ({ open, battery, heat, showHeat, frame }) => (
  <g>
    {open > 0 && (
      <g>
        <rect y={-24 + Math.round(24 * open)} width={W} height={24} fill="#000" />
        <rect y={H - Math.round(24 * open)} width={W} height={24} fill="#000" />
        <g opacity={open}>
          {[[40, 40], [600, 40], [40, 320], [600, 320]].map(([x, y]) => (
            <g key={`${x}${y}`} fill={C.white}>
              <rect x={x < 320 ? x : x - 40} y={y < 180 ? y : y - 2} width={40} height={2} />
              <rect x={x < 320 ? x : x - 2} y={y < 180 ? y : y - 40} width={2} height={40} />
            </g>
          ))}
          <rect x={52} y={50} width={6} height={6} fill={C.red} opacity={Math.floor(frame / 15) % 2 ? 1 : 0.2} />
          <rect x={316} y={176} width={8} height={8} fill="none" stroke={C.white} strokeWidth={1} />
          <rect x={319} y={179} width={2} height={2} fill={C.white} />
        </g>
      </g>
    )}
    {(open > 0 || showHeat) && (
      <g>
        <rect x={520} y={48} width={64} height={16} fill="none" stroke={C.white} strokeWidth={2} />
        <rect x={584} y={53} width={4} height={6} fill={C.white} />
        {Array.from({ length: 10 }, (_, i) => i < Math.round(battery * 10) && <rect key={i} x={523 + i * 6} y={51} width={4} height={10} fill={C.white} />)}
        {showHeat && (
          <>
            <rect x={520} y={70} width={68} height={8} fill="none" stroke={C.white} strokeWidth={1} />
            <rect x={522} y={72} width={Math.round(64 * heat)} height={4} fill={heat > 0.85 && rnd(frame) > 0.5 ? C.amberHi : C.red} />
          </>
        )}
      </g>
    )}
  </g>
);

const Label: React.FC<{ x: number; y: number; text: string; color?: string; size?: number; o?: number }> = ({ x, y, text, color = C.white, size = 14, o = 1 }) => (
  <div style={{ position: "absolute", left: x, top: y, transform: "translateX(-50%)", fontFamily: "VCR", fontSize: size, color, whiteSpace: "nowrap", opacity: o }}>{text}</div>
);

const Key: React.FC<{ x: number; y: number; k: string; t: number; from: number; to: number }> = ({ x, y, k, t, from, to }) => {
  if (t < from || t >= to) return null;
  const s = interpolate(t, [from, from + 5], [0.6, 1], { ...clamp, easing: Easing.out(Easing.back(2)) });
  return (
    <div style={{ position: "absolute", left: x - 11, top: y - 11, width: 22, height: 22, background: C.paper, boxShadow: `0 3px 0 ${C.paperDark}, 0 0 0 2px ${C.ink}`, color: C.ink, fontFamily: "VCR", fontSize: 14, textAlign: "center", lineHeight: "22px", scale: s }}>{k}</div>
  );
};

const Prompt: React.FC<{ y: number; text: string; t: number; from: number; to: number; k: string }> = ({ y, text, t, from, to, k }) => {
  if (t < from || t >= to) return null;
  return (
    <>
      <div style={{ position: "absolute", left: W / 2 - 4, top: y - 11, transform: "translateX(-50%)", background: "#000000c0", padding: "3px 10px 3px 34px", fontFamily: "VCR", fontSize: 14, color: C.white, whiteSpace: "nowrap" }}>{text}</div>
      <Key x={W / 2 - text.length * 4.2 - 2} y={y} k={k} t={t} from={from} to={to} />
    </>
  );
};

// 00:51–01:08 · localize, fotografe, colete
export const FindScene: React.FC = () => {
  const frame = useCurrentFrame();
  const s = (x: number) => f(x);
  const pan = Math.round(interpolate(frame, [s(4.5), s(5.8)], [70, 0], { ...clamp, easing: Easing.inOut(Easing.sin) }));
  const zoom = interpolate(frame, [s(10), s(11.5)], [1, 1.6], { ...clamp, easing: Easing.inOut(Easing.sin) });
  const open = interpolate(frame, [s(1.8), s(2.1)], [0, 1], clamp) * interpolate(frame, [s(8.5), s(8.8)], [1, 0], clamp);
  const pieceOn = frame >= s(2.6) && frame < s(13.2);
  const collected = frame >= s(13.6);
  return (
    <>
      <svg width={W} height={H} shapeRendering="crispEdges" style={{ position: "absolute" }}>
        <Dither />
        <g transform={`translate(${PIECE[0]} ${PIECE[1]}) scale(${zoom}) translate(${-PIECE[0] + pan} ${-PIECE[1]})`}>
          <PovBench />
          {pieceOn && <Piece x={PIECE[0]} y={PIECE[1]} glitch={frame < s(7.3)} frame={frame} />}
          {pieceOn && <Bracket x={PIECE[0]} y={PIECE[1]} s={24} frame={frame} />}
          {frame >= s(8.6) && frame < s(13.0) && (
            <g transform={`rotate(${-frame * 3} ${PIECE[0] + 34} ${PIECE[1] - 26})`}>
              {Array.from({ length: 10 }, (_, i) => i < Math.round(10 - (frame - s(8.6)) / 15) && (
                <rect key={i} x={PIECE[0] + 34 + Math.round(Math.cos(i / 10 * 6.28) * 8) - 1} y={PIECE[1] - 26 + Math.round(Math.sin(i / 10 * 6.28) * 8) - 1} width={2} height={2} fill={C.phos} />
              ))}
            </g>
          )}
        </g>
        <Hud open={open} battery={1} heat={0} showHeat={false} frame={frame} />
        {frame >= s(7.3) && frame < s(7.6) && <rect width={W} height={H} fill={C.white} opacity={1 - (frame - s(7.3)) / 9} />}
        {frame >= s(13.0) && frame < s(13.4) && <rect x={PIECE[0] - 30} y={PIECE[1] - 30} width={60} height={60} fill={C.phos} opacity={0.4} />}
      </svg>
      <Label x={160} y={70} text="Localize" o={frame >= s(1.3) ? 1 : 0} size={18} />
      <Label x={320} y={70} text="> Fotografe" o={frame >= s(6) ? 1 : 0} size={18} />
      <Label x={480} y={70} text="> Colete" o={frame >= s(11.8) ? 1 : 0} size={18} />
      <Key x={320} y={280} k="C" t={frame} from={s(1.3)} to={s(2.3)} />
      <Key x={320} y={280} k="C" t={frame} from={s(8.2)} to={s(9)} />
      {frame >= s(6) && frame < s(7.6) && (
        <>
          <svg width={W} height={H} style={{ position: "absolute" }} shapeRendering="crispEdges">
            <rect x={264} y={266} width={20} height={28} fill={C.paper} /><rect x={264} y={266} width={10} height={12} fill={C.amber} />
          </svg>
          <Label x={348} y={272} text="Clique esquerdo" />
        </>
      )}
      <Prompt y={262} text="E para coletar" k="E" t={frame} from={s(11.8)} to={s(13)} />
      <div style={{ position: "absolute", left: 50, top: 268, fontFamily: "VCR", fontSize: 14, color: collected ? C.phos : C.white, background: "#000000a0", padding: "2px 6px", opacity: frame >= s(0.5) ? 1 : 0 }}>
        Coletar peças [{collected ? 1 : 0}/5]
      </div>
      <FadeFromBlack frames={10} />
    </>
  );
};

// 01:08–01:22 · bateria e calor
export const MetersScene: React.FC = () => {
  const frame = useCurrentFrame();
  if (frame < f(2.6)) return <MetersRec />;
  const t = frame - f(2.6);
  const s = (x: number) => f(x);
  const open = t < s(4.4) ? 1 : 0;
  const battery = interpolate(t, [s(0.5), s(4)], [1, 0.6], clamp);
  const heat = interpolate(t, [s(0.5), s(4)], [0, 0.7], clamp) * (t < s(4.6) ? 1 : interpolate(t, [s(4.6), s(7.9)], [1, 0], clamp));
  const batt = t >= s(7.4);
  return (
    <>
      <svg width={W} height={H} shapeRendering="crispEdges" style={{ position: "absolute" }}>
        <PovBench />
        {batt && (
          <g transform={`translate(0 ${Math.round(interpolate(t, [s(7.4), s(7.8)], [-80, 0], { ...clamp, easing: Easing.out(Easing.cubic) }))})`}>
            <rect x={96} y={214} width={30} height={24} fill={C.phos} opacity={0.16} />
            <rect x={104} y={204} width={14} height={30} fill="#2b2f33" /><rect x={104} y={212} width={14} height={10} fill={C.phos} /><rect x={108} y={200} width={6} height={4} fill={C.copper} />
          </g>
        )}
        {t >= s(7.8) && <Bracket x={111} y={219} s={24} frame={t} />}
        <Hud open={open} battery={battery} heat={heat} showHeat frame={t} />
      </svg>
      {t < s(4.4) && <Label x={500} y={66} text="CALOR" size={10} />}
      <Key x={320} y={280} k="C" t={t} from={s(4)} to={s(4.8)} />
      {t >= s(4.4) && t < s(7.3) && <Label x={320} y={250} text="Feche o visor entre as buscas" />}
      {t >= s(8) && <Label x={111} y={170} text="Pilhas recarregam a bateria" color={C.phos} />}
    </>
  );
};

// 01:22–01:40 · superaquecimento, ruptura, câmera perdida em outro ponto
export const RuptureScene: React.FC = () => {
  const frame = useCurrentFrame();
  const s = (x: number) => f(x);
  if (frame >= s(8.5)) return <LostCamera t={frame - s(8.5)} />;
  const heat = interpolate(frame, [s(0.3), s(4)], [0.6, 1], clamp);
  const shift = interpolate(frame, [s(2), s(4)], [0, 1], clamp);
  const hudOn = frame < s(4.5) ? 1 : 0;
  const shutter = interpolate(frame, [s(4.35), s(4.6)], [0, 1], clamp) * interpolate(frame, [s(4.9), s(5.8)], [1, 0], clamp);
  return (
    <>
      <svg width={W} height={H} shapeRendering="crispEdges" style={{ position: "absolute" }}>
        <defs>
          <filter id="onlyR"><feColorMatrix type="matrix" values="1 0 0 0 0  0 0 0 0 0  0 0 0 0 0  0 0 0 1 0" /></filter>
          <filter id="onlyGB"><feColorMatrix type="matrix" values="0 0 0 0 0  0 1 0 0 0  0 0 1 0 0  0 0 0 1 0" /></filter>
        </defs>
        <PovBench />
        {shift > 0 && frame < s(4) && (
          <>
            <g filter="url(#onlyR)" opacity={0.5 * shift} style={{ mixBlendMode: "screen" }} transform={`translate(${Math.round(4 * shift)} 0)`}><PovBench /></g>
            <g filter="url(#onlyGB)" opacity={0.4 * shift} style={{ mixBlendMode: "screen" }} transform={`translate(${-Math.round(4 * shift)} 0)`}><PovBench /></g>
          </>
        )}
        <Hud open={hudOn} battery={0.5} heat={heat} showHeat={hudOn > 0} frame={frame} />
        {frame >= s(4) && frame < s(4.5) && Array.from({ length: 500 }, (_, i) => (
          <rect key={i} x={Math.floor(rnd(i + frame * 31) * W)} y={Math.floor(rnd(i * 7 + frame) * H)} width={4} height={2} fill={C.metalHi} opacity={0.8} />
        ))}
        {frame >= s(4) && frame < s(4.3) && <rect width={W} height={H} fill={C.white} opacity={1 - (frame - s(4)) / 9} />}
        {shutter > 0 && (
          <>
            <rect y={Math.round(-180 + 180 * shutter)} width={W} height={180} fill="#000" />
            <rect y={Math.round(H - 180 * shutter)} width={W} height={180} fill="#000" />
          </>
        )}
      </svg>
      <Key x={300} y={226} k="C" t={frame} from={s(6.2)} to={s(7.8)} />
      {frame >= s(6.5) && frame < s(7.8) && (
        <>
          <div style={{ position: "absolute", left: 322, top: 214, fontFamily: "VCR", fontSize: 22, color: C.red, rotate: `${Math.sin(frame) * 8}deg` }}>X</div>
          <Label x={320} y={248} text="Câmera perdida" color={C.red} />
        </>
      )}
    </>
  );
};

const LostCamera: React.FC<{ t: number }> = ({ t }) => {
  const s = (x: number) => f(x);
  const zoom = interpolate(t, [s(2.5), s(4.5)], [1, 1.45], { ...clamp, easing: Easing.inOut(Easing.sin) });
  const has = t < s(6);
  return (
    <>
      <svg width={W} height={H} shapeRendering="crispEdges" style={{ position: "absolute" }}>
        <g transform={`translate(${LOSTCAM[0]} ${LOSTCAM[1]}) scale(${zoom}) translate(${-LOSTCAM[0]} ${-LOSTCAM[1]})`}>
          <PovCorner />
          {has && <LostCam x={LOSTCAM[0]} y={LOSTCAM[1]} />}
          {has && t >= s(0.6) && <Bracket x={LOSTCAM[0]} y={LOSTCAM[1]} s={30} frame={t} />}
          {t >= s(6) && t < s(6.4) && <rect x={LOSTCAM[0] - 30} y={LOSTCAM[1] - 30} width={60} height={60} fill={C.phos} opacity={0.4} />}
        </g>
      </svg>
      {t >= s(0.3) && t < s(3) && <Label x={320} y={40} text="Outro ponto do laboratório" size={16} />}
      <Prompt y={250} text="E para recuperar a camera" k="E" t={t} from={s(4.6)} to={s(6)} />
      {t >= s(6.1) && <Label x={320} y={240} text="Câmera recuperada" color={C.phos} size={16} />}
      <FadeFromBlack frames={9} from={f(8.5)} />
    </>
  );
};

const FadeFromBlack: React.FC<{ frames: number; from?: number }> = ({ frames, from = 0 }) => {
  const frame = useCurrentFrame();
  const o = interpolate(frame, [from, from + frames], [1, 0], clamp);
  return o > 0 ? <div style={{ position: "absolute", inset: 0, background: "#000", opacity: o }} /> : null;
};
