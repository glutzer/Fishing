using Vintagestory.API.Common.Entities;

namespace Fishing3;

[Effect]
public class EffectTranslocation : AlchemyEffect, IBloodBound
{
    public string? EntityType { get; set; }
    public long EntityId { get; set; }

    public override int MinimumVolume => 500;

    public override void ApplyInstantEffect()
    {
        if (EntityId != -1 && MainAPI.Sapi.World.GetEntityById(EntityId) is Entity entity)
        {
            Entity.TeleportToDouble(entity.Pos.X, entity.Pos.Y, entity.Pos.Z);
            MainAPI.Sapi.World.PlaySoundAt("fishing:sounds/teleport", Entity.Pos.X, Entity.Pos.Y, Entity.Pos.Z);
        }
    }
}