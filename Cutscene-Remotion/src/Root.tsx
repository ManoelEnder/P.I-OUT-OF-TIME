import { Composition } from "remotion";
import { Intro } from "./Intro";
import { Outro, OUTRO_S } from "./Outro";

// 640x360 = mesma resolução pixelada do jogo; a Unity amplia sem filtro.
export const RemotionRoot: React.FC = () => {
  return (
    <>
    <Composition
      id="IntroCutscene"
      component={Intro}
      durationInFrames={112 * 30}
      fps={30}
      width={640}
      height={360}
    />
    <Composition
      id="OutroCutscene"
      component={Outro}
      durationInFrames={OUTRO_S * 30}
      fps={30}
      width={640}
      height={360}
    />
    </>
  );
};
