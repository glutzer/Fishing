using MareLib;
using OpenTK.Mathematics;
using System;
using Vintagestory.API.Common;

namespace Fishing3;

public class WidgetAlchemyItemGrid : WidgetBaseItemGrid
{
    private readonly NineSliceTexture tex;
    private readonly Texture blank;
    private readonly TextObject countObject;
    private readonly string[] slotSounds;

    public WidgetAlchemyItemGrid(ItemSlot[] slots, int width, int height, int slotSize, Widget? parent, string[]? slotSounds = null) : base(slots, width, height, slotSize, parent)
    {
        tex = GuiThemes.Button;
        blank = GuiThemes.Blank;
        this.slotSounds = slotSounds ?? new[] { "fishing:sounds/pinpull" };

        countObject = new TextObject("", GuiThemes.Font, slotSize, GuiThemes.TextColor)
        {
            Shadow = true
        };
    }

    public override void RenderBackground(Vector2 start, int size, float dt, MareShader shader, ItemSlot slot, int slotIndex)
    {
        shader.Uniform("color", MousedSlotIndex == slotIndex ? GuiThemes.DarkColor * 4f : GuiThemes.DarkColor * 2);
        RenderTools.RenderNineSlice(tex, shader, start.X, start.Y, size, size);
        shader.Uniform("color", Vector4.One);
    }

    public override void OnSlotActivated(int slotIndex, ItemSlot slot)
    {
        MainAPI.Capi.World.PlaySoundAt(slotSounds[Random.Shared.Next(slotSounds.Length)], MainAPI.Capi.World.Player);
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