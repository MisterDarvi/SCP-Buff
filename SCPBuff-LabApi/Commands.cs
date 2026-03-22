using System;
using System.Linq;
using System.Text;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using PlayerRoles;

namespace SCPBuff.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public class SCPBuffCommand : ICommand
    {
        public string Command { get; } = "scpbuff";
        public string[] Aliases { get; } = { "buff", "scpconfig" };
        public string Description { get; } = "Configure SCP Buff plugin settings";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            // LabAPI permission check via Player.HasPermissions(string)
            var executor = Player.Get(sender);
            if (executor == null || !executor.HasPermissions("scpbuff.admin"))
            {
                response = "You don't have permission to use this command!";
                return false;
            }

            if (arguments.Count < 1)
            {
                response = GetUsage();
                return false;
            }

            switch (arguments.At(0).ToLower())
            {
                case "rolehelp":
                case "roles":
                case "listroles":
                    response = GetRoleList();
                    return true;

                case "god":
                    return HandleGodCommand(arguments, out response);

                case "hp":
                case "health":
                    return HandleHealthCommand(arguments, out response);

                case "hs":
                case "humeshield":
                case "shield":
                    return HandleHumeShieldCommand(arguments, out response);

                case "status":
                case "info":
                    return HandleStatusCommand(arguments, out response);

                case "enable":
                case "disable":
                    return HandleEnableDisableCommand(arguments, arguments.At(0).ToLower(), out response);

                default:
                    response = GetUsage();
                    return false;
            }
        }

        // ── Sub-commands ─────────────────────────────────────────────────────

        private bool HandleGodCommand(ArraySegment<string> arguments, out string response)
        {
            if (arguments.Count < 3)
            {
                response = "Usage: scpbuff god <role> <true/false>\nExample: scpbuff god scp173 true";
                return false;
            }

            var roleType = ParseRoleType(arguments.At(1));
            if (roleType == RoleTypeId.None)
            {
                response = $"Unknown role: {arguments.At(1)}\nUse 'scpbuff rolehelp' to see available roles.";
                return false;
            }

            if (!TryParseBool(arguments.At(2), out bool enable))
            {
                response = "Invalid value. Use true or false.";
                return false;
            }

            if (RoleExtensions.ProtectedRoles.Contains(roleType) && enable)
            {
                response = $"Cannot enable god mode for protected role: {roleType}";
                return false;
            }

            SCPBuff.Instance.SetRoleGodMode(roleType, enable);
            response = $"God mode for {roleType} has been {(enable ? "enabled" : "disabled")}!";
            return true;
        }

        private bool HandleHealthCommand(ArraySegment<string> arguments, out string response)
        {
            if (arguments.Count < 3)
            {
                response = "Usage: scpbuff hp <role> <value>\nExample: scpbuff hp scp173 5000";
                return false;
            }

            var roleType = ParseRoleType(arguments.At(1));
            if (roleType == RoleTypeId.None)
            {
                response = $"Unknown role: {arguments.At(1)}\nUse 'scpbuff rolehelp' to see available roles.";
                return false;
            }

            if (!float.TryParse(arguments.At(2), out float health) || health < 0)
            {
                response = "Invalid health value. Must be a positive number.";
                return false;
            }

            SCPBuff.Instance.SetRoleHealth(roleType, health, permanent: true);
            response = $"Health for {roleType} permanently set to {health}!";
            return true;
        }

        private bool HandleHumeShieldCommand(ArraySegment<string> arguments, out string response)
        {
            if (arguments.Count < 3)
            {
                response = "Usage: scpbuff hs <role> <value>\nExample: scpbuff hs scp173 1000";
                return false;
            }

            var roleType = ParseRoleType(arguments.At(1));
            if (roleType == RoleTypeId.None)
            {
                response = $"Unknown role: {arguments.At(1)}\nUse 'scpbuff rolehelp' to see available roles.";
                return false;
            }

            if (!float.TryParse(arguments.At(2), out float hs) || hs < 0)
            {
                response = "Invalid hume shield value. Must be a positive number.";
                return false;
            }

            SCPBuff.Instance.SetRoleHumeShield(roleType, hs, permanent: true);
            response = $"Hume shield for {roleType} permanently set to {hs}!";
            return true;
        }

        private bool HandleStatusCommand(ArraySegment<string> arguments, out string response)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== SCP Buff Role Status ===");

            if (arguments.Count >= 2)
            {
                var roleType = ParseRoleType(arguments.At(1));
                if (roleType == RoleTypeId.None)
                {
                    response = $"Unknown role: {arguments.At(1)}";
                    return false;
                }

                bool enabled = SCPBuff.Instance.GetRoleStatus(roleType);
                bool god = SCPBuff.Instance.GetRoleGodMode(roleType);

                sb.AppendLine($"Role:     {roleType}");
                sb.AppendLine($"Status:   {(enabled ? "Enabled" : "Disabled")}");
                sb.AppendLine($"God Mode: {(god ? "Yes" : "No")}");

                if (SCPBuff.Instance.Config.RoleConfigs.TryGetValue(roleType, out var cfg))
                {
                    sb.AppendLine($"Health:      {cfg.Health}");
                    sb.AppendLine($"Hume Shield: {cfg.HumeShield}");
                }

                response = sb.ToString();
                return true;
            }

            foreach (var kvp in SCPBuff.Instance.Config.RoleConfigs)
            {
                bool enabled = SCPBuff.Instance.GetRoleStatus(kvp.Key);
                bool god = SCPBuff.Instance.GetRoleGodMode(kvp.Key);
                sb.AppendLine($"  {(enabled ? "+" : "-")} {kvp.Key,-20} HP:{kvp.Value.Health,6}  HS:{kvp.Value.HumeShield,6}  God:{(god ? "Yes" : "No")}");
            }

            response = sb.ToString();
            return true;
        }

        private bool HandleEnableDisableCommand(ArraySegment<string> arguments, string action, out string response)
        {
            if (arguments.Count < 2)
            {
                response = $"Usage: scpbuff {action} <role/all>\nExample: scpbuff {action} scp173";
                return false;
            }

            bool enable = action == "enable";
            string roleArg = arguments.At(1).ToLower();

            if (roleArg == "all")
            {
                int count = 0;
                foreach (var role in SCPBuff.Instance.Config.RoleConfigs.Keys.ToList())
                {
                    if (RoleExtensions.ProtectedRoles.Contains(role)) continue;
                    SCPBuff.Instance.ToggleRole(role, enable);
                    count++;
                }

                response = $"{count} roles {(enable ? "enabled" : "disabled")}. Protected roles remain unchanged.";
                return true;
            }

            var roleType = ParseRoleType(roleArg);
            if (roleType == RoleTypeId.None)
            {
                response = $"Unknown role: {roleArg}\nUse 'scpbuff rolehelp' to see available roles.";
                return false;
            }

            if (RoleExtensions.ProtectedRoles.Contains(roleType))
            {
                response = $"Role {roleType} is protected and cannot be modified!";
                return false;
            }

            SCPBuff.Instance.ToggleRole(roleType, enable);
            response = $"Role {roleType} has been {(enable ? "enabled" : "disabled")}!";
            return true;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static bool TryParseBool(string value, out bool result)
        {
            switch (value.ToLower())
            {
                case "true":
                case "1":
                case "on":
                case "yes":
                    result = true; return true;
                case "false":
                case "0":
                case "off":
                case "no":
                    result = false; return true;
                default:
                    result = false; return false;
            }
        }

        private static string GetUsage() =>
            "SCP Buff Commands:\n" +
            "  scpbuff rolehelp                   - List all available roles\n" +
            "  scpbuff status [role]              - Show status of all/specific role\n" +
            "  scpbuff enable/disable <role/all>  - Enable or disable a role\n" +
            "  scpbuff god <role> <true/false>    - Toggle god mode for a role\n" +
            "  scpbuff hp  <role> <value>         - Set health (permanent)\n" +
            "  scpbuff hs  <role> <value>         - Set hume shield (permanent)\n\n" +
            "Examples:\n" +
            "  scpbuff disable all\n" +
            "  scpbuff god scp173 true\n" +
            "  scpbuff hp  scp173 5000\n" +
            "  scpbuff hs  scp173 1000\n" +
            "  scpbuff status scp173";

        private static string GetRoleList()
        {
            var sb = new StringBuilder();
            var allRoles = Enum.GetValues(typeof(RoleTypeId)).Cast<RoleTypeId>().OrderBy(r => r.ToString()).ToList();

            sb.AppendLine("=== ALL AVAILABLE ROLES ===");

            sb.AppendLine("\n[SCP Roles]");
            foreach (var role in allRoles.Where(r => r.ToString().StartsWith("Scp") && !RoleExtensions.ProtectedRoles.Contains(r)))
                AppendRoleEntry(sb, role);

            sb.AppendLine("\n[Human / MTF / Chaos]");
            foreach (var role in allRoles.Where(r => !r.ToString().StartsWith("Scp") && !RoleExtensions.ProtectedRoles.Contains(r)))
                AppendRoleEntry(sb, role);

            sb.AppendLine("\n[Protected Roles - always enabled]");
            foreach (var role in RoleExtensions.ProtectedRoles)
                sb.AppendLine($"  * {role}");

            sb.AppendLine("\nUsage: full name (Scp173, ClassD) or short (173, classd, guard, zombie...)");
            return sb.ToString();
        }

        private static void AppendRoleEntry(StringBuilder sb, RoleTypeId role)
        {
            string status = SCPBuff.Instance.Config.RoleConfigs.ContainsKey(role)
                ? (SCPBuff.Instance.GetRoleStatus(role) ? "+" : "-") : "?";
            string god = SCPBuff.Instance.GetRoleGodMode(role) ? " [GOD]" : "";
            sb.AppendLine($"  {status} {role}{god}");
        }

        private static RoleTypeId ParseRoleType(string input)
        {
            input = input.ToLower().Trim().Replace(" ", "").Replace("scp", "");

            foreach (RoleTypeId role in Enum.GetValues(typeof(RoleTypeId)))
            {
                string roleName = role.ToString().ToLower();
                if (roleName == input || roleName.Replace("scp", "") == input || roleName.Contains(input))
                    return role;
            }

            if (input == "173") return RoleTypeId.Scp173;
            if (input == "096") return RoleTypeId.Scp096;
            if (input == "106") return RoleTypeId.Scp106;
            if (input == "049") return RoleTypeId.Scp049;
            if (input == "939") return RoleTypeId.Scp939;
            if (input == "3114") return RoleTypeId.Scp3114;
            if (input == "0492" || input == "zombie") return RoleTypeId.Scp0492;
            if (input == "079") return RoleTypeId.Scp079;

            if (input == "classd" || input == "dclass" || input == "d-class") return RoleTypeId.ClassD;
            if (input == "scientist" || input == "sci") return RoleTypeId.Scientist;
            if (input == "facilityguard" || input == "guard") return RoleTypeId.FacilityGuard;
            if (input == "tutorial" || input == "tut") return RoleTypeId.Tutorial;
            if (input == "spectator" || input == "spec") return RoleTypeId.Spectator;
            if (input == "overwatch") return RoleTypeId.Overwatch;
            if (input == "filmmaker") return RoleTypeId.Filmmaker;
            if (input == "ntfprivate" || input == "private") return RoleTypeId.NtfPrivate;
            if (input == "ntfsergeant" || input == "sergeant") return RoleTypeId.NtfSergeant;
            if (input == "ntfspecialist" || input == "specialist") return RoleTypeId.NtfSpecialist;
            if (input == "ntfcaptain" || input == "captain") return RoleTypeId.NtfCaptain;
            if (input == "chaosconscript" || input == "conscript") return RoleTypeId.ChaosConscript;
            if (input == "chaosrepressor" || input == "repressor") return RoleTypeId.ChaosRepressor;
            if (input == "chaosmarauder" || input == "marauder") return RoleTypeId.ChaosMarauder;
            if (input == "chaosrifleman" || input == "rifleman") return RoleTypeId.ChaosRifleman;

            return RoleTypeId.None;
        }
    }
}