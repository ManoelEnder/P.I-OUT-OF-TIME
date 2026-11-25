using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Occlusion culling da mobilia: do meio do escritorio (terreo) olhando para a parede do laboratorio,
// as bancadas do laboratorio estao no campo de visao mas atras da parede -> devem ser cortadas.
// Os moveis do proprio escritorio, na frente, continuam visiveis. Sem occlusion, o laboratorio volta.
// Usa CullingGroup (resultado do culling da propria camera, inclusive occlusion).
public class OcclusionScenario : QAScenario
{
    public override string Name => "Occlusion";

    public override IEnumerator Run()
    {
        yield return LoadScene("Principal");
        yield return WaitGameplay();
        DisableEnemy();

        GameObject lab = GameObject.Find("Mobilia/T1_laboratorio");
        GameObject office = GameObject.Find("Mobilia/T2_escritorio");
        Check(lab != null && office != null, "mobilia do terreo existe");
        if (lab == null || office == null)
            yield break;

        var rt = new RenderTexture(640, 360, 24);
        Camera cam = new GameObject("QA_OcclusionCam", typeof(Camera)).GetComponent<Camera>();
        cam.targetTexture = rt;
        cam.depth = 100;
        cam.fieldOfView = 75f;
        cam.farClipPlane = 120f;
        cam.useOcclusionCulling = true;
        cam.transform.position = new Vector3(-12.5f, 1.6f, -154.5f);
        cam.transform.rotation = Quaternion.LookRotation(Vector3.left);

        Bounds[] labB = lab.transform.Cast<Transform>().Select(BoundsOf).ToArray();
        Bounds[] officeB = office.transform.Cast<Transform>().Select(BoundsOf).ToArray();
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
        int labInFrustum = labB.Count(b => GeometryUtility.TestPlanesAABB(planes, b));
        Check(labInFrustum >= 10, "bancadas do laboratorio dentro do campo de visao (" + labInFrustum + ")");

        int labOn = 0, officeOn = 0, officeInFrustum = 0, labOff = 0;
        yield return CountVisible(cam, labB, v => labOn = v);
        yield return CountVisible(cam, officeB.Where(b => GeometryUtility.TestPlanesAABB(planes, b)).ToArray(), v => officeOn = v);
        officeInFrustum = officeB.Count(b => GeometryUtility.TestPlanesAABB(planes, b));

        cam.useOcclusionCulling = false;
        yield return CountVisible(cam, labB, v => labOff = v);

        Debug.Log("QA TRACE occlusion: laboratorio no campo=" + labInFrustum + " visiveis com occlusion=" + labOn + " sem occlusion=" + labOff +
                  " | escritorio no campo=" + officeInFrustum + " visiveis=" + officeOn);
        // A parede tem duas passagens: o que aparece por elas continua visivel (correto).
        Check(labOn <= labInFrustum * 0.8f, "occlusion corta parte do laboratorio atras da parede (" + labOn + " de " + labInFrustum + " visiveis pelas passagens)");
        Check(labOff >= labInFrustum * 0.8f, "sem occlusion eles voltariam a ser desenhados (" + labOff + " de " + labInFrustum + ")");
        Check(officeInFrustum > 0 && officeOn >= officeInFrustum * 0.7f, "moveis na frente continuam visiveis (" + officeOn + " de " + officeInFrustum + ")");

        // Laje inteira: do andar de cima olhando para baixo, a mobilia do terreo (12 m abaixo) some toda.
        cam.useOcclusionCulling = true;
        cam.transform.position = new Vector3(-21.6f, 13.7f, -158f);
        cam.transform.rotation = Quaternion.Euler(80f, -90f, 0f);
        cam.farClipPlane = 40f;
        Bounds[] ground = lab.transform.Cast<Transform>().Concat(office.transform.Cast<Transform>()).Select(BoundsOf).ToArray();
        planes = GeometryUtility.CalculateFrustumPlanes(cam);
        Bounds[] below = ground.Where(b => GeometryUtility.TestPlanesAABB(planes, b)).ToArray();
        int belowOn = 0, belowOff = 0;
        yield return CountVisible(cam, below, v => belowOn = v);
        cam.useOcclusionCulling = false;
        yield return CountVisible(cam, below, v => belowOff = v);
        Debug.Log("QA TRACE occlusion laje: no campo=" + below.Length + " com occlusion=" + belowOn + " sem=" + belowOff);
        Check(below.Length >= 10, "mobilia do terreo dentro do campo de visao de cima (" + below.Length + ")");
        Check(belowOn <= below.Length * 0.05f, "laje esconde a mobilia do terreo (" + belowOn + " de " + below.Length + " visiveis)");
        Check(belowOff >= below.Length * 0.8f, "sem occlusion ela seria desenhada (" + belowOff + " de " + below.Length + ")");

        Object.Destroy(cam.gameObject);
        rt.Release();
    }

    private static Bounds BoundsOf(Transform t)
    {
        Renderer[] rs = t.GetComponentsInChildren<Renderer>();
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    private IEnumerator CountVisible(Camera cam, Bounds[] bounds, System.Action<int> result)
    {
        var group = new CullingGroup { targetCamera = cam };
        var spheres = bounds.Select(b => new BoundingSphere(b.center, b.extents.magnitude * 0.6f)).ToArray();
        group.SetBoundingSpheres(spheres);
        group.SetBoundingSphereCount(spheres.Length);
        for (int i = 0; i < 5; i++)
        {
            cam.Render();
            yield return null;
        }
        int visible = 0;
        for (int i = 0; i < spheres.Length; i++)
            if (group.IsVisible(i)) visible++;
        group.Dispose();
        result(visible);
    }
}
