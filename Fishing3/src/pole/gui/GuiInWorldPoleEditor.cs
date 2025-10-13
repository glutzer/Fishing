using OpenTK.Mathematics;
using Vintagestory.API.Common;

namespace Fishing3;

public class GuiInWorldPoleEditor : Gui
{
    private readonly ItemInventory rodSlot;

    public GuiInWorldPoleEditor(ItemInventory rodSlot)
    {
        this.rodSlot = rodSlot;
    }

    public override void OnGuiOpened()
    {
        base.OnGuiOpened();

        MainAPI.Capi.World.Player.Entity.AnimManager.StartAnimation("InspectRod");
    }

    public override void OnGuiClosed()
    {
        base.OnGuiClosed();

        MainAPI.Capi.World.Player.Entity.AnimManager.StopAnimation("InspectRod");

        MainAPI.Capi.World.Player.InventoryManager.CloseInventory(rodSlot);

        FishingGameSystem sys = MainAPI.GetGameSystem<FishingGameSystem>(EnumAppSide.Client);

        FishingInventoryPacket packet = new()
        {
            openInventory = false
        };

        sys.SendPacket(packet);
    }

    public override void PopulateWidgets()
    {
        AddWidget(new WidgetInWorldItemSlot(new ItemSlot[] { rodSlot[3] }, 1, 1, 96, null, () =>
        {
            return ItemFishingPole.GetSwayedPosition(MainAPI.Capi.World.Player.Entity, 2f);
        }, "Catch", () =>
        {
            return rodSlot[3].Itemstack != null;
        }, false));

        AddWidget(new WidgetInWorldItemSlot(new ItemSlot[] { rodSlot[2] }, 1, 1, 96, null, () =>
        {
            return ItemFishingPole.GetSwayedPosition(MainAPI.Capi.World.Player.Entity, 2f);
        }, "Bait", () =>
        {
            return rodSlot[0].Itemstack != null && rodSlot[3].Itemstack == null;
        }, false));

        AddWidget(new WidgetInWorldItemSlot(new ItemSlot[] { rodSlot[1] }, 1, 1, 96, null, () =>
        {
            return ItemFishingPole.GetSwayedPosition(MainAPI.Capi.World.Player.Entity, 1f);
        }, "Bobber", () =>
        {
            return rodSlot[0].Itemstack != null && rodSlot[3].Itemstack == null;
        }, false));

        AddWidget(new WidgetInWorldItemSlot(new ItemSlot[] { rodSlot[0] }, 1, 1, 96, null, () =>
        {
            AnimationUtility.GetRightHandPosition(MainAPI.Capi.World.Player.Entity, new Vector3(0.5f - 2, 0, 0.5f), out Vector3d position);
            return position;
        }, "Line", () =>
        {
            return rodSlot[3].Itemstack == null;
        }, true));
    }
}