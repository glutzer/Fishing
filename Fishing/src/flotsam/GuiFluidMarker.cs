using Vintagestory.API.Common;

namespace Fishing;

public class GuiNamedFluidMarker : GuiFluidMarker
{
    private string name = "";

    public GuiNamedFluidMarker(ItemStack fluidContainerStack) : base(fluidContainerStack)
    {
        name = fluidContainerStack.Attributes.GetString("label", fluidContainerStack.GetName());
    }

    public override void OnGuiClosed()
    {
        base.OnGuiClosed();

        MainAPI.GetGameSystem<FluidItemPacketSystem>(EnumAppSide.Client).SendPacket(new AlchemyFlaskNamePacket() { name = name });
    }

    public override void PopulateWidgets()
    {
        new WidgetFluidMarker(null, this, MaxFluid, CurrentMark, OnNewValue)
            .Alignment(Align.Center)
            .Fixed(0, 0, 100, 10)
            .As<WidgetFluidMarker>(out WidgetFluidMarker? widget);
        AddWidget(widget);

        VanillaThemes.AddTitleBar(widget, "Mark");

        new WidgetVanillaTextInputBox(widget, this, false, true, text =>
        {
            name = text;
        }, name, "Container Label").Alignment(Align.CenterBottom, AlignFlags.OutsideV).PercentSize(1f, 1f);
    }
}

public class GuiFluidMarker : Gui
{
    public int MaxFluid { get; private set; } = 10;
    public int CurrentMark { get; private set; } = 10;

    public GuiFluidMarker(ItemStack fluidContainerStack) : base()
    {
        if (fluidContainerStack.Collectible is not ItemFluidStorage itemFluidStorage) return;

        FluidContainer container = itemFluidStorage.GetContainer(fluidContainerStack);

        MaxFluid = container.Capacity;

        CurrentMark = fluidContainerStack.Attributes.GetInt("mark", MaxFluid);
    }

    public override void PopulateWidgets()
    {
        new WidgetFluidMarker(null, this, MaxFluid, CurrentMark, OnNewValue)
            .Alignment(Align.Center)
            .Fixed(0, 0, 100, 10)
            .As<WidgetFluidMarker>(out WidgetFluidMarker? widget);

        VanillaThemes.AddTitleBar(widget, "Mark");

        AddWidget(widget);
    }

    protected static void OnNewValue(int value)
    {
        MainAPI.GetGameSystem<FluidItemPacketSystem>(EnumAppSide.Client).SendPacket(new AlchemyMarkerPacket() { mark = value });
    }
}