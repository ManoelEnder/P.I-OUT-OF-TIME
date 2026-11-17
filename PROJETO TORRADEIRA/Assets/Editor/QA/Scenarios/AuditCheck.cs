using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AuditCheck : QAScenario
{
    public override string Name => "AuditCheck";
    public override bool NeedsPlayMode => false;

    public override IEnumerator Run()
    {
        int enabled = 0;
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            if (s.enabled) enabled++;

        AuditReport report = SceneAudit.Run();
        Check(report.ScenesAudited == enabled, "auditou todas as cenas habilitadas (" + report.ScenesAudited + "/" + enabled + ")");
        Check(File.Exists(SceneAudit.ReportPath), "gerou " + SceneAudit.ReportPath);
        Check(report.Total == report.MissingScripts.Count + report.NullReferences.Count + report.MissingPrefabs.Count, "total soma as tres listas");
        yield break;
    }
}
