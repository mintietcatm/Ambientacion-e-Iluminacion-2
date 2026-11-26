using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

// Runs in the editor, without changing or saving the user's scene.
[InitializeOnLoad]
public static class FakeInteriorValidation
{
    const string Root = "Assets/MichelleRamirez_2doParcial/";
    const string Output = "Evidence/XP-25";
    static FakeInteriorValidation() { EditorApplication.delayCall += RunIfRequested; }

    static void RunIfRequested()
    {
        if (!File.Exists("Temp/XP25/validate")) return;
        File.Delete("Temp/XP25/validate");
        Run();
    }

    [MenuItem("Tools/Fake Interior/Validar XP-25")]
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        var report = new StringBuilder();
        Material diagnostic = null;
        Mesh mesh = null;
        Texture2D clear = null;
        bool asyncCompilation = ShaderUtil.allowAsyncCompilation;
        try
        {
            ShaderUtil.allowAsyncCompilation = false;
            AssetDatabase.ImportAsset(Root + "FakeInterior.shadergraph", ImportAssetOptions.ForceSynchronousImport);
            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "Shader Graphs_FakeInterior.mat");
            if (material == null) throw new Exception("No se encontró el material de la escena.");
            var shader = material.shader;
            report.AppendLine("Unity " + Application.unityVersion);
            report.AppendLine("Shader: " + shader.name + "; supported=" + shader.isSupported);
            report.AppendLine("Cubemap: " + AssetDatabase.GetAssetPath(material.GetTexture("_Interior_Image")));
            var errors = ShaderUtil.GetShaderMessages(shader).Where(m => m.severity.ToString() == "Error").ToArray();
            foreach (var error in errors) report.AppendLine("ERROR: " + error.message);
            if (!shader.isSupported || errors.Length > 0) throw new Exception("Shader no válido.");

            mesh = new Mesh();
            mesh.vertices = new[] { new Vector3(-1,-1,0), new Vector3(1,-1,0), new Vector3(1,1,0), new Vector3(-1,1,0) };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            mesh.normals = Enumerable.Repeat(Vector3.back, 4).ToArray();
            mesh.tangents = Enumerable.Repeat(new Vector4(1,0,0,-1), 4).ToArray();
            mesh.triangles = new[] { 0,2,1,0,3,2 };
            mesh.RecalculateBounds();
            diagnostic = new Material(material);
            clear = new Texture2D(1,1);
            clear.SetPixel(0,0,Color.clear); clear.Apply();
            diagnostic.SetTexture("_Window_Frame",clear);
            diagnostic.SetColor("_Window_Tint",Color.black);
            diagnostic.SetColor("_Room_Light_Intensity",Color.white);

            foreach (var angle in new[] { -30f, 0f, 30f })
                Capture(mesh,material,angle, "material-" + angle, report);
            foreach (var depth in new[] { 0f, 0.25f, 1f })
            {
                diagnostic.SetFloat("_Interior_Depth",depth);
                Capture(mesh,diagnostic,25,"depth-" + depth.ToString(System.Globalization.CultureInfo.InvariantCulture),report);
            }
            foreach (var error in ShaderUtil.GetShaderMessages(shader).Where(m => m.severity.ToString() == "Error"))
                throw new Exception(error.message);
            report.AppendLine("PASS: material importado y renderizado desde tres ángulos y tres profundidades, sin errores de shader.");
        }
        catch (Exception ex) { report.AppendLine("FAIL: " + ex); Debug.LogException(ex); }
        finally
        {
            ShaderUtil.allowAsyncCompilation = asyncCompilation;
            if (diagnostic != null) UnityEngine.Object.DestroyImmediate(diagnostic);
            if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
            if (clear != null) UnityEngine.Object.DestroyImmediate(clear);
            File.WriteAllText(Output + "/validation.txt",report.ToString());
            Debug.Log("XP-25: " + report);
        }
    }

    static void Capture(Mesh mesh, Material material, float angle, string name, StringBuilder report)
    {
        var preview = new PreviewRenderUtility();
        try
        {
        preview.camera.fieldOfView = 35;
        preview.camera.nearClipPlane = 0.01f;
        preview.camera.farClipPlane = 50;
        var hd = preview.camera.GetComponent<HDAdditionalCameraData>();
        if (hd == null) hd = preview.camera.gameObject.AddComponent<HDAdditionalCameraData>();
        hd.volumeLayerMask = 0;
        var rotation = Quaternion.Euler(0,angle,0);
        preview.camera.transform.position = rotation * new Vector3(0,0,-4);
        preview.camera.transform.LookAt(Vector3.zero);
        preview.BeginStaticPreview(new Rect(0,0,512,512));
        preview.DrawMesh(mesh,Matrix4x4.identity,material,0);
        preview.Render(true);
        var image = preview.EndStaticPreview();
        File.WriteAllBytes(Output + "/" + name + ".png",image.EncodeToPNG());
        var pixels = image.GetPixels();
        var center = pixels.Where((c,i)=>i%512>180 && i%512<332 && i/512>180 && i/512<332).ToArray();
        float range = center.Max(c=>c.maxColorComponent)-center.Min(c=>c.maxColorComponent);
        report.AppendLine(name + ": rango=" + range);
        UnityEngine.Object.DestroyImmediate(image);
        if (range < 0.01f) throw new Exception("Render uniforme: " + name);
        }
        finally { preview.Cleanup(); }
    }
}
