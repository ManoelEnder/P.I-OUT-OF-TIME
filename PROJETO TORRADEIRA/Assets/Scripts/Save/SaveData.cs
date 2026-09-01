using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public float playerX;
    public float playerY;
    public float playerZ;

    public float playerRotX;
    public float playerRotY;
    public float playerRotZ;

    public List<ObjectSaveData> objects = new();
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

    public bool active;
}