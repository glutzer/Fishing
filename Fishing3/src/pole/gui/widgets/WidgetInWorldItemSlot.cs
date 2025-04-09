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
    private readonly TextObject countObject;

    protected override int SlotSize => (int)(base.SlotSize * sizeMulti);

    private readonly Func<bool> shouldRender;
    protected override bool IsEnabled => shouldRender();
    protected bool labelRight;

    public WidgetInWorldItemSlot(ItemSlot[] slots, int width, int height, int slotSize, Widget? parent, Func<Vector3d> getPosDelegate, string label, Func<bool> shouldRender, bool labelRight) : base(slots, width, height, slotSize, parent)
    {
        this.shouldRender = shouldRender;
        this.labelRight = labelRight;

        bgTex = GuiThemes.Button;
        blank = GuiThemes.Blank;
        this.getPosDelegate = getPosDelegate;
        NoScaling();
        textObject = new TextObject(label, GuiThemes.Font, 50, GuiThemes.TextColor);
        countObject = new TextObject("", GuiThemes.Font, 50, GuiThemes.TextColor);
    }

    public WidgetInWorldItemSlot(ItemSlot[] slots, int width, int height, int slotSize, Widget? parent, Func<Vector3d> getPosDelegate, string label, bool labelRight) : base(slots, width, height, slotSize, parent)
    {
        shouldRender = () => true;
        this.labelRight = labelRight;

        bgTex = GuiThemes.Button;
        blank = GuiThemes.Blank;
        this.getPosDelegate = getPosDelegate;
        NoScaling();
        textObject = new TextObject(label, GuiThemes.Font, 50, GuiThemes.TextColor);
        countObject = new TextObject("", GuiThemes.Font, 50, GuiThemes.TextColor);
    }

    public override void OnSlotActivated(int slotIndex, ItemSlot slot)
    {
        Vector3d pos = getPosDelegate();
        MainAPI.Capi.World.PlaySoundAt("fishing:sounds/pinpull", pos.X, pos.Y, pos.Z, null, true, 8);
    }

    public override void RegisterEvents(GuiEvents guiEvents)
    {
        base.RegisterEvents(guiEvents);

        guiEvents.BeforeRender += dt =>
        {
            Vector3d slotPos = getPosDelegate();
            RenderTools.WorldPosToPixelCoords(slotPos, out int x, out int y, out float depth);

            float size = Math.Clamp(1f - depth, 0.04f, 0.1f);
            size *= 15f;

            sizeMulti = size;

            FixedPos((int)(x - (SlotSize * width * size)), (int)(y - (SlotSize * height * size)));
            FixedSize((int)(SlotSize * 2 * width * size), (int)(SlotSize * 2 * height * size));
            SetBounds();

            textObject.SetScale((int)(size * 50));
            countObject.SetScale((int)(size * 30));
        };
    }

    public override void RenderBackground(Vector2 start, int size, float dt, MareShader shader, ItemSlot slot, int slotIndex)
    {
        shader.Uniform("color", MousedSlotIndex == slotIndex ? GuiThemes.DarkColor * 1.5f : GuiThemes.DarkColor);
        RenderTools.RenderNineSlice(bgTex, shader, start.X, start.Y, size, size);
        shader.Uniform("color", Vector4.One);

        if (labelRight)
        {
            textObject.RenderLine(start.X + size, start.Y + (size / 2), shader, 0, true);
        }
        else
        {
            textObject.RenderLeftAlignedLine(start.X, start.Y + (size / 2), shader, true);
        }
    }

    public override void RenderOverlay(Vector2 start, int size, float dt, MareShader shader, ItemSlot slot, int slotIndex)
    {
        // Get durability of stack.
        if (slot.Itemstack == null) return;

        if (slot.Itemstack.Collectible.MaxStackSize > 1)
        {
            countObject.Text = $"x{slot.Itemstack.StackSize}";
            countObject.RenderLeftAlignedLine(start.X + (size * 0.9f), start.Y + (size * 0.2f), shader, true);
        }

        int maxDurability = slot.Itemstack.Collectible.GetMaxDurability(slot.Itemstack);
        if (maxDurability == 1) return;
        int currentDurability = slot.Itemstack.Collectible.GetRemainingDurability(slot.Itemstack);
        if (currentDurability == maxDurability) return;

        float ratio = currentDurability / (float)maxDurability;

        // Lerp between red and green based on ratio.
        Vector3 lerpedColor = Vector3.Lerp(GuiThemes.Red, GuiThemes.Green, ratio);
        shader.Uniform("color", new Vector4(lerpedColor, 0.5f));

        shader.BindTexture(blank, "tex2d");

        // Durability.
        RenderTools.RenderQuad(shader, start.X, start.Y + (size * 0.95f), size * ratio, size * 0.05f);

        shader.Uniform("color", Vector4.One);
    }
}