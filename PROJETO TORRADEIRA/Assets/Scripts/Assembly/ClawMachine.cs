using System.Collections;
using UnityEngine;

// Garra mecanica de teto (portico): trilhos no teto, ponte que corre em Z, carro que corre em X,
// braco telescopico que desce e uma garra de 3 dedos. Pega uma peca num ponto e solta em outro.
public class ClawMachine : MonoBehaviour
{
    public float ceilingY = 4.1f;
    public Vector2 railX = new Vector2(-3.2f, 3.2f);
    public Vector2 railZ = new Vector2(-2.6f, 3.2f);
    public float travelSpeed = 1.6f;
    public float dropSpeed = 1.1f;

    private Transform bridge, carriage, armOuter, armInner, head;
    private Transform[] fingers = new Transform[3];
    private AudioSource motor, sfx;
    private static AudioClip servo, clank;
    private float armLength = 0.6f;   // comprimento estendido abaixo do carro
    private Material metal, dark, accent, lamp;
    private Light headLight;

    public void Build(Material metalMat, Material darkMat, Material accentMat, Material lampMat)
    {
        metal = metalMat; dark = darkMat; accent = accentMat; lamp = lampMat;
        float y = ceilingY;
        float spanX = railX.y - railX.x, spanZ = railZ.y - railZ.x;
        float cx = (railX.x + railX.y) * 0.5f, cz = (railZ.x + railZ.y) * 0.5f;

        // Trilhos fixos (em Z) nas laterais.
        Box("RailL", transform, new Vector3(railX.x - 0.25f, y, cz), new Vector3(0.18f, 0.22f, spanZ + 0.8f), dark);
        Box("RailR", transform, new Vector3(railX.y + 0.25f, y, cz), new Vector3(0.18f, 0.22f, spanZ + 0.8f), dark);
        for (int i = 0; i < 4; i++)
        {
            float z = Mathf.Lerp(railZ.x - 0.3f, railZ.y + 0.3f, i / 3f);
            Box("Hanger", transform, new Vector3(railX.x - 0.25f, y + 0.3f, z), new Vector3(0.06f, 0.4f, 0.06f), metal);
            Box("Hanger", transform, new Vector3(railX.y + 0.25f, y + 0.3f, z), new Vector3(0.06f, 0.4f, 0.06f), metal);
        }

        // Ponte (corre em Z) com faixa de aviso.
        bridge = new GameObject("Bridge").transform;
        bridge.SetParent(transform, false);
        bridge.localPosition = new Vector3(cx, y - 0.2f, cz);
        Box("Beam", bridge, Vector3.zero, new Vector3(spanX + 0.9f, 0.24f, 0.32f), accent);
        Box("BeamTrim", bridge, new Vector3(0f, -0.13f, 0f), new Vector3(spanX + 0.9f, 0.04f, 0.36f), dark);
        Box("TruckL", bridge, new Vector3(-(spanX * 0.5f + 0.25f), 0.12f, 0f), new Vector3(0.3f, 0.16f, 0.5f), dark);
        Box("TruckR", bridge, new Vector3(spanX * 0.5f + 0.25f, 0.12f, 0f), new Vector3(0.3f, 0.16f, 0.5f), dark);

        // Carro (corre em X) com motor e luz.
        carriage = new GameObject("Carriage").transform;
        carriage.SetParent(bridge, false);
        carriage.localPosition = new Vector3(0f, -0.26f, 0f);
        Box("Body", carriage, Vector3.zero, new Vector3(0.5f, 0.3f, 0.5f), metal);
        Box("Motor", carriage, new Vector3(0.18f, 0.08f, -0.3f), new Vector3(0.22f, 0.22f, 0.16f), dark);
        Box("Lamp", carriage, new Vector3(-0.18f, 0.05f, -0.26f), new Vector3(0.08f, 0.08f, 0.04f), lamp);

        // Braco telescopico (dois tubos) e cabeca.
        armOuter = new GameObject("ArmOuter").transform;
        armOuter.SetParent(carriage, false);
        Cyl("Tube", armOuter, new Vector3(0f, -0.35f, 0f), new Vector3(0.14f, 0.35f, 0.14f), metal);
        armInner = new GameObject("ArmInner").transform;
        armInner.SetParent(carriage, false);
        Cyl("Piston", armInner, new Vector3(0f, -0.35f, 0f), new Vector3(0.09f, 0.35f, 0.09f), dark);
        head = new GameObject("Head").transform;
        head.SetParent(carriage, false);
        Cyl("Hub", head, Vector3.zero, new Vector3(0.3f, 0.06f, 0.3f), accent);
        Box("Neck", head, new Vector3(0f, 0.07f, 0f), new Vector3(0.12f, 0.08f, 0.12f), dark);

        // Tres dedos articulados (120 graus).
        for (int i = 0; i < 3; i++)
        {
            Transform pivot = new GameObject("Finger" + i).transform;
            pivot.SetParent(head, false);
            float a = i * 120f;
            pivot.localRotation = Quaternion.Euler(0f, a, 0f);
            pivot.localPosition = Quaternion.Euler(0f, a, 0f) * new Vector3(0f, -0.04f, 0.12f);
            Transform hinge = new GameObject("Hinge").transform;
            hinge.SetParent(pivot, false);
            Box("Upper", hinge, new Vector3(0f, -0.11f, 0.02f), new Vector3(0.05f, 0.22f, 0.04f), metal);
            Box("Tip", hinge, new Vector3(0f, -0.25f, -0.03f), new Vector3(0.045f, 0.1f, 0.04f), dark);
            fingers[i] = hinge;
        }

        GameObject lightGo = new GameObject("HeadLight", typeof(Light));
        lightGo.transform.SetParent(head, false);
        lightGo.transform.localPosition = new Vector3(0f, -0.1f, 0f);
        lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        headLight = lightGo.GetComponent<Light>();
        headLight.type = LightType.Spot;
        headLight.spotAngle = 40f;
        headLight.range = 5f;
        headLight.intensity = 0f;
        headLight.color = new Color(1f, 0.95f, 0.85f);

        motor = gameObject.AddComponent<AudioSource>();
        motor.loop = true;
        motor.spatialBlend = 0.6f;
        motor.volume = 0f;
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.spatialBlend = 0.6f;
        if (servo == null) servo = BuildServo();
        if (clank == null) clank = BuildClank();
        motor.clip = servo;
        motor.Play();

        SetArm(0.6f);
        SetGrip(0f);
    }

