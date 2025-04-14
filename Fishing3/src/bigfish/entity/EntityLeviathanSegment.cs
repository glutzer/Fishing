using MareLib;
using OpenTK.Mathematics;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace Fishing3;

[Entity]
public class EntityLeviathanSegment : EntityLeviathanBase
{
    public override void OnGameTick(float dt)
    {
        base.OnGameTick(dt);

        if (Api.Side == EnumAppSide.Server && ParentSegment == null)
        {
            Die(EnumDespawnReason.Expire);
        }
    }

    public void CascadingPhysicsTick(float dt)
    {
        if (ParentSegment == null) return;

        Vector3d parentPos = ParentSegment.ServerPos.ToVector();
        Vector3d pos = ServerPos.ToVector();

        // Need to move the segment to it's parent, minus the parent size. Only if it's > 0.
        Vector3d delta = parentPos - pos;
        Vector3d normal = delta.Normalized();
        double desiredDistance = delta.Length - 10;

        if (desiredDistance > 0.1f)
        {
            ServerPos.Add(desiredDistance * normal.X, desiredDistance * normal.Y, desiredDistance * normal.Z);

            // Set rotation.
            delta = parentPos - pos;
            normal = delta.Normalized();
            ServerPos.Yaw = (float)Math.Atan2(-normal.X, -normal.Z);
            ServerPos.Pitch = (float)Math.Asin(normal.Y);
        }
    }

    public override bool ReceiveDamage(DamageSource damageSource, float damage)
    {
        // Lower damage to tail.
        float damageMultiplier = 1f - (SegmentId / (float)MAX_SEGMENTS);
        Head?.ReceiveDamage(damageSource, damage * damageMultiplier * 0.5f);

        if ((!Alive || IsActivityRunning("invulnerable")) && damageSource.Type != EnumDamageType.Heal) return false;

        if (ShouldReceiveDamage(damageSource, damage))
        {
            foreach (EntityBehavior behavior in SidedProperties.Behaviors)
            {
                behavior.OnEntityReceiveDamage(damageSource, ref damage);
            }

            if (damageSource.Type != EnumDamageType.Heal && damage > 0f)
            {
                WatchedAttributes.SetInt("onHurtCounter", WatchedAttributes.GetInt("onHurtCounter") + 1);
                WatchedAttributes.SetFloat("onHurt", damage);
                if (damage > 0.05f)
                {
                    AnimManager.StartAnimation("hurt");
                }
            }

            if (damageSource.GetSourcePosition() != null)
            {

            }

            return damage > 0f;
        }

        EntityBehaviorHealth? behHealth = GetBehavior<EntityBehaviorHealth>();
        if (behHealth != null)
        {
            behHealth.Health = 200f;
        }

        return false;
    }
}