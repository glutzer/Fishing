using OpenTK.Mathematics;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Fishing3;

public class CollTester
{
    public CachedCuboidList collisionBoxList = [];
    public Cuboidd entityBox = new();

    public BlockPos minPos = new(0, 0, 0, 0);
    public BlockPos maxPos = new(0, 0, 0, 0);

    public CollTester()
    {
    }

    /// <summary>
    /// Return the new end position.
    /// </summary>
    public Vector3d DoCollision(Vector3d startPos, Vector3d endPos, Entity entity, ICoreAPI api)
    {
        entityBox.SetAndTranslate(entity.CollisionBox, startPos.X, startPos.Y, startPos.Z);
        entityBox.RemoveRoundingErrors();

        Vector3d delta = endPos - startPos;
        EnumPushDirection pushDirection = EnumPushDirection.None;
        GenerateCollisionBoxList(api.World.BlockAccessor, entityBox, delta, 0, 0);

        // Y collision.
        for (int i = 0; i < collisionBoxList.Count; i++)
        {
            delta.Y = collisionBoxList.cuboids[i].pushOutY(entityBox, delta.Y, ref pushDirection);
            if (pushDirection == EnumPushDirection.None) continue;
        }
        entityBox.Translate(0, delta.Y, 0);

        bool horizontallyBlocked = false;
        entityBox.Translate(delta.X, 0, delta.Z);
        foreach (Cuboidd cuboid in collisionBoxList)
        {
            if (cuboid.Intersects(entityBox))
            {
                horizontallyBlocked = true;
                break;
            }
        }
        entityBox.Translate(-delta.X, 0, -delta.Z);

        if (horizontallyBlocked)
        {
            // X collision
            for (int i = 0; i < collisionBoxList.Count; i++)
            {
                delta.X = collisionBoxList.cuboids[i].pushOutX(entityBox, delta.X, ref pushDirection);
                if (pushDirection == EnumPushDirection.None) continue;
            }
            entityBox.Translate(delta.X, 0, 0);

            // Z collision.
            for (int i = 0; i < collisionBoxList.Count; i++)
            {
                delta.Z = collisionBoxList.cuboids[i].pushOutZ(entityBox, delta.Z, ref pushDirection);
                if (pushDirection == EnumPushDirection.None) continue;
            }
        }

        return startPos + delta;
    }

    public void GenerateCollisionBoxList(IBlockAccessor blockAccessor, Cuboidd entityBox, Vector3d delta, float stepHeight, float yExtra)
    {
        // Check if the min and max positions of the collision test are unchanged and use the old list if they are.
        bool minPosIsUnchanged = minPos.SetAndEquals(
            (int)(entityBox.X1 + Math.Min(0, delta.X)),
            (int)(entityBox.Y1 + Math.Min(0, delta.Y) - yExtra), // yExtra looks at blocks below to allow for the extra high collision box of fences.
            (int)(entityBox.Z1 + Math.Min(0, delta.Z))
        );

        double y2 = Math.Max(entityBox.Y1 + stepHeight, entityBox.Y2);

        bool maxPosIsUnchanged = maxPos.SetAndEquals(
            (int)(entityBox.X2 + Math.Max(0, delta.X)),
            (int)(y2 + Math.Max(0, delta.Y)),
            (int)(entityBox.Z2 + Math.Max(0, delta.Z))
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