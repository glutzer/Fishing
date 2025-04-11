using Fishing3.src.alchemy.gui;
using MareLib;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace Fishing3;

public static class GuiThemes
{
    public static void AddTitleBar(Gui gui, string title, Widget parent)
    {
        new WidgetTitleBar(parent, parent, title)
            .Alignment(Align.CenterTop, AlignFlags.OutsideV)
            .FixedHeight(8)
            .PercentWidth(1f)
            .As(out WidgetTitleBar titleBar);

        new WidgetLabeledButton(titleBar, () =>
        {
            gui.TryClose();
        }, "", new Vector4(0.75f, 0f, 0f, 1f)).Alignment(Align.RightMiddle).FixedSize(8, 8);
    }

    public static Font Font => FontRegistry.GetFont("soria");

    public static Vector3 Red => new(1, 0, 0);
    public static Vector3 Green => new(0, 1, 0);
    public static Vector3 Blue => new(0, 0, 1);

    public static Vector4 ButtonColor => new(0.5f, 0.2f, 0, 1);
    public static Vector4 TextColor => new(0, 0.7f, 0.7f, 1);
    public static Vector4 DarkColor => new(0.1f, 0.1f, 0.1f, 1);

    private static readonly Dictionary<string, object> cache = new();

    public static Texture Blank => GetOrCreate("blank", () => Texture.Create("fishing:textures/gui/blank.png"));
    public static NineSliceTexture Background => GetOrCreate("background", () => Texture.Create("fishing:textures/gui/background.png").AsNineSlice(14, 14));
    public static NineSliceTexture Button => GetOrCreate("button", () => Texture.Create("fishing:textures/gui/button.png").AsNineSlice(14, 14));
    public static NineSliceTexture ScrollBar => GetOrCreate("scrollbar", () => Texture.Create("fishing:textures/gui/title.png").AsNineSlice(14, 14));
    public static NineSliceTexture Title => GetOrCreate("title", () => Texture.Create("fishing:textures/gui/title.png").AsNineSlice(14, 14));
    public static NineSliceTexture TitleBorder => GetOrCreate("titleborder", () => Texture.Create("fishing:textures/gui/titleborder.png").AsNineSlice(14, 14));

    public static NineSliceTexture SyringeMarker => GetOrCreate("syringemarker", () => Texture.Create("fishing:textures/gui/syringemarker.png").AsNineSlice(0, 20000)); // 84x20 slice, do not scale on y. Always repeat on x.

    // Nine slice is over y coordinate to display entire thing, so sizing is important here.
    public static Texture Tab => GetOrCreate("tab", () => Texture.Create("fishing:textures/gui/tab40.png"));

    private static T GetOrCreate<T>(string path, Func<T> makeTex)
    {
        if (cache.TryGetValue(path, out object? value))
        {
            return (T)value;
        }
        else
        {
            object tex = makeTex()!;
            cache.Add(path, tex);
            return (T)tex;
        }
    }

    public static void ClearCache()
    {
        foreach (object obj in cache)
        {
            if (obj is IDisposable tex)
            {
                tex.Dispose();
            }
        }

        cache.Clear();
    }
}