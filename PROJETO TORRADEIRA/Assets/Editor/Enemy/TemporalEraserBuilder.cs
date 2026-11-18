using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

// Monta o inimigo TemporalEraser na cena Principal: modelo animado no lugar da capsula,
// som e glitch, trigger de ataque, ponto de respawn e a tela de derrota.
// Pode ser executado de novo: reaproveita o que ja existe.
public static class TemporalEraserBuilder
{
    // Modelo feito no Blender pelo script Tools/Blender/temporal_eraser.py (rig Generic com Idle, Walk e Run).
    private const string ModelPath = "Assets/Prefabs/Enemy/TemporalEraser.fbx";
    private const string MaterialFolder = "Assets/Art/Materials/Enemy";

    // Velocidades (m/s) em que cada ciclo foi animado: pes sem deslizar nessas velocidades.
    private const float WalkClipSpeed = 2.1f;
    private const float RunClipSpeed = 4.1f;
    private const string ControllerFolder = "Assets/Animation/Enemy";
    private const string ControllerPath = ControllerFolder + "/TemporalEraser.controller";
    private const string PixelFontPath = "Assets/Fonts/VCR_OSD_MONO SDF.asset";

    private const float ModelHeight = 1.95f;

    private static readonly Color Red = new Color(0.85f, 0.12f, 0.12f, 1f);
    private static readonly Color Phosphor = Color.white;
    private static readonly Color TextIdle = new Color(0.72f, 0.72f, 0.72f, 1f);

    [MenuItem("Ferramentas/Inimigo/Montar TemporalEraser")]
    public static void Build()
    {
        TemporalEraserController enemy = Resources.FindObjectsOfTypeAll<TemporalEraserController>()
            .FirstOrDefault(c => c.gameObject.scene.IsValid());

        if (enemy == null)
        {
            Debug.LogError("TemporalEraserBuilder: abra a cena Principal (TemporalEraser nao encontrado).");
            return;
        }

        Transform player = (Transform)new SerializedObject(enemy).FindProperty("player").objectReferenceValue;
        if (player == null)
        {
            Debug.LogError("TemporalEraserBuilder: TemporalEraserController sem referencia do Player.");
            return;
        }

        ConfigureImports();
        AnimationClip idle = LoadClip("Idle");
        AnimationClip walk = LoadClip("Walk");
        AnimationClip run = LoadClip("Run");

        if (walk == null || run == null || idle == null)
        {
            Debug.LogError("TemporalEraserBuilder: clipes Idle/Walk/Run nao encontrados em " + ModelPath);
            return;
        }

        AnimatorController controller = BuildController(idle, walk, run);

        Transform parent = enemy.transform.parent;
        Transform spawn = BuildSpawnPoint(parent, player);
        DefeatController defeat = BuildDefeat(parent, player.gameObject, spawn);

        BuildEnemy(enemy, controller, defeat);

        EditorSceneManager.MarkSceneDirty(enemy.gameObject.scene);
        Debug.Log("TemporalEraserBuilder: inimigo montado e ativado.");
    }

    [MenuItem("Ferramentas/Inimigo/Focar TemporalEraser na cena")]
    public static void Focus()
    {
        TemporalEraserController enemy = Object.FindFirstObjectByType<TemporalEraserController>(FindObjectsInactive.Include);
        if (enemy == null)
            return;

        Selection.activeGameObject = enemy.gameObject;
        SceneView view = SceneView.lastActiveSceneView;
        if (view == null)
            return;

        // Vista de frente, um pouco de lado, na altura do peito.
        Transform t = enemy.transform;
        view.LookAt(t.position + Vector3.up * 1.1f, Quaternion.LookRotation(-(t.forward + t.right * 0.35f).normalized), 2.6f);
    }

    // ---------- Importacao ----------

