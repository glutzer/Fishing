using MareLib;
using OpenTK.Mathematics;
using Vintagestory.API.Common;

namespace Fishing3;

[BlockEntity]
public class BlockEntityAlembic : BlockEntityHeatedAlchemyEquipment
{
    public override void Initialize(ICoreAPI api)
    {
        AlchemyAttachPoints = new[]
        {
            new AlchemyAttachPoint(new Vector3(0.2f, 0.7f, 0.5f), false),
            new AlchemyAttachPoint(new Vector3(0.5f, 1f, 0.5f), true)
        };

        base.Initialize(api);
    }
}