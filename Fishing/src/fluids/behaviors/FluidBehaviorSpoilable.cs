using System.Text.Json.Nodes;
using Vintagestory.API.Common;

namespace Fishing;

[FluidBehavior]
public class FluidBehaviorSpoilable : FluidBehavior
{
    public readonly long spoilMs;

    public FluidBehaviorSpoilable(JsonObject data) : base(data)
    {
        double spoilHours = data.Get<double>("SpoilHours", 24);
        spoilMs = (long)(spoilHours * 60 * 60 * 1000);
    }

    public override void RegisterEvents(Fluid fluid)
    {
        fluid.EventCheckFluid.Register(args =>
        {
            FluidStack? stack = args.container.HeldStack;
            if (stack == null || args.api.Side == EnumAppSide.Client) return;

            if (!stack.Attributes.HasAttribute("lastCheckedMs"))
            {
                stack.Attributes.SetLong("lastCheckedMs", MainAPI.Sapi.World.ElapsedMilliseconds);
                return;
            }

            float transitionState = stack.Attributes.GetFloat("transitionState", 0f);
            long lastChecked = stack.Attributes.GetLong("lastCheckedMs");
            long elapsed = MainAPI.Sapi.World.ElapsedMilliseconds - lastChecked;

            transitionState += elapsed / (float)spoilMs;

            if (transitionState > 1f)
            {
                // Transition the fluid in the container.
                Fluid liquidRot = MainAPI.GetGameSystem<FluidRegistry>(args.api.Side).GetFluid("liquidrot");
                FluidStack liquidRotStack = liquidRot.CreateFluidStack();
                liquidRotStack.Units = stack.Units;

                args.container.SetStack(stack);
            }
        });

        fluid.EventGetFluidInfo.Register(args =>
        {
            float transitionState = args.thisStack.Attributes.GetFloat("transitionState", 0f);
            float spoilHours = spoilMs / (60 * 60 * 1000);
            float spoiledHours = spoilHours * (1f - transitionState);

            args.builder.AppendLine($"Spoils in {spoilHours} hours");
        });

        fluid.EventBeforeFluidAddedToOwnStack.Register(args =>
        {
            if (args.toMove <= 0) return;

            float sourceSpoilage = args.sourceStack.Attributes.GetFloat("transitionState", 0f);
            float destinationSpoilage = args.thisStack.Attributes.GetFloat("transitionState", 0f);

            float ratio = args.toMove / (float)(args.toMove + args.thisStack.Units);

            float newSpoilage = (sourceSpoilage * ratio) + (destinationSpoilage * (1f - ratio));

            args.thisStack.Attributes.SetFloat("transitionState", newSpoilage);
        });
    }
}