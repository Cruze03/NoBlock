using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Cvars;

namespace NoBlock;
[MinimumApiVersion(309)]

public class NoBlock : BasePlugin
{
    public override string ModuleName => "[Custom] No Block";
    public override string ModuleAuthor => "Manifest @Road To Glory & WD- & Cruze";
    public override string ModuleDescription => "Allows for players to walk through each other without being stopped due to colliding.";
    public override string ModuleVersion => "V1.0.2";

    private readonly int OnCollisionRulesChangedOffset = GameData.GetOffset("OnCollisionRulesChangedOffset");

    public FakeConVar<bool> cEnable = new("css_noblock_enable", "Toggle between noblock", true);

    [GameEventHandler]
    public HookResult Event_PlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player == null || !player.IsValid)
        {
            return HookResult.Continue;
        }

        CHandle<CCSPlayerPawn> pawn = player.PlayerPawn;

        Server.NextFrame(() => PlayerSpawnNextFrame(player, pawn));

        return HookResult.Continue;
    }

    private void PlayerSpawnNextFrame(CCSPlayerController player, CHandle<CCSPlayerPawn> pawn)
    {
        if(player == null || !player.IsValid || pawn == null || !pawn.IsValid || pawn.Value == null || !pawn.Value.IsValid || pawn.Value.Collision.Handle == IntPtr.Zero) return;

        CollisionGroup collision = CollisionGroup.COLLISION_GROUP_PLAYER;

        if(cEnable.Value)
        {
            collision = CollisionGroup.COLLISION_GROUP_DEBRIS;
        }

        if(pawn.Value.Collision.CollisionGroup != (byte)collision)
        {
            pawn.Value.Collision.CollisionGroup = (byte)collision;
            pawn.Value.Collision.CollisionAttribute.CollisionGroup = (byte)collision;

            Utilities.SetStateChanged(pawn.Value, "CCollisionProperty", "m_CollisionGroup");
            Utilities.SetStateChanged(pawn.Value, "CCollisionProperty", "m_collisionAttribute");

            VirtualFunctionVoid<nint> collisionRulesChanged = new VirtualFunctionVoid<nint>(pawn.Value.Handle, OnCollisionRulesChangedOffset);
            collisionRulesChanged.Invoke(pawn.Value.Handle);
        }
    }
}