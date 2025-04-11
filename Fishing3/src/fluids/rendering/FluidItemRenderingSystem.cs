using MareLib;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Fishing3;

public struct LiquidItemMeshInfo : IEquatable<LiquidItemMeshInfo>
{
    public string itemCode;
    public Vector4 color;
    public float glow;
    public float fill;

    public LiquidItemMeshInfo(string itemCode, Vector4 color, float glow, float fill)
    {
        this.itemCode = itemCode;
        this.color = color;
        this.glow = glow;
        this.fill = fill;
    }

    public LiquidItemMeshInfo(string itemCode)
    {
        this.itemCode = itemCode;
    }

    public readonly bool Equals(LiquidItemMeshInfo other)
    {
        return color == other.color && glow == other.glow && fill == other.fill;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is LiquidItemMeshInfo other && Equals(other);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(itemCode, color, glow, fill);
    }

    public static bool operator ==(LiquidItemMeshInfo left, LiquidItemMeshInfo right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LiquidItemMeshInfo left, LiquidItemMeshInfo right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Handles fluid items.
/// </summary>
[GameSystem(forSide = EnumAppSide.Client)]
public class FluidItemRenderingSystem : GameSystem
{
    /// <summary>
    /// Keep a dictionary of cached models. It's cleared/disposed periodically.
    /// </summary>
    private readonly Dictionary<LiquidItemMeshInfo, MultiTextureMeshRef> fluidModelCache = new();

    private int maxElements = 256;
    private long timeSinceLastClear = 0;

    public FluidItemRenderingSystem(bool isServer, ICoreAPI api) : base(isServer, api)
    {
    }

    /// <summary>
    /// Lerps the item once per frame (for all targets) if lerpDt > 0.
    /// </summary>
    public MultiTextureMeshRef? GetFluidItemModel(ItemFluidStorage item, ItemStack stack, float lerpDt = 0f)
    {
        FluidContainer container = item.GetContainer(stack);
        FluidStack? fluidContainerStack = container.HeldStack;

        Vector4 color;
        float glow;
        float fill;

        if (fluidContainerStack != null)
        {
            Fluid fluid = fluidContainerStack.fluid;
            color = fluid.GetColor(fluidContainerStack);
            glow = fluid.GetGlowLevel(fluidContainerStack);
            fill = (float)fluidContainerStack.Units / container.Capacity;
        }
        else
        {
            color = stack.TempAttributes.GetVector4("lc", Vector4.Zero);
            glow = stack.TempAttributes.GetFloat("gl", 0f);
            fill = 0f;
        }

        if (lerpDt > 0)
        {
            if (stack.TempAttributes.GetInt("fn") != MainAPI.FrameNumber)
            {
                float currentMl = stack.TempAttributes.GetFloat("cMl", fill);
                currentMl = Math.Clamp(GameMath.Lerp(currentMl, fill, lerpDt), 0f, 1f);
                if (Math.Abs(currentMl - fill) < 0.03f) currentMl = fill;
                stack.TempAttributes.SetFloat("cMl", currentMl);
                fill = currentMl;

                stack.TempAttributes.SetInt("fn", MainAPI.FrameNumber);
            }
            else
            {
                fill = stack.TempAttributes.GetFloat("cMl");
            }

            if (fluidContainerStack != null)
            {
                stack.TempAttributes.SetVector4("lc", color);
                stack.TempAttributes.SetFloat("gl", glow);
            }
            else if (fill == 0f)
            {
                color = Vector4.Zero; // Empty container should have nothing there.
                glow = 0;
            }
        }

        // Round, should be integers later.
        LiquidItemMeshInfo info = new(item.Code, color, MathF.Round(glow, 2), MathF.Round(fill, 2));

        return GetOrCreate(info, () =>
        {
            return MainAPI.Capi.Render.UploadMultiTextureMesh(CreateFluidItemModel(item, color, glow, fill));
        });
    }

    private MultiTextureMeshRef GetOrCreate(LiquidItemMeshInfo meshInfo, Func<MultiTextureMeshRef> meshRef)
    {
        if (fluidModelCache.Count > maxElements)
        {
            foreach (MultiTextureMeshRef value in fluidModelCache.Values)
            {
                value.Dispose();
            }

            fluidModelCache.Clear();

            if ((MainAPI.Capi.World.ElapsedMilliseconds - timeSinceLastClear) / 1000f > 5)
            {
                maxElements *= 2;
            }

            timeSinceLastClear = MainAPI.Capi.World.ElapsedMilliseconds;
        }

        if (fluidModelCache.TryGetValue(meshInfo, out MultiTextureMeshRef? mesh))
        {
            return mesh;
        }

        mesh = meshRef();
        fluidModelCache.Add(meshInfo, mesh);
        return mesh;
    }

    /// <summary>
    /// Create a fluid item model.
    /// For every element that starts with "Contents", it is altered.
    /// Must be facing up.
    /// </summary>
    public static MeshData CreateFluidItemModel(Item item, Vector4 color, float glowLevel, float fillLevel)
    {
        ICoreClientAPI capi = MainAPI.Capi;

        // Get the shape this item uses.
        IAsset? shapeAsset = capi.Assets.TryGet($"{item.Shape.Base.Domain}:shapes/{item.Shape.Base.Path}.json") ?? throw new Exception("Unable to find shape for item: " + item.Shape.Base.Path + " in domain: " + item.Shape.Base.Domain);
        Shape shape = shapeAsset.ToObject<Shape>();

        // Add every texture for this item.
        shape.Textures.Clear();
        foreach (KeyValuePair<string, CompositeTexture> texture in item.Textures)
        {
            shape.Textures.Add(texture.Key, new AssetLocation(texture.Value.ToString().Split('@')[0]));
        }

        fillLevel = MathF.Round(fillLevel, 2);

        // Alter all "Contents" shapes.
        void RecursivelyAlter(ShapeElement element)
        {
            if (element.Children != null)
            {
                foreach (ShapeElement child in element.Children)
                {
                    RecursivelyAlter(child);
                }
            }

            if (!element.Name.StartsWith("Contents")) return;

            double height = element.To[1] - element.From[1];
            element.To[1] = element.From[1] + (height * fillLevel);

            if (element.FacesResolved != null)
            {
                foreach (ShapeElementFace face in element.FacesResolved)
                {
                    if (face != null)
                    {
                        // Minimum 1.
                        face.Glow = Math.Max((int)(glowLevel * 255), 1);
                    }
                }
            }
        }

        foreach (ShapeElement shapeElement in shape.Elements)
        {
            RecursivelyAlter(shapeElement);
        }

        // Tessellate, change every vertex with glow level > 0 to the color.
        ShapeTextureSource textureSource = new(capi, shape, "");
        capi.Tesselator.TesselateShape("", shape, out MeshData meshData, textureSource);

        for (int i = 0; i < meshData.Flags.Length; i++)
        {
            // Get value of bits 0-7.
            int glow = meshData.Flags[i] & 0x7F;

            if (glow > 0)
            {
                meshData.Rgba[i * 4] = (byte)(color.X * 255);
                meshData.Rgba[(i * 4) + 1] = (byte)(color.Y * 255);
                meshData.Rgba[(i * 4) + 2] = (byte)(color.Z * 255);
                meshData.Rgba[(i * 4) + 3] = (byte)(color.W * 255);
            }
        }

        return meshData;
    }
}