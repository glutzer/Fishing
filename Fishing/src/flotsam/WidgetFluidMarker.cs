using OpenTK.Mathematics;
using System;

namespace Fishing;

public class WidgetFluidMarker : WidgetBaseSlider
{
    public int MaxFluid { get; private set; }
    private readonly NineSliceTexture tex;
    private readonly NineSliceTexture background;
    private readonly Texture blank;

    private readonly TextObject text;
    private int lastStep;

    public WidgetFluidMarker(Widget? parent, Gui gui, int maxFluid, int currentMark, Action<int> onNewValue) : base(parent, gui, onNewValue, maxFluid, true)
    {
        MaxFluid = maxFluid;
        cursorStep = currentMark;

        tex = GuiThemes.SyringeMarker;
        background = GuiThemes.TitleBorder;
        blank = GuiThemes.Blank;

        text = new($"{currentMark}mL", GuiThemes.Font, Gui.Scaled(10), GuiThemes.TextColor)
        {
            Shadow = true
        };
    }

    public override void OnRender(float dt, ShaderGui shader)
    {
        if (lastStep != cursorStep)
        {
            text.Text = $"{cursorStep}mL";
            lastStep = cursorStep;
            MainAPI.Capi.Gui.PlaySound("tick");
        }

        float currentRatio = cursorStep / (float)MaxFluid;
        int currentPixel = (int)(Width * currentRatio);
        shader.BindTexture(blank, "tex2d");
        shader.Uniform("color", new Vector4(0.8f, 0.8f, 0.6f, 0.7f));
        RenderTools.RenderQuad(shader, X, Y, currentPixel, Height);
        shader.Uniform("color", Vector4.One);

        //int originalWidth = tex.texture.Width;
        //float widthRatio = Width / (float)originalWidth;
        //float volumeInBounds = widthRatio * 50f;

        //float xScale = volumeInBounds / MaxFluid; // How much to inflate it.
        //xScale = Math.Clamp(xScale, 0.5f, 2f);

        float xScale = 1f;
        float yScale = Height / tex.texture.Height;

        RenderTools.RenderNineSliceSplit(tex, shader, X, Y, Width, Height, xScale, yScale);
        RenderTools.RenderNineSlice(background, shader, X, Y, Width, Height);

        if (state == EnumButtonState.Active || IsInAllBounds(Gui.MouseX, Gui.MouseY)) text.RenderLine(Gui.MouseX + 24, Gui.MouseY + 24, shader);
    }
}