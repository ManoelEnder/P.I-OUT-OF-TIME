using UnityEngine;

// Apoio para os construtores que montam objetos tanto no editor (salvos na cena) quanto no jogo.
public static class SceneBake
{
    // Destroy no jogo, DestroyImmediate no editor (fora do Play o Destroy nao funciona).
    public static void Kill(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Object.Destroy(o);
        else Object.DestroyImmediate(o);
    }

    // Primitivo sem colisor (o colisor e removido na hora, nos dois modos).
    public static GameObject Primitive(PrimitiveType type, string name, bool keepCollider = false)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        if (!keepCollider)
            Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
}
