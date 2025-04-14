using MareLib;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;

namespace Fishing3;

public class HudBossHealthBar : Gui
{
    public override EnumDialogType DialogType => EnumDialogType.HUD;
    public readonly List<Entity> bossEntities = new();

    public void EntityLoaded(Entity entity)
    {
        bossEntities.Add(entity);
        SetWidgets();
    }

    public void EntityUnloaded(Entity entity)
    {
        bossEntities.Remove(entity);
        SetWidgets();
    }

    public override void PopulateWidgets()
    {
        WidgetBossHealthBar? bar = null;

        int index = 1;

        foreach (Entity entity in bossEntities)
        {
            bar = new WidgetBossHealthBar(bar, entity);

            bar.Percent(0f, 0f, 0.5f, 0f).FixedY(index * 12).FixedHeight(10).Alignment(Align.CenterTop);
            index++;

            AddWidget(bar);
        }
    }
}