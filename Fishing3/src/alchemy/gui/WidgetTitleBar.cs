using MareLib;
using OpenTK.Mathematics;

namespace Fishing3;

public class WidgetTitleBar : WidgetBaseDraggableTitle
{
    private readonly TextObject textObj;
    private readonly NineSliceTexture background;

    public WidgetTitleBar(Widget? parent, Widget draggableWidget, string title) : base(parent, draggableWidget)
    {
        textObj = new TextObject(title, GuiThemes.Font, 50, GuiThemes.TextColor)
        {
            Shadow = true
        };

        OnResize += () =>
        {
            textObj.SetScaleFromWidget(this, 0.9f, 0.6f);
        };

        background = GuiThemes.Background;
    }

    public override void OnRender(float dt, MareShader shader)
    {
        shader.Uniform("color", new Vector4(0.1f, 0.1f, 0.1f, 1));
        RenderTools.RenderNineSlice(background, shader, X, Y, Width, Height);
        shader.Uniform("color", Vector4.One);

        textObj.RenderCenteredLine(XCenter, YCenter, shader, true);
    }
}