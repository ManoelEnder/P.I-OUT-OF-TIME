using UnityEngine;

// Pontos fixos da cena Principal usados nas capturas antes/depois e no trecho "principal_pesado"
// do benchmark. Preenchidos pela linha de base (SceneCapture escolhe os mais pesados).
public static class PerfPoints
{
    public struct Point
    {
        public string Name;
        public Vector3 Position;
        public float Yaw;

        public Point(string name, Vector3 position, float yaw)
        {
            Name = name;
            Position = position;
            Yaw = yaw;
        }
    }

    public static readonly Point[] Heavy =
    {
    };
}
