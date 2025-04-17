using MareLib;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Fishing3;

[WormTask]
public class WormTaskBurrowToSurface : WormTask
{
    public Entity? targetEntity;

    public WormTaskBurrowToSurface(float priority, EntityLeviathanHead head) : base(priority, head)
    {
    }

    public override bool CanStartTask(float dt)
    {
        IPlayer[] players = MainAPI.Server.GetPlayersAround(Head.ServerPos.XYZ, 200, 200).Where(p => p.Entity.ServerPos.Y > Head.ServerPos.Y).ToArray();
        return players.Length > 0;
    }

    public override void OnTaskStarted()
    {
        if (!IsServer) return;

        IPlayer[] players = MainAPI.Server.GetPlayersAround(Head.ServerPos.XYZ, 200, 200).Where(p => p.Entity.ServerPos.Y > Head.ServerPos.Y).ToArray();

        if (players.Length > 0)
        {
            IPlayer closestPlayer = players.OrderBy(p => p.Entity.ServerPos.SquareDistanceTo(Head.ServerPos)).First();
        }
    }

    public override void TickTask(float dt)
    {

    }
}