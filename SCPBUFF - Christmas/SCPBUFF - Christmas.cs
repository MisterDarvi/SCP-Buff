using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;
using PlayerRoles;

namespace SCPBuff
{
    public static class RoleExtensions
    {
        public static bool IsSCP(this RoleTypeId role) => role.GetTeam() == Team.SCPs;
    }

    public class SCPBuff : Plugin<Config>
    {
        private readonly HashSet<Player> _processedPlayers = new HashSet<Player>();
        private readonly Dictionary<RoleTypeId, bool> _disabledRolesCache = new Dictionary<RoleTypeId, bool>();
        private string _configFilePath;

        public override string Author => "MrDarvi";
        public override string Name => "SCPBuff-Christmas";
        public override string Prefix => "scpbuff_christmas";
        public override Version Version => new Version(2, 0, 0);
        public override Version RequiredExiledVersion => new Version(9, 12, 0);

        public static SCPBuff Instance;

        public override void OnEnabled()
        {
            Instance = this;

            // Initialize config file path
            _configFilePath = Path.Combine(Paths.Configs, "scpbuff_disabled_roles.txt");

            // Load disabled roles
            LoadDisabledRoles();

            // Subscribe to events
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;

            Log.Info($"{Name} v{Version} loaded successfully!");
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
            Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;

            _processedPlayers.Clear();
            _disabledRolesCache.Clear();
            Instance = null;
            base.OnDisabled();
        }

