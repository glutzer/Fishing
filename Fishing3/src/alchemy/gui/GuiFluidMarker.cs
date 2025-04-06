using MareLib;
using Vintagestory.API.Common;

namespace Fishing3;

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
        new WidgetFluidMarker(null, MaxFluid, CurrentMark, OnNewValue).Alignment(Align.Center).Fixed(0, 0, 100, 10).As<WidgetFluidMarker>(out WidgetFluidMarker? widget);
        AddWidget(widget);
    }

    private static void OnNewValue(int value)
    {
        MainAPI.GetGameSystem<AlchemyConnectionSystem>(EnumAppSide.Client).SendPacket(new AlchemyMarkerPacket() { mark = value });
    }
}