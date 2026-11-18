using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

// Capturas para revisao visual (nao reprova nada sozinho): inimigo parado/andando/correndo,
// diafragma do visor abrindo e fechando, superaquecimento e a tela de creditos.
// Saida: docs/revisao/qa/looks/*.png
public class LooksScenario : QAScenario
{
    public override string Name => "Looks";

    private static string Folder => Path.Combine(QARunner.ResultsFolder, "looks");

    public override IEnumerator Run()
    {
        Directory.CreateDirectory(Folder);

        yield return LoadScene("Principal");
        yield return WaitGameplay();

        PhotoCamera photo = Require<PhotoCamera>();
        TemporalEraserController enemy = Require<TemporalEraserController>();
        Transform player = GameObject.FindGameObjectWithTag("Player").transform;

        if (enemy != null)
            yield return EnemyShots(enemy, player);

        if (photo != null)
            yield return VisorShots(photo);

        yield return LoadScene("Credits");
        yield return WaitSeconds(4f);
        yield return Shot("credits_a");
        yield return WaitSeconds(6f);
        yield return Shot("credits_b");
    }

    private IEnumerator EnemyShots(TemporalEraserController enemy, Transform player)
    {
        // Congela a IA e controla a animacao na mao.
        enemy.enabled = false;
        var presence = enemy.GetComponent<TemporalEraserPresence>();
        if (presence != null)
            presence.enabled = false;
        var agent = enemy.GetComponent<NavMeshAgent>();
        agent.enabled = false;

        Animator animator = enemy.GetComponentInChildren<Animator>();
        Check(animator != null && animator.runtimeAnimatorController != null, "inimigo tem Animator com controller");

        // Inimigo a 3 m na frente do jogador, olhando para ele.
        Camera eye = Object.FindFirstObjectByType<PhotoCamera>().playerCam;
        Vector3 flat = Vector3.ProjectOnPlane(eye.transform.forward, Vector3.up).normalized;
        Vector3 spot = player.position + flat * 3f;
        if (NavMesh.SamplePosition(spot, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            spot = hit.position;
        enemy.transform.position = spot;
        enemy.transform.rotation = Quaternion.LookRotation(-flat, Vector3.up);
        eye.transform.LookAt(enemy.transform.position + Vector3.up * 1.2f);

        // Luz para a captura: o corredor e escuro.
        var lightGo = new GameObject("QA_LooksLight", typeof(Light));
        lightGo.transform.position = player.position + Vector3.up * 2.2f + flat * 1.2f;
        Light light = lightGo.GetComponent<Light>();
        light.type = LightType.Point;
        light.range = 8f;
        light.intensity = 3f;

        foreach (var (label, speed) in new[] { ("idle", 0f), ("walk", 2.1f), ("run", 4.1f) })
        {
            if (animator != null)
                animator.SetFloat("Speed", speed);

            yield return WaitSeconds(0.6f);
            yield return Shot("enemy_" + label + "_1");
            yield return WaitSeconds(0.13f);
            yield return Shot("enemy_" + label + "_2");
        }

        // Vista lateral andando.
        enemy.transform.rotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, flat), Vector3.up);
        if (animator != null)
            animator.SetFloat("Speed", 2.1f);
        yield return WaitSeconds(0.4f);
        yield return Shot("enemy_side_walk");
        if (animator != null)
            animator.SetFloat("Speed", 4.1f);
        yield return WaitSeconds(0.5f);
        yield return Shot("enemy_side_run");

        Object.Destroy(lightGo);
        enemy.gameObject.SetActive(false);
    }

    private IEnumerator VisorShots(PhotoCamera photo)
    {
        photo.AddBattery(100);

        yield return QAInput.Tap(Key.C);
        for (int i = 0; i < 6; i++)
        {
            yield return Shot("visor_open_" + i);
            yield return WaitSeconds(0.06f);
        }

        yield return WaitSeconds(0.8f);
        yield return Shot("visor_aberto");

        // Superaquecimento alto: so o efeito de calor, sem estourar a camera.
        CameraOverheat heat = Object.FindFirstObjectByType<CameraOverheat>();
        if (heat != null)
        {
            FieldInfo field = typeof(CameraOverheat).GetField("currentHeat", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo rate = typeof(CameraOverheat).GetField("heatPerSecond", BindingFlags.NonPublic | BindingFlags.Instance);
            rate.SetValue(heat, 0f);
            field.SetValue(heat, 88f);
            yield return WaitSeconds(0.8f);
            yield return Shot("calor_a");
            yield return WaitSeconds(1.1f);
            yield return Shot("calor_b");
            field.SetValue(heat, 10f);
            yield return WaitSeconds(0.5f);
        }

        yield return QAInput.Tap(Key.C);
        for (int i = 0; i < 5; i++)
        {
            yield return Shot("visor_close_" + i);
            yield return WaitSeconds(0.05f);
        }
        yield return WaitSeconds(1f);
    }

    private IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(Folder, name + ".png"));
        yield return null;
    }
}
