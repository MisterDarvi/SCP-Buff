using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using PlayerRoles;

namespace SCPBuff
{
    public static class RoleExtensions
    {
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
        public override string Name => "SCPBuff";
        public override string Description => "Allows customizing HP, Hume Shield and god mode per role.";
        public override string Author => "MrDarvi";
        public override Version Version => new Version(3, 0, 0);
        public override Version RequiredApiVersion => new Version(1, 0, 0);

        public static SCPBuff Instance { get; private set; }

        private readonly HashSet<Player> _processedPlayers = new HashSet<Player>();
        private readonly Dictionary<RoleTypeId, bool> _disabledRolesCache = new Dictionary<RoleTypeId, bool>();
        private readonly Dictionary<RoleTypeId, bool> _godModeCache = new Dictionary<RoleTypeId, bool>();

        private string _configFilePath;
        private string _godConfigFilePath;
        private string _roleConfigBackupPath;

        public override void Enable()
        {
            Instance = this;

            // GetConfigDirectory() is an extension method from LabApi.Loader
            // Returns the per-plugin config folder inside LabAPI/configs/{port}/SCPBuff/
            string configDir = this.GetConfigDirectory().FullName;
            if (!Directory.Exists(configDir))
                Directory.CreateDirectory(configDir);

            _configFilePath = Path.Combine(configDir, "disabled_roles.txt");
            _godConfigFilePath = Path.Combine(configDir, "godmode_roles.txt");
            _roleConfigBackupPath = Path.Combine(configDir, "role_configs.txt");

            LoadDisabledRoles();
            LoadGodModeRoles();
            LoadRoleConfigs();

            ServerEvents.RoundStarted += OnRoundStarted;
            PlayerEvents.Spawned += OnSpawned;
            PlayerEvents.Hurting += OnHurting;

            Logger.Info($"{Name} v{Version} loaded successfully! (LabAPI)");
        }

        public override void Disable()
        {
            ServerEvents.RoundStarted -= OnRoundStarted;
            PlayerEvents.Spawned -= OnSpawned;
            PlayerEvents.Hurting -= OnHurting;

            _processedPlayers.Clear();
            _disabledRolesCache.Clear();
            _godModeCache.Clear();
            Instance = null;
        }

        // ── Event handlers ───────────────────────────────────────────────────

        private void OnRoundStarted()
        {
            _processedPlayers.Clear();
            foreach (var player in Player.ReadyList)
            {
                if (player.IsAlive)
                    ApplyRoleSettings(player);
            }
        }

        private void OnSpawned(PlayerSpawnedEventArgs ev)
        {
            if (ev.Player != null)
                ApplyRoleSettings(ev.Player);
        }

        private void OnHurting(PlayerHurtingEventArgs ev)
        {
            try
            {
                if (ev.Player == null || !ev.Player.IsAlive)
                    return;

                var roleType = ev.Player.Role;

                if (_godModeCache.TryGetValue(roleType, out bool godFromCache) && godFromCache)
                {
                    ev.IsAllowed = false;
                    if (Config.Debug)
                        Logger.Debug($"[SCPBuff] God mode (cache) blocked damage to {ev.Player.Nickname} ({roleType})");
                    return;
                }

                if (Config.RoleConfigs.TryGetValue(roleType, out var roleCfg) && roleCfg.IsGod)
                {
                    ev.IsAllowed = false;
                    if (Config.Debug)
                        Logger.Debug($"[SCPBuff] God mode (config) blocked damage to {ev.Player.Nickname} ({roleType})");
                }
            }
            catch (Exception e)
            {
                Logger.Error($"OnHurting error: {e}");
            }
        }

        // ── Core logic ───────────────────────────────────────────────────────

