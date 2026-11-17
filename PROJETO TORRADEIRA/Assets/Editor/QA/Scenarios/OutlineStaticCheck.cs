using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// O contorno (QuickOutline) le a malha no Awake; com static batching a malha vira o
// "Combined Mesh" nao legivel da build e o jogo loga erro. Nenhum objeto com Outline pode ser estatico de batching.
public class OutlineStaticCheck : QAScenario
{
    public override string Name => "OutlineStaticCheck";
    public override bool NeedsPlayMode => false;

    public override IEnumerator Run()
    {
        foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
        {
            if (!entry.enabled)
                continue;

            var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
            var offenders = new List<string>();

            foreach (Outline outline in Object.FindObjectsByType<Outline>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                foreach (MeshFilter filter in outline.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (GameObjectUtility.AreStaticEditorFlagsSet(filter.gameObject, StaticEditorFlags.BatchingStatic))
                        offenders.Add(filter.name);
                }
            }

            Check(offenders.Count == 0, scene.name + ": Outline sem static batching (" + string.Join(", ", offenders) + ")");
        }

        yield break;
    }
}
