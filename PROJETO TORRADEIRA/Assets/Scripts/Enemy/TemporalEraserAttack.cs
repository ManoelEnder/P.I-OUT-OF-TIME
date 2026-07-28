using UnityEngine;

public class TemporalEraserAttack : MonoBehaviour
{
    [SerializeField] private DefeatController defeatController;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (defeatController == null)
        {
            Debug.LogError(
                $"{name}: DefeatController reference is missing.",
                this
            );

            return;
        }

        defeatController.TriggerDefeat();
    }
}