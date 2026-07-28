using UnityEditor;
using UnityEngine;

namespace FenceGenerator
{
    public static class FenceGenerator
    {
        public static void Generate(FencePath path)
        {
            Transform parent = path.transform;

            foreach (Transform child in parent)
            {
                GameObject.DestroyImmediate(child.gameObject);
            }

            GameObject segmentPrefab = path.primarySegmentPrefab;
            GameObject brokenPrefab = path.brokenFenceSegmentPrefab;

            if (segmentPrefab == null)
            {
                Debug.LogError("FenceSegment prefab not assigned in FencePath!");
                return;
            }

            for (int i = 0; i < path.points.Count - 1; i++)
            {
                Vector3 start = path.points[i];
                Vector3 end = path.points[i + 1];
                Vector3 direction = (end - start);
                float distance = direction.magnitude;
                direction.Normalize();

                int segmentCount = Mathf.FloorToInt(distance / 2f);

                for (int j = 0; j < segmentCount; j++)
                {
                    Vector3 position = start + direction * (j * 2f + 1f);
                    Quaternion rotation = Quaternion.LookRotation(direction);

                    GameObject prefabToUse = Random.value < 0.1f ? brokenPrefab : segmentPrefab;
                    if (prefabToUse == null) continue;

                    GameObject fence = GameObject.Instantiate(prefabToUse, position, rotation, parent);
                }
            }
        }
    }
}

