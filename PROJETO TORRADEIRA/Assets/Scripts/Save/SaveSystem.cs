using System.IO;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    private string savePath;

    private void Awake()
    {
        savePath = Path.Combine(
            Application.persistentDataPath,
            "save.json"
        );
    }

    public bool Save(SaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(savePath, json);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool Load(out SaveData data)
    {
        data = null;

        if (!File.Exists(savePath))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(savePath);

            data = JsonUtility.FromJson<SaveData>(json);

            return data != null;
        }
        catch
        {
            return false;
        }
    }

    public bool SaveExists()
    {
        return File.Exists(savePath);
    }

    public void DeleteSave()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
    }
}