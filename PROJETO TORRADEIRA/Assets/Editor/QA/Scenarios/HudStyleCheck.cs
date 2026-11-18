using System.Collections;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// HUD da fase padronizado: fonte VCR, escala fixa de tamanhos, missao com moldura,
// bateria (branca) e calor (vermelho) diferentes na cor alem da forma.
public class HudStyleCheck : QAScenario
{
    public override string Name => "HudStyleCheck";
    public override bool NeedsPlayMode => false;

    public override IEnumerator Run()
    {
        EditorSceneManager.OpenScene(HudStyleSetup.ScenePath);

        Canvas hud = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(c => c.name == HudStyleSetup.HudCanvas);
        Check(hud != null, "fase tem o " + HudStyleSetup.HudCanvas);
        if (hud == null)
            yield break;

        foreach (TMP_Text text in hud.GetComponentsInChildren<TMP_Text>(true))
        {
            float size = text.enableAutoSizing ? text.fontSizeMax : text.fontSize;
            Check(HudStyleSetup.Sizes.Contains(size), text.name + " usa tamanho da escala (" + size + ")");
            Check(text.font != null && text.font.name.StartsWith("VCR"), text.name + " usa a fonte VCR");
        }

        Transform missions = hud.transform.Find("MissionsPanel");
        Check(missions != null && missions.Find("Frame") != null, "missao tem moldura");

        CameraOverheat heat = Object.FindFirstObjectByType<CameraOverheat>(FindObjectsInactive.Include);
        CameraBattery battery = Object.FindFirstObjectByType<CameraBattery>(FindObjectsInactive.Include);
        if (heat != null && battery != null)
        {
            Color heatColor = new SerializedObject(heat).FindProperty("barraNormal").colorValue;
            Color batteryColor = new SerializedObject(battery).FindProperty("corNormal").colorValue;
            Check(heatColor.r > heatColor.g * 2f, "calor em vermelho (" + heatColor + ")");
            Check(Mathf.Abs(batteryColor.r - batteryColor.g) < 0.1f, "bateria em branco/cinza (" + batteryColor + ")");
        }

        yield break;
    }
}
