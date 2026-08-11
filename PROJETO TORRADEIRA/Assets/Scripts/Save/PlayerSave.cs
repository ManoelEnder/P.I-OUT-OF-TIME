using UnityEngine;

public class PlayerSave : MonoBehaviour
{
    [Header("Sistema de Save")]
    [SerializeField] private SaveSystem saveSystem;

    [Header("Opcional")]
    [SerializeField] private CharacterController characterController;

    [Header("Auto Save")]
    [SerializeField] private float intervaloAutoSave = 180f; // 3 minutos

    private float contadorAutoSave;

    private string mensagem = "";
    private float tempoMensagem = 0f;

    private void Awake()
    {
        if (saveSystem == null)
        {
            saveSystem = FindFirstObjectByType<SaveSystem>();
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        contadorAutoSave = intervaloAutoSave;
    }

    private void Update()
    {
        // AUTO SAVE A CADA 3 MINUTOS
        contadorAutoSave -= Time.deltaTime;

        if (contadorAutoSave <= 0f)
        {
            SalvarJogo();

            contadorAutoSave = intervaloAutoSave;
        }

        // Opcional: F5 para salvar manualmente
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SalvarJogo();
        }

        // Opcional: F9 para carregar
        if (Input.GetKeyDown(KeyCode.F9))
        {
            CarregarJogo();
        }

        if (tempoMensagem > 0f)
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

        dados.posX = transform.position.x;
        dados.posY = transform.position.y;
        dados.posZ = transform.position.z;

        dados.rotX = transform.eulerAngles.x;
        dados.rotY = transform.eulerAngles.y;
        dados.rotZ = transform.eulerAngles.z;

        bool salvou = saveSystem.Salvar(dados);

        if (salvou)
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
            MostrarMensagem("NENHUM SAVE ENCONTRADO!");
            return;
        }

        Vector3 novaPosicao = new Vector3(
            dados.posX,
            dados.posY,
            dados.posZ
        );

        Quaternion novaRotacao = Quaternion.Euler(
            dados.rotX,
            dados.rotY,
            dados.rotZ
        );

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        transform.position = novaPosicao;
        transform.rotation = novaRotacao;

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        MostrarMensagem("JOGO CARREGADO!");
    }

    private void OnApplicationQuit()
    {
        SalvarJogo();

        Debug.Log("Jogo salvo antes de fechar!");
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
        if (tempoMensagem <= 0f)
            return;

        GUIStyle estilo = new GUIStyle(GUI.skin.label);

        estilo.fontSize = 30;
        estilo.alignment = TextAnchor.MiddleCenter;
        estilo.fontStyle = FontStyle.Bold;

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