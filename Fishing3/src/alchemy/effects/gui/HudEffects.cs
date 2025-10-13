using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;

namespace Fishing;

/// <summary>
/// Will show all duration effects and their remaining time for the current player.
/// Sorted by remaining duration.
/// Will only show when holding the keybind.
/// </summary>
public class HudEffects : Gui
{
    public override EnumDialogType DialogType => EnumDialogType.HUD;

    private long listenerId;
    private WidgetSliceBackground? background;

    private readonly List<WidgetEffectDisplay> effectWidgets = [];

    public override void PopulateWidgets()
    {
        background = (WidgetSliceBackground)new WidgetSliceBackground(null, GuiThemes.Background, new Vector4(0.1f, 0.1f, 0.1f, 0.5f))
            .Alignment(Align.RightBottom)
            .Fixed(-20, -20, 10, 10)
            .SetChildSizing(ChildSizing.Width | ChildSizing.Height);

        AddWidget(background);

        ResizeEffects(effectWidgets.Count);
    }

    public void UpdateEffects(int tick)
    {
        if (tick % 20 != 0) return;

        // Every second, update all effects.
        EntityBehaviorEffects? effects = MainAPI.Capi.World.Player?.Entity.GetBehavior<EntityBehaviorEffects>();
        if (effects == null || background == null) return;

        // Sort effects by duration, low to high.
        List<Effect> effectsList = effects.ActiveEffects.Values.OrderBy(x => x.Duration).ToList();

        if (effectsList.Count != effectWidgets.Count)
        {
            ResizeEffects(effectsList.Count);
        }

        for (int i = 0; i < effectsList.Count; i++)
        {
            WidgetEffectDisplay widget = effectWidgets[i];
            widget.SetEffect(effectsList[i]);
        }

        background.SetBounds();
    }

    public void ResizeEffects(int newCount)
    {
        if (background == null) return;

        background.ClearChildren<WidgetEffectDisplay>();

        effectWidgets.Clear();

        for (int i = 0; i < newCount; i++)
        {
            new WidgetEffectDisplay(background)
                .Alignment(Align.RightBottom)
                .Fixed(0, -i * Scaled(12), 64, 12)
                .As(out WidgetEffectDisplay widget);

            effectWidgets.Add(widget);
        }

        UpdateEffects(0);
        MarkForRepartition();
    }

    public override void OnGuiOpened()
    {
        base.OnGuiOpened();

        UpdateEffects(0);
        listenerId = TickSystem.Client?.RegisterTicker(UpdateEffects) ?? -1;
    }

    public override void OnGuiClosed()
    {
        base.OnGuiClosed();
        effectWidgets.Clear();
        TickSystem.Client?.UnregisterTicker(listenerId);
    }
}