        private void LoadDisabledRoles()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var lines = File.ReadAllLines(_configFilePath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            if (Enum.TryParse<RoleTypeId>(parts[0], out var roleType))
                            {
                                _disabledRolesCache[roleType] = parts[1] == "1";
                            }
                        }
                    }
                    Log.Info($"Loaded {_disabledRolesCache.Count} disabled roles from config");
                }
                else
                {
                    Log.Info("No disabled roles config found, creating new one");
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed to load disabled roles: {e}");
            }
        }

        private void SaveDisabledRoles()
        {
            try
            {
                var lines = new List<string>();
                foreach (var kvp in _disabledRolesCache)
                {
                    lines.Add($"{kvp.Key}={(kvp.Value ? "1" : "0")}");
                }

                File.WriteAllLines(_configFilePath, lines);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to save disabled roles: {e}");
            }
        }

        private bool IsRoleDisabled(RoleTypeId roleType)
        {
            return _disabledRolesCache.ContainsKey(roleType) && _disabledRolesCache[roleType];
        }

        private void OnRoundStarted()
        {
            _processedPlayers.Clear();
            Timing.CallDelayed(1f, () =>
            {
                foreach (var player in Player.List)
                {
                    if (player.IsAlive)
                        ApplyRoleSettings(player);
                }
            });
        }

        private void OnSpawned(SpawnedEventArgs ev)
        {
            Timing.CallDelayed(0.2f, () => ApplyRoleSettings(ev.Player));
        }

        private void OnChangingRole(ChangingRoleEventArgs ev)
        {
            if (ev.Player == null || ev.NewRole == RoleTypeId.None)
                return;

            Timing.CallDelayed(0.3f, () => ApplyRoleSettings(ev.Player));
        }

        public void ApplyRoleSettings(Player player)
        {
            try
            {
                if (player == null || !player.IsAlive || player.ReferenceHub.serverRoles.BypassMode)
                    return;

                var roleType = player.Role.Type;

                // Check if role is disabled globally
                if (IsRoleDisabled(roleType))
                {
                    HandleDisabledRole(player);
                    return;
                }

                // Check if role is disabled in config
                if (Config.RoleConfigs.TryGetValue(roleType, out var roleConfig))
                {
                    if (!roleConfig.IsEnabled)
                    {
                        HandleDisabledRole(player);
                        return;
                    }

                    // Apply health and hume shield settings
                    player.MaxHealth = roleConfig.Health;
                    player.Health = roleConfig.Health;

                    if (roleConfig.HumeShield > 0)
                    {
                        player.MaxHumeShield = roleConfig.HumeShield;
                        player.HumeShield = roleConfig.HumeShield;
                    }

                    if (Config.Debug)
                    {
                        Log.Debug($"[SCPBuff] Applied to {player.Nickname} ({roleType}): HP={player.Health}, HS={player.HumeShield}");
                    }
                }
                else if (Config.Debug)
                {
                    Log.Debug($"[SCPBuff] No config found for role: {roleType}");
                }
            }
            catch (Exception e)
            {
                Log.Error($"ApplyRoleSettings error: {e}");
            }
        }

        private void HandleDisabledRole(Player player)
        {
            try
            {
                // Get team of disabled role
                var team = player.Role.Team;

                // Find alternative role from same team
                var alternativeRole = GetAlternativeRoleFromTeam(team, player.Role.Type);

                if (alternativeRole.HasValue)
                {
                    player.RoleManager.ServerSetRole(alternativeRole.Value, RoleChangeReason.RemoteAdmin);
                    Log.Debug($"[SCPBuff] Changed {player.Nickname}'s role to {alternativeRole} because original role was disabled");
                }
                else
                {
                    // If no alternative found, use default
                    player.RoleManager.ServerSetRole(RoleTypeId.Tutorial, RoleChangeReason.RemoteAdmin);
                    Log.Debug($"[SCPBuff] Changed {player.Nickname}'s role to Tutorial (no alternative found)");
                }
            }
            catch (Exception e)
            {
                Log.Error($"HandleDisabledRole error: {e}");
            }
        }

        private RoleTypeId? GetAlternativeRoleFromTeam(Team team, RoleTypeId currentRole)
        {
            try
            {
                var possibleRoles = new List<RoleTypeId>();

                switch (team)
                {
                    case Team.SCPs:
                        possibleRoles = Config.RoleConfigs
                            .Where(kvp => kvp.Key.GetTeam() == Team.SCPs
                                        && kvp.Value.IsEnabled
                                        && !IsRoleDisabled(kvp.Key)
                                        && kvp.Key != currentRole)
                            .Select(kvp => kvp.Key)
                            .ToList();
                        break;

                    case Team.FoundationForces:
                        possibleRoles = Config.RoleConfigs
                            .Where(kvp => kvp.Key.GetTeam() == Team.FoundationForces
                                        && kvp.Value.IsEnabled
                                        && !IsRoleDisabled(kvp.Key))
                            .Select(kvp => kvp.Key)
                            .ToList();
                        break;

                    case Team.ChaosInsurgency:
                        possibleRoles = Config.RoleConfigs
                            .Where(kvp => kvp.Key.GetTeam() == Team.ChaosInsurgency
                                        && kvp.Value.IsEnabled
                                        && !IsRoleDisabled(kvp.Key))
                            .Select(kvp => kvp.Key)
                            .ToList();
                        break;

                    case Team.ClassD:
                        possibleRoles = new List<RoleTypeId> { RoleTypeId.ClassD };
                        break;

                    case Team.Scientists:
                        possibleRoles = new List<RoleTypeId> { RoleTypeId.Scientist };
                        break;
                }

                if (possibleRoles.Count > 0)
                {
                    var random = new Random();
                    return possibleRoles[random.Next(possibleRoles.Count)];
                }

                return null;
            }
            catch (Exception e)
            {
                Log.Error($"GetAlternativeRoleFromTeam error: {e}");
                return null;
            }
        }

        // Public methods for commands
        public void ToggleRole(RoleTypeId roleType, bool enabled)
        {
            _disabledRolesCache[roleType] = !enabled;
            SaveDisabledRoles();

            // Apply changes to existing players
            Timing.CallDelayed(0.5f, () =>
            {
                foreach (var player in Player.List.Where(p => p.IsAlive && p.Role.Type == roleType))
                {
                    ApplyRoleSettings(player);
                }
            });
        }

        public bool GetRoleStatus(RoleTypeId roleType)
        {
            return !IsRoleDisabled(roleType);
        }
    }
}
