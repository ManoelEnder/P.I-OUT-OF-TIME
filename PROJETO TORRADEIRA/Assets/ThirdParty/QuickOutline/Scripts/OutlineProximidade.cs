using UnityEngine;

public class OutlineProximidade : MonoBehaviour
{
    [Header("Proximity")]
    [SerializeField] private float detectionDistance = 3f;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Outline outline;

    private void Awake()
    {
        if (outline == null)
        {
            outline = GetComponent<Outline>();
        }

        if (outline == null)
        {
            return;
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (player == null)
        {
            return;
        }

        outline.enabled = false;
    }

    private void Update()
    {
        if (player == null || outline == null)
        {
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        bool shouldShowOutline = distance <= detectionDistance;

        if (outline.enabled != shouldShowOutline)
        {
            outline.enabled = shouldShowOutline;
        }
    }
}