using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Fishing;

public class CollectibleBehaviorBobber : CollectibleBehavior
{
    // Bobber class to be created.
    public string bobberType = "BobberReelable";
    public float modelScale = 1f;

    // Properties given to the bobber behavior.
    public JsonObject properties = null!;

    public CollectibleBehaviorBobber(CollectibleObject collObj) : base(collObj)
    {
    }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        this.properties = properties;

        if (properties["bobberType"].Exists)
        {
            bobberType = properties["bobberType"].AsString("BobberReelable");
        }

        if (properties["modelScale"].Exists)
        {
            modelScale = properties["modelScale"].AsFloat(1f);
        }
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder builder, IWorldAccessor world, bool withDebugInfo)
    {
        builder.AppendLine($"Bobber type: {Lang.Get($"fishing:bobber-{bobberType}")}");
    }
}