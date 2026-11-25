using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Jumpscare do Apagador, em fases (tempo real, nao depende do timeScale):
// 1) Encarada: a camera vira de supetao para o rosto dele; tudo prende a respiracao.
// 2) Bote: ele acelera ate o rosto do jogador, cabeca torta; estrondo grave + grito.
// 3) Ataque: tremor, distorcao de lente, aberracao cromatica, granulado, vinheta vermelha, glitch de
//    fita (barras rasgando, quadros travando), inimigo tremendo colado na camera.
// 4) Fim: fechamento em circulo pixelado sobre o rosto dele (borda vermelha), fita parando, preto.
// Com flashes desligados (fotossensibilidade) nao ha piscadas: so escurece.
public static class JumpscareSequence
{
    private const float StareTime = 0.16f;
    private const float LungeTime = 0.34f;
    private const float Close = 0.34f;     // olhos a esta distancia: rosto enche a tela sem atravessar
    private const float AttackTime = 0.95f;
    private const float CircleTime = 0.9f;
    private const float BlackHold = 0.35f;

    private static AudioClip scream, boom, stat, tapeStop;

    public static IEnumerator Play(MonoBehaviour host, Camera cam, TemporalEraserController enemy, float volume)
    {
        bool flashes = FlashSettings.Enabled;
        // O Cinemachine para de mexer na camera: o jumpscare controla ela direto.
        if (RunCameraRig.Instance != null) RunCameraRig.Instance.SetBrainEnabled(false);
        if (CameraFlashlight.Instance != null) CameraFlashlight.Instance.Set(false, false);

        // ---- Som ----
        AudioSource source = host.gameObject.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.priority = 0;
        source.ignoreListenerPause = true;
        if (scream == null) scream = BuildScream();
        if (boom == null) boom = BuildBoom();
        if (stat == null) stat = BuildStatic();
        if (tapeStop == null) tapeStop = BuildTapeStop();

        // ---- Tela (overlay + pos-processamento) ----
        // HUD (radar, avisos) some durante o susto.
        GameObject hud = GameObject.Find("HUDCanvas");
        Canvas hudCanvas = hud != null ? hud.GetComponent<Canvas>() : null;
        if (hudCanvas != null) hudCanvas.enabled = false;
        Overlay overlay = new Overlay();
        VolumeFx fx = new VolumeFx();

        Transform enemyT = enemy != null ? enemy.transform : null;
        float startFov = cam != null ? cam.fieldOfView : 60f;
        Quaternion camStart = cam != null ? cam.transform.rotation : Quaternion.identity;
        Vector3 camPos = cam != null ? cam.transform.position : Vector3.zero;
        float faceHeight = 1.6f;
        Vector3 headLocal = Vector3.zero;
        float startNear = cam != null ? cam.nearClipPlane : 0.3f;
        if (cam != null) cam.nearClipPlane = 0.03f;
        Vector3 forward = Vector3.forward;
        Vector3 enemyStart = Vector3.zero;
        Animator animator = null;

        if (cam != null && enemyT != null)
        {
            var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;
            enemy.enabled = false;
            animator = enemy.GetComponentInChildren<Animator>();
            if (animator != null) animator.SetFloat("Speed", 4.1f);

            Renderer[] rs = enemy.GetComponentsInChildren<Renderer>();
            Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(enemyT.position, Vector3.one * 2f);
            foreach (Renderer r in rs) b.Encapsulate(r.bounds);
            faceHeight = Mathf.Max(0.5f, b.max.y - enemyT.position.y - 0.28f);
            // Ponto da cabeca no espaco do inimigo (o pivo do modelo nao fica no meio do corpo).
            headLocal = enemyT.InverseTransformPoint(new Vector3(b.center.x, b.max.y - b.size.y * 0.07f, b.center.z));
            if (animator != null) animator.enabled = false;   // congela a pose antes de medir
            if (EyesWorld(enemyT, out Vector3 eyes))
                headLocal = enemyT.InverseTransformVector(eyes - enemyT.position);

            // De onde ele veio (posicao real) para a frente do jogador.
            Vector3 toEnemy = enemyT.position - camPos;
            toEnemy.y = 0f;
            forward = toEnemy.sqrMagnitude > 0.04f ? toEnemy.normalized : Flat(cam.transform.forward);
            enemyStart = camPos + forward * Mathf.Clamp(toEnemy.magnitude, 1.2f, 2.4f);
        }

        headOffset = headLocal;
        Debug.Log("Jumpscare: olhos em " + headLocal.ToString("F3") + " (local)");
        Vector3 FacePoint(float dist) => camPos + forward * dist + Vector3.up * 0.03f;
        Arms arms = enemyT != null ? new Arms(enemyT) : null;
        Vector3 camRight = Vector3.Cross(Vector3.up, forward).normalized;

        // 1) Encarada: camera vira para o rosto, zoom leve, vinheta fechando, silencio antes do grito.
        for (float t = 0f; t < StareTime; t += Time.unscaledDeltaTime)
        {
            float k = Smooth(t / StareTime);
            if (cam != null && enemyT != null)
            {
                Vector3 face = FacePoint(Vector3.Distance(Flat3(enemyStart), Flat3(camPos)));
                PlaceEnemy(enemyT, face, faceHeight, forward, 0f, Vector3.zero);
                cam.transform.rotation = Quaternion.Slerp(camStart, Quaternion.LookRotation(face - camPos), k);
                cam.fieldOfView = Mathf.Lerp(startFov, startFov * 0.92f, k);
            }
            fx.Set(vignette: Mathf.Lerp(0.2f, 0.55f, k), chroma: 0.15f * k, grain: 0.3f * k, distortion: 0f, exposure: -1.6f * k);
            yield return null;
        }

        source.PlayOneShot(boom, volume);
        source.PlayOneShot(scream, volume);
        source.PlayOneShot(stat, volume * 0.55f);

        // 2) Bote em pancadas: avanca acelerando, recua um pouco e bate de novo; braços abrindo em
        //    direcao a tela; a cada pancada o zoom "chuta" (sensacao de motion blur).
        float tiltDir = Random.value < 0.5f ? -1f : 1f;
        float startDist = cam != null && enemyT != null ? Vector3.Distance(Flat3(enemyStart), Flat3(camPos)) : 1.5f;
        if (animator != null) animator.enabled = false;
        for (float t = 0f; t < LungeTime; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.Clamp01(t / LungeTime);
            float dist, kick;
            if (k < 0.55f)
            {
                float a = k / 0.55f;
                dist = Mathf.Lerp(startDist, Close, a * a * a);
                kick = a > 0.85f ? 1f : 0f;
            }
            else if (k < 0.75f)
            {
                float a = (k - 0.55f) / 0.2f;
                dist = Mathf.Lerp(Close, Close + 0.13f, 1f - (1f - a) * (1f - a));
                kick = 0f;
            }
            else
            {
                float a = (k - 0.75f) / 0.25f;
                dist = Mathf.Lerp(Close + 0.13f, Close - 0.03f, a * a);
                kick = a > 0.7f ? 1f : 0f;
            }
            if (cam != null && enemyT != null)
            {
                Vector3 face = FacePoint(dist);
                float shakeHead = Mathf.Sin(t * 70f) * 9f * k;
                PlaceEnemy(enemyT, face, faceHeight, forward, tiltDir * 14f * k + shakeHead, new Vector3(0f, Mathf.Sin(t * 53f) * 6f * k, 0f));
                arms?.Reach(cam.transform.position, camRight, k);
                cam.transform.rotation = Quaternion.LookRotation(face - camPos) * Shake(0.4f + 2.4f * k);
                cam.fieldOfView = Mathf.Lerp(startFov * 0.92f, startFov * 0.74f, k) - kick * startFov * 0.08f;
            }
            fx.Set(vignette: 0.55f + 0.1f * k, chroma: 0.15f + 0.85f * k + kick * 0.5f, grain: 0.3f + 0.7f * k, distortion: -0.45f * k - kick * 0.2f, exposure: Mathf.Lerp(-1.6f, -0.4f, k));
            overlay.Glitch(0.15f + 0.4f * k, flashes);
            if (flashes && kick > 0f) overlay.Flash(new Color(0.75f, 0.04f, 0.04f, 0.14f));
            else overlay.Flash(Color.clear);
            yield return null;
        }

        // 3) Ataque: colado na camera, tremendo; a fita trava e pula quadros.
        Vector3 held = Vector3.zero;
        int frame = 0;
        for (float t = 0f; t < AttackTime; t += Time.unscaledDeltaTime, frame++)
        {
            float k = t / AttackTime;
            float decay = 1f - k * 0.6f;
            bool skip = (frame % 4) == 0 || Random.value < 0.15f;   // quadro travado
            if (!skip) held = Random.insideUnitSphere * 0.035f;
            if (cam != null && enemyT != null)
            {
                Vector3 face = FacePoint(Close + Mathf.Sin(t * 31f) * 0.015f);
                float side = Mathf.Sin(t * 64f) * 11f + Random.Range(-3f, 3f);       // cabeca chacoalhando
                PlaceEnemy(enemyT, face + held, faceHeight, forward, tiltDir * 12f + side, new Vector3(Random.Range(-4f, 4f), Mathf.Sin(t * 41f) * 7f, 0f));
                arms?.Reach(cam.transform.position, camRight, 1f);
                cam.transform.rotation = Quaternion.LookRotation(face - camPos) * Shake(2.2f * decay);
                cam.fieldOfView = startFov * 0.74f + Mathf.Sin(t * 23f) * 1.2f;

            }

            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 26f);
            fx.Set(vignette: 0.6f + 0.1f * pulse, chroma: 1f, grain: 1f, distortion: -0.45f - 0.15f * pulse, exposure: flashes && Random.value < 0.06f ? 0.5f : -0.2f);
            overlay.Glitch((0.85f * decay + 0.15f) * 0.6f, flashes);
            if (flashes)
            {
                float r = Random.value;
                if (r < 0.06f) overlay.Flash(new Color(0.8f, 0.03f, 0.03f, Random.Range(0.1f, 0.2f)));
                else if (r < 0.1f) overlay.Flash(new Color(0.95f, 0.95f, 0.95f, Random.Range(0.1f, 0.2f)));
                else overlay.Flash(Color.clear);
            }
            else
                overlay.Flash(new Color(0f, 0f, 0f, 0.15f + 0.1f * pulse));
            yield return null;
        }


