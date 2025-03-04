using Guilds;
using MareLib;
using OpenTK.Mathematics;
using System;
using Vintagestory.API.Common;

namespace Fishing3;

public class WidgetInWorldItemSlot : WidgetBaseItemGrid
{
    private readonly NineSliceTexture bgTex;
    private readonly Texture blank;
    private readonly Func<Vector3d> getPosDelegate;
    private float sizeMulti;
    private readonly TextObject textObject;

    protected override int SlotSize => (int)(base.SlotSize * sizeMulti);

    private readonly Func<bool> shouldRender;
    protected override bool IsEnabled => shouldRender();

    public WidgetInWorldItemSlot(ItemSlot[] slots, int width, int height, int slotSize, Widget? parent, Func<Vector3d> getPosDelegate, string label, Func<bool> shouldRender) : base(slots, width, height, slotSize, parent)
    {
        this.shouldRender = shouldRender;

        bgTex = GuiThemes.Background;
        blank = GuiThemes.Blank;
        this.getPosDelegate = getPosDelegate;
        NoScaling();
        textObject = new TextObject(label, GuiThemes.Font, 50, GuiThemes.TextColor);
    }

    public WidgetInWorldItemSlot(ItemSlot[] slots, int width, int height, int slotSize, Widget? parent, Func<Vector3d> getPosDelegate, string label) : base(slots, width, height, slotSize, parent)
    {
        shouldRender = () => true;

        bgTex = GuiThemes.Background;
        blank = GuiThemes.Blank;
        this.getPosDelegate = getPosDelegate;
        NoScaling();
        textObject = new TextObject(label, GuiThemes.Font, 50, GuiThemes.TextColor);
    }

    public override void RegisterEvents(GuiEvents guiEvents)
    {
        base.RegisterEvents(guiEvents);

        guiEvents.BeforeRender += dt =>
        {
            Vector3d slotPos = getPosDelegate();
            RenderTools.WorldPosToPixelCoords(slotPos, out int x, out int y, out float depth);

            float size = Math.Clamp(1f - depth, 0.05f, 0.15f);
            size *= 10f;

            sizeMulti = size;

            FixedPos((int)(x - (SlotSize * width * size)), (int)(y - (SlotSize * height * size)));
            FixedSize((int)(SlotSize * 2 * width * size), (int)(SlotSize * 2 * height * size));
            SetBounds();

            textObject.SetScale((int)(size * 50));
        };
    }

    public override void RenderBackground(Vector2 start, int size, float dt, MareShader shader, ItemSlot slot)
    {
        shader.Uniform("color", GuiThemes.DarkColor);
        RenderTools.RenderNineSlice(bgTex, shader, start.X, start.Y, size, size);
        shader.Uniform("color", Vector4.One);

        textObject.RenderLine(start.X + size, start.Y + (size / 2), shader, 0, true);
    }

    public override void RenderOverlay(Vector2 start, int size, float dt, MareShader shader, ItemSlot slot)
    {
        // Get durability of stack.
        if (slot.Itemstack == null) return;
        int maxDurability = slot.Itemstack.Collectible.GetMaxDurability(slot.Itemstack);
        if (maxDurability == 1) return;
        int currentDurability = slot.Itemstack.Collectible.GetRemainingDurability(slot.Itemstack);
        if (currentDurability == maxDurability) return;

        float ratio = currentDurability / (float)maxDurability;

        // Lerp between red and green based on ratio.
        Vector3 lerpedColor = Vector3.Lerp(GuiThemes.Red, GuiThemes.Green, ratio);
        shader.Uniform("color", new Vector4(lerpedColor, 0.5f));

        shader.BindTexture(blank, "tex2d");

        RenderTools.RenderQuad(shader, start.X, start.Y + (size * 0.95f), size * ratio, size * 0.05f);

        shader.Uniform("color", Vector4.One);
    }
}