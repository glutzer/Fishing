using MareLib;
using OpenTK.Mathematics;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Fishing3;

public class WidgetBossHealthBar : Widget
{
    public NineSliceTexture background;
    private readonly Entity entity;
    private readonly TextObject textObj;

    public WidgetBossHealthBar(Widget? parent, Entity entity) : base(parent)
    {
        background = GuiThemes.Background;
        this.entity = entity;

        textObj = new TextObject(entity.GetName(), GuiThemes.Font, 12, GuiThemes.TextColor);

        OnResize += () =>
        {
            textObj.SetScaleFromWidget(this, 0.9f, 0.6f);
        };
    }

    public override void OnRender(float dt, MareShader shader)
    {

        ITreeAttribute? healthTree = entity.WatchedAttributes.GetTreeAttribute("health");
        if (healthTree == null) return;

        float currentHealth = healthTree.GetFloat("currenthealth", 0.5f);
        float maxHealth = healthTree.GetFloat("maxhealth", 1f);

        shader.Uniform("color", GuiThemes.DarkColor);
        RenderTools.RenderNineSlice(background, shader, X, Y, Width, Height);

        float ratio = currentHealth / maxHealth;

        RenderTools.PushScissor(X, Y, (int)(Width * ratio), Height);
        shader.Uniform("color", new Vector4(0.8f, 0f, 0f, 0.8f));
        RenderTools.RenderNineSlice(background, shader, X, Y, Width * ratio, Height);
        shader.Uniform("color", Vector4.One);
        RenderTools.PopScissor();

        textObj.RenderCenteredLine(XCenter, YCenter, shader, true);
    }
}