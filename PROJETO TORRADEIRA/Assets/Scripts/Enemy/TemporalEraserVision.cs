using UnityEngine;

public class TemporalEraserVision : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform eyePoint;

    [Header("Vision")]
    [SerializeField] private float detectionDistance = 10f;
    [SerializeField] private float fieldOfView = 120f;

    [Header("Peripheral Vision")]
    [SerializeField] private float peripheralDetectionDistance = 4f;
    [SerializeField] private float peripheralFieldOfView = 240f;

    public bool CanSeePlayer()
    {
        if (player == null || eyePoint == null)
        {
            return false;
        }

        Vector3 directionToPlayer =
            player.position - eyePoint.position;

        float distanceToPlayer =
            directionToPlayer.magnitude;

        if (distanceToPlayer > detectionDistance)
        {
            return false;
        }

        Vector3 normalizedDirection =
            directionToPlayer.normalized;

        float angle = Vector3.Angle(
            eyePoint.forward,
            normalizedDirection
        );

        bool isInsideNormalVision =
            angle <= fieldOfView * 0.5f;

        bool isInsidePeripheralVision =
            distanceToPlayer <= peripheralDetectionDistance &&
            angle <= peripheralFieldOfView * 0.5f;

        if (!isInsideNormalVision &&
            !isInsidePeripheralVision)
        {
            return false;
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (eyePoint == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            eyePoint.position,
            detectionDistance
        );

        Vector3 leftDirection =
            Quaternion.Euler(
                0f,
                -fieldOfView * 0.5f,
                0f
            ) * eyePoint.forward;

        Vector3 rightDirection =
            Quaternion.Euler(
                0f,
                fieldOfView * 0.5f,
                0f
            ) * eyePoint.forward;

        Gizmos.DrawRay(
            eyePoint.position,
            leftDirection * detectionDistance
        );

        Gizmos.DrawRay(
            eyePoint.position,
            rightDirection * detectionDistance
        );
    }
}