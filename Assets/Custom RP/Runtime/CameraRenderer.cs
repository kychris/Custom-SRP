using System.Runtime.CompilerServices;
using UnityEditor.PackageManager.UI;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    // Parameters
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

        // Give each camera unique buffer
        PrepareBuffer();
        // Render UI
        PrepareForSceneWindow();

        // Abort if cull failed
        if (!Cull())
        {
            return;
        }

        Setup();
        DrawVisibleGeometry();
        DrawUnsupportedShaders();
        DrawGizmos();
        // Execute the queued work
        Submit();
    }

    // Setup view projection matrix 
    void Setup()
    {
        context.SetupCameraProperties(camera);

        // Clear last projected frame: depth, color, color to reset
        buffer.ClearRenderTarget(true, true, Color.clear);

        buffer.BeginSample(SampleName);
        ExecuteBuffer();
    }

    void DrawVisibleGeometry()
    {
        // Determine whether orthographic or distance-based sorting applies.
        var sortingSettings = new SortingSettings(camera)
        {
            // Force draw order
            criteria = SortingCriteria.CommonOpaque
        };

        var drawingSettings = new DrawingSettings(
            unlitShaderTagId, sortingSettings
        );

        // Indicate which render queues are allowed, filter for non transparent
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );

        // Draw skybox with dedicated method
        context.DrawSkybox(camera);

        // Draw transparent objects after skybox
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

        // If parameters can be retrieved, store results and ret true
        if (camera.TryGetCullingParameters(out p))
        {
            // Use ref to pass as reference
            cullingResults = context.Cull(ref p); //ref same as out, except not required overwrite
            return true;
        }
        return false;
    }

    // Submit the buffered context to execution
    void Submit()
    {
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();
    }

    void ExecuteBuffer()
    {
        // Executes buffer
        context.ExecuteCommandBuffer(buffer);
        // Clear buffer for further reuse
        buffer.Clear();
    }
}