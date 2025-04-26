using MareLib;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Fishing3;

[Item]
public class ItemFish : ItemFluidStorage, IInFirepitRendererSupplier
{
    public FishSpeciesSystem speciesSystem = null!;
    public FoodNutritionProperties tempFoodProperties = null!;

    // Max container, can't have fluids put into it.
    public override int ContainerCapacity => int.MaxValue;

    public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
    {
        handling = EnumHandHandling.PreventDefault;

        // Only add to container.
        InteractWithSelection(blockSel, false, slot, 100);
    }

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        speciesSystem = MainAPI.GetGameSystem<FishSpeciesSystem>(api.Side);

        if (api.Side == EnumAppSide.Server)
        {
            CreativeInventoryStacks = speciesSystem.GetCreativeStacks();
        }

        tempFoodProperties = new FoodNutritionProperties
        {
            FoodCategory = EnumFoodCategory.Protein,
            Satiety = 100
        };
    }

    public FishSpecies? GetSpecies(ItemStack stack)
    {
        string? species = stack.Attributes.GetString("species") ?? "salmon";
        speciesSystem.TryGetSpecies(species, out FishSpecies? speciesOut);
        return speciesOut;
    }

    public static double GetWeight(ItemStack stack)
    {
        return stack.Attributes.GetDouble("kg", 1f);
    }

    /// <summary>
    /// Consume weight, returns if none left.
    /// </summary>
    public static bool ConsumeWeight(ItemStack stack, float amount)
    {
        double kg = GetWeight(stack);
        kg -= amount;
        kg = Math.Round(kg, 2);
        if (kg <= 0) return false;
        stack.Attributes.SetDouble("kg", kg);
        return true;
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack stack, EnumItemRenderTarget target, ref ItemRenderInfo renderInfo)
    {
        float scale = target switch
        {
            EnumItemRenderTarget.Ground => 6f,
            EnumItemRenderTarget.Gui => 2f,
            _ => 1f
        };

        string? species = stack.Attributes.GetString("species") ?? "salmon";

        if (speciesSystem.TryGetSpecies(species, out FishSpecies? fishSpecies))
        {
            double weight = GetWeight(stack);
            renderInfo.Transform.Scale = GetScale(weight) * scale;

            MultiTextureMeshRef model = GetModel(MainAPI.Capi, fishSpecies, stack.Attributes.GetBool("smoked"));
            renderInfo.ModelRef = model;
        }
    }

    public static float GetScale(double kg)
    {
        return MathF.Sqrt((float)kg) / 4f;
    }

    public static MultiTextureMeshRef GetModel(ICoreClientAPI capi, FishSpecies species, bool smoked)
    {
        return ObjectCacheUtil.GetOrCreate(capi, species.code + smoked.ToString(), () =>
        {
            Shape shape = species.shape.Clone();
            shape.Textures = new Dictionary<string, AssetLocation>(species.shape.Textures);

            if (smoked)
            {
                shape.Textures["scales"] = new AssetLocation("fishing:food/smokedoverlay.png");
                shape.Textures["fins"] = new AssetLocation("fishing:food/smokedoverlay.png");
            }

            ShapeTextureSource textureSource = new(capi, shape, "");

            capi.Tesselator.TesselateShape("", shape, out MeshData meshData, textureSource);
            return capi.Render.UploadMultiTextureMesh(meshData);
        });
    }

    public override float GetMeltingPoint(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
    {
        return 150f;
    }

    public override float GetMeltingDuration(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
    {
        double weight = GetWeight(inputSlot.Itemstack);
        return 30f * (float)weight;
    }

    public override void DoSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot, ItemSlot outputSlot)
    {
        outputSlot.Itemstack = inputSlot.Itemstack;
        inputSlot.TakeOutWhole();
        outputSlot.Itemstack.Attributes.SetBool("smoked", true);
        outputSlot.Itemstack.StackSize = 1;
    }

    public override bool CanSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemStack inputStack, ItemStack outputStack)
    {
        return inputStack.Attributes.GetBool("smoked") == false;
    }

    public override float GetTransitionRateMul(IWorldAccessor world, ItemSlot slot, EnumTransitionType transType)
    {
        // Rot 5x slower when snoked.
        float multi = base.GetTransitionRateMul(world, slot, transType);
        return multi / (slot.Itemstack.Attributes.GetBool("smoked") ? 5f : 1f);
    }

    public override void OnHandbookRecipeRender(ICoreClientAPI capi, GridRecipe recipe, ItemSlot slot, double x, double y, double z, double size)
    {
        // Remove stack count from handbook (since it's 0).
        capi.Render.RenderItemstackToGui(slot, x, y, z, (float)size * 0.58f, -1, showStackSize: false);
    }

    public override FoodNutritionProperties GetNutritionProperties(IWorldAccessor world, ItemStack stack, Entity forEntity)
    {
        double kg = GetWeight(stack);
        bool smoked = stack.Attributes.GetBool("smoked");

        FishSpecies? species = GetSpecies(stack);
        if (species == null)
        {
            tempFoodProperties.Satiety = 0f;
            return tempFoodProperties;
        }

        tempFoodProperties.Satiety = (float)kg * (smoked ? 150f : 50f);
        tempFoodProperties.Satiety *= species.satietyMultiplier;
        return tempFoodProperties;
    }

    public override string GetHeldItemName(ItemStack stack)
    {
        FishSpecies? species = GetSpecies(stack);

        if (species == null) return "Unknown Fish";

        bool smoked = stack.Attributes.GetBool("smoked");
        double kg = GetWeight(stack);

        return smoked
            ? $"{Lang.Get("fishing:smoked")} {Lang.Get($"fishing:species-{species.code}")} ({kg}kg)"
            : $"{Lang.Get($"fishing:species-{species.code}")} ({kg}kg)";
    }

    public override void GetHeldItemInfo(ItemSlot slot, StringBuilder builder, IWorldAccessor world, bool withDebugInfo)
    {
        bool smoked = slot.Itemstack.Attributes.GetBool("smoked");

        if (!smoked) builder.AppendLine("Dryable over a fire.");

        // No debug?
        base.GetHeldItemInfo(slot, builder, world, withDebugInfo);
    }

    /// <summary>
    /// Eat 0.5kg at a time.
    /// </summary>
    protected override void tryEatStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
    {
        FoodNutritionProperties nutritionProperties = GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity);
        if (byEntity.World is not IServerWorldAccessor || nutritionProperties == null || !(secondsUsed >= 0.95f))
        {
            return;
        }

        float spoilState = UpdateAndGetTransitionState(api.World, slot, EnumTransitionType.Perish)?.TransitionLevel ?? 0f;
        float satLossMulti = GlobalConstants.FoodSpoilageSatLossMul(spoilState, slot.Itemstack, byEntity);
        float healthLossMulti = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, slot.Itemstack, byEntity);

        IPlayer player = null!;
        if (byEntity is EntityPlayer entityPlayer)
        {
            player = byEntity.World.PlayerByUid(entityPlayer.PlayerUID);
        }

        double kg = GetWeight(slot.Itemstack);

        if (kg <= 0) return;

        // Only receive saturation for 0.5kg.
        byEntity.ReceiveSaturation(nutritionProperties.Satiety * satLossMulti / ((float)kg * 2), nutritionProperties.FoodCategory);

        if (!ConsumeWeight(slot.Itemstack, 0.5f)) slot.TakeOutWhole();

        float healthMultiplier = nutritionProperties.Health * healthLossMulti;
        float intoxication = byEntity.WatchedAttributes.GetFloat("intoxication");
        byEntity.WatchedAttributes.SetFloat("intoxication", Math.Min(1.1f, intoxication + nutritionProperties.Intoxication));
        if (healthMultiplier != 0f)
        {
            byEntity.ReceiveDamage(new DamageSource
            {
                Source = EnumDamageSource.Internal,
                Type = (healthMultiplier > 0f) ? EnumDamageType.Heal : EnumDamageType.Poison
            }, Math.Abs(healthMultiplier));
        }

        slot.MarkDirty();
        player.InventoryManager.BroadcastHotbarSlot();
    }

    public IInFirepitRenderer GetRendererWhenInFirepit(ItemStack stack, BlockEntityFirepit firepit, bool forOutputSlot)
    {
        return new ItemFishFirepitRenderer((ICoreClientAPI)api, stack, firepit.Pos);
    }

    public EnumFirepitModel GetDesiredFirepitModel(ItemStack stack, BlockEntityFirepit firepit, bool forOutputSlot)
    {
        return EnumFirepitModel.Spit;
    }

    public override string? GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity)
    {
        return null;
    }
}