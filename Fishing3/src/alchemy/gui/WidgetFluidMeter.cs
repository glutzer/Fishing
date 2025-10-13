using OpenTK.Mathematics;
using Vintagestory.API.Client;

namespace Fishing;

public class WidgetFluidMeter : Widget
{
    private readonly FluidContainer container;
    private readonly NineSliceTexture tex;
    private readonly Texture blank;

    private readonly TextObject textObj;

    private bool hovered;

    public WidgetFluidMeter(Widget? parent, FluidContainer container) : base(parent)
    {
        this.container = container;
        tex = GuiThemes.TitleBorder;
        blank = GuiThemes.Blank;

        textObj = new TextObject("", GuiThemes.Font, 30, GuiThemes.TextColor)
        {
            Shadow = true
        };
    }

    public override void RegisterEvents(GuiEvents guiEvents)
    {
        guiEvents.MouseMove += GuiEvents_MouseMove;
    }

    private void GuiEvents_MouseMove(MouseEvent obj)
    {
        if (!obj.Handled && IsInAllBounds(obj))
        {
            obj.Handled = true;
            hovered = true;
        }
        else
        {
            hovered = false;
        }
    }

    public override void OnRender(float dt, NuttyShader shader)
    {
        shader.BindTexture(blank, "tex2d");
        shader.Uniform("color", GuiThemes.DarkColor * 0.5f);
        RenderTools.RenderQuad(shader, X, Y, Width, Height); // Background.

        if (container.HeldStack != null)
        {
            float percent = container.FillPercent;

            shader.Uniform("color", hovered ? container.HeldStack.fluid.GetColor(container.HeldStack) * 1.2f : container.HeldStack.fluid.GetColor(container.HeldStack));

            RenderTools.RenderQuad(shader, X, Y + Height, Width, -Height * percent);
        }

        shader.Uniform("color", Vector4.One);

        RenderTools.RenderNineSlice(tex, shader, X, Y, Width, Height); // Border.

        if (hovered)
        {
            textObj.Text = container.HeldStack == null ? "Empty" : $"{container.HeldStack.fluid.GetName(container.HeldStack)} {container.RoomUsed}mL";

            textObj.RenderLine(Gui.MouseX + 30, Gui.MouseY, shader, 0, true);
        }
    }
}