    private static readonly (string name, Color color, bool doubleSided, bool glow)[] Materials =
    {
        ("Eraser_Coat", new Color(0.24f, 0.23f, 0.19f), true, false),
        ("Eraser_CoatDark", new Color(0.13f, 0.125f, 0.1f), true, false),
        ("Eraser_Pants", new Color(0.1f, 0.085f, 0.07f), false, false),
        ("Eraser_Boots", new Color(0.2f, 0.125f, 0.07f), false, false),
        ("Eraser_Sole", new Color(0.06f, 0.05f, 0.045f), false, false),
        ("Eraser_Skin", new Color(0.6f, 0.56f, 0.5f), false, false),
        ("Eraser_Hair", new Color(0.06f, 0.05f, 0.045f), false, false),
        ("Eraser_Belt", new Color(0.15f, 0.11f, 0.08f), false, false),
        ("Eraser_Badge", new Color(0.66f, 0.58f, 0.44f), false, false),
        ("Eraser_FaceVoid", new Color(0f, 0f, 0f), false, false),
        ("Eraser_Eyes", new Color(0.9f, 0.95f, 1f) * 5f, false, true),
    };

    private static void ConfigureImports()
    {
        var model = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        bool dirty = false;

        if (model.animationType != ModelImporterAnimationType.Generic)
        {
            model.animationType = ModelImporterAnimationType.Generic;
            model.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            dirty = true;
        }

        if (model.importCameras || model.importLights)
        {
            model.importCameras = false;
            model.importLights = false;
            dirty = true;
        }

        // Todos os ciclos em loop.
        ModelImporterClipAnimation[] clips = model.clipAnimations.Length > 0 ? model.clipAnimations : model.defaultClipAnimations;
        foreach (ModelImporterClipAnimation clip in clips)
        {
            string clean = CleanClipName(clip.name);
            if (clip.name != clean || !clip.loopTime)
            {
                clip.name = clean;
                clip.loopTime = true;
                clip.loopPose = true;
                dirty = true;
            }
        }
        model.clipAnimations = clips;

        // Materiais do jogo no lugar dos que vieram do Blender.
        foreach (var entry in Materials)
        {
            Material material = EnemyMaterial(entry.name, entry.color, entry.doubleSided, entry.glow);
            var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), entry.name);
            if (!model.GetExternalObjectMap().TryGetValue(id, out Object current) || current != material)
            {
                model.AddRemap(id, material);
                dirty = true;
            }
        }

        if (dirty)
            model.SaveAndReimport();
    }

    // O Blender exporta "TemporalEraser|Walk"; fica so "Walk".
    private static string CleanClipName(string name)
    {
        int bar = name.LastIndexOf('|');
        return bar >= 0 ? name.Substring(bar + 1) : name;
    }

    private static Material EnemyMaterial(string name, Color color, bool doubleSided, bool glow)
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets/Art/Materials", "Enemy");

        string path = MaterialFolder + "/" + name + ".mat";
        string shaderName = glow ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(Shader.Find(shaderName));
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader.name != shaderName)
        {
            material.shader = Shader.Find(shaderName);
        }

        material.SetColor("_BaseColor", color);

        if (!glow)
        {
            material.SetFloat("_Smoothness", 0.12f);
            material.SetFloat("_Metallic", 0f);
        }

        // Barra do jaleco nao tem espessura: desenha os dois lados.
        material.SetFloat("_Cull", doubleSided ? 0f : 2f);
        material.doubleSidedGI = doubleSided;

        EditorUtility.SetDirty(material);
        return material;
    }

    private static AnimationClip LoadClip(string name)
    {
        return AssetDatabase.LoadAllAssetsAtPath(ModelPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => c.name == name);
    }

    // Idle -> Walk -> Run misturados pela velocidade do NavMeshAgent (parametro Speed).
    private static AnimatorController BuildController(AnimationClip idle, AnimationClip walk, AnimationClip run)
    {
        if (!AssetDatabase.IsValidFolder(ControllerFolder))
            AssetDatabase.CreateFolder("Assets/Animation", "Enemy");

        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState state = controller.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
        tree.blendType = BlendTreeType.Simple1D;
        tree.blendParameter = "Speed";
        tree.useAutomaticThresholds = false;
        tree.AddChild(idle, 0f);
        tree.AddChild(walk, WalkClipSpeed);
        tree.AddChild(run, RunClipSpeed);
        machine.defaultState = state;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    // ---------- Inimigo ----------

    private static void BuildEnemy(TemporalEraserController enemy, AnimatorController controller, DefeatController defeat)
    {
        GameObject root = enemy.gameObject;
        Undo.RecordObject(root.transform, "Inimigo");

        // A capsula antiga tinha escala (1, 1.28, 1) e o pivo no centro; agora o pivo fica no pe.
        if (root.transform.localScale != Vector3.one)
        {
            Vector3 feet = root.transform.position - new Vector3(0f, root.transform.lossyScale.y, 0f);
            root.transform.localScale = Vector3.one;
            root.transform.position = NavMesh.SamplePosition(feet, out NavMeshHit hit, 3f, NavMesh.AllAreas) ? hit.position : feet;
        }

        // Camada Default: a camera do jogador (CameraPlayer) so desenha as camadas 0-5; na camada
        // "Enemy" o inimigo so apareceria pelo visor.
        root.layer = 0;

        MeshRenderer capsuleRenderer = root.GetComponent<MeshRenderer>();
        if (capsuleRenderer != null)
            Undo.DestroyObjectImmediate(capsuleRenderer);

        MeshFilter capsuleFilter = root.GetComponent<MeshFilter>();
        if (capsuleFilter != null)
            Undo.DestroyObjectImmediate(capsuleFilter);

        CapsuleCollider body = root.GetComponent<CapsuleCollider>();
        if (body == null)
            body = Undo.AddComponent<CapsuleCollider>(root);
        Undo.RecordObject(body, "Inimigo");
        body.isTrigger = false;
        body.radius = 0.4f;
        body.height = ModelHeight;
        body.center = new Vector3(0f, ModelHeight * 0.5f, 0f);

        NavMeshAgent agent = root.GetComponent<NavMeshAgent>();
        Undo.RecordObject(agent, "Inimigo");
        agent.radius = 0.4f;
        agent.height = ModelHeight;
        agent.baseOffset = 0f;
        agent.angularSpeed = 360f;
        agent.acceleration = 10f;

        // Modelo.
        Transform old = root.transform.Find("Model");
        if (old != null)
            Undo.DestroyObjectImmediate(old.gameObject);

        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
        Undo.RegisterCreatedObjectUndo(model, "Inimigo");
        model.name = "Model";
        model.transform.SetParent(root.transform, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);
        model.transform.localScale = Vector3.one * (ModelHeight / Mathf.Max(0.1f, bounds.size.y));

        foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = root.layer;

        foreach (Renderer r in renderers)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            if (r is SkinnedMeshRenderer skinned)
                skinned.updateWhenOffscreen = false;
        }

        Animator animator = model.GetComponent<Animator>();
        if (animator == null)
            animator = Undo.AddComponent<Animator>(model);
        animator.runtimeAnimatorController = controller;
        animator.avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
        animator.speed = 1f;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

        // Olhos na altura da cabeca.
        Transform eye = root.transform.Find("EyePoint");
        if (eye != null)
        {
            Undo.RecordObject(eye, "Inimigo");
            eye.localPosition = new Vector3(0f, ModelHeight - 0.2f, 0.15f);
            eye.localRotation = Quaternion.identity;
            eye.localScale = Vector3.one;
        }

        BuildAttack(root.transform, defeat);
        BuildPresence(root, animator, model.transform, renderers);

        // Tentar de novo: inimigo volta ao inicio.
        SerializedObject defeatSo = new SerializedObject(defeat);
        SerializedProperty calls = defeatSo.FindProperty("onRetry.m_PersistentCalls.m_Calls");
        calls.ClearArray();
        defeatSo.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddVoidPersistentListener(GetRetryEvent(defeat), enemy.ResetToStart);

        Undo.RecordObject(root, "Inimigo");
        root.SetActive(true);
    }

    private static UnityEvent GetRetryEvent(DefeatController defeat)
    {
        return (UnityEvent)typeof(DefeatController)
            .GetField("onRetry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(defeat);
    }

    private static void BuildAttack(Transform root, DefeatController defeat)
    {
        Transform trigger = root.Find("AttackTrigger");
        if (trigger == null)
        {
            trigger = new GameObject("AttackTrigger").transform;
            Undo.RegisterCreatedObjectUndo(trigger.gameObject, "Inimigo");
            trigger.SetParent(root, false);
        }

        Undo.RecordObject(trigger, "Inimigo");
        trigger.localPosition = Vector3.zero;
        trigger.localRotation = Quaternion.identity;
        trigger.localScale = Vector3.one;
        trigger.gameObject.layer = root.gameObject.layer;

        foreach (Collider c in trigger.GetComponents<Collider>())
        {
            if (!(c is SphereCollider))
                Undo.DestroyObjectImmediate(c);
        }

        SphereCollider sphere = trigger.GetComponent<SphereCollider>();
        if (sphere == null)
            sphere = Undo.AddComponent<SphereCollider>(trigger.gameObject);
        sphere.isTrigger = true;
        sphere.radius = 0.75f;
        sphere.center = new Vector3(0f, 1.1f, 0.35f);

        // O Player usa CharacterController: o trigger precisa de um Rigidbody cinematico
        // para disparar mesmo com o jogador parado.
        Rigidbody body = trigger.GetComponent<Rigidbody>();
        if (body == null)
            body = Undo.AddComponent<Rigidbody>(trigger.gameObject);
        body.isKinematic = true;
        body.useGravity = false;

        TemporalEraserAttack attack = trigger.GetComponent<TemporalEraserAttack>();
        if (attack == null)
            attack = Undo.AddComponent<TemporalEraserAttack>(trigger.gameObject);

        SerializedObject so = new SerializedObject(attack);
        so.FindProperty("defeatController").objectReferenceValue = defeat;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildPresence(GameObject root, Animator animator, Transform model, Renderer[] renderers)
    {
        AudioSource drone = root.GetComponent<AudioSource>();
        if (drone == null)
            drone = Undo.AddComponent<AudioSource>(root);
        ConfigureSpatial(drone, 2f, 18f);

        Transform stepsGo = root.transform.Find("Steps");
        if (stepsGo == null)
        {
            stepsGo = new GameObject("Steps").transform;
            Undo.RegisterCreatedObjectUndo(stepsGo.gameObject, "Inimigo");
            stepsGo.SetParent(root.transform, false);
        }

        AudioSource steps = stepsGo.GetComponent<AudioSource>();
        if (steps == null)
            steps = Undo.AddComponent<AudioSource>(stepsGo.gameObject);
        ConfigureSpatial(steps, 1.5f, 14f);

        TemporalEraserPresence presence = root.GetComponent<TemporalEraserPresence>();
        if (presence == null)
            presence = Undo.AddComponent<TemporalEraserPresence>(root);

        SerializedObject so = new SerializedObject(presence);
        so.FindProperty("animator").objectReferenceValue = animator;
        so.FindProperty("clipWalkSpeed").floatValue = WalkClipSpeed;
        so.FindProperty("droneSource").objectReferenceValue = drone;
        so.FindProperty("stepSource").objectReferenceValue = steps;
        so.FindProperty("model").objectReferenceValue = model;
        SerializedProperty list = so.FindProperty("renderers");
        list.arraySize = renderers.Length;
        for (int i = 0; i < renderers.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureSpatial(AudioSource source, float min, float max)
    {
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = min;
        source.maxDistance = max;
        source.dopplerLevel = 0f;
    }

    // ---------- Respawn e derrota ----------

    private static Transform BuildSpawnPoint(Transform parent, Transform player)
    {
        Transform spawn = parent != null ? parent.Find("PlayerSpawn") : GameObject.Find("PlayerSpawn")?.transform;

        if (spawn == null)
        {
            spawn = new GameObject("PlayerSpawn").transform;
            Undo.RegisterCreatedObjectUndo(spawn.gameObject, "Inimigo");
            spawn.SetParent(parent, false);
            // Comeca onde o jogador nasce na cena; depois pode ser movido a mao.
            spawn.SetPositionAndRotation(player.position, player.rotation);
        }

        return spawn;
    }

    private static DefeatController BuildDefeat(Transform parent, GameObject player, Transform spawn)
    {
        DefeatController defeat = Object.FindFirstObjectByType<DefeatController>(FindObjectsInactive.Include);

        if (defeat == null)
        {
            GameObject go = new GameObject("DefeatManager");
            Undo.RegisterCreatedObjectUndo(go, "Inimigo");
            go.transform.SetParent(parent, false);
            defeat = go.AddComponent<DefeatController>();
        }

        GameObject panel = BuildDefeatUI(defeat);

        SerializedObject so = new SerializedObject(defeat);
        so.FindProperty("defeatPanel").objectReferenceValue = panel;
        so.FindProperty("player").objectReferenceValue = player;
        so.FindProperty("currentSpawnPoint").objectReferenceValue = spawn;
        so.FindProperty("menuSceneName").stringValue = "Menu";
        so.ApplyModifiedPropertiesWithoutUndo();

        if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) == null)
            Debug.LogWarning("TemporalEraserBuilder: a cena nao tem EventSystem; os botoes da derrota nao vao responder.");

        return defeat;
    }

    private static GameObject BuildDefeatUI(DefeatController defeat)
    {
        GameObject old = GameObject.Find("DefeatCanvas");
        if (old != null)
            Undo.DestroyObjectImmediate(old);

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontPath);

        GameObject canvasGo = new GameObject("DefeatCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Derrota");
        canvasGo.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 55;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = new GameObject("DefeatPanel", typeof(RectTransform));
        panel.layer = canvasGo.layer;
        panel.transform.SetParent(canvasGo.transform, false);
        Stretch((RectTransform)panel.transform);

        Image dim = new GameObject("Dim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        dim.gameObject.layer = canvasGo.layer;
        dim.transform.SetParent(panel.transform, false);
        dim.color = new Color(0.03f, 0f, 0f, 0.82f);
        dim.raycastTarget = true;
        Stretch(dim.rectTransform);

        Label(panel.transform, font, "Title", "VOCÊ FOI APAGADO DO TEMPO", 44f, Red, new Vector2(0f, 90f));
        Label(panel.transform, font, "Subtitle", "A FITA FOI CORTADA.", 22f, TextIdle, new Vector2(0f, 40f));

        Button retry = Option(panel.transform, font, "RetryButton", "TENTAR DE NOVO", new Vector2(0f, -40f));
        Button menu = Option(panel.transform, font, "MenuButton", "MENU", new Vector2(0f, -90f));

        UnityEventTools.AddVoidPersistentListener(retry.onClick, defeat.Retry);
        UnityEventTools.AddVoidPersistentListener(menu.onClick, defeat.GoToMenu);

        panel.SetActive(false);
        return panel;
    }

    private static TextMeshProUGUI Label(Transform parent, TMP_FontAsset font, string name, string value, float size, Color color, Vector2 position)
    {
        TextMeshProUGUI text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.gameObject.layer = parent.gameObject.layer;
        text.transform.SetParent(parent, false);

        if (font != null)
        {
            text.font = font;
            text.fontSharedMaterial = font.material;
        }

        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1000f, 60f);
        rect.anchoredPosition = position;
        return text;
    }

    // Opcao no estilo da pausa: so texto, "> " no hover.
    private static Button Option(Transform parent, TMP_FontAsset font, string name, string value, Vector2 position)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(320f, 42f);
        rect.anchoredPosition = position;

        Image hit = go.GetComponent<Image>();
        hit.color = new Color(0f, 0f, 0f, 0f);

        Button button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.None;

        TextMeshProUGUI label = Label(go.transform, font, "Label", value, 26f, TextIdle, Vector2.zero);
        Stretch(label.rectTransform);

        ButtonHoverEffect hover = go.AddComponent<ButtonHoverEffect>();
        SerializedObject so = new SerializedObject(hover);
        so.FindProperty("hoverScale").floatValue = 1f;
        so.FindProperty("label").objectReferenceValue = label;
        so.FindProperty("normalTextColor").colorValue = TextIdle;
        so.FindProperty("hoverTextColor").colorValue = Phosphor;
        so.FindProperty("hoverPrefix").stringValue = "> ";
        so.ApplyModifiedPropertiesWithoutUndo();

        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
