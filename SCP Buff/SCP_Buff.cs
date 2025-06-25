using System;
using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using PlayerRoles;
using MEC;

namespace SCPBuff
{
    public static class RoleExtensions
    {
        public static bool IsSCP(this RoleTypeId role) => role.GetTeam() == Team.SCPs && role != RoleTypeId.Scp079;
    }

    public class SCPBuff : Plugin<Config>
    {
        private readonly HashSet<Player> _processedPlayers = new HashSet<Player>();
        private readonly List<RoleTypeId> _alternativeRoles = new List<RoleTypeId>
        {
            RoleTypeId.Scientist,
            RoleTypeId.ClassD,
            RoleTypeId.FacilityGuard
        };

        public override string Author => "MrDarvi";
        public override string Name => "SCPBuff";
        public override string Prefix => "scpbuff";
        public override Version Version => new Version(1, 0, 0);
        public override Version RequiredExiledVersion => new Version(9, 6, 1);

        public static SCPBuff Instance;

        public override void OnEnabled()
        {
            Instance = this;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Log.Info($"{Name} v{Version} loaded successfully!");
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
            _processedPlayers.Clear();
            Instance = null;
            base.OnDisabled();
        }

        private void OnRoundStarted() => _processedPlayers.Clear();

        private void OnSpawned(SpawnedEventArgs ev) => Timing.CallDelayed(0.1f, () => ForceApplySettings(ev.Player));

        private void OnChangingRole(ChangingRoleEventArgs ev)
        {
            if (ev.Player == null || !ev.NewRole.IsSCP())
                return;

            Timing.CallDelayed(0.1f, () => ForceApplySettings(ev.Player));
        }

        private void ForceApplySettings(Player player)
        {
            try
            {
                if (player == null || !player.IsAlive || !player.IsScp)
                    return;

                // Skip administrators
                if (player.ReferenceHub.serverRoles.BypassMode)
                    return;

                if (!Config.ScpConfigs.TryGetValue(player.Role, out var scpConfig) || !scpConfig.IsEnabled)
                {
                    SetAlternativeRole(player);
                    return;
                }

                // Repeated use at intervals
                for (int i = 0; i < 5; i++)
                {
                    Timing.CallDelayed(0.2f * i, () => ApplyScpSettings(player, scpConfig));
                }
            }
            catch (Exception e)
            {
                Log.Error($"ForceApplySettings error: {e}");
            }
        }

        private void SetAlternativeRole(Player player)
        {
            var random = new Random();
            var newRole = _alternativeRoles[random.Next(_alternativeRoles.Count)];
            player.Role.Set(newRole);
        }

        private void ApplyScpSettings(Player player, ScpConfig scpConfig)
        {
            try
            {
                if (!player.IsAlive || !player.IsScp)
                    return;

                // Special settings for SCP-3114 (skeleton)
                if (player.Role == RoleTypeId.Scp3114)
                {
                    player.MaxHealth = 550;
                    player.Health = 550;
                    player.MaxHumeShield = 100;
                    player.HumeShield = Math.Min(player.HumeShield, 100); // Жесткая блокировка HS
                    return;
                }

                // For the rest of the SCP
                player.MaxHealth = scpConfig.Health;
                player.Health = scpConfig.Health;
                player.MaxHumeShield = scpConfig.HumeShield;
                player.HumeShield = scpConfig.HumeShield;

                Log.Debug($"[SCPBuff] Applied to {player.Nickname} ({player.Role}): HP={player.Health}, HS={player.HumeShield}");
            }
            catch (Exception e)
            {
                Log.Error($"ApplyScpSettings error: {e}");
            }
        }
    }
}