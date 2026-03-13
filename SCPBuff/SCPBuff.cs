using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;
using PlayerRoles;
using Exiled.API.Enums;

namespace SCPBuff
{
    public static class RoleExtensions
    {
        public static bool IsSCP(this RoleTypeId role) => role.GetTeam() == Team.SCPs;

        public static readonly HashSet<RoleTypeId> ProtectedRoles = new HashSet<RoleTypeId>
        {
            RoleTypeId.Tutorial,
            RoleTypeId.Spectator,
            RoleTypeId.None,
            RoleTypeId.Overwatch,
            RoleTypeId.Filmmaker
        };
    }

    public class SCPBuff : Plugin<Config>
    {
        private readonly HashSet<Player> _processedPlayers = new HashSet<Player>();
        private readonly Dictionary<RoleTypeId, bool> _disabledRolesCache = new Dictionary<RoleTypeId, bool>();
        private readonly Dictionary<RoleTypeId, bool> _godModeCache = new Dictionary<RoleTypeId, bool>();
        private string _configFilePath;
        private string _godConfigFilePath;
        private string _roleConfigBackupPath;

        public override string Author => "MrDarvi";
        public override string Name => "SCPBuff";
        public override string Prefix => "scpbuff";
        public override Version Version => new Version(2, 0, 1);
        public override Version RequiredExiledVersion => new Version(9, 13, 1);

        public static SCPBuff Instance;

        public override void OnEnabled()
        {
            Instance = this;

            _configFilePath = Path.Combine(Paths.Configs, "scpbuff_disabled_roles.txt");
            _godConfigFilePath = Path.Combine(Paths.Configs, "scpbuff_godmode_roles.txt");
            _roleConfigBackupPath = Path.Combine(Paths.Configs, "scpbuff_role_configs.txt");

            LoadDisabledRoles();
            LoadGodModeRoles();
            LoadRoleConfigs();

            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
            Exiled.Events.Handlers.Player.Hurting += OnHurting;

            Log.Info($"{Name} v{Version} loaded successfully!");
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
            Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
            Exiled.Events.Handlers.Player.Hurting -= OnHurting;

            _processedPlayers.Clear();
            _disabledRolesCache.Clear();
            _godModeCache.Clear();
            Instance = null;
            base.OnDisabled();
        }

