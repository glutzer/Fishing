using OpenTK.Mathematics;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Fishing;

public class EntityLeviathanBase : EntityAgent
{
    public const int MAX_SEGMENTS = 40;

    public int SegmentId { get; set; }
    public EntityLeviathanHead? Head { get; set; }
    public EntityLeviathanBase? ParentSegment { get; set; }

    public override bool AllowOutsideLoadedRange => true;

    public override void SetSelectionBox(float length, float height)
    {
        SelectionBox = new Cuboidf
        {
            X1 = -length / 2f,
            Z1 = -length / 2f,
            X2 = length / 2f,
            Z2 = length / 2f,
            Y1 = -height / 2f,
            Y2 = height / 2f
        };

        OriginSelectionBox = SelectionBox.Clone();
    }

    public override void SetCollisionBox(float length, float height)
    {
        CollisionBox = new Cuboidf
        {
            X1 = -length / 2f,
            Z1 = -length / 2f,
            X2 = length / 2f,
            Z2 = length / 2f,
            Y1 = -height / 2f,
            Y2 = height / 2f
        };

        OriginCollisionBox = CollisionBox.Clone();
    }

    public void MoveToSegment(EntityLeviathanBase? segment)
    {
        segment ??= ParentSegment;

        if (segment == null) return;

        Vector3d parentPos = segment.ServerPos.ToVector();
        Vector3d pos = ServerPos.ToVector();

        Vector3d delta = parentPos - pos;
        Vector3d normal = delta.Normalized();

        double desiredDistance = delta.Length - 10;

        if (desiredDistance > 0.1f)
        {
            ServerPos.Add(desiredDistance * normal.X, desiredDistance * normal.Y, desiredDistance * normal.Z);
        }

        // Set rotation.
        delta = parentPos - pos;
        normal = delta.Normalized();
        ServerPos.Yaw = (float)Math.Atan2(-normal.X, -normal.Z);
        ServerPos.Pitch = (float)Math.Asin(normal.Y);
    }

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public void FaceToParent()
    //{
    //    if (ParentSegment == null) return;

    //    Vector3d facingNormal = new(ParentSegment.ServerPos.X - ServerPos.X, ParentSegment.ServerPos.Y - ServerPos.Y, ParentSegment.ServerPos.Z - ServerPos.Z);
    //    facingNormal.Normalize();
    //    ServerPos.Yaw = (float)Math.Atan2(-facingNormal.X, -facingNormal.Z);
    //    ServerPos.Pitch = (float)Math.Asin(facingNormal.Y);
    //}
}