using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

// Bateria zerada (visor nao abre, pilha recarrega), superaquecimento (camera some uma unica vez,
// num ponto acessivel), recuperacao e visor usavel de novo.
public class BatteryHeatScenario : QAScenario
{
    public override string Name => "BatteryHeat";

    public override IEnumerator Run()
    {
        yield return LoadScene("Principal");
        var placed = Object.FindObjectsByType<BatteryPickup>(FindObjectsSortMode.None)
            .ToDictionary(b => b, b => b.transform.position);
        yield return WaitGameplay();
        DisableEnemy();

        // Pilhas ficam onde o level design colocou (nao caem atraves do piso).
        foreach (var pair in placed)
            Check(Vector3.Distance(pair.Key.transform.position, pair.Value) < 0.2f,
                "pilha " + pair.Key.name + " fica no lugar (" + pair.Value + " -> " + pair.Key.transform.position + ")");

        PhotoCamera photo = Require<PhotoCamera>();
        CameraBattery battery = Require<CameraBattery>();
        CameraOverheat heat = Require<CameraOverheat>();
        if (photo == null || battery == null || heat == null)
            yield break;

        Transform player = GameObject.FindGameObjectWithTag("Player").transform;
        Camera view = photo.playerCam;
        float eye = view.transform.position.y - player.position.y;

        CheckTeleportPoints(heat, eye);

        // Bateria zerada.
        battery.Use(battery.Current);
        Check(battery.IsEmpty, "bateria zera");
        yield return QAInput.Tap(Key.C);
        yield return WaitTransition(photo);
        Check(!photo.cameraHUD.activeSelf, "visor nao abre sem bateria");

        BatteryPickup pickup = Object.FindObjectsByType<BatteryPickup>(FindObjectsSortMode.None)
            .OrderBy(b => Vector3.Distance(b.transform.position, player.position))
            .FirstOrDefault();
        Check(pickup != null, "fase tem pilha para recarregar");
        if (pickup != null)
        {
            Collider col = pickup.GetComponentInChildren<Collider>();
            yield return Approach(player, view, col);
            yield return Aim(view, col);
            yield return QAInput.Tap(Key.E);
            yield return WaitSeconds(0.3f);
            Check(!battery.IsEmpty, "pilha recarrega a bateria (" + battery.Current + ")");
        }

        // Superaquecimento.
        photo.AddBattery(100);
        yield return QAInput.Tap(Key.C);
        yield return WaitTransition(photo);

        for (int i = 0; i < 30 && !heat.IsCameraLost; i++)
        {
            heat.RegisterPhotoHeat();
            yield return WaitSeconds(0.1f);
        }

        yield return WaitTransition(photo);
        Check(heat.IsCameraLost, "camera some ao superaquecer");
        Check(!photo.cameraHUD.activeSelf, "visor fecha quando a camera some");
        Check(photo.crosshair == null || photo.crosshair.activeSelf, "mira volta quando a camera some");

        LostCameraPickup[] lost = Object.FindObjectsByType<LostCameraPickup>(FindObjectsSortMode.None);
        Check(lost.Length == 1, "uma unica camera perdida no mapa (" + lost.Length + ")");
        if (lost.Length == 1)
        {
            Collider body = lost[0].GetComponentInChildren<Collider>();
            Check(body.bounds.min.y >= GroundUnder(body) - 0.02f, "camera perdida apoiada no piso, nao enterrada (base " + body.bounds.min.y.ToString("0.00") + ", piso " + GroundUnder(body).ToString("0.00") + ")");
        }

        for (int i = 0; i < 5; i++)
        {
            heat.RegisterPhotoHeat();
            yield return null;
        }
        yield return WaitSeconds(0.5f);
        lost = Object.FindObjectsByType<LostCameraPickup>(FindObjectsSortMode.None);
        Check(lost.Length == 1, "superaquecer de novo nao cria outra camera (" + lost.Length + ")");

        // Recuperacao.
        if (lost.Length > 0)
        {
            Collider col = lost[0].GetComponentInChildren<Collider>();
            Debug.Log("QA TRACE lost cam at " + lost[0].transform.position + " col=" + (col != null ? col.GetType().Name + " trig=" + col.isTrigger + " b=" + col.bounds : "none") + " layer=" + LayerMask.LayerToName(lost[0].gameObject.layer));
            Check(col != null, "camera perdida tem colisor para interagir");
            if (col != null)
            {
                yield return Approach(player, view, col);
                yield return Aim(view, col);
                yield return QAInput.Tap(Key.E);
                yield return WaitSeconds(0.5f);
            }
        }

        Check(!heat.IsCameraLost, "E recupera a camera");
        photo.AddBattery(100);
        yield return QAInput.Tap(Key.C);
        yield return WaitSeconds(0.2f);
        yield return WaitTransition(photo);
        Check(photo.cameraHUD.activeSelf, "visor abre de novo depois de recuperar");

        yield return QAInput.Tap(Key.C);
        yield return WaitSeconds(0.2f);
        yield return WaitTransition(photo);
        yield return CheckEveryRespawn(photo, heat);
    }

