using Fishing;
using OpenTK.Mathematics;
using System;

namespace Guilds;

public class WidgetNameInput : Widget
{
    public NineSliceTexture texture;
    public WidgetTextBoxSingle textBox;
    public string lastText;

    public Action<string> onNewText;
    public Func<string, bool> isTextValid;

    public WidgetNameInput(Widget? parent, string defaultText, string label, Action<string> onNewText, Func<string, bool> isTextValid) : base(parent)
    {
        texture = GuiThemes.Background;

        this.onNewText = onNewText;
        this.isTextValid = isTextValid;

        textBox = (WidgetTextBoxSingle)new WidgetTextBoxSingle(this, GuiThemes.Font, GuiThemes.TextColor, false, true, OnNewText, defaultText)
            .Alignment(Align.LeftTop)
            .Percent(0.5f, 0, 0.5f, 1);

        new WidgetTextLine(this, GuiThemes.Font, label, GuiThemes.TextColor, true)
            .Alignment(Align.LeftTop)
            .Percent(0, 0, 0.5f, 1);

        lastText = defaultText;
    }

    public void OnNewText(string newText)
    {
        if (!isTextValid(newText))
        {
            textBox.SetTextNoEvent(lastText);
            return;
        }

        lastText = newText;

        onNewText(newText);
    }

    public override void OnRender(float dt, NuttyShader shader)
    {
        shader.Uniform("color", new Vector4(0.1f, 0.1f, 0.1f, 1));
        RenderTools.RenderNineSlice(texture, shader, X, Y, Width, Height);
        shader.Uniform("color", Vector4.One);
    }
}