        public void ApplyRoleSettings(Player player)
        {
            try
            {
                if (player == null || !player.IsAlive)
                    return;

                var roleType = player.Role;

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

                    if (roleConfig.Health > 0)
                    {
                        player.MaxHealth = roleConfig.Health;
                        player.Health = roleConfig.Health;
                    }

                    if (roleConfig.HumeShield > 0)
                    {
                        player.MaxHumeShield = roleConfig.HumeShield;
                        player.HumeShield = roleConfig.HumeShield;
                    }

                    ApplyGodMode(player, roleType);

                    if (Config.Debug)
                        Logger.Debug($"[SCPBuff] Applied to {player.Nickname} ({roleType}): HP={player.Health}, HS={player.HumeShield}, God={IsRoleGod(roleType) || roleConfig.IsGod}");
                }
            }
            catch (Exception e)
            {
                Logger.Error($"ApplyRoleSettings error: {e}");
            }
        }

        private void ApplyGodMode(Player player, RoleTypeId roleType)
        {
            bool isGod = IsRoleGod(roleType);
            if (!isGod && Config.RoleConfigs.TryGetValue(roleType, out var cfg))
                isGod = cfg.IsGod;

            player.IsGodModeEnabled = isGod;
        }

        private void HandleDisabledRole(Player player)
        {
            try
            {
                if (RoleExtensions.ProtectedRoles.Contains(player.Role))
                    return;

                player.SetRole(RoleTypeId.Tutorial, RoleChangeReason.RemoteAdmin);

                if (Config.Debug)
                    Logger.Debug($"[SCPBuff] Changed {player.Nickname} to Tutorial (role was disabled)");
            }
            catch (Exception e)
            {
                Logger.Error($"HandleDisabledRole error: {e}");
            }
        }

        // ── Public API ───────────────────────────────────────────────────────

        public bool IsRoleDisabled(RoleTypeId roleType)
        {
            if (RoleExtensions.ProtectedRoles.Contains(roleType)) return false;
            return _disabledRolesCache.TryGetValue(roleType, out bool v) && v;
        }

        public bool IsRoleGod(RoleTypeId roleType)
        {
            if (RoleExtensions.ProtectedRoles.Contains(roleType)) return false;
            return _godModeCache.TryGetValue(roleType, out bool v) && v;
        }

        public bool GetRoleStatus(RoleTypeId roleType)
        {
            if (RoleExtensions.ProtectedRoles.Contains(roleType)) return true;
            return !IsRoleDisabled(roleType);
        }

        public bool GetRoleGodMode(RoleTypeId roleType) => IsRoleGod(roleType);

        public void ToggleRole(RoleTypeId roleType, bool enabled)
        {
            if (RoleExtensions.ProtectedRoles.Contains(roleType))
            {
                Logger.Warn($"Attempted to modify protected role: {roleType}");
                return;
            }

            _disabledRolesCache[roleType] = !enabled;
            SaveDisabledRoles();

            foreach (var player in Player.ReadyList.Where(p => p.IsAlive && p.Role == roleType).ToList())
                ApplyRoleSettings(player);
        }

        public void SetRoleHealth(RoleTypeId roleType, float health, bool permanent = false)
        {
            if (!Config.RoleConfigs.TryGetValue(roleType, out var config)) return;

            config.Health = health;
            if (permanent) SaveRoleConfigs();

            foreach (var player in Player.ReadyList.Where(p => p.IsAlive && p.Role == roleType).ToList())
            {
                player.MaxHealth = health;
                player.Health = health;
            }

            Logger.Info($"Set health for {roleType} to {health} ({(permanent ? "permanent" : "temporary")})");
        }

        public void SetRoleHumeShield(RoleTypeId roleType, float humeShield, bool permanent = false)
        {
            if (!Config.RoleConfigs.TryGetValue(roleType, out var config)) return;

            config.HumeShield = humeShield;
            if (permanent) SaveRoleConfigs();

            foreach (var player in Player.ReadyList.Where(p => p.IsAlive && p.Role == roleType).ToList())
            {
                if (humeShield > 0)
                {
                    player.MaxHumeShield = humeShield;
                    player.HumeShield = humeShield;
                }
            }

            Logger.Info($"Set hume shield for {roleType} to {humeShield} ({(permanent ? "permanent" : "temporary")})");
        }

