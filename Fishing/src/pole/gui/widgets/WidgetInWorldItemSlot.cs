using OpenTK.Mathematics;
using System;
using Vintagestory.API.Common;

namespace Fishing;

public class WidgetInWorldItemSlot : WidgetBaseItemGrid
{
    private readonly NineSliceTexture bgTex = VanillaThemes.ItemSlotTexture;
    private readonly Texture blank;
    private readonly Func<Vector3d> getPosDelegate;
    private float sizeMulti;
    private readonly TextObject textObject;
    private readonly TextObject countObject;

    protected override int SlotSize => (int)(base.SlotSize * sizeMulti);

    private readonly Func<bool> shouldRender;
    protected override bool IsEnabled => shouldRender();
    protected bool labelRight;

    public WidgetInWorldItemSlot(ItemSlot[] slots, int width, int height, int slotSize, Widget? parent, Func<Vector3d> getPosDelegate, string label, Func<bool> shouldRender, bool labelRight, Gui gui) : base(slots, width, height, slotSize, parent, gui)
    {
        this.shouldRender = shouldRender;
        this.labelRight = labelRight;
        blank = GuiThemes.Blank;
        this.getPosDelegate = getPosDelegate;
        NoScaling();
        textObject = new TextObject(label, VanillaThemes.Font, 1f, VanillaThemes.WhitishTextColor);
        countObject = new TextObject("", VanillaThemes.Font, 1f, VanillaThemes.WhitishTextColor);

        textObject.Shadow = true;
        countObject.Shadow = true;
    }

    public WidgetInWorldItemSlot(ItemSlot[] slots, int width, int height, int slotSize, Widget? parent, Func<Vector3d> getPosDelegate, string label, bool labelRight, Gui gui) : base(slots, width, height, slotSize, parent, gui)
    {
        shouldRender = () => true;
        this.labelRight = labelRight;

        blank = GuiThemes.Blank;
        this.getPosDelegate = getPosDelegate;
        NoScaling();
        textObject = new TextObject(label, GuiThemes.Font, 1f, GuiThemes.TextColor);
        countObject = new TextObject("", GuiThemes.Font, 1f, GuiThemes.TextColor);
    }

    public override void OnSlotActivated(int slotIndex, ItemSlot slot)
    {
        Vector3d pos = getPosDelegate();
        MainAPI.Capi.World.PlaySoundAt("fishing:sounds/pinpull", pos.X, pos.Y, pos.Z, null, true, 8f);
    }

    public override void RegisterEvents(GuiEvents guiEvents)
    {
        base.RegisterEvents(guiEvents);

        guiEvents.BeforeRender += dt =>
        {
            Vector3d slotPos = getPosDelegate();
            RenderTools.WorldPosToPixelCoords(slotPos, out float x, out float y, out double depth, out bool isBehind);

            double size = Math.Clamp(1f - depth, 0.04f, 0.1f);
            size *= 15f;

            sizeMulti = (float)size;

            FixedPos((int)(x - (SlotSize * width * size)), (int)(y - (SlotSize * height * size)));
            FixedSize((int)(SlotSize * 2 * width * size), (int)(SlotSize * 2 * height * size));
        };
    }

    public override void RenderBackground(Vector2 start, int size, float dt, ShaderGui shader, ItemSlot slot, int slotIndex)
    {
        RenderTools.RenderNineSlice(bgTex, shader, start.X, start.Y, size, size);

        textObject.SetScaleFromWidth(size * 2f, size * 2f, 1f, 0.8f);

        if (labelRight)
        {
            textObject.RenderLine(start.X + size + (size * 0.25f), start.Y + (size / 2f), shader, 0, true);
        }
        else
        {
            textObject.RenderLeftAlignedLine(start.X - (size * 0.25f), start.Y + (size / 2f), shader, true);
        }
    }

    public override void RenderOverlay(Vector2 start, int size, float dt, ShaderGui shader, ItemSlot slot, int slotIndex)
    {
        // Get durability of stack.
        if (slot.Itemstack == null) return;

        if (slot.Itemstack.Collectible.MaxStackSize > 1)
        {
            int slotCount = slot.Itemstack.StackSize;

            countObject.Text = slotCount.ToString();
            countObject.SetScaleFromWidth(size / 2f, size / 2f, 1f, 0.8f);
            countObject.RenderCenteredLine(start.X + (size * 0.75f), start.Y + (size * 0.25f), shader, true);
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