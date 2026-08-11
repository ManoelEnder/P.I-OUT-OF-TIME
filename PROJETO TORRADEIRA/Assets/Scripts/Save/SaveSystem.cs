using UnityEngine;
using System.IO;

public class SaveSystem : MonoBehaviour
{
    private string caminhoSave;

    private void Awake()
    {
        caminhoSave = Path.Combine(
            Application.persistentDataPath,
            "save.json"
        );

        Debug.Log("Caminho do Save: " + caminhoSave);
    }

    public bool Salvar(SaveData dados)
    {
        try
        {
            string json = JsonUtility.ToJson(dados, true);

            File.WriteAllText(caminhoSave, json);

            Debug.Log("JOGO SALVO!");
            Debug.Log(json);

            return true;
        }
        catch (System.Exception erro)
        {
            Debug.LogError("ERRO AO SALVAR: " + erro.Message);
            return false;
        }
    }

    public bool Carregar(out SaveData dados)
    {
        dados = null;

        if (!File.Exists(caminhoSave))
        {
            Debug.LogWarning("Nenhum save encontrado!");
            return false;
        }

        try
        {
            string json = File.ReadAllText(caminhoSave);

            dados = JsonUtility.FromJson<SaveData>(json);

            Debug.Log("JOGO CARREGADO!");
            Debug.Log(json);

            return true;
        }
        catch (System.Exception erro)
        {
            Debug.LogError("ERRO AO CARREGAR: " + erro.Message);
            return false;
        }
    }

    public bool ExisteSave()
    {
        return File.Exists(caminhoSave);
    }
}