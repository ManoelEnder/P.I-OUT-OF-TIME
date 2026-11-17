# Cutscene de abertura (Remotion)

Vídeo em pixel art, 640x360 a 30 fps, com 1:52 de duração. A Unity toca esse vídeo na cena `IntroCutsceneVideo` e o amplia sem filtro.

- `src/timeline.ts`: tempos das cenas, legendas, efeitos sonoros e volume da trilha.
- `src/scenes/`: Flashback (1975 e sabotagem), Recording (pesquisador) e Demo (visor do jogo, desenhado).
- **Prévia:** `npm run dev` abre o Remotion Studio.
- **Dublagem:** salve `public/voz/voz_01.mp3` … `voz_18.mp3` (ElevenLabs). As falas entram sozinhas nos tempos de `LINES`.
- **Gerar o vídeo e atualizar o jogo:**

```
npx remotion render IntroCutscene out/intro.mp4 --codec=h264 --crf=20
copy out\intro.mp4 "..\PROJETO TORRADEIRA\Assets\Cutscene\Video\intro_remotion.mp4"
```
