using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEngine.Profiling;

partial class CameraRenderer //use partial class to organize code
{
    //declare signature to be callable
    partial void DrawGizmos();
    partial void DrawUnsupportedShaders();
    partial void PrepareForSceneWindow();
    partial void PrepareBuffer();


#if UNITY_EDITOR
    //shader variables
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"); //unlit shader
    static ShaderTagId[] legacyShaderTagIds = { //all unity default shaders
		new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };
    static Material errorMaterial;

    string SampleName { get; set; }

    partial void DrawGizmos() //draw gizmos in editor
    {
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }

    partial void PrepareForSceneWindow() //draw UI in editor
    {
        if (camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }

    partial void DrawUnsupportedShaders()
    { //function for drawing unity shaders
        if (errorMaterial == null) //define error material as default
        {
            errorMaterial =
                new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        var drawingSettings = new DrawingSettings(
            legacyShaderTagIds[0], new SortingSettings(camera)
        )
        {
            overrideMaterial = errorMaterial //set unsupported material
        };

        for (int i = 1; i < legacyShaderTagIds.Length; i++)
        {
            drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }

        var filteringSettings = FilteringSettings.defaultValue;
        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );
    }

    partial void PrepareBuffer() //assign unique buffer to each camera
    {
        Profiler.BeginSample("Editor Only"); //wrap in profiler sample to check memory usage
        buffer.name = SampleName = camera.name;
        Profiler.EndSample();
    }

#else

    const string SampleName = bufferName;


#endif
}