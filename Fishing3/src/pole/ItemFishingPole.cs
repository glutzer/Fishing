using MareLib;
using OpenTK.Mathematics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Fishing3;

[Item]
public partial class ItemFishingPole : Item
{
    public MeshHandle? cubeMesh;
    public MareShader? shader;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        if (api is ICoreClientAPI)
        {
            shader = MareShaderRegistry.Get("debugfishing");
            cubeMesh = CubeMeshUtility.CreateCenteredCubeMesh(v => new StandardVertex(v.position, v.uv, v.normal, Vector4.One));
        }
    }

    // This is only called on the using client, then the server.

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        // Only do initial use.
        if (!firstEvent) return;
        if (byEntity is not EntityPlayer player) return;
        EntityBobber? currentBobber = TryGetBobber(slot, api);

        if (api.Side == EnumAppSide.Client)
        {
            if (currentBobber != null)
            {
                byEntity.AnimManager.StartAnimation("LineReel");
            }
            else
            {
                if (byEntity.Controls.Sneak)
                {
                    FishingGameSystem sys = MainAPI.GetGameSystem<FishingGameSystem>(EnumAppSide.Client);

                    FishingInventoryPacket packet = new()
                    {
                        openInventory = true
                    };

                    sys.SendPacket(packet);
                    return;
                }
            }
        }
        else
        {
            if (currentBobber != null)
            {
                byEntity.AnimManager.StartAnimation("LineReel");
                currentBobber.OnUseStart(true, slot, player);
                currentBobber.BroadcastPacket(RodUseType.UseStart, player);
                return;
            }
            else
            {
                // Begin charging.
            }
        }

        handling = EnumHandHandling.Handled;
    }

    public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        return true;
    }

    public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        if (byEntity is not EntityPlayer player) return;
        EntityBobber? currentBobber = TryGetBobber(slot, api);

        if (api.Side == EnumAppSide.Client)
        {
            if (currentBobber != null)
            {
                byEntity.AnimManager.StopAnimation("LineReel");
            }
            else
            {

            }
        }
        else
        {
            if (currentBobber != null)
            {
                byEntity.AnimManager.StartAnimation("LineReel");
                currentBobber.OnUseEnd(true, slot, player);
                currentBobber.BroadcastPacket(RodUseType.UseEnd, player);
                return;
            }
            else
            {

            }
        }
    }

    public override string? GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
    {
        return null;
    }

    public override string? GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
    {
        return null;
    }

    public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
    {
        // Only do initial use.
        if (byEntity is not EntityPlayer player) return;
        EntityBobber? currentBobber = TryGetBobber(slot, api);

        if (api.Side == EnumAppSide.Client)
        {
            // Nothing particular.
        }
        else
        {
            if (currentBobber != null)
            {
                currentBobber.OnAttackStart(true, slot, player);
                currentBobber.BroadcastPacket(RodUseType.AttackStart, player);
                return;
            }
        }

        handling = EnumHandHandling.Handled;
    }

    public override bool OnHeldAttackStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
    {
        return true;
    }

    public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
    {
        if (byEntity is not EntityPlayer player) return;
        EntityBobber? currentBobber = TryGetBobber(slot, api);

        if (api.Side == EnumAppSide.Client)
        {
            // Nothing particular.
        }
        else
        {
            if (currentBobber != null)
            {
                currentBobber.OnAttackEnd(true, slot, player);
                currentBobber.BroadcastPacket(RodUseType.AttackEnd, player);
                return;
            }
        }
    }
}