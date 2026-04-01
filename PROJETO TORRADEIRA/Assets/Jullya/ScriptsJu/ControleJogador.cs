using UnityEngine;

public class ControleJogador : MonoBehaviour
{
    public CharacterController controller;
    public float velocidade = 10f;
    public float gravidade = -20f;

    private Vector3 velocidadeQueda;

    void Update()
    {
     
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 mover = transform.right * x + transform.forward * z;

   
        controller.Move(mover * velocidade * Time.deltaTime);

  
        if (controller.isGrounded && velocidadeQueda.y < 0)
        {
            velocidadeQueda.y = -2f;
        }

        velocidadeQueda.y += gravidade * Time.deltaTime;
        controller.Move(velocidadeQueda * Time.deltaTime);
    }
}