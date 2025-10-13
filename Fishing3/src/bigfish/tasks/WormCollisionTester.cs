using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Fishing;

public class WormCollisionTester
{
    public CachedCuboidList collisionBoxList = [];
    public Cuboidd entityBox = new();

    public BlockPos minPos = new(0, 0, 0, 0);
    public BlockPos maxPos = new(0, 0, 0, 0);

    public WormCollisionTester()
    {
    }

    /// <summary>
    /// Return if this entity is colliding with the ground.
    /// </summary>
    public bool IsColliding(Entity entity, ICoreAPI api)
    {
        entityBox.SetAndTranslate(entity.CollisionBox, entity.SidedPos.X, entity.SidedPos.Y, entity.SidedPos.Z);
        entityBox.RemoveRoundingErrors();

        GenerateCollisionBoxList(api.World.BlockAccessor, entityBox);

        for (int i = 0; i < collisionBoxList.Count; i++)
        {
            if (collisionBoxList.cuboids[i].Intersects(entityBox)) return true;
        }

        return false;
    }

    public void GenerateCollisionBoxList(IBlockAccessor blockAccessor, Cuboidd entityBox)
    {
        // Check if the min and max positions of the collision test are unchanged and use the old list if they are.
        bool minPosIsUnchanged = minPos.SetAndEquals(
            (int)entityBox.X1,
            (int)entityBox.Y1,
            (int)entityBox.Z1
        );

        bool maxPosIsUnchanged = maxPos.SetAndEquals(
            (int)entityBox.X2,
            (int)entityBox.Y1,
            (int)entityBox.Z2
        );

        if (minPosIsUnchanged && maxPosIsUnchanged) return;

        BlockPos tempPos = new(0, 0, 0, 0);

        // Clear the list and add every cuboid the block has to it.
        collisionBoxList.Clear();
        blockAccessor.WalkBlocks(minPos, maxPos, (block, x, y, z) =>
        {
            Cuboidf[] collisionBoxes = block.GetCollisionBoxes(blockAccessor, tempPos.Set(x, y, z));
            if (collisionBoxes != null)
            {
                collisionBoxList.Add(collisionBoxes, x, y, z, block);
            }
        }, true);
    }
}