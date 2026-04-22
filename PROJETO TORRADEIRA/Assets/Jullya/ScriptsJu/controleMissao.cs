using UnityEngine;

public class ControleMissao : MonoBehaviour
{
    public static bool falouComNPC = false;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}