        // 4) Circulo fechando sobre o rosto dele; fita parando.
        source.PlayOneShot(tapeStop, volume * 0.9f);
        Vector2 center = new Vector2(0.5f, 0.5f);
        if (cam != null && enemyT != null)
        {
            Vector3 vp = cam.WorldToViewportPoint(FacePoint(Close));
            if (vp.z > 0f) center = new Vector2(Mathf.Clamp01(vp.x), Mathf.Clamp01(vp.y));
        }
        for (float t = 0f; t < CircleTime; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.Clamp01(t / CircleTime);
            float radius = Mathf.Lerp(1.25f, 0f, k * k * k);                   // devagar no comeco, rapido no fim
            if (cam != null && enemyT != null)
                cam.transform.rotation = Quaternion.LookRotation(FacePoint(Close) - camPos) * Shake(1.2f * (1f - k));
            fx.Set(vignette: 0.7f, chroma: 1f - k * 0.5f, grain: 1f, distortion: -0.5f * (1f - k), exposure: -0.4f - k);
            overlay.Glitch(0.4f * (1f - k), flashes);
            overlay.Flash(Color.clear);
            overlay.Circle(center, radius);
            source.volume = Mathf.Lerp(1f, 0.35f, k);
            yield return null;
        }
        overlay.Circle(center, 0f);
        overlay.Glitch(0f, false);

