using MareLib;
using Vintagestory.API.Config;

namespace Fishing3;

public class WidgetEffectDisplay : Widget
{
    public Effect? effect;

    private readonly WidgetFittedText effectNameText;
    private readonly WidgetFittedText effectDurationText;

    public WidgetEffectDisplay(Widget? parent) : base(parent)
    {
        effectNameText = new(this, "", GuiThemes.TextColor);
        effectDurationText = new(this, "", GuiThemes.TextColor);

        effectNameText.Alignment(Align.LeftTop).Percent(0, 0, 0.7f, 1f);
        effectDurationText.Alignment(Align.RightTop).Percent(0, 0, 0.3f, 1f);
    }

    public void SetEffect(Effect effect)
    {
        this.effect = effect;

        string effectName = Lang.Get(effect.Code);
        effectNameText.SetText(effectName);

        float secondsLeft = effect.Duration;

        if (secondsLeft < 60)
        {
            effectDurationText.SetText($"{(int)secondsLeft}s");
        }
        else if (secondsLeft < 3600)
        {
            secondsLeft /= 60f;
            effectDurationText.SetText($"{(int)secondsLeft}m");
        }
        else if (secondsLeft < 86400)
        {
            secondsLeft /= 3600f;
            effectDurationText.SetText($"{(int)secondsLeft}h");
        }
        else
        {
            secondsLeft /= 86400f;
            effectDurationText.SetText($"{(int)secondsLeft}d");
        }
    }
}