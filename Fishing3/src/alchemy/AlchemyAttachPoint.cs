using OpenTK.Mathematics;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Runtime.CompilerServices;

namespace Fishing;

/// <summary>
/// Defines, in local space a point of an attachment and a size.
/// Used to connect points for alchemy equipment.
/// </summary>
[ProtoContract]
public class AlchemyAttachPoint
{
    [ModuleInitializer]
    internal static void Init()
    {
        RuntimeTypeModel.Default.Add(typeof(Vector3), false)
            .Add("X")
            .Add("Y")
            .Add("Z");
    }

    [ProtoMember(1)]
    public Vector3 Position { get; }

    [ProtoMember(2)]
    public bool IsOutput { get; }

    [ProtoMember(3)]
    public GridPos ConnectedToPos { get; private set; }

    /// <summary>
    /// Index of the point on the other block that this is connected to.
    /// </summary>
    [ProtoMember(4)]
    public int ConnectedToIndex { get; private set; }

    /// <summary>
    /// Offset of the position (in local space) to the attached point, if attached.
    /// </summary>
    [ProtoMember(5)]
    public Vector3 CachedOffset { get; private set; }

    public bool Connected => ConnectedToIndex != -1;

    public AlchemyAttachPoint(Vector3 position, bool isOutput)
    {
        Position = position;
        IsOutput = isOutput;
        ConnectedToIndex = -1;
    }

    [Obsolete("For protobuf only")]
    public AlchemyAttachPoint()
    {

    }

    /// <summary>
    /// Return if connection successful.
    /// </summary>
    public bool Connect(BlockEntityAlchemyEquipment fromBe, BlockEntityAlchemyEquipment targetBe, int index)
    {
        if (index >= targetBe.AlchemyAttachPoints.Length) return false;
        AlchemyAttachPoint targetPoint = targetBe.AlchemyAttachPoints[index];

        if (targetPoint.IsOutput || !IsOutput) return false; // Can only connect from output -> input.

        ConnectedToPos = new GridPos(targetBe.Pos.X, targetBe.Pos.Y, targetBe.Pos.Z);
        ConnectedToIndex = index;

        // Calculate the offset to the other point.
        Vector3 otherPointPos = targetPoint.Position;

        GridPos blockOffset = new(targetBe.Pos.X - fromBe.Pos.X, targetBe.Pos.Y - fromBe.Pos.Y, targetBe.Pos.Z - fromBe.Pos.Z);
        otherPointPos.X += blockOffset.X;
        otherPointPos.Y += blockOffset.Y;
        otherPointPos.Z += blockOffset.Z;

        CachedOffset = otherPointPos - Position;

        return true;
    }

    public void Disconnect()
    {
        ConnectedToPos = default;
        ConnectedToIndex = -1;
    }
}