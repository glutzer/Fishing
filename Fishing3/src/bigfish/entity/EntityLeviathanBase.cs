using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Fishing3;

public class EntityLeviathanBase : EntityAgent
{
    public const int MAX_SEGMENTS = 80;

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
}