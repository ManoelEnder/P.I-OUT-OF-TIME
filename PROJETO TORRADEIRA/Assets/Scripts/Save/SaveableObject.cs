using UnityEngine;
using System;

public class SaveableObject : MonoBehaviour
{
    [SerializeField] private string id;

    public string ID => id;

    private void Awake()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = Guid.NewGuid().ToString();
        }
    }

    public ObjectSaveData CriarDados()
    {
        ObjectSaveData dados = new ObjectSaveData();

        dados.id = id;

        dados.posX = transform.position.x;
        dados.posY = transform.position.y;
        dados.posZ = transform.position.z;

        dados.rotX = transform.eulerAngles.x;
        dados.rotY = transform.eulerAngles.y;
        dados.rotZ = transform.eulerAngles.z;

        dados.ativo = gameObject.activeSelf;

        return dados;
    }

    public void AplicarDados(ObjectSaveData dados)
    {
        Vector3 posicao = new Vector3(
            dados.posX,
            dados.posY,
            dados.posZ
        );

        Quaternion rotacao = Quaternion.Euler(
            dados.rotX,
            dados.rotY,
            dados.rotZ
        );

  
        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.position = posicao;
            rb.rotation = rotacao;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            transform.position = posicao;
            transform.rotation = rotacao;
        }

        gameObject.SetActive(dados.ativo);
    }
}