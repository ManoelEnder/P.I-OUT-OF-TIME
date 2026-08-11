using UnityEngine;

public class PlayerSave : MonoBehaviour
{
    [Header("Sistema de Save")]
    [SerializeField] private SaveSystem saveSystem;

    [Header("Opcional")]
    [SerializeField] private CharacterController characterController;

    private string mensagem = "";
    private float tempoMensagem = 0f;

    private void Awake()
    {
        // Se você esquecer de arrastar o SaveSystem,
        // ele tenta encontrar automaticamente.
        if (saveSystem == null)
        {
            saveSystem = FindFirstObjectByType<SaveSystem>();
        }

        // Procura CharacterController automaticamente
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }
    }

    private void Update()
    {
        // F5 = SALVAR
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SalvarJogo();
        }

        // F9 = CARREGAR
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
            MostrarMensagem("ERRO: SaveSystem não encontrado!");
            Debug.LogError("SaveSystem não encontrado!");
            return;
        }

        SaveData dados = new SaveData();

        // POSIÇÃO
        dados.posX = transform.position.x;
        dados.posY = transform.position.y;
        dados.posZ = transform.position.z;

        // ROTAÇÃO
        dados.rotX = transform.eulerAngles.x;
        dados.rotY = transform.eulerAngles.y;
        dados.rotZ = transform.eulerAngles.z;

        bool salvou = saveSystem.Salvar(dados);

        if (salvou)
        {
            MostrarMensagem("JOGO SALVO!");
        }
        else
        {
            MostrarMensagem("ERRO AO SALVAR!");
        }
    }

    public void CarregarJogo()
    {
        if (saveSystem == null)
        {
            MostrarMensagem("ERRO: SaveSystem não encontrado!");
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

        // CharacterController pode impedir teleporte.
        // Por isso desligamos temporariamente.
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

        Debug.Log("Player carregado na posição: " + novaPosicao);
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