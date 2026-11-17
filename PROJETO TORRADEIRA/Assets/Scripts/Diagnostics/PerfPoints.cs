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
        new Point("p1", new Vector3(-21.37f, 0.08f, -156.30f), 90.00f),
        new Point("p2", new Vector3(-21.37f, 0.08f, -146.13f), 90.00f),
        new Point("p3", new Vector3(-21.37f, 0.08f, -166.47f), 90.00f),
    };
}
