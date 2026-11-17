using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class AutomaticLODTools
{
    private static readonly Regex LodRegex =
        new Regex(
            @"^Object_(\d+)_LOD(\d+)$",
            RegexOptions.IgnoreCase
        );

    [MenuItem("Tools/LOD/Montar LOD Groups")]
    public static void BuildLODGroups()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning(
                "Saia do Play Mode antes de executar."
            );

            return;
        }

        Scene scene =
            SceneManager.GetActiveScene();

        GameObject[] roots =
            scene.GetRootGameObjects();

        Dictionary<int, Dictionary<int, List<Renderer>>> groups =
            new Dictionary<int, Dictionary<int, List<Renderer>>>();

        Dictionary<int, Transform> parents =
            new Dictionary<int, Transform>();

        foreach (GameObject root in roots)
        {
            Transform[] transforms =
                root.GetComponentsInChildren<Transform>(true);

            foreach (Transform current in transforms)
            {
                Match match =
                    LodRegex.Match(current.name);

                if (!match.Success)
                    continue;

                int groupId =
                    int.Parse(match.Groups[1].Value);

                int lodId =
                    int.Parse(match.Groups[2].Value);

                Renderer renderer =
                    current.GetComponent<Renderer>();

                if (renderer == null)
                    continue;

                if (!groups.ContainsKey(groupId))
                {
                    groups.Add(
                        groupId,
                        new Dictionary<int, List<Renderer>>()
                    );

                    parents.Add(
                        groupId,
                        current.parent
                    );
                }

                if (!groups[groupId].ContainsKey(lodId))
                {
                    groups[groupId].Add(
                        lodId,
                        new List<Renderer>()
                    );
                }

                groups[groupId][lodId].Add(renderer);
            }
        }

        int configuredGroups = 0;

        foreach (
            KeyValuePair<int, Dictionary<int, List<Renderer>>> group
            in groups
        )
        {
            if (!parents.ContainsKey(group.Key))
                continue;

            Transform parent =
                parents[group.Key];

            if (parent == null)
                continue;

            LODGroup lodGroup =
                parent.GetComponent<LODGroup>();

            if (lodGroup == null)
            {
                lodGroup =
                    Undo.AddComponent<LODGroup>(
                        parent.gameObject
                    );
            }

            List<int> indices =
                new List<int>(
                    group.Value.Keys
                );

            indices.Sort();

            List<LOD> lods =
                new List<LOD>();

            int lodCount =
                indices.Count;

            for (int i = 0; i < lodCount; i++)
            {
                int lodIndex =
                    indices[i];

                float threshold =
                    CalculateThreshold(
                        i,
                        lodCount
                    );

                lods.Add(
                    new LOD(
                        threshold,
                        group.Value[lodIndex].ToArray()
                    )
                );
            }

            lodGroup.SetLODs(
                lods.ToArray()
            );

            lodGroup.fadeMode =
                LODFadeMode.None;

            lodGroup.RecalculateBounds();

            EditorUtility.SetDirty(
                lodGroup
            );

            configuredGroups++;
        }

        EditorSceneManager.MarkSceneDirty(
            scene
        );

        Debug.Log(
            $"LOD Groups configurados: {configuredGroups}"
        );
    }

    private static float CalculateThreshold(
        int index,
        int total
    )
    {
        if (total <= 1)
            return 0.01f;

        float start = 0.6f;
        float end = 0.01f;

        float t =
            (float)index /
            (total - 1);

        float value =
            Mathf.Lerp(
                start,
                end,
                t
            );

        return Mathf.Clamp(
            value,
            0.01f,
            0.99f
        );
    }

    [MenuItem(
        "Tools/LOD/Montar LOD Groups",
        true
    )]
    private static bool Validate()
    {
        return !EditorApplication.isPlaying;
    }
}