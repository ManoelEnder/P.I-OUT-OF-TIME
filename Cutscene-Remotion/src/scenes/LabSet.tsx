import React from "react";
import { interpolate, Easing } from "remotion";
import { C, clamp, dropY, rnd } from "../px";
import { f } from "../timeline";

// Bancada de 1975. `abs` = frame absoluto da cutscene (as entradas dos recortes usam o tempo absoluto).
export type LabProps = {
  abs: number;
  steve: boolean;
  cold?: boolean;
  rewind?: boolean;
  silhouette?: "none" | "shadow" | "push";
  tilt?: number; // 0..1 o protótipo tombando
};

const D: React.FC<{ abs: number; at: number; dist?: number; children: React.ReactNode }> = ({ abs, at, dist = 260, children }) =>
  abs < f(at) ? null : <g transform={`translate(0 ${dropY(abs, f(at), dist)})`}>{children}</g>;

const sway = (abs: number, deg: number, period: number, phase = 0) => Math.sin((abs / period) * Math.PI * 2 + phase) * deg;

export const Hand: React.FC<{ x: number; y: number; flip?: boolean; rot?: number; sleeve?: string }> = ({ x, y, flip, rot = 0, sleeve = C.shirt }) => (
  <g transform={`translate(${x} ${y}) rotate(${rot}) scale(${flip ? -1 : 1} 1)`}>
    <rect x={8} y={-7} width={30} height={14} fill={sleeve} />
    <rect x={6} y={-6} width={4} height={12} fill={C.beigeHi} />
    <rect x={-6} y={-6} width={13} height={12} fill={C.skin} />
    <rect x={-16} y={-6} width={11} height={3} fill={C.skin} />
    <rect x={-17} y={-2} width={12} height={3} fill={C.skin} />
    <rect x={-15} y={2} width={10} height={3} fill={C.skin} />
    <rect x={-4} y={5} width={8} height={4} fill={C.skin} />
    <rect x={-6} y={3} width={13} height={3} fill={C.skinDark} />
  </g>
);

