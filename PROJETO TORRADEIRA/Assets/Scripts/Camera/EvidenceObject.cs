using UnityEngine;

public class EvidenceObject : MonoBehaviour
{
    public string evidenceName;
    public string description;
    public bool collected = false;

    public void Collect()
    {
        if (collected) return;
        collected = true;
        Debug.Log("Evidência coletada: " + evidenceName);
    }
}