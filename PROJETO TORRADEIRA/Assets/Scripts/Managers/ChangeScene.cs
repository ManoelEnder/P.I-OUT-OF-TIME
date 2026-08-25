using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        Debug.Log("Botão clicado! Cena: " + sceneName);

        SceneManager.LoadScene(sceneName);
    }
}