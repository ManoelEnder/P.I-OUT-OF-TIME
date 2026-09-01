using System;
using UnityEngine;

public class SaveableObject : MonoBehaviour
{
    [SerializeField] private string id;

    public string ID => id;

    private void Reset()
    {
        GenerateID();
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
        {
            GenerateID();
        }
    }

    private void GenerateID()
    {
        id = Guid.NewGuid().ToString();
    }

    public ObjectSaveData CreateData()
    {
        return new ObjectSaveData
        {
            id = id,

            posX = transform.position.x,
            posY = transform.position.y,
            posZ = transform.position.z,

            rotX = transform.eulerAngles.x,
            rotY = transform.eulerAngles.y,
            rotZ = transform.eulerAngles.z,

            active = gameObject.activeSelf
        };
    }

    public void ApplyData(ObjectSaveData data)
    {
        if (data.active && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        Vector3 position = new(
            data.posX,
            data.posY,
            data.posZ
        );

        Quaternion rotation = Quaternion.Euler(
            data.rotX,
            data.rotY,
            data.rotZ
        );

        Rigidbody rigidbodyComponent = GetComponent<Rigidbody>();

        if (rigidbodyComponent != null)
        {
            rigidbodyComponent.position = position;
            rigidbodyComponent.rotation = rotation;
            rigidbodyComponent.linearVelocity = Vector3.zero;
            rigidbodyComponent.angularVelocity = Vector3.zero;
        }
        else
        {
            transform.SetPositionAndRotation(
                position,
                rotation
            );
        }

        if (!data.active && gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }
}

