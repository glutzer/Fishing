namespace Fishing3;

[Effect]
public class MetaEffectBlood : AlchemyEffect, IMetaEffect
{
    public float BaseRatio => 0.1f;

    public string EntityType { get; set; } = "none";
    public long EntityId { get; set; } = -1;

    public void ApplyTo(Effect effect, float ratioStrengthMultiplier)
    {
        // Must be full strength to apply.
        if (ratioStrengthMultiplier < 1f) return;

        if (effect is IBloodBound bloodBound)
        {
            bloodBound.EntityType = EntityType;
            bloodBound.EntityId = EntityId;
        }
    }

    public override void CollectDataFromFluidStack(FluidStack stack, ApplicationMethod method)
    {
        EntityType = stack.Attributes.GetString("entityType", "none");
        EntityId = stack.Attributes.GetLong("entityId", -1);
    }
}