namespace Fishing3;

[Effect]
public class EffectMoveSpeed : AlchemyEffect
{
    public override float BaseDuration => 30f;
    public override EffectType Type => EffectType.Duration;

    public override void OnLoaded()
    {
        // 10% increased movement speed at baseline.
        Entity.Stats.Set("walkspeed", "potioneffect", StrengthMultiplier * 0.1f, true);
    }

    public override void OnUnloaded()
    {
        Entity.Stats.Remove("walkspeed", "potioneffect");
    }
}