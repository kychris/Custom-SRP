using System.Runtime.CompilerServices;
using UnityEditor.PackageManager.UI;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    //parameters
    ScriptableRenderContext context;
    Camera camera;

    const string bufferName = "Render Camera";
    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };
    CullingResults cullingResults;

    public void Render(ScriptableRenderContext context, Camera camera)
    {
        this.context = context;
        this.camera = camera;

        PrepareBuffer(); //give each camera unique buffer
        PrepareForSceneWindow(); //render UI

        if (!Cull()) //abort if cull failed
        {
            return;
        }

        Setup();
        DrawVisibleGeometry();
        DrawUnsupportedShaders();
        DrawGizmos();
        Submit(); //execute the queued work
    }

    void Setup()
    { //setup view projection matrix 
        context.SetupCameraProperties(camera);
        buffer.ClearRenderTarget(true, true, Color.clear); //clear last projected frame: depth, color, color to reset
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
    }

    void DrawVisibleGeometry()
    {
        //determine whether orthographic or distance-based sorting applies.
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque //force draw order
        };
        var drawingSettings = new DrawingSettings(
            unlitShaderTagId, sortingSettings
        );

        //indicate which render queues are allowed, here include non transparent
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );

        //draw skybox with dedicated method
        context.DrawSkybox(camera);

        //draw transparent objects after skybox
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );
    }

    bool Cull()
    {
        ScriptableCullingParameters p;
        if (camera.TryGetCullingParameters(out p))
        { //if parameters can be retrieved, store results and ret true
            //use ref to pass as reference
            cullingResults = context.Cull(ref p); //ref same as out, except not required overwrite
            return true;
        }
        return false;
    }

    void Submit() //submit the buffered context to execution
    {
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer); //executes buffer
        buffer.Clear(); //clear buffer for further reuse
    }


}