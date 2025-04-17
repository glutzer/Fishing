using MareLib;
using OpenTK.Mathematics;
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

    private void DoIK()
    {
        if (Head == null) return;

        EntityLeviathanBase[] segments = Head.segments;

        // Move segments from front.
        EntityLeviathanBase lastSegment = this;
        for (int i = SegmentId; i > 0; i--)
        {
            EntityLeviathanBase segment = segments[i];

            segment.MoveToSegment(lastSegment);
            lastSegment = segment;
        }

        // Move segments from back.
        lastSegment = this;
        for (int i = SegmentId + 1; i < segments.Length; i++)
        {
            EntityLeviathanBase segment = segments[i];

            segment.MoveToSegment(lastSegment);
            lastSegment = segment;
        }

        // Move the head
        lastSegment = segments[0];
        for (int i = 1; i < segments.Length; i++)
        {
            EntityLeviathanBase segment = segments[i];

            segment.MoveToSegment(lastSegment);
            lastSegment = segment;
        }
    }

    public void MoveToWithIK(Vector3d position)
    {
        ServerPos.SetPos(position.X, position.Y, position.Z);
        if (ServerPos.Y < 1) ServerPos.Y = 1;
        DoIK();
    }

    public void MoveWithIK(Vector3 movement)
    {
        ServerPos.Add(movement.X, movement.Y, movement.Z);
        if (ServerPos.Y < 1) ServerPos.Y = 1;
        DoIK();
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