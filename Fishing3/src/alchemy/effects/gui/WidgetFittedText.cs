using MareLib;
using OpenTK.Mathematics;

namespace Fishing3;

/// <summary>
/// Renders a line of text centered and fitted on the bounds.
/// </summary>
public class WidgetFittedText : Widget
{
    private readonly TextObject text;

    public WidgetFittedText(Widget? parent, string initialText, Vector4 color) : base(parent)
    {
        text = new TextObject(initialText, GuiThemes.Font, 50, color);

        OnResize += () =>
        {
            text.SetScaleFromWidget(this, 0.8f, 0.6f);
        };
    }

    public void SetText(string newText)
    {
        text.Text = newText;
    }

    public override void OnRender(float dt, MareShader shader)
    {
        text.RenderCenteredLine(XCenter, YCenter, shader, true);
    }
}