        private void LoadRoleConfigs()
        {
            try
            {
                if (File.Exists(_roleConfigBackupPath))
                {
                    var lines = File.ReadAllLines(_roleConfigBackupPath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('=');
                        if (parts.Length >= 3)
                        {
                            if (Enum.TryParse<RoleTypeId>(parts[0], out var roleType))
                            {
                                if (Config.RoleConfigs.TryGetValue(roleType, out var config))
                                {
                                    float health;
                                    if (float.TryParse(parts[1], out health))
                                        config.Health = health;

                                    float hs;
                                    if (float.TryParse(parts[2], out hs))
                                        config.HumeShield = hs;
                                }
                            }
                        }
                    }
                    Log.Info($"Loaded role configs from backup");
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed to load role configs: {e}");
            }
        }

        private void SaveRoleConfigs()
        {
            try
            {
                var lines = new List<string>();
                foreach (var kvp in Config.RoleConfigs)
                {
                    lines.Add($"{kvp.Key}={kvp.Value.Health}={kvp.Value.HumeShield}");
                }
                File.WriteAllLines(_roleConfigBackupPath, lines);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to save role configs: {e}");
            }
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
                        if (parts.Length == 2 && Enum.TryParse<RoleTypeId>(parts[0], out var roleType))
                        {
                            _disabledRolesCache[roleType] = parts[1] == "1";
                        }
                    }
                    Log.Info($"Loaded {_disabledRolesCache.Count} disabled roles from config");
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

        private void LoadGodModeRoles()
        {
            try
            {
                if (File.Exists(_godConfigFilePath))
                {
                    var lines = File.ReadAllLines(_godConfigFilePath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('=');
                        if (parts.Length == 2 && Enum.TryParse<RoleTypeId>(parts[0], out var roleType))
                        {
                            _godModeCache[roleType] = parts[1] == "1";
                        }
                    }
                    Log.Info($"Loaded {_godModeCache.Count} god mode roles from config");
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed to load god mode roles: {e}");
            }
        }

        private void SaveGodModeRoles()
        {
            try
            {
                var lines = new List<string>();
                foreach (var kvp in _godModeCache)
                {
                    lines.Add($"{kvp.Key}={(kvp.Value ? "1" : "0")}");
                }
                File.WriteAllLines(_godConfigFilePath, lines);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to save god mode roles: {e}");
            }
        }

        private bool IsRoleDisabled(RoleTypeId roleType)
        {
            if (RoleExtensions.ProtectedRoles.Contains(roleType))
                return false;

            return _disabledRolesCache.ContainsKey(roleType) && _disabledRolesCache[roleType];
        }

        private bool IsRoleGod(RoleTypeId roleType)
        {
            if (RoleExtensions.ProtectedRoles.Contains(roleType))
                return false;

            return _godModeCache.ContainsKey(roleType) && _godModeCache[roleType];
        }

        private void OnHurting(HurtingEventArgs ev)
        {
            try
            {
                if (ev.Player == null || !ev.Player.IsAlive)
                    return;

                var roleType = ev.Player.Role.Type;

                if (IsRoleGod(roleType))
                {
                    ev.IsAllowed = false;
                    if (Config.Debug)
                        Log.Debug($"[SCPBuff] God mode prevented damage to {ev.Player.Nickname} ({roleType})");
                }
                else if (Config.RoleConfigs.TryGetValue(roleType, out var config) && config.IsGod)
                {
                    ev.IsAllowed = false;
                    if (Config.Debug)
                        Log.Debug($"[SCPBuff] God mode (config) prevented damage to {ev.Player.Nickname} ({roleType})");
                }
            }
            catch (Exception e)
            {
                Log.Error($"OnHurting error: {e}");
            }
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

                if (RoleExtensions.ProtectedRoles.Contains(roleType))
                {
                    ApplyGodMode(player, roleType);
                    return;
                }

                if (IsRoleDisabled(roleType))
                {
                    HandleDisabledRole(player);
                    return;
                }

                if (Config.RoleConfigs.TryGetValue(roleType, out var roleConfig))
                {
                    if (!roleConfig.IsEnabled)
                    {
                        HandleDisabledRole(player);
                        return;
                    }

                    player.MaxHealth = roleConfig.Health;
                    player.Health = roleConfig.Health;

                    if (roleConfig.HumeShield > 0)
                    {
                        player.MaxHumeShield = roleConfig.HumeShield;
                        player.HumeShield = roleConfig.HumeShield;
                    }

                    ApplyGodMode(player, roleType);

                    if (Config.Debug)
                        Log.Debug($"[SCPBuff] Applied to {player.Nickname} ({roleType}): HP={player.Health}, HS={player.HumeShield}, God={IsRoleGod(roleType) || roleConfig.IsGod}");
                }
            }
            catch (Exception e)
            {
                Log.Error($"ApplyRoleSettings error: {e}");
            }
        }

        private void ApplyGodMode(Player player, RoleTypeId roleType)
        {
            bool isGod = IsRoleGod(roleType);

            if (!isGod && Config.RoleConfigs.TryGetValue(roleType, out var config))
                isGod = config.IsGod;

            player.IsGodModeEnabled = isGod;
        }

        private void HandleDisabledRole(Player player)
        {
            try
            {
                if (RoleExtensions.ProtectedRoles.Contains(player.Role.Type))
                    return;

                player.RoleManager.ServerSetRole(RoleTypeId.Tutorial, RoleChangeReason.RemoteAdmin);

                if (Config.Debug)
                    Log.Debug($"[SCPBuff] Changed {player.Nickname} to Tutorial (original role was disabled)");
            }
            catch (Exception e)
            {
                Log.Error($"HandleDisabledRole error: {e}");
            }
        }

        public void ToggleRole(RoleTypeId roleType, bool enabled)
        {
            if (RoleExtensions.ProtectedRoles.Contains(roleType))
            {
                Log.Warn($"Attempted to modify protected role: {roleType}");
                return;
            }

            _disabledRolesCache[roleType] = !enabled;
            SaveDisabledRoles();

            Timing.CallDelayed(0.5f, () =>
            {
                foreach (var player in Player.List.Where(p => p.IsAlive && p.Role.Type == roleType).ToList())
                {
                    ApplyRoleSettings(player);
                }
            });
        }

        public bool GetRoleStatus(RoleTypeId roleType)
        {
            if (RoleExtensions.ProtectedRoles.Contains(roleType))
                return true;

            return !IsRoleDisabled(roleType);
        }

        public void SetRoleHealth(RoleTypeId roleType, float health, bool permanent = false)
        {
            if (Config.RoleConfigs.TryGetValue(roleType, out var config))
            {
                config.Health = health;

                if (permanent)
                {
                    SaveRoleConfigs();
                }

                foreach (var player in Player.List.Where(p => p.IsAlive && p.Role.Type == roleType).ToList())
                {
                    player.MaxHealth = health;
                    player.Health = health;
                }

                Log.Info($"Set health for {roleType} to {health} ({(permanent ? "permanent" : "temporary")})");
            }
        }

        public void SetRoleHumeShield(RoleTypeId roleType, float humeShield, bool permanent = false)
        {
            if (Config.RoleConfigs.TryGetValue(roleType, out var config))
            {
                config.HumeShield = humeShield;

                if (permanent)
                {
                    SaveRoleConfigs();
                }

                foreach (var player in Player.List.Where(p => p.IsAlive && p.Role.Type == roleType).ToList())
                {
                    if (humeShield > 0)
                    {
                        player.MaxHumeShield = humeShield;
                        player.HumeShield = humeShield;
                    }
                }

                Log.Info($"Set hume shield for {roleType} to {humeShield} ({(permanent ? "permanent" : "temporary")})");
            }
        }

        public void SetRoleGodMode(RoleTypeId roleType, bool enabled)
        {
            if (RoleExtensions.ProtectedRoles.Contains(roleType) && enabled)
            {
                Log.Warn($"Cannot enable god mode for protected role: {roleType}");
                return;
            }

            _godModeCache[roleType] = enabled;
            SaveGodModeRoles();

            foreach (var player in Player.List.Where(p => p.IsAlive && p.Role.Type == roleType).ToList())
            {
                player.IsGodModeEnabled = enabled;
            }

            Log.Info($"Set god mode for {roleType} to {enabled}");
        }

        public bool GetRoleGodMode(RoleTypeId roleType)
        {
            return IsRoleGod(roleType);
        }

        public List<string> GetDisabledRoles()
        {
            return _disabledRolesCache
                .Where(kvp => kvp.Value && !RoleExtensions.ProtectedRoles.Contains(kvp.Key))
                .Select(kvp => kvp.Key.ToString())
                .ToList();
        }

        public List<string> GetGodRoles()
        {
            return _godModeCache
                .Where(kvp => kvp.Value && !RoleExtensions.ProtectedRoles.Contains(kvp.Key))
                .Select(kvp => kvp.Key.ToString())
                .ToList();
        }
    }
}