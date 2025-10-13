using OpenTK.Mathematics;
using System;

namespace Fishing3;

public class WidgetLabeledButton : WidgetBaseButton
{
    private readonly TextObject label;
    private readonly Vector4 color;
    private readonly NineSliceTexture tex;

    public WidgetLabeledButton(Widget? parent, Action onClick, string label, Vector4 color) : base(parent, onClick)
    {
        this.label = new TextObject(label, GuiThemes.Font, 50, GuiThemes.TextColor);

        OnResize += () =>
        {
            this.label.SetScaleFromWidget(this, 0.9f, 0.6f);
        };

        this.color = color;
        tex = GuiThemes.Button;
        this.label.Shadow = true;
    }

    public override void OnRender(float dt, MareShader shader)
    {
        Vector4 c = color;

        if (state == EnumButtonState.Hovered)
        {
            c.Xyz *= 1.2f;
        }

        if (state == EnumButtonState.Active)
        {
            c.Xyz *= 0.8f;
        }

        shader.Uniform("color", c);
        RenderTools.RenderNineSlice(tex, shader, X, Y, Width, Height);
        shader.Uniform("color", Vector4.One);

        label.RenderCenteredLine(XCenter, YCenter, shader, true);
    }
}