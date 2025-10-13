using System.Text.Json.Nodes;

namespace Fishing;

[FluidBehavior]
public class FluidBehaviorBlood : FluidBehavior
{
    public FluidBehaviorBlood(JsonObject data) : base(data)
    {
    }

    public override void RegisterEvents(Fluid fluid)
    {
        fluid.EventCanTakeFrom.Register(args =>
        {
            // Blood must be of the same entity type.
            string? sourceEntity = args.sourceStack.Attributes.GetString("entityType");
            string? thisEntity = args.thisStack.Attributes.GetString("entityType");

            // If this blood stack has no entity type, it can take from any blood stack (uninitialized).
            return sourceEntity == thisEntity || thisEntity == null;
        });

        fluid.EventBeforeFluidAddedToOwnStack.Register(args =>
        {
            // Entity ids in blood can't mix.
            long sourceEntityId = args.sourceStack.Attributes.GetLong("entityId", -1);
            long thisEntityId = args.thisStack.Attributes.GetLong("entityId", -1);

            if (sourceEntityId != thisEntityId)
            {
                args.thisStack.Attributes.RemoveAttribute("entityId");
            }

            // New stack.
            if (args.thisStack.Units == 0) args.thisStack.Attributes.SetLong("entityId", sourceEntityId);

            string? sourceEntity = args.sourceStack.Attributes.GetString("entityType");

            if (sourceEntity != null)
            {
                args.thisStack.Attributes.SetString("entityType", sourceEntity);
            }
        });

        fluid.EventGetFluidInfo.Register(args =>
        {
            // Append entity type.
            args.builder.AppendLine($"Blood type: {args.thisStack.Attributes.GetString("entityType", "unknown")} | Tagged: {args.thisStack.Attributes.HasAttribute("entityId")}");
        });
    }
}