    // Forca cada ponto de reaparecimento, superaquece de verdade e confere onde a camera caiu.
    private IEnumerator CheckEveryRespawn(PhotoCamera photo, CameraOverheat heat)
    {
        var so = new SerializedObject(heat);
        SerializedProperty points = so.FindProperty("teleportPoints");
        var all = new Transform[points.arraySize];
        for (int i = 0; i < all.Length; i++)
            all[i] = points.GetArrayElementAtIndex(i).objectReferenceValue as Transform;

        foreach (Transform point in all)
        {
            if (point == null)
                continue;

            points.arraySize = 1;
            points.GetArrayElementAtIndex(0).objectReferenceValue = point;
            so.ApplyModifiedPropertiesWithoutUndo();

            photo.AddBattery(100);
            yield return QAInput.Tap(Key.C);
            yield return WaitSeconds(0.2f);
            yield return WaitTransition(photo);

            for (int i = 0; i < 30 && !heat.IsCameraLost; i++)
            {
                heat.RegisterPhotoHeat();
                yield return WaitSeconds(0.1f);
            }
            yield return WaitTransition(photo);

            LostCameraPickup lost = Object.FindFirstObjectByType<LostCameraPickup>();
            Check(lost != null, "camera some com o ponto " + point.name);
            if (lost != null)
            {
                Collider body = lost.GetComponentInChildren<Collider>();
                float ground = GroundUnder(body);
                Check(body.bounds.min.y >= ground - 0.02f,
                    "camera perdida em " + point.name + " apoiada no piso (base " + body.bounds.min.y.ToString("0.00") + ", piso " + ground.ToString("0.00") + ")");
            }

            heat.RecoverCamera();
            yield return WaitSeconds(0.3f);
        }

        points.arraySize = all.Length;
        for (int i = 0; i < all.Length; i++)
            points.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Todo ponto de reaparecimento precisa estar num piso, com espaco para o jogador chegar perto.
    private void CheckTeleportPoints(CameraOverheat heat, float eye)
    {
        SerializedProperty points = new SerializedObject(heat).FindProperty("teleportPoints");
        Check(points.arraySize > 0, "ha pontos de reaparecimento");

        for (int i = 0; i < points.arraySize; i++)
        {
            var point = points.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
            Check(point != null, "ponto de reaparecimento " + i + " configurado");
            if (point == null)
                continue;

            GameObject probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            probe.transform.position = point.position + Vector3.up * 0.15f;
            probe.transform.localScale = Vector3.one * 0.3f;
            Physics.SyncTransforms();
            Collider col = probe.GetComponent<Collider>();

            bool reachable = TryFindStandPoint(col, eye, 0.35f, out _);
            Object.DestroyImmediate(probe);
            Check(reachable, "ponto de reaparecimento " + point.name + " acessivel " + point.position);
        }
    }
}
