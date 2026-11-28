using UnityEngine;

// Dicas do jogo (antes tocadas pelo radio). Aparecem no menu de pausa e nas telas de carregamento.
public static class GameTips
{
    public static readonly string[] All =
    {
        "Alguns objetos só podem ser encontrados através da câmera.",
        "A bateria da câmera diminui enquanto ela estiver aberta. Use-a com sabedoria.",
        "Você pode ver suas fotos no álbum.",
        "Explore o mapa e procure por baterias. Você vai precisar delas.",
        "Algumas pistas só aparecem quando observadas pela câmera.",
        "Se a câmera esquentar demais, você pode perdê-la. Dê um tempo para ela esfriar.",
        "Ele ouve você correr. Andar é mais lento, mas bem mais silencioso.",
        "Fitas de proteção não seguram ele por muito tempo. Continue se movendo.",
        "Feche portas atrás de você: ele perde tempo para abri-las.",
        "Se ele te viu, não pare de correr. Ele vai até o último lugar onde te viu.",
    };

    private static int last = -1;

    public static string Random()
    {
        if (All.Length == 0) return string.Empty;
        int i = UnityEngine.Random.Range(0, All.Length);
        if (All.Length > 1 && i == last) i = (i + 1) % All.Length;
        last = i;
        return All[i];
    }
}
