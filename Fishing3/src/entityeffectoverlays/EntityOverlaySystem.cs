using MareLib;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Fishing3;

public class OverlayRenderInfo
{
    public MultiTextureMeshRef mesh;
    public IAnimator entityAnimator;
    public Matrix4 modelMatrix;
    public int renderFlags;

    public OverlayRenderInfo(MultiTextureMeshRef mesh, IAnimator entityAnimator, Matrix4 modelMatrix)
    {
        this.mesh = mesh;
        this.entityAnimator = entityAnimator;
        this.modelMatrix = modelMatrix;
    }

    public OverlayRenderInfo(MultiTextureMeshRef mesh, IAnimator entityAnimator, float[] modelMatrix)
    {
        this.mesh = mesh;
        this.entityAnimator = entityAnimator;
        this.modelMatrix = AnimationUtility.ConvertMatrix(modelMatrix);
    }
}

[GameSystem(forSide = EnumAppSide.Client)]
public class EntityOverlaySystem : GameSystem
{
    private readonly Dictionary<long, List<(Action<OverlayRenderInfo> action, float order)>> handlersByEntityId = new();
    public static EntityOverlaySystem? Instance { get; private set; }

    public EntityOverlaySystem(bool isServer, ICoreAPI api) : base(isServer, api)
    {
    }

    public override void PreInitialize()
    {
        Instance = this;
    }

    public override void OnClose()
    {
        Instance = null;
    }

    /// <summary>
    /// Does this entity have overlays to render?
    /// </summary>
    public static bool EntityHasHandler(Entity entity)
    {
        return entity.Attributes.HasAttribute("hasov");
    }

    /// <summary>
    /// Is this entity disabling overlays?
    /// </summary>
    public static bool EntityHasRenderingDisabled(Entity entity)
    {
        return entity.Attributes.GetInt("hasov", 0) > 0;
    }

    /// <summary>
    /// Executes all handlers for this entity.
    /// Returns if original model should render.
    /// </summary>
    public void ExecuteHandlers(Entity entity, OverlayRenderInfo renderInfo)
    {
        if (handlersByEntityId.TryGetValue(entity.EntityId, out List<(Action<OverlayRenderInfo> action, float order)>? handlerList))
        {
            foreach ((Action<OverlayRenderInfo> action, float order) in handlerList)
            {
                action(renderInfo);
            }
        }
    }

    /// <summary>
    /// Registers an overlay handler for this entity.
    /// Handler should return if original model should render.
    /// </summary>
    public void Register(Entity entity, (Action<OverlayRenderInfo>, float order) handler, bool disableOriginalModel = false)
    {
        if (!handlersByEntityId.TryGetValue(entity.EntityId, out List<(Action<OverlayRenderInfo> action, float order)>? handlers))
        {
            handlers = new List<(Action<OverlayRenderInfo> action, float order)>();
            handlersByEntityId.Add(entity.EntityId, handlers);
        }

        handlers.Add(handler);
        // Sort by order, with higher order being last.
        handlers.Sort((a, b) => a.order.CompareTo(b.order));

        int disables = entity.Attributes.GetInt("hasov", 0);
        entity.Attributes.SetInt("hasov", disableOriginalModel ? disables + 1 : disables);
    }

    /// <summary>
    /// Unregisters an overlay handler for this entity.
    /// </summary>
    public void Unregister(Entity entity, Action<OverlayRenderInfo> action, bool disableOriginalModel = false)
    {
        if (handlersByEntityId.TryGetValue(entity.EntityId, out List<(Action<OverlayRenderInfo> action, float order)>? handlers))
        {
            handlers.RemoveAll(x => x.action == action);

            int disables = entity.Attributes.GetInt("hasov", 0);
            entity.Attributes.SetInt("hasov", disableOriginalModel ? disables - 1 : disables);

            if (handlers.Count == 0)
            {
                entity.Attributes.RemoveAttribute("hasov");
                handlersByEntityId.Remove(entity.EntityId);
            }
        }
    }
}