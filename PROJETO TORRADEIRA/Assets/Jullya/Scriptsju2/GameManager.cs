using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instancia;

    
    public bool temLente;
    public bool temBateria;
    public bool temSensor;
    public bool temCircuito;

    
    public bool missaoAceita = false;
    public bool missaoCompleta = false;

    void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}