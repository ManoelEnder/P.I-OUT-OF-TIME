using UnityEngine;

// Gira um objeto em volta de um eixo local (rolos da fita, ventoinhas).
public class MenuSpin : MonoBehaviour
{
    [SerializeField] private Vector3 axis = Vector3.forward;
    [SerializeField] private float degreesPerSecond = 180f;

    private void Update()
    {
        transform.Rotate(axis, degreesPerSecond * Time.unscaledDeltaTime, Space.Self);
    }
}
