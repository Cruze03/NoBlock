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

    public FakeConVar<bool> cEnable = new("css_noblock_enable", "Toggle between noblock", true);
    public FakeConVar<bool> cGrenadeEnable = new("css_noblock_grenade_enable", "Toggle between noblock for grenades", true);

    public override void Load(bool hotReload)
    {
        base.Load(hotReload);
        RegisterListener<Listeners.OnEntityCreated>(OnEntityCreated);
    }

    public override void Unload(bool hotReload)
    {
        base.Unload(hotReload);
        RemoveListener<Listeners.OnEntityCreated>(OnEntityCreated);
    }

    private void OnEntityCreated(CEntityInstance entity)
    {
        if (!cGrenadeEnable.Value) return;

        var className = entity.Entity?.DesignerName ?? "";

        if (string.IsNullOrWhiteSpace(className)) return;

        if (className.Contains("_projectile"))
            entity.As<CBaseEntity>().SetCollisionGroup(CollisionGroup.COLLISION_GROUP_NEVER);
    }

    [GameEventHandler]
    public HookResult Event_PlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player == null || !player.IsValid) return HookResult.Continue;

        CHandle<CCSPlayerPawn> pawn = player.PlayerPawn;

        Server.NextFrame(() => PlayerSpawnNextFrame(player, pawn));

        return HookResult.Continue;
    }

    private void PlayerSpawnNextFrame(CCSPlayerController player, CHandle<CCSPlayerPawn> pawn)
    {
        if(player == null
        || !player.IsValid
        || pawn == null
        || !pawn.IsValid
        || pawn.Value == null
        || !pawn.Value.IsValid) return;

        CollisionGroup collision = CollisionGroup.COLLISION_GROUP_PLAYER;

        if(cEnable.Value) collision = CollisionGroup.COLLISION_GROUP_DEBRIS;

        if(pawn.Value.Collision.CollisionGroup != (byte)collision) pawn.Value.SetCollisionGroup(collision);
    }
}

public static class Extensions
{
    private static readonly int OnCollisionRulesChangedOffset = GameData.GetOffset("OnCollisionRulesChangedOffset");
    public static void SetCollisionGroup(this CBaseEntity entity, CollisionGroup collision)
    {
        if(entity == null || !entity.IsValid || entity.Handle == IntPtr.Zero || entity.Collision == null || entity.Collision.Handle == IntPtr.Zero) return;

        entity.Collision.CollisionGroup = (byte)collision;
        Utilities.SetStateChanged(entity, "CCollisionProperty", "m_collisionAttribute");

        VirtualFunctionVoid<nint> collisionRulesChanged = new VirtualFunctionVoid<nint>(entity.Handle, OnCollisionRulesChangedOffset);
        collisionRulesChanged.Invoke(entity.Handle);
    }
}