using MareLib;

namespace Fishing3;

[Effect]
public class MetaEffectBlood : AlchemyEffect, IMetaEffect
{
    // Must be 10% of the fluid to apply to it.
    public float BaseRatio => 0.1f;

    public string EntityType { get; set; } = "none";
    public long EntityId { get; set; } = -1;

    public void ApplyTo(Effect effect, float ratioStrengthMultiplier)
    {
        if (ratioStrengthMultiplier < 1f) return;

        if (effect is IBloodBound bloodBound)
        {
            bloodBound.EntityType = EntityType;
            bloodBound.EntityId = EntityId;
        }
    }

    public override void CollectDataFromFluidStack(FluidStack stack)
    {
        EntityType = stack.Attributes.GetString("entityType", "none");
        EntityId = stack.Attributes.GetLong("entityId", -1);
    }
}