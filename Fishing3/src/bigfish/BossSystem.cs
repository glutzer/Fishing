using MareLib;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace Fishing3;

[GameSystem]
public class BossSystem : NetworkedGameSystem
{
    private HudBossHealthBar? bossHud;
    private readonly List<Entity> bossEntities = new();

    public BossSystem(bool isServer, ICoreAPI api) : base(isServer, api, "bigfish")
    {
    }

    protected override void RegisterMessages(INetworkChannel channel)
    {

    }

    protected override void RegisterClientMessages(IClientNetworkChannel channel)
    {

    }

    protected override void RegisterServerMessages(IServerNetworkChannel channel)
    {

    }

    public void RegisterEntity(Entity entity)
    {
        bossEntities.Add(entity);

        if (bossEntities.Count == 1)
        {
            bossHud = new HudBossHealthBar();
            bossHud.TryOpen();
        }

        bossHud?.EntityLoaded(entity);
    }

    public void UnregisterEntity(Entity entity)
    {
        bossEntities.Remove(entity);

        if (bossEntities.Count == 0)
        {
            bossHud?.TryClose();
            bossHud = null;
        }

        bossHud?.EntityUnloaded(entity);
    }
}