        for (float t = 0f; t < BlackHold; t += Time.unscaledDeltaTime)
            yield return null;

        if (animator != null) animator.enabled = true;
        if (cam != null)
        {
            cam.fieldOfView = startFov;
            cam.transform.rotation = camStart;
            cam.nearClipPlane = startNear;
        }
        fx.Dispose();
        HostedOverlay = overlay;
    }

    // Depois que a tela de derrota aparece, o preto some devagar por cima dela.
    public static Overlay HostedOverlay;

    public static IEnumerator FadeOutOverlay()
    {
        Overlay o = HostedOverlay;
        HostedOverlay = null;
        if (o == null) yield break;
        for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
        {
            o.SetBlackAlpha(1f - t / 0.5f);
            yield return null;
        }
        o.Dispose();
    }

    // ---------- Apoio ----------

    private static Vector3 headOffset;

    // Centro dos olhos (submalha com material "Eyes") na pose atual, em coordenadas do mundo.
    private static bool EyesWorld(Transform root, out Vector3 world)
    {
        world = Vector3.zero;
        foreach (SkinnedMeshRenderer smr in root.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            Material[] mats = smr.sharedMaterials;
            int sub = -1;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != null && mats[i].name.Contains("Eyes")) { sub = i; break; }
            if (sub < 0 || smr.sharedMesh == null || sub >= smr.sharedMesh.subMeshCount) continue;
            var baked = new Mesh();
            smr.BakeMesh(baked, true);
            Vector3[] v = baked.vertices;
            int[] idx = baked.GetIndices(sub);
            if (idx.Length == 0) { Object.Destroy(baked); continue; }
            Vector3 sum = Vector3.zero;
            foreach (int i in idx) sum += v[i];
            Vector3 local = sum / idx.Length;
            world = smr.transform.position + smr.transform.rotation * local;
            Object.Destroy(baked);
            return true;
        }
        return false;
    }

    private static void PlaceEnemy(Transform enemy, Vector3 face, float faceHeight, Vector3 forward, float tilt, Vector3 jitterEuler)
    {
        enemy.rotation = Quaternion.LookRotation(-forward, Vector3.up) * Quaternion.Euler(jitterEuler.x, jitterEuler.y, tilt);
        // A cabeca (e nao o pivo) fica exatamente no ponto do rosto.
        enemy.position = face - enemy.TransformVector(headOffset);
    }

    private static Quaternion Shake(float deg) => Quaternion.Euler(Random.Range(-1f, 1f) * deg, Random.Range(-1f, 1f) * deg, Random.Range(-1f, 1f) * deg * 0.6f);
    private static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
    private static Vector3 Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward; }
    private static Vector3 Flat3(Vector3 v) => new Vector3(v.x, 0f, v.z);

    private class Arms
    {
        private readonly Transform[] upper = new Transform[2], fore = new Transform[2], hand = new Transform[2];

        public Arms(Transform root)
        {
            string[] sides = { "L", "R" };
            for (int i = 0; i < 2; i++)
            {
                upper[i] = Find(root, "upperarm." + sides[i]);
                fore[i] = Find(root, "forearm." + sides[i]);
                hand[i] = Find(root, "hand." + sides[i]);
            }
        }

        private static Transform Find(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t)
            {
                Transform f = Find(c, name);
                if (f != null) return f;
            }
            return null;
        }

        public void Reach(Vector3 cam, Vector3 camRight, float amount)
        {
            for (int i = 0; i < 2; i++)
            {
                if (upper[i] == null || fore[i] == null) continue;
                // Lado do braco em relacao a camera (abre para o canto do mesmo lado).
                float outSign = Mathf.Sign(Vector3.Dot(upper[i].position - (upper[0].position + upper[1 - i].position) * 0.5f, camRight));
                if (outSign == 0f) outSign = i == 0 ? 1f : -1f;
                Vector3 corner = cam + camRight * outSign * 0.38f + Vector3.up * 0.12f;
                Aim(upper[i], fore[i].position, Vector3.Lerp(fore[i].position, corner, 0.65f), amount);
                if (hand[i] != null)
                {
                    Aim(fore[i], hand[i].position, corner + camRight * outSign * 0.1f, amount);
                }
            }
        }

        private static void Aim(Transform bone, Vector3 childPos, Vector3 target, float amount)
        {
            Vector3 cur = childPos - bone.position;
            Vector3 want = target - bone.position;
            if (cur.sqrMagnitude < 1e-6f || want.sqrMagnitude < 1e-6f) return;
            Quaternion full = Quaternion.FromToRotation(cur, want);
            bone.rotation = Quaternion.Slerp(Quaternion.identity, full, Mathf.Clamp01(amount)) * bone.rotation;
        }
    }

    // Volume global temporario com os efeitos de lente.
    private class VolumeFx
    {
        private readonly GameObject go;
        private readonly VolumeProfile profile;
        private readonly Vignette vignette;
        private readonly ChromaticAberration chroma;
        private readonly FilmGrain grain;
        private readonly LensDistortion distortion;
        private readonly ColorAdjustments color;

        public VolumeFx()
        {
            go = new GameObject("JumpscareVolume");
            Volume v = go.AddComponent<Volume>();
            v.isGlobal = true;
            v.priority = 999f;
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            v.sharedProfile = profile;
            vignette = profile.Add<Vignette>(true);
            vignette.color.Override(new Color(0.35f, 0f, 0f));
            vignette.smoothness.Override(0.6f);
            chroma = profile.Add<ChromaticAberration>(true);
            grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Thin2);
            distortion = profile.Add<LensDistortion>(true);
            color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(-30f);
            color.contrast.Override(25f);
        }

        public void Set(float vignette, float chroma, float grain, float distortion, float exposure)
        {
            this.vignette.intensity.Override(vignette);
            this.chroma.intensity.Override(chroma);
            this.grain.intensity.Override(grain);
            this.distortion.intensity.Override(distortion);
            this.color.postExposure.Override(exposure);
        }

        public void Dispose()
        {
            Object.Destroy(go);
            Object.Destroy(profile);
        }
    }

    // Overlay de tela: flash, glitch de fita e o circulo pixelado.
    public class Overlay
    {
        private const int GW = 160, GH = 90;   // glitch em baixa resolucao (pixelado)
        private const int CW = 320, CH = 180;  // circulo pixelado
        private readonly GameObject canvasGo;
        private readonly Image flash;
        private readonly RawImage glitch, circle;
        private readonly Texture2D glitchTex, circleTex;
        private readonly Color32[] gp, cp;

        public Overlay()
        {
            canvasGo = new GameObject("JumpscareOverlay", typeof(Canvas));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            flash = Full<Image>("Flash");
            flash.color = Color.clear;

            glitchTex = new Texture2D(GW, GH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            gp = new Color32[GW * GH];
            glitch = Full<RawImage>("Glitch");
            glitch.texture = glitchTex;
            glitch.enabled = false;   // textura nova vem cinza: so aparece depois de desenhada

            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
            int cw = Mathf.RoundToInt(CH * aspect);
            circleTex = new Texture2D(Mathf.Max(32, cw), CH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            cp = new Color32[circleTex.width * CH];
            circle = Full<RawImage>("Circle");
            circle.texture = circleTex;
            circle.enabled = false;
        }

        private T Full<T>(string name) where T : Graphic
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(T));
            go.transform.SetParent(canvasGo.transform, false);
            RectTransform r = go.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            T g = go.GetComponent<T>();
            g.raycastTarget = false;
            return g;
        }

        public void Flash(Color c) => flash.color = c;

        // Barras horizontais rasgando, faixas deslocadas e chiado (estilo fita VHS).
        public void Glitch(float amount, bool flashes)
        {
            if (amount <= 0.01f)
            {
                glitch.enabled = false;
                return;
            }
            glitch.enabled = true;
            for (int i = 0; i < gp.Length; i++) gp[i] = new Color32(0, 0, 0, 0);
            int bars = Mathf.RoundToInt(2 + amount * 9);
            for (int b = 0; b < bars; b++)
            {
                int y = Random.Range(0, GH);
                int h = Random.Range(1, 3 + Mathf.RoundToInt(amount * 4));
                int x0 = Random.Range(-GW / 2, GW);
                int len = Random.Range(GW / 6, GW);
                bool red = Random.value < 0.35f;
                byte a = (byte)(Random.Range(70, 170) * Mathf.Clamp01(amount + 0.2f) * (flashes ? 1f : 0.6f));
                for (int yy = y; yy < Mathf.Min(GH, y + h); yy++)
                    for (int x = Mathf.Max(0, x0); x < Mathf.Min(GW, x0 + len); x++)
                    {
                        byte v = (byte)Random.Range(30, 230);
                        gp[yy * GW + x] = red ? new Color32(v, 0, 0, a) : new Color32(v, v, v, a);
                    }
            }
            // Chiado esparso.
            int dots = Mathf.RoundToInt(amount * 260);
            for (int d = 0; d < dots; d++)
            {
                byte v = (byte)Random.Range(120, 255);
                gp[Random.Range(0, gp.Length)] = new Color32(v, v, v, (byte)(90 * amount));
            }
            glitchTex.SetPixels32(gp);
            glitchTex.Apply(false);
            // A faixa toda pula de vez em quando (quadro rolando).
            glitch.uvRect = new Rect(0f, Random.value < 0.2f ? Random.Range(-0.08f, 0.08f) : 0f, 1f, 1f);
        }

        // Preto fora do circulo (centro em viewport 0..1, raio relativo a altura), com borda suave escura.
        public void Circle(Vector2 center, float radius)
        {
            circle.enabled = true;
            int w = circleTex.width;
            float cx = center.x * w, cy = center.y * CH;
            float r = radius * CH;
            for (int y = 0; y < CH; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                    Color32 c;
                    if (d > r) c = new Color32(0, 0, 0, 255);
                    else if (d > r - 2f) c = new Color32(0, 0, 0, 120);      // sombra interna
                    else c = new Color32(0, 0, 0, 0);
                    cp[y * w + x] = c;
                }
            circleTex.SetPixels32(cp);
            circleTex.Apply(false);
        }

        public void SetBlackAlpha(float a)
        {
            circle.color = new Color(1f, 1f, 1f, a);
            glitch.enabled = false;
            flash.color = Color.clear;
        }

        public void Dispose()
        {
            Object.Destroy(canvasGo);
            Object.Destroy(glitchTex);
            Object.Destroy(circleTex);
        }
    }

    // ---------- Sons sintetizados ----------

    // Grito: serra grave descendo + chiado agudo + ruido, saturado.
    private static AudioClip BuildScream()
    {
        const int rate = 44100;
        const float seconds = 1.7f;
        int n = (int)(rate * seconds);
        float[] data = new float[n];
        var rng = new System.Random(66);
        float pa = 0f, pb = 0f, pc = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            float env = Mathf.Min(1f, t / 0.01f) * Mathf.Clamp01((seconds - t) / 0.5f);
            float wobble = Mathf.Sin(t * 2f * Mathf.PI * 19f) * 40f;
            pa += (Mathf.Lerp(460f, 150f, t / seconds) + wobble) / rate;
            pb += (Mathf.Lerp(2100f, 1250f, t / seconds) + wobble * 5f) / rate;
            pc += 58f / rate;
            float saw = (pa % 1f) * 2f - 1f;
            float screech = Mathf.Sin(pb * 2f * Mathf.PI);
            float rumble = Mathf.Sin(pc * 2f * Mathf.PI);
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            data[i] = (float)System.Math.Tanh((saw * 0.6f + screech * 0.5f + noise * 0.45f + rumble * 0.5f) * 2.4f) * env * 0.95f;
        }
        return Clip("JumpscareScream", data, rate);
    }

    // Estrondo grave (impacto) no inicio do bote.
    private static AudioClip BuildBoom()
    {
        const int rate = 44100;
        int n = (int)(rate * 1.2f);
        float[] data = new float[n];
        float ph = 0f;
        var rng = new System.Random(7);
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            ph += Mathf.Lerp(90f, 32f, Mathf.Clamp01(t / 0.6f)) / rate;
            float env = Mathf.Exp(-t * 3.2f);
            float click = t < 0.02f ? (float)(rng.NextDouble() * 2.0 - 1.0) * (1f - t / 0.02f) : 0f;
            data[i] = (float)System.Math.Tanh(Mathf.Sin(ph * 2f * Mathf.PI) * 2.5f) * env * 0.9f + click * 0.6f;
        }
        return Clip("JumpscareBoom", data, rate);
    }

    // Estatica de fita (ruido filtrado com picos).
    private static AudioClip BuildStatic()
    {
        const int rate = 44100;
        int n = (int)(rate * 1.4f);
        float[] data = new float[n];
        var rng = new System.Random(13);
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += (noise - lp) * 0.35f;
            float burst = (Mathf.Sin(t * 37f) > 0.6f) ? 1f : 0.55f;
            data[i] = lp * burst * Mathf.Clamp01((1.4f - t) / 0.4f) * 0.8f;
        }
        return Clip("JumpscareStatic", data, rate);
    }

    // Fita parando: tom descendo ate parar, com ruido.
    private static AudioClip BuildTapeStop()
    {
        const int rate = 44100;
        int n = (int)(rate * 0.9f);
        float[] data = new float[n];
        float ph = 0f;
        var rng = new System.Random(21);
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            float k = t / 0.9f;
            ph += Mathf.Lerp(220f, 8f, k * k) / rate;
            float tone = ((ph % 1f) * 2f - 1f) * 0.5f + Mathf.Sin(ph * 2f * Mathf.PI * 2f) * 0.3f;
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.15f;
            data[i] = (tone + noise) * (1f - k) * 0.7f;
        }
        return Clip("JumpscareTapeStop", data, rate);
    }

    private static AudioClip Clip(string name, float[] data, int rate)
    {
        AudioClip c = AudioClip.Create(name, data.Length, 1, rate, false);
        c.SetData(data, 0);
        return c;
    }
}
