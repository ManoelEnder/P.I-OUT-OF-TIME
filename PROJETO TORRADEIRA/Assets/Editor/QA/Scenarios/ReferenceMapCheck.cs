using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ReferenceMapCheck : QAScenario
{
    public override string Name => "ReferenceMapCheck";
    public override bool NeedsPlayMode => false;

    public override IEnumerator Run()
    {
        HashSet<string> used = ReferenceMap.UsedAssets();
        Check(used.Contains("Assets/Scenes/Principal.unity"), "Principal e usada");
        Check(used.Contains("Assets/Font/VCR_OSD_MONO SDF.asset"), "fonte VCR e usada");
        Check(used.Contains("Assets/Prefabs/Enemy/Vampire A Lusth.fbx"), "modelo do inimigo e usado");
        Check(!used.Contains("Assets/_Recovery/0.unity"), "_Recovery nao e usada");
        yield break;
    }
}
