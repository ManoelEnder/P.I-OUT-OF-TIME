using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
         LoadingScreen.Load(sceneName);
    }
}