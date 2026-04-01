using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    public string sceneName; // Nome da cena que vai carregar

    public void LoadScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
