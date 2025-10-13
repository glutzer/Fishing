using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Fishing;

public class GuiAlchemyEquipment : Gui
{
    private readonly List<object> parts = [];
    private readonly Action? onClosed;

    public GuiAlchemyEquipment(Action? onClosed = null)
    {
        this.onClosed = onClosed;
    }

    public override void OnGuiClosed()
    {
        base.OnGuiClosed();
        onClosed?.Invoke();
    }

    public override void PopulateWidgets()
    {
        WidgetSliceBackground slice = new(null, GuiThemes.Background, GuiThemes.DarkColor);
        slice.Fixed(0, 0, 200, 100).Alignment(Align.Center);
        AddWidget(slice);

        GuiThemes.AddTitleBar(this, "Alchemy Equipment", slice);

        WidgetContainer elementsContainer = new(slice);
        elementsContainer.Percent(0, 0, 0.8f, 0.8f).Alignment(Align.Center);

        int index = 0;

        foreach (object obj in parts)
        {
            Widget? widget = null;

            if (obj is ItemSlot[] slots)
            {
                widget = new WidgetAlchemyItemGrid(slots, 1, slots.Length, 15, elementsContainer);
                widget.Fixed(index * Scaled(25), 0, 15, 15).PercentHeight(0.8f);
            }

            if (obj is FluidContainer cont)
            {
                widget = new WidgetFluidMeter(elementsContainer, cont);
                widget.Fixed(index * Scaled(25), 0, 15, 15).PercentHeight(0.8f);
            }

            if (obj is Func<bool> func)
            {
                widget = new WidgetProcessingIndicator(elementsContainer, func);
                widget.Fixed(index * Scaled(25), 0, 15, 15);
            }

            if (widget == null) continue;

            widget.Alignment(Align.LeftMiddle);

            index++;
        }

        float totalContainerWidth = (index - 1) * 25f / 0.8f;
        totalContainerWidth += 15f / 0.8f;

        slice.FixedWidth((int)Math.Round(totalContainerWidth));
    }

    /// <summary>
    /// Adds a vertical grid.
    /// </summary>
    public void AddItemGrid(params ItemSlot[] slots)
    {
        parts.Add(slots);
    }

    /// <summary>
    /// Adds a vertical fluid meter.
    /// </summary>
    public void AddFluidMeter(FluidContainer container)
    {
        parts.Add(container);
    }

    /// <summary>
    /// Adds something that begins emitting gui particles when processing.
    /// </summary>
    public void AddProcessingDisplay(Func<bool> isProcessing)
    {
        parts.Add(isProcessing);
    }
}