        public void SetRoleGodMode(RoleTypeId roleType, bool enabled)
        {
            if (RoleExtensions.ProtectedRoles.Contains(roleType) && enabled)
            {
                Logger.Warn($"Cannot enable god mode for protected role: {roleType}");
                return;
            }

            _godModeCache[roleType] = enabled;
            SaveGodModeRoles();

            foreach (var player in Player.ReadyList.Where(p => p.IsAlive && p.Role == roleType).ToList())
                player.IsGodModeEnabled = enabled;

            Logger.Info($"Set god mode for {roleType} to {enabled}");
        }

        public List<string> GetDisabledRoles() =>
            _disabledRolesCache
                .Where(kvp => kvp.Value && !RoleExtensions.ProtectedRoles.Contains(kvp.Key))
                .Select(kvp => kvp.Key.ToString())
                .ToList();

        public List<string> GetGodRoles() =>
            _godModeCache
                .Where(kvp => kvp.Value && !RoleExtensions.ProtectedRoles.Contains(kvp.Key))
                .Select(kvp => kvp.Key.ToString())
                .ToList();

        // ── Persistence ──────────────────────────────────────────────────────

        private void LoadRoleConfigs()
        {
            try
            {
                if (!File.Exists(_roleConfigBackupPath)) return;
                foreach (var line in File.ReadAllLines(_roleConfigBackupPath))
                {
                    var parts = line.Split('=');
                    if (parts.Length < 3) continue;
                    if (!Enum.TryParse<RoleTypeId>(parts[0], out var roleType)) continue;
                    if (!Config.RoleConfigs.TryGetValue(roleType, out var cfg)) continue;
                    if (float.TryParse(parts[1], out float hp)) cfg.Health = hp;
                    if (float.TryParse(parts[2], out float hs)) cfg.HumeShield = hs;
                }
                Logger.Info("Loaded role configs from backup.");
            }
            catch (Exception e) { Logger.Error($"Failed to load role configs: {e}"); }
        }

        private void SaveRoleConfigs()
        {
            try
            {
                File.WriteAllLines(_roleConfigBackupPath,
                    Config.RoleConfigs.Select(kvp => $"{kvp.Key}={kvp.Value.Health}={kvp.Value.HumeShield}"));
            }
            catch (Exception e) { Logger.Error($"Failed to save role configs: {e}"); }
        }

        private void LoadDisabledRoles()
        {
            try
            {
                if (!File.Exists(_configFilePath)) return;
                foreach (var line in File.ReadAllLines(_configFilePath))
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2 && Enum.TryParse<RoleTypeId>(parts[0], out var roleType))
                        _disabledRolesCache[roleType] = parts[1] == "1";
                }
                Logger.Info($"Loaded {_disabledRolesCache.Count} disabled role entries.");
            }
            catch (Exception e) { Logger.Error($"Failed to load disabled roles: {e}"); }
        }

        private void SaveDisabledRoles()
        {
            try
            {
                File.WriteAllLines(_configFilePath,
                    _disabledRolesCache.Select(kvp => $"{kvp.Key}={(kvp.Value ? "1" : "0")}"));
            }
            catch (Exception e) { Logger.Error($"Failed to save disabled roles: {e}"); }
        }

        private void LoadGodModeRoles()
        {
            try
            {
                if (!File.Exists(_godConfigFilePath)) return;
                foreach (var line in File.ReadAllLines(_godConfigFilePath))
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2 && Enum.TryParse<RoleTypeId>(parts[0], out var roleType))
                        _godModeCache[roleType] = parts[1] == "1";
                }
                Logger.Info($"Loaded {_godModeCache.Count} god mode entries.");
            }
            catch (Exception e) { Logger.Error($"Failed to load god mode roles: {e}"); }
        }

        private void SaveGodModeRoles()
        {
            try
            {
                File.WriteAllLines(_godConfigFilePath,
                    _godModeCache.Select(kvp => $"{kvp.Key}={(kvp.Value ? "1" : "0")}"));
            }
            catch (Exception e) { Logger.Error($"Failed to save god mode roles: {e}"); }
        }
    }
}