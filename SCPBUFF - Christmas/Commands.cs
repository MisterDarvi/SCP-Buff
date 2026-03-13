using System;
using System.Linq;
using System.Text;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using PlayerRoles;

namespace SCPBuff.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public class SCPBuffCommand : ICommand
    {
        public string Command { get; } = "scpbuff";
        public string[] Aliases { get; } = new[] { "buff", "scpconfig" };
        public string Description { get; } = "Configure SCP Buff plugin settings";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission("scpbuff.admin"))
            {
                response = "You don't have permission to use this command!";
                return false;
            }

            if (arguments.Count < 1)
            {
                response = GetUsage();
                return false;
            }

            var subCommand = arguments.At(0).ToLower();

            if (subCommand == "rolehelp" || subCommand == "roles" || subCommand == "listroles")
            {
                response = GetRoleList();
                return true;
            }
            else if (subCommand == "god")
            {
                return HandleGodCommand(arguments, sender, out response);
            }
            else if (subCommand == "hp" || subCommand == "health")
            {
                return HandleHealthCommand(arguments, sender, out response);
            }
            else if (subCommand == "hs" || subCommand == "humeshield" || subCommand == "shield")
            {
                return HandleHumeShieldCommand(arguments, sender, out response);
            }
            else if (subCommand == "status" || subCommand == "info")
            {
                return HandleStatusCommand(arguments, sender, out response);
            }
            else if (subCommand == "enable" || subCommand == "disable")
            {
                return HandleEnableDisableCommand(arguments, subCommand, sender, out response);
            }
            else
            {
                response = GetUsage();
                return false;
            }
        }

        private bool HandleGodCommand(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 3)
            {
                response = "Usage: scpbuff god <role> <true/false>\nExample: scpbuff god scp173 true";
                return false;
            }

            var roleArg = arguments.At(1);
            var roleType = ParseRoleType(roleArg);

            if (roleType == RoleTypeId.None)
            {
                response = $"Unknown role: {roleArg}\nUse 'scpbuff rolehelp' to see available roles";
                return false;
            }

            bool enable;
            var valueArg = arguments.At(2).ToLower();
            if (valueArg == "true" || valueArg == "1" || valueArg == "on" || valueArg == "yes")
            {
                enable = true;
            }
            else if (valueArg == "false" || valueArg == "0" || valueArg == "off" || valueArg == "no")
            {
                enable = false;
            }
            else
            {
                response = "Invalid value. Use true or false.";
                return false;
            }

            // Protected roles always have god mode false
            if (RoleExtensions.ProtectedRoles.Contains(roleType) && enable)
            {
                response = $"Cannot enable god mode for protected role: {roleType}";
                return false;
            }

            SCPBuff.Instance.SetRoleGodMode(roleType, enable);
            response = $"God mode for {roleType} has been {(enable ? "enabled" : "disabled")}!";
            return true;
        }

        private bool HandleHealthCommand(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 3)
            {
                response = "Usage: scpbuff hp <role> <value>\nExample: scpbuff hp scp173 5000";
                return false;
            }

            var roleArg = arguments.At(1);
            var roleType = ParseRoleType(roleArg);

            if (roleType == RoleTypeId.None)
            {
                response = $"Unknown role: {roleArg}\nUse 'scpbuff rolehelp' to see available roles";
                return false;
            }

            float health;
            if (!float.TryParse(arguments.At(2), out health) || health < 0)
            {
                response = "Invalid health value. Must be a positive number.";
                return false;
            }

            SCPBuff.Instance.SetRoleHealth(roleType, health, true);
            response = $"Health for {roleType} has been permanently set to {health}!";
            return true;
        }

        private bool HandleHumeShieldCommand(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 3)
            {
                response = "Usage: scpbuff hs <role> <value>\nExample: scpbuff hs scp173 1000";
                return false;
            }

            var roleArg = arguments.At(1);
            var roleType = ParseRoleType(roleArg);

            if (roleType == RoleTypeId.None)
            {
                response = $"Unknown role: {roleArg}\nUse 'scpbuff rolehelp' to see available roles";
                return false;
            }

            float humeShield;
            if (!float.TryParse(arguments.At(2), out humeShield) || humeShield < 0)
            {
                response = "Invalid hume shield value. Must be a positive number.";
                return false;
            }

            SCPBuff.Instance.SetRoleHumeShield(roleType, humeShield, true);
            response = $"Hume shield for {roleType} has been permanently set to {humeShield}!";
            return true;
        }

        private bool HandleStatusCommand(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== SCP Buff Role Status ===");

            if (arguments.Count >= 2)
            {
                var roleArg = arguments.At(1);
                var roleType = ParseRoleType(roleArg);

                if (roleType == RoleTypeId.None)
                {
                    response = $"Unknown role: {roleArg}";
                    return false;
                }

                bool enabled = SCPBuff.Instance.GetRoleStatus(roleType);
                bool god = SCPBuff.Instance.GetRoleGodMode(roleType);

                if (SCPBuff.Instance.Config.RoleConfigs.TryGetValue(roleType, out var config))
                {
                    sb.AppendLine($"Role: {roleType}");
                    sb.AppendLine($"  Protected: {RoleExtensions.ProtectedRoles.Contains(roleType)}");
                    sb.AppendLine($"  Enabled: {enabled}");
                    sb.AppendLine($"  Health: {config.Health}");
                    sb.AppendLine($"  Hume Shield: {config.HumeShield}");
                    sb.AppendLine($"  God Mode: {god}");
                }
                else
                {
                    sb.AppendLine($"No configuration found for {roleType}");
                }
            }
            else
            {
                var disabledRoles = SCPBuff.Instance.GetDisabledRoles();
                var godRoles = SCPBuff.Instance.GetGodRoles();

                sb.AppendLine($"Total roles configured: {SCPBuff.Instance.Config.RoleConfigs.Count}");
                sb.AppendLine($"Protected roles: {string.Join(", ", RoleExtensions.ProtectedRoles)}");
                sb.AppendLine($"Disabled roles: {(disabledRoles.Count == 0 ? "None" : string.Join(", ", disabledRoles))}");
                sb.AppendLine($"God mode roles: {(godRoles.Count == 0 ? "None" : string.Join(", ", godRoles))}");
                sb.AppendLine("\nUse 'scpbuff status <role>' for detailed info");
            }

            response = sb.ToString();
            return true;
        }

        private bool HandleEnableDisableCommand(ArraySegment<string> arguments, string action, ICommandSender sender, out string response)
        {
            if (arguments.Count < 2)
            {
                response = $"Usage: scpbuff {action} <role/all>\nExample: scpbuff {action} scp173";
                return false;
            }

            var roleArg = arguments.At(1).ToLower();
            bool enable = action == "enable";

            if (roleArg == "all")
            {
                int count = 0;
                foreach (var role in SCPBuff.Instance.Config.RoleConfigs.Keys.ToList())
                {
                    if (RoleExtensions.ProtectedRoles.Contains(role))
                        continue;

                    SCPBuff.Instance.ToggleRole(role, enable);
                    count++;
                }

                response = $"{count} roles have been {(enable ? "enabled" : "disabled")}! (Protected roles: Tutorial, Spectator, None, Overwatch, Filmmaker remain unchanged)";
                return true;
            }

            var roleType = ParseRoleType(roleArg);

            if (roleType == RoleTypeId.None)
            {
                response = $"Unknown role: {roleArg}\nUse 'scpbuff rolehelp' to see available roles";
                return false;
            }

            if (RoleExtensions.ProtectedRoles.Contains(roleType))
            {
                response = $"Role {roleType} is protected and cannot be {(enable ? "enabled/disabled" : "disabled")}!";
                return false;
            }

            SCPBuff.Instance.ToggleRole(roleType, enable);

            response = $"Role {roleType} has been {(enable ? "enabled" : "disabled")}!";
            return true;
        }

        private string GetUsage()
        {
            return "SCP Buff Commands:\n" +
                   "  scpbuff rolehelp                    - List all available roles\n" +
                   "  scpbuff status [role]               - Show status of all roles or specific role\n" +
                   "  scpbuff enable/disable <role/all>   - Enable or disable a role\n" +
                   "  scpbuff god <role> <true/false>     - Enable/disable god mode for a role\n" +
                   "  scpbuff hp <role> <value>           - PERMANENTLY set health for a role\n" +
                   "  scpbuff hs <role> <value>           - PERMANENTLY set hume shield for a role\n\n" +
                   "Examples:\n" +
                   "  scpbuff disable all                  - Disable all non-protected roles (players spawn as Tutorial)\n" +
                   "  scpbuff god scp173 true             - Make SCP-173 immortal\n" +
                   "  scpbuff hp scp173 5000              - Permanently set SCP-173 health to 5000\n" +
                   "  scpbuff hs scp173 1000              - Permanently set SCP-173 hume shield to 1000\n" +
                   "  scpbuff status scp173                - Check SCP-173 status";
        }

        private string GetRoleList()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== ALL AVAILABLE ROLES ===");

            var allRoles = Enum.GetValues(typeof(RoleTypeId)).Cast<RoleTypeId>().ToList();
            allRoles.Sort((a, b) => a.ToString().CompareTo(b.ToString()));

            var scpRoles = new System.Collections.Generic.List<RoleTypeId>();
            var humanRoles = new System.Collections.Generic.List<RoleTypeId>();
            var otherRoles = new System.Collections.Generic.List<RoleTypeId>();
            var protectedRoles = new System.Collections.Generic.List<RoleTypeId>();

            foreach (var role in allRoles)
            {
                if (RoleExtensions.ProtectedRoles.Contains(role))
                {
                    protectedRoles.Add(role);
                }
                else if (role.ToString().StartsWith("Scp") && role != RoleTypeId.Scp0492)
                {
                    scpRoles.Add(role);
                }
                else if (role == RoleTypeId.Scp0492 || role == RoleTypeId.ClassD || role == RoleTypeId.Scientist ||
                         role.ToString().StartsWith("Facility") || role.ToString().StartsWith("Ntf") ||
                         role.ToString().StartsWith("Chaos"))
                {
                    humanRoles.Add(role);
                }
                else
                {
                    otherRoles.Add(role);
                }
            }

            sb.AppendLine("\n🔴 SCP Roles:");
            foreach (var role in scpRoles)
            {
                string status = SCPBuff.Instance.Config.RoleConfigs.ContainsKey(role) ?
                    (SCPBuff.Instance.GetRoleStatus(role) ? "✓" : "✗") : "?";
                string god = SCPBuff.Instance.GetRoleGodMode(role) ? " [GOD]" : "";
                sb.AppendLine($"  {status} {role}{god}");
            }

            sb.AppendLine("\n👤 Human Roles:");
            foreach (var role in humanRoles)
            {
                string status = SCPBuff.Instance.Config.RoleConfigs.ContainsKey(role) ?
                    (SCPBuff.Instance.GetRoleStatus(role) ? "✓" : "✗") : "?";
                string god = SCPBuff.Instance.GetRoleGodMode(role) ? " [GOD]" : "";
                sb.AppendLine($"  {status} {role}{god}");
            }

            sb.AppendLine("\n⚪ Other Roles:");
            foreach (var role in otherRoles)
            {
                if (RoleExtensions.ProtectedRoles.Contains(role)) continue;
                string status = SCPBuff.Instance.Config.RoleConfigs.ContainsKey(role) ?
                    (SCPBuff.Instance.GetRoleStatus(role) ? "✓" : "✗") : "?";
                string god = SCPBuff.Instance.GetRoleGodMode(role) ? " [GOD]" : "";
                sb.AppendLine($"  {status} {role}{god}");
            }

            sb.AppendLine("\n🛡️ Protected Roles (always enabled, cannot be disabled):");
            foreach (var role in protectedRoles)
            {
                sb.AppendLine($"  • {role}");
            }

            sb.AppendLine("\n📝 Usage in commands:");
            sb.AppendLine("  • Use full role name: Scp173, ClassD, NtfCaptain");
            sb.AppendLine("  • Or short names: 173, classd, captain, zombie, guard");

            return sb.ToString();
        }

        private RoleTypeId ParseRoleType(string input)
        {
            input = input.ToLower().Replace(" ", "").Replace("scp", "");

            // Try direct parse
            foreach (RoleTypeId role in Enum.GetValues(typeof(RoleTypeId)))
            {
                if (role.ToString().ToLower() == input ||
                    role.ToString().ToLower().Replace("scp", "") == input ||
                    role.ToString().ToLower().Contains(input))
                {
                    return role;
                }
            }

            // Manual mapping (old style)
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