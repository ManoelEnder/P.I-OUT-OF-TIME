using UnityEngine;
using UnityEngine.AI;

public class PlayerSave : MonoBehaviour
{
    [Header("Sistema de Save")]
    [SerializeField] private SaveSystem saveSystem;

    [Header("Auto Save")]
    [SerializeField] private float intervaloAutoSave = 180f;

    private float contadorAutoSave;

    private CharacterController characterController;

    private string mensagem = "";
    private float tempoMensagem;

    private void Awake()
    {
        saveSystem = FindFirstObjectByType<SaveSystem>();

        characterController =
            GetComponent<CharacterController>();

        contadorAutoSave = intervaloAutoSave;
    }

    private void Update()
    {
        

        contadorAutoSave -= Time.deltaTime;

        if (contadorAutoSave <= 0)
        {
            SalvarJogo();

            contadorAutoSave = intervaloAutoSave;
        }

      
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SalvarJogo();
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            CarregarJogo();
        }

        if (tempoMensagem > 0)
        {
            tempoMensagem -= Time.deltaTime;
        }
    }

    public void SalvarJogo()
    {
        if (saveSystem == null)
        {
            Debug.LogError("SaveSystem não encontrado!");
            return;
        }

        SaveData dados = new SaveData();

    

        dados.playerX = transform.position.x;
        dados.playerY = transform.position.y;
        dados.playerZ = transform.position.z;

        dados.playerRotX = transform.eulerAngles.x;
        dados.playerRotY = transform.eulerAngles.y;
        dados.playerRotZ = transform.eulerAngles.z;

        SaveableObject[] objetos =
            FindObjectsByType<SaveableObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (SaveableObject objeto in objetos)
        {
            
            if (objeto.gameObject == gameObject)
                continue;

            dados.objetos.Add(
                objeto.CriarDados()
            );
        }

        if (saveSystem.Salvar(dados))
        {
            MostrarMensagem("JOGO SALVO!");
        }
    }

    public void CarregarJogo()
    {
        if (saveSystem == null)
        {
            Debug.LogError("SaveSystem não encontrado!");
            return;
        }

        if (!saveSystem.Carregar(out SaveData dados))
        {
            MostrarMensagem("NENHUM SAVE!");
            return;
        }

        Vector3 posicaoPlayer = new Vector3(
            dados.playerX,
            dados.playerY,
            dados.playerZ
        );

        Quaternion rotacaoPlayer =
            Quaternion.Euler(
                dados.playerRotX,
                dados.playerRotY,
                dados.playerRotZ
            );

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        transform.position = posicaoPlayer;
        transform.rotation = rotacaoPlayer;

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        
        SaveableObject[] objetos =
            FindObjectsByType<SaveableObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (SaveableObject objeto in objetos)
        {
            foreach (ObjectSaveData dadosObjeto in dados.objetos)
            {
                if (objeto.ID == dadosObjeto.id)
                {
                    objeto.AplicarDados(dadosObjeto);
                    break;
                }
            }
        }

        MostrarMensagem("JOGO CARREGADO!");
    }

    private void OnApplicationQuit()
    {
        SalvarJogo();
    }

    private void OnApplicationPause(bool pausado)
    {
        if (pausado)
        {
            SalvarJogo();
        }
    }

    private void MostrarMensagem(string texto)
    {
        mensagem = texto;
        tempoMensagem = 3f;

        Debug.Log(texto);
    }

    private void OnGUI()
    {
        if (tempoMensagem <= 0)
            return;

        GUIStyle estilo =
            new GUIStyle(GUI.skin.label);

        estilo.fontSize = 30;
        estilo.fontStyle = FontStyle.Bold;
        estilo.alignment = TextAnchor.MiddleCenter;

        GUI.Box(
            new Rect(
                Screen.width / 2 - 175,
                40,
                350,
                70
            ),
            ""
        );

        GUI.Label(
            new Rect(
                Screen.width / 2 - 175,
                40,
                350,
                70
            ),
            mensagem,
            estilo
        );
    }
}