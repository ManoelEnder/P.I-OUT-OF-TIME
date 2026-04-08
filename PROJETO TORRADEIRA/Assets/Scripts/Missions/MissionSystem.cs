using UnityEngine;
using TMPro;

public class MissionSystem : MonoBehaviour
{
    public TextMeshProUGUI tirarFotosText;
    public TextMeshProUGUI descobrirPecaText;

    int fotos = 0;
    public int fotosNecessarias = 5;

    bool fotosCompletas = false;
    bool pecaDescoberta = false;

    public void AddFoto()
    {
        if (fotosCompletas) return;

        fotos++;

        tirarFotosText.text = "[ ] Tirar " + fotosNecessarias + " fotos (" + fotos + "/" + fotosNecessarias + ")";

        if (fotos >= fotosNecessarias)
        {
            fotosCompletas = true;
            tirarFotosText.text = "[X] Tirar " + fotosNecessarias + " fotos";
            tirarFotosText.color = Color.gray;
        }
    }

    public void DescobriuPeca()
    {
        if (pecaDescoberta) return;

        pecaDescoberta = true;
        descobrirPecaText.text = "[X] Descobrir a primeira peça";
        descobrirPecaText.color = Color.gray;
    }
}