    public Vector3 HeadPosition => head.position;

    private void SetArm(float length)
    {
        armLength = Mathf.Max(0.45f, length);
        // Tubo externo ate a metade, pistao ate o fim; cabeca na ponta.
        float outer = Mathf.Min(armLength, 1.2f);
        armOuter.localScale = new Vector3(1f, outer / 0.7f, 1f);
        armInner.localScale = new Vector3(1f, armLength / 0.7f, 1f);
        head.localPosition = new Vector3(0f, -0.15f - armLength, 0f);
    }

    // 0 = aberta, 1 = fechada.
    private void SetGrip(float g)
    {
        foreach (Transform f in fingers)
            f.localRotation = Quaternion.Euler(Mathf.Lerp(-32f, 6f, g), 0f, 0f);
    }

    private IEnumerator MoveTo(float x, float z)
    {
        Vector3 b = bridge.localPosition, c = carriage.localPosition;
        float dist = Mathf.Max(Mathf.Abs(z - b.z), Mathf.Abs(x - c.x));
        float time = Mathf.Max(0.3f, dist / travelSpeed);
        motor.volume = 0.35f;
        motor.pitch = 1f;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = Smooth(t / time);
            bridge.localPosition = new Vector3(b.x, b.y, Mathf.Lerp(b.z, z, k));
            carriage.localPosition = new Vector3(Mathf.Lerp(c.x, x, k), c.y, c.z);
            // Leve balanco da cabeca pela inercia.
            head.localRotation = Quaternion.Euler(Mathf.Sin(k * Mathf.PI) * 4f, 0f, 0f);
            yield return null;
        }
        head.localRotation = Quaternion.identity;
        motor.volume = 0f;
        sfx.PlayOneShot(clank, 0.4f);
    }

    private IEnumerator Extend(float targetY)
    {
        float start = armLength;
        float wanted = Mathf.Max(0.45f, carriage.position.y - 0.15f - targetY);
        float time = Mathf.Max(0.25f, Mathf.Abs(wanted - start) / dropSpeed);
        motor.volume = 0.3f;
        motor.pitch = wanted > start ? 0.8f : 1.2f;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            SetArm(Mathf.Lerp(start, wanted, Smooth(t / time)));
            yield return null;
        }
        SetArm(wanted);
        motor.volume = 0f;
    }

    private IEnumerator Grip(float from, float to)
    {
        sfx.PlayOneShot(clank, 0.6f);
        for (float t = 0f; t < 0.35f; t += Time.deltaTime)
        {
            SetGrip(Mathf.Lerp(from, to, t / 0.35f));
            yield return null;
        }
        SetGrip(to);
    }

    // Pega o objeto em 'item' e o coloca em 'target' (posicao e rotacao no mundo).
    public IEnumerator Transfer(Transform item, Vector3 target, Quaternion targetRot, Transform newParent)
    {
        headLight.intensity = 2.5f;
        yield return Grip(1f, 0f);
        yield return MoveTo(item.position.x - transform.position.x, item.position.z - transform.position.z);
        yield return Extend(item.position.y + 0.32f);
        yield return Grip(0f, 1f);
        item.SetParent(head, true);
        yield return new WaitForSeconds(0.15f);
        yield return Extend(target.y + 1.4f);
        yield return MoveTo(target.x - transform.position.x, target.z - transform.position.z);
        yield return Extend(target.y + 0.32f);

        // Encaixa: ajusta a peca para a posicao final antes de soltar.
        Vector3 p0 = item.position; Quaternion r0 = item.rotation;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            item.position = Vector3.Lerp(p0, target, t / 0.3f);
            item.rotation = Quaternion.Slerp(r0, targetRot, t / 0.3f);
            yield return null;
        }
        item.SetParent(newParent, true);
        item.position = target;
        item.rotation = targetRot;
        yield return Grip(1f, 0f);
        yield return Extend(target.y + 1.4f);
        headLight.intensity = 0.6f;
    }

    public IEnumerator Park()
    {
        yield return Extend(transform.position.y + ceilingY - 1.2f);
        yield return MoveTo(0f, (railZ.x + railZ.y) * 0.5f);
        yield return Grip(0f, 1f);
    }

    // Comemora: gira a cabeca e pisca a luz.
    public IEnumerator Flourish()
    {
        for (float t = 0f; t < 1.2f; t += Time.deltaTime)
        {
            head.localRotation = Quaternion.Euler(0f, t * 600f, 0f);
            headLight.intensity = Mathf.Repeat(t * 6f, 1f) < 0.5f ? 4f : 0.5f;
            yield return null;
        }
        head.localRotation = Quaternion.identity;
    }

    private static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

    private static Transform Box(string name, Transform parent, Vector3 pos, Vector3 size, Material m)
        => Prim(PrimitiveType.Cube, name, parent, pos, size, m);

    private static Transform Cyl(string name, Transform parent, Vector3 pos, Vector3 size, Material m)
        => Prim(PrimitiveType.Cylinder, name, parent, pos, size, m);

    private static Transform Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 size, Material m)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = m;
        return go.transform;
    }

    private static AudioClip BuildServo()
    {
        const int rate = 44100;
        int n = rate;
        float[] d = new float[n];
        float ph = 0f;
        var rng = new System.Random(4);
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            ph += (180f + Mathf.Sin(t * 2f * Mathf.PI * 3f) * 6f) / rate;
            float tone = (ph % 1f) * 2f - 1f;
            d[i] = (tone * 0.25f + Mathf.Sin(ph * 2f * Mathf.PI * 2f) * 0.2f + (float)(rng.NextDouble() * 2 - 1) * 0.05f) * 0.6f;
        }
        AudioClip c = AudioClip.Create("ClawServo", n, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }

    private static AudioClip BuildClank()
    {
        const int rate = 44100;
        int n = (int)(rate * 0.3f);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            d[i] = (Mathf.Sin(t * 2f * Mathf.PI * 820f) * 0.5f + Mathf.Sin(t * 2f * Mathf.PI * 1310f) * 0.35f) * Mathf.Exp(-t * 22f);
        }
        AudioClip c = AudioClip.Create("ClawClank", n, 1, rate, false);
        c.SetData(d, 0);
        return c;
    }
}
