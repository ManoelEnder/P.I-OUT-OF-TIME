using UnityEngine;
using TMPro;
public class Créditos : MonoBehaviour
{
    public float velocidade = 1.0f; 
    private RectTransform rectTransform;
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    
    void Update()
    {
        rectTransform.anchoredPosition += new Vector2(0, velocidade * Time.deltaTime);
    }
    public void VoltarProInicio()
    {
        rectTransform.anchoredPosition = new Vector2(0, -1100); 
    }
}