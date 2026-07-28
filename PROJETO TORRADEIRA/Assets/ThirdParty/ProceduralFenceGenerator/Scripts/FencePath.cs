using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FenceGenerator
{
    [ExecuteAlways]
    public class FencePath : MonoBehaviour
    {
        [Header("Fence Path Points")]
        public List<Vector3> points = new List<Vector3>();

        [Header("Fence Prefabs")]
        public GameObject primarySegmentPrefab;
        public GameObject brokenFenceSegmentPrefab;

        private List<GameObject> spawnedFences = new List<GameObject>();

        public void GenerateFence()
        {
            ClearSpawnedFences();

            if (primarySegmentPrefab == null)
            {
                Debug.LogError("Fence Segment Prefab is not assigned.");
                return;
            }

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 start = points[i];
                Vector3 end = points[i + 1];
                Vector3 direction = (end - start);
                float distance = direction.magnitude;
                direction.Normalize();

                int segmentCount = Mathf.FloorToInt(distance / 2f);

                for (int j = 0; j < segmentCount; j++)
                {
                    Vector3 position = start + direction * (j * 2f + 1f);
                    Quaternion rotation = Quaternion.LookRotation(direction);

                    GameObject prefabToUse = Random.value < 0.1f && brokenFenceSegmentPrefab != null ? brokenFenceSegmentPrefab : primarySegmentPrefab;
                    GameObject fence = Instantiate(prefabToUse, position, rotation, transform);
                    spawnedFences.Add(fence);
                }
            }
        }

        public void ClearSpawnedFences()
        {
            foreach (var fence in spawnedFences)
            {
                if (fence != null)
                {
#if UNITY_EDITOR
                    DestroyImmediate(fence);
#else
                Destroy(fence);
#endif
                }
            }
            spawnedFences.Clear();
        }

        public void ClearPoints()
        {
#if UNITY_EDITOR
            Undo.RecordObject(this, "Clear Fence Points");
#endif
            points.Clear();
        }

        public void RemoveLastPoint()
        {
            if (points.Count > 0)
            {
#if UNITY_EDITOR
                Undo.RecordObject(this, "Remove Last Fence Point");
#endif
                points.RemoveAt(points.Count - 1);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < points.Count - 1; i++)
            {
                Gizmos.DrawLine(points[i], points[i + 1]);
                Gizmos.DrawSphere(points[i], 0.2f);
            }

            if (points.Count > 0)
                Gizmos.DrawSphere(points[points.Count - 1], 0.2f);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(FencePath))]
    public class FencePathEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            FencePath fencePath = (FencePath)target;

            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 13;

            EditorGUILayout.LabelField("Fence Generator Tool", headerStyle);
            EditorGUILayout.Space();

            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", headerStyle);

            if (GUILayout.Button("Add Point"))
            {
                Undo.RecordObject(fencePath, "Add Fence Point");
                Vector3 newPoint = fencePath.points.Count > 0 ? fencePath.points[fencePath.points.Count - 1] + Vector3.right * 2f : fencePath.transform.position;
                fencePath.points.Add(newPoint);
            }

            if (GUILayout.Button("Remove Last Point"))
            {
                fencePath.RemoveLastPoint();
            }

            if (GUILayout.Button("Clear Points"))
            {
                fencePath.ClearPoints();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate Fence"))
            {
                fencePath.GenerateFence();
            }

            if (GUILayout.Button("Clear Fences"))
            {
                fencePath.ClearSpawnedFences();
            }

            EditorGUILayout.Space();
            if (GUI.changed)
            {
                EditorUtility.SetDirty(fencePath);
            }
        }
    }
#endif
}

