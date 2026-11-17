using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Coloca o aviso de fotossensibilidade na cena aberta (deve ser a TelaInicio).
public static class PhotosensitivityWarningSetup
{
    private const string PixelFontPath = "Assets/Font/VCR_OSD_MONO SDF.asset";

    [MenuItem("Ferramentas/Pixel Art/Adicionar aviso de fotossensibilidade")]
    public static void Add()
    {
        PhotosensitivityWarning warning =
            Object.FindFirstObjectByType<PhotosensitivityWarning>(FindObjectsInactive.Include);

        if (warning == null)
        {
            GameObject go = new GameObject("PhotosensitivityWarning");
            Undo.RegisterCreatedObjectUndo(go, "Aviso de fotossensibilidade");
            warning = go.AddComponent<PhotosensitivityWarning>();
        }

        warning.transform.SetAsFirstSibling();

        SerializedObject so = new SerializedObject(warning);
        so.FindProperty("font").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontPath);
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(warning.gameObject.scene);
    }
}
