using MareLib;
using OpenTK.Mathematics;
using Vintagestory.API.Common;

namespace Fishing3;

public class GuiFlotsam : Gui
{
    private readonly BlockEntityFlotsam flotsam;
    private NineSliceTexture? flotsamTex;

    public GuiFlotsam(BlockEntityFlotsam flotsam)
    {
        this.flotsam = flotsam;
    }

    public override void OnGuiOpened()
    {
        // Make tex.
        flotsamTex = Texture.Create("fishing:textures/gui/flotsam.png").AsNineSlice(6, 6);

        base.OnGuiOpened();
    }

    public override void OnGuiClosed()
    {
        base.OnGuiClosed();

        flotsam.genericInventory.Close(MainAPI.Capi.World.Player);
        flotsam.SendClosePacketFromClient();

        // Delete tex.
        flotsamTex?.Dispose();
        flotsamTex = null;
    }

    public override void PopulateWidgets()
    {
        new WidgetSliceBackground(null, flotsamTex!, Vector4.One)
            .Alignment(Align.Center)
            .Fixed(0, 0, 90, 90)
            .As(out WidgetSliceBackground bg);
        bg.SliceScale = MainAPI.GuiScale;

        GuiThemes.AddTitleBar(this, "Flotsam", bg);

        AddWidget(bg);

        ItemSlot[] slots = flotsam.genericInventory.GetField<ItemSlot[]>("slots");

        new WidgetAlchemyItemGrid(slots, 3, 3, 16, bg)
            .Percent(0f, 0f, 0.8f, 0.8f)
            .Alignment(Align.Center);
    }
}