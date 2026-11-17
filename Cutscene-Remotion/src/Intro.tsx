import React from "react";
import { AbsoluteFill, getStaticFiles, Img, interpolate, Sequence, staticFile, useCurrentFrame, useVideoConfig } from "remotion";
import { Audio } from "@remotion/media";
import { loadFont } from "@remotion/fonts";
import { AMBIENCE_KEYS, CUES, DURATION_S, f, LINES, MUSIC_KEYS, SCENES } from "./timeline";
import { C, clamp, W, H } from "./px";
import { AlteredScene, LabScene, SabotageScene } from "./scenes/Flashback";
import { EndingScene, RecordScene } from "./scenes/Recording";
import { FindScene, MetersScene, RuptureScene } from "./scenes/Demo";

loadFont({ family: "VCR", url: staticFile("VCR_OSD_MONO.ttf") });

const curve = (keys: [number, number][], fps: number) => (frame: number) =>
  interpolate(frame / fps, keys.map((k) => k[0]), keys.map((k) => k[1]), clamp);

const Subtitles: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const line = LINES.find((l) => frame >= l.at * fps && frame < (l.at + l.dur + 0.3) * fps);
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

const Vignette: React.FC = () => (
  <div style={{ position: "absolute", inset: 0, background: "radial-gradient(ellipse at center, transparent 55%, rgba(0,0,0,0.55) 100%)" }} />
);

const FinalFade: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const o = interpolate(frame, [(DURATION_S - 0.4) * fps, DURATION_S * fps], [0, 1], clamp);
  return o > 0 ? <AbsoluteFill style={{ background: "#000", opacity: o }} /> : null;
};

export const Intro: React.FC = () => {
  const { fps } = useVideoConfig();
  const files = new Set(getStaticFiles().map((s) => s.name));
  const scene = (name: keyof typeof SCENES, el: React.ReactNode) => (
    <Sequence name={name} from={SCENES[name].from * fps} durationInFrames={(SCENES[name].to - SCENES[name].from) * fps}>{el}</Sequence>
  );
  return (
    <AbsoluteFill style={{ background: "#000", overflow: "hidden" }}>
      {scene("lab", <LabScene />)}
      {scene("sabotage", <SabotageScene />)}
      {scene("altered", <AlteredScene />)}
      {scene("record", <RecordScene />)}
      {scene("find", <FindScene />)}
      {scene("meters", <MetersScene />)}
      {scene("rupture", <RuptureScene />)}
      {scene("ending", <EndingScene />)}

      <Grain />
      <Vignette />
      <Subtitles />
      <FinalFade />

      <Audio src={staticFile("music/darkness.mp3")} loop volume={curve(MUSIC_KEYS, fps)} />
      <Audio src={staticFile("sfx/amb_lab.wav")} loop volume={curve(AMBIENCE_KEYS, fps)} />
      {CUES.map((c, i) => (
        <Sequence key={i} name={`sfx ${c.sfx}`} from={f(c.at)} layout="none">
          <Audio src={staticFile(`sfx/${c.sfx}.wav`)} volume={c.vol} />
        </Sequence>
      ))}
      {LINES.map((l, i) => {
        const name = `voz/voz_${String(i + 1).padStart(2, "0")}.mp3`;
        return files.has(name) ? (
          <Sequence key={name} name={`fala ${i + 1}`} from={f(l.at)} layout="none">
            <Audio src={staticFile(name)} />
          </Sequence>
        ) : null;
      })}
    </AbsoluteFill>
  );
};
