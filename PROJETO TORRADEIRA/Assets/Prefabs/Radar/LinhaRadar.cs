
using UnityEngine;

public class LinhaRadar : MonoBehaviour
{
    public float velocidade = 180f;

    void Update()
    {
        transform.Rotate(
            0f,
            0f,
            -velocidade * Time.deltaTime
        );
    }
}
