namespace Fishing;

[Effect]
public class EffectIgnition : AlchemyEffect
{
    public override void ApplyInstantEffect()
    {
        Entity.Ignite();
    }
}