export const LabSet: React.FC<LabProps> = ({ abs, steve, cold, rewind, silhouette = "none", tilt = 0 }) => {
  const reel = (abs * 9) % 360;
  const clockA = rewind ? -abs * 24 : abs * 0.4;
  const flick = cold ? (rnd(Math.floor(abs / 2)) > 0.72 ? 0.25 : 1) : 1;
  const lightOn = interpolate(abs, [f(2.4), f(2.5)], [0, 1], clamp) * flick;
  const lensRot = interpolate(abs, [f(10.3), f(11.5)], [0, 50], { ...clamp, easing: Easing.inOut(Easing.sin) });
  const plug = interpolate(abs, [f(12.4), f(12.9)], [0, 6], clamp);

  return (
    <g>
      {/* parede */}
      <rect x={-200} y={-100} width={1040} height={560} fill={C.wall} />
      {Array.from({ length: 30 }, (_, i) => <rect key={i} x={-200 + i * 36} y={-100} width={2} height={330} fill={C.wallLine} />)}
      <rect x={-200} y={228} width={1040} height={6} fill={C.wallLine} />
      <rect x={-200} y={234} width={1040} height={30} fill={C.wall2} />

      {/* prateleira com livros e caixas */}
      <D abs={abs} at={2.3}>
        <rect x={30} y={96} width={160} height={6} fill={C.woodDark} />
        {[["#5b3b2a", 14, 34], ["#3d4a3a", 10, 40], ["#6e5a3a", 12, 30], ["#2f3a4a", 9, 38], ["#7a3a2a", 12, 36], ["#4a4030", 30, 22]].reduce<{ x: number; els: React.ReactNode[] }>(
          (acc, [col, w, h], i) => {
            acc.els.push(<rect key={i} x={acc.x} y={96 - (h as number)} width={w as number} height={h as number} fill={col as string} />);
            acc.x += (w as number) + 2;
            return acc;
          }, { x: 38, els: [] }).els}
        <rect x={140} y={70} width={40} height={26} fill={C.beigeDark} />
        <rect x={142} y={72} width={36} height={4} fill={C.beige} />
      </D>

      {/* quadro de ferramentas */}
      <D abs={abs} at={2.6}>
        <rect x={420} y={50} width={150} height={92} fill="#6a5638" />
        {Array.from({ length: 11 }, (_, i) => Array.from({ length: 6 }, (_, j) => <rect key={`${i}-${j}`} x={428 + i * 13} y={58 + j * 14} width={2} height={2} fill="#3d311f" />))}
        <rect x={440} y={64} width={6} height={50} fill={C.metalDark} />
        <rect x={436} y={60} width={14} height={8} fill={C.red} />
        <rect x={470} y={66} width={4} height={44} fill={C.metal} />
        <rect x={500} y={70} width={30} height={30} fill="none" stroke={C.metalDark} strokeWidth={4} />
        <rect x={545} y={62} width={14} height={60} fill={C.woodDark} />
      </D>

      {/* papel suspenso */}
      <D abs={abs} at={2.8}>
        <g transform={`rotate(${sway(abs, 3, 120).toFixed(2)} 470 142)`}>
          <rect x={469} y={142} width={2} height={10} fill={C.ink} />
          <rect x={456} y={150} width={28} height={36} fill={C.paper} />
          {[0, 1, 2, 3].map((i) => <rect key={i} x={460} y={156 + i * 7} width={20 - (i % 2) * 6} height={2} fill={C.paperDark} />)}
        </g>
      </D>

      {/* relógio (recua na sabotagem) */}
      <D abs={abs} at={2.9}>
        <rect x={292} y={28} width={32} height={32} fill={C.metalDark} />
        <rect x={295} y={31} width={26} height={26} fill={C.paper} />
        {[0, 3, 6, 9].map((h) => <rect key={h} x={307 + Math.round(Math.sin(h / 6 * Math.PI) * 10)} y={43 - Math.round(Math.cos(h / 6 * Math.PI) * 10)} width={2} height={2} fill={C.ink} />)}
        <g transform={`rotate(${clockA.toFixed(1)} 308 44)`}><rect x={307} y={34} width={2} height={11} fill={C.ink} /></g>
        <g transform={`rotate(${(clockA / 12 + 120).toFixed(1)} 308 44)`}><rect x={307} y={38} width={2} height={7} fill={C.red} /></g>
      </D>

      {/* cabo pendurado */}
      <D abs={abs} at={3.0}>
        <g transform={`rotate(${sway(abs, 2.5, 140, 1).toFixed(2)} 262 -10)`}>
          <path d="M262 -10 Q250 40 266 92" stroke={C.ink} strokeWidth={3} fill="none" />
          <rect x={261} y={90} width={10} height={14} fill={C.metalDark} />
        </g>
      </D>

      {/* Steve, de costas (rosto não aparece) */}
      {steve && (
        <D abs={abs} at={3.1}>
          <polygon points="92,250 182,250 190,170 168,150 106,150 84,170" fill={C.shirt} />
          <polygon points="92,250 120,250 116,160 106,150 84,170" fill={C.shirtDark} />
          <rect x={128} y={144} width={18} height={10} fill={C.skinDark} />
          <rect x={118} y={104} width={40} height={42} fill={C.skin} />
          <rect x={114} y={98} width={46} height={30} fill={C.hair} />
          <rect x={116} y={128} width={14} height={14} fill={C.hair} />
          <rect x={154} y={120} width={6} height={12} fill={C.skinDark} />
          <rect x={156} y={116} width={14} height={2} fill={C.ink} />
          <g transform={`rotate(${sway(abs, 3, 70).toFixed(2)} 178 162)`}>
            <polygon points="172,156 186,166 226,232 214,240" fill={C.shirt} />
            <rect x={210} y={232} width={18} height={10} fill={C.skin} />
            <rect x={226} y={234} width={8} height={3} fill={C.skin} />
          </g>
        </D>
      )}

      {/* bancada (firme depois de assentar) */}
      <D abs={abs} at={1.9} dist={300}>
        <rect x={-200} y={238} width={1040} height={12} fill={C.woodHi} />
        <rect x={-200} y={250} width={1040} height={4} fill={C.woodDark} />
        <rect x={-200} y={254} width={1040} height={200} fill={C.wood} />
        {[40, 200, 360, 520].map((x) => (
          <g key={x}>
            <rect x={x} y={272} width={110} height={44} fill={C.woodDark} />
            <rect x={x + 44} y={290} width={22} height={4} fill={C.metalHi} />
          </g>
        ))}
      </D>

      {/* luminária de mesa + luz âmbar */}
      <D abs={abs} at={2.1}>
        <rect x={196} y={234} width={30} height={5} fill={C.metalDark} />
        <polygon points="208,234 214,234 232,170 228,168" fill={C.metal} />
        <polygon points="226,172 230,168 260,150 258,146" fill={C.metal} />
        <polygon points="250,140 280,148 272,166 246,156" fill="#3f4a3f" />
      </D>
      {lightOn > 0 && (
        <g opacity={lightOn}>
          <polygon points="252,160 278,166 360,238 170,238" fill="url(#d50amber)" opacity={0.28} />
          <ellipse cx={265} cy={240} rx={95} ry={8} fill={C.amberHi} opacity={0.25} />
          {Array.from({ length: 14 }, (_, i) => (
            <rect key={i} x={200 + Math.round(rnd(i) * 130 + Math.sin(abs / 50 + i) * 6)} y={170 + Math.round(((rnd(i + 20) * 60 + abs * 0.15) % 66))} width={1} height={1} fill={C.amberHi} />
          ))}
        </g>
      )}

      {/* ferramentas e anotações */}
      <D abs={abs} at={3.7}>
        <g transform={`translate(0 ${Math.round(sway(abs, 1, 200))})`}>
          <polygon points="20,232 66,226 70,242 24,246" fill={C.paper} />
          {[0, 1, 2].map((i) => <rect key={i} x={28} y={232 + i * 4} width={30} height={1} fill="#5d6b8a" />)}
        </g>
      </D>
      <D abs={abs} at={3.9}><rect x={76} y={234} width={14} height={5} fill={C.red} /><rect x={90} y={235} width={20} height={2} fill={C.metalHi} /></D>
      <D abs={abs} at={4.1}><rect x={150} y={232} width={30} height={5} fill="#3d3a35" /><rect x={180} y={233} width={12} height={3} fill={C.metalHi} /></D>
      <D abs={abs} at={4.0}>
        <polygon points="410,236 432,230 434,234 412,240" fill="#2f4f7a" /><polygon points="410,238 432,242 434,238 412,234" fill="#2f4f7a" />
      </D>
      <D abs={abs} at={4.3}>
        {[0, 1, 2, 3].map((i) => <rect key={i} x={290 - 60 + i * 9} y={236} width={5} height={3} fill={i % 2 ? "#5c7fa6" : "#c9a36b"} />)}
      </D>

      {/* terminal CRT de 1975 */}
      <D abs={abs} at={4.3}>
        <polygon points="452,238 556,238 548,150 460,150" fill={C.beige} />
        <rect x={462} y={156} width={84} height={64} fill={C.beigeDark} />
        <rect x={466} y={160} width={76} height={56} fill="#0c120d" />
        <polygon points="446,248 562,248 556,238 452,238" fill={C.beigeHi} />
        {Array.from({ length: 12 }, (_, i) => <rect key={i} x={456 + i * 9} y={241} width={7} height={3} fill={C.beigeDark} />)}
      </D>

      {/* protótipo: tomba no golpe */}
      <g transform={`rotate(${(-tilt * 18).toFixed(1)} 300 238) translate(${Math.round(tilt * 14)} ${Math.round(-tilt * 6)})`}>
        {/* gravador de fita (visível desde o close inicial) */}
        <rect x={232} y={208} width={58} height={32} fill={C.beigeDark} />
        <rect x={234} y={210} width={54} height={28} fill="#9a9282" />
        <rect x={238} y={213} width={46} height={18} fill="#1c1a18" />
        <rect x={240} y={215} width={42} height={14} fill="#cfc3a2" />
        <rect x={244} y={216} width={34} height={3} fill={C.paper} />
        {[249, 273].map((cx) => (
          <g key={cx} transform={`rotate(${reel.toFixed(0)} ${cx} 224)`}>
            <rect x={cx - 4} y={220} width={8} height={8} fill="#3c3127" />
            <rect x={cx - 1} y={219} width={2} height={10} fill={C.white} />
            <rect x={cx - 5} y={223} width={10} height={2} fill={C.white} />
          </g>
        ))}
        {[0, 1, 2, 3, 4].map((i) => <rect key={i} x={236 + i * 10} y={233} width={8} height={4} fill={C.beigeHi} />)}

        <D abs={abs} at={5.3}>
          <path d="M290 230 Q298 238 305 228" stroke={C.red} strokeWidth={2} fill="none" />
          <path d="M290 226 Q300 236 305 224" stroke="#2c3f66" strokeWidth={2} fill="none" />
          <rect x={300 + plug} y={222} width={6} height={4} fill={C.metalDark} />
        </D>
        <D abs={abs} at={4.6}>
          <rect x={302} y={196} width={80} height={44} fill={C.metal} />
          <rect x={302} y={196} width={80} height={5} fill={C.metalHi} />
          <polygon points="320,196 326,186 358,186 364,196 358,196 354,190 330,190 326,196" fill={C.metalDark} />
          <rect x={306} y={204} width={36} height={30} fill="#1e2120" />
          {[0, 1, 2, 3].map((i) => <rect key={i} x={350} y={206 + i * 7} width={26} height={3} fill={C.metalDark} />)}
          {[[304, 198], [380, 198], [304, 238], [380, 238]].map(([x, y]) => <rect key={x * y} x={x - 1} y={y - 1} width={2} height={2} fill={C.metalDark} />)}
        </D>
        <D abs={abs} at={5.1}>
          <g transform={`rotate(${sway(abs, 0.8, 150).toFixed(2)} 323 217)`}>
            <rect x={308} y={206} width={32} height={26} fill={C.pcb} />
            {[0, 1, 2, 3, 4].map((i) => <rect key={i} x={309} y={208 + i * 5} width={30} height={1} fill={C.copper} />)}
            <rect x={312} y={214} width={10} height={8} fill={C.ink} />
            <rect x={326} y={210} width={10} height={6} fill={C.ink} />
            <rect x={328} y={222} width={8} height={6} fill="#26302a" />
            <rect x={314} y={226} width={3} height={3} fill={C.pcbHi} />
          </g>
        </D>
        <D abs={abs} at={5.9} dist={220}>
          <g transform={`rotate(${(lensRot + sway(abs, 1.5, 110)).toFixed(1)} 394 218)`}>
            <rect x={378} y={202} width={32} height={32} fill={C.metalDark} />
            <rect x={381} y={205} width={26} height={26} fill={C.ink} />
            <rect x={384} y={208} width={20} height={20} fill={C.metalHi} />
            <rect x={387} y={211} width={14} height={14} fill="#22303a" />
            <rect x={389} y={213} width={4} height={3} fill="#9fc4d6" />
            <rect x={393} y={202} width={2} height={4} fill={C.metalHi} />
          </g>
        </D>
      </g>

      {/* sabotador: silhueta sem rosto, contorno frio */}
      {silhouette === "shadow" && (
        <g opacity={interpolate(abs, [f(24.5), f(25.5)], [0, 1], clamp) * (rnd(Math.floor(abs / 2)) > 0.12 ? 1 : 0.4)}>
          <Figure x={60} />
        </g>
      )}
      {silhouette === "push" && (
        <g>
          <Figure x={196} />
          <g transform={`rotate(${interpolate(abs, [f(26.6), f(27.2)], [-30, 18], clamp).toFixed(1)} 246 150)`}>
            <polygon points="244,146 252,142 330,198 322,206" fill="#0b0d10" />
            <rect x={320} y={196} width={14} height={12} fill="#0b0d10" />
          </g>
        </g>
      )}

      {cold && <rect x={-200} y={-100} width={1040} height={560} fill="#14303c" opacity={0.42} />}
    </g>
  );
};

export const Figure: React.FC<{ x: number }> = ({ x }) => (
  <g transform={`translate(${x} 0)`}>
    <polygon points="6,252 86,252 84,150 70,132 22,132 8,150" fill="#4d6c78" />
    <rect x={30} y={88} width={34} height={46} fill="#4d6c78" />
    <polygon points="2,252 82,252 80,150 66,132 18,132 4,150" fill="#0b0d10" />
    <rect x={26} y={88} width={34} height={46} fill="#0b0d10" />
    <rect x={20} y={84} width={46} height={10} fill="#0b0d10" />
  </g>
);
