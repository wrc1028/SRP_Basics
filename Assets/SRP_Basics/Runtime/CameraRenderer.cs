using UnityEngine;
using UnityEngine.Rendering;
// Our camera renderer is roughly equivalent to the scriptable renderers of the Universal RP.
// 可以按照自己想法去自定义渲染逻辑
public class CameraRenderer
{
    private ScriptableRenderContext context;
    private Camera camera;
    public void Render(ScriptableRenderContext context, Camera camera)
    {
        this.context = context;
        this.camera = camera;

        DrawVisibleGeometry();
    }

    private void DrawVisibleGeometry()
    {
        context.DrawSkybox(camera);
    }
}
