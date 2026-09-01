using UnityEngine;

public class DistanceOptimizer : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float activationDistance = 50f;

    private bool isActive = true;

    private void Update()
    {
        if (player == null)
            return;

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        bool shouldBeActive =
            distance <= activationDistance;

        if (shouldBeActive == isActive)
            return;

        isActive = shouldBeActive;

        gameObject.SetActive(isActive);
    }
}