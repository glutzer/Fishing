namespace Fishing3;

[Effect]
public class EffectIgnition : AlchemyEffect
{
    public override void ApplyInstantEffect()
    {
        Entity.Ignite();
    }
}