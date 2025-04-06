using MareLib;
using Vintagestory.API.Common;

namespace Fishing3;

[Bobber]
public class BobberFlask : BobberReelable
{
    public BobberFlask(EntityBobber bobber, bool isServer) : base(bobber, isServer)
    {
    }

    // Placeholder using bomb behavior, remake to splash.

    public override void OnAttackStart(bool isServer, ItemSlot rodSlot, EntityPlayer player)
    {
        base.OnAttackStart(isServer, rodSlot, player);

        // A better blast system needs to be put in place, with less particles and claim checking (this currently bypasses it completely).
        if (isServer && player.Controls.ShiftKey)
        {
            bobber.Die();
            MainAPI.Sapi.World.PlaySoundAt("fishing:sounds/pinpull", player, null, true, 16);
            MainAPI.Sapi.World.CreateExplosion(bobber.ServerPos.AsBlockPos, EnumBlastType.EntityBlast, 4, 4, 0.1f);

            ItemFishingPole.ReadStack(1, rodSlot.Itemstack, MainAPI.Sapi, out ItemStack? bobberStack);

            if (bobberStack == null || bobberStack.StackSize == 1)
            {
                ItemFishingPole.SetStack(1, rodSlot.Itemstack, null);
            }
            else
            {
                bobberStack.StackSize--;
                ItemFishingPole.SetStack(1, rodSlot.Itemstack, bobberStack);
            }

            rodSlot.MarkDirty();
        }
    }
}