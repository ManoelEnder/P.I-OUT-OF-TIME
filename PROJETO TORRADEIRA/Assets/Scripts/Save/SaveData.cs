using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    // PLAYER
    public float playerX;
    public float playerY;
    public float playerZ;

    public float playerRotX;
    public float playerRotY;
    public float playerRotZ;

    // OBJETOS DO MUNDO
    public List<ObjectSaveData> objetos = new List<ObjectSaveData>();
}

[Serializable]
public class ObjectSaveData
{
    public string id;

    public float posX;
    public float posY;
    public float posZ;

    public float rotX;
    public float rotY;
    public float rotZ;

    public bool ativo;
}