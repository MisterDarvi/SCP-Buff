using System;
using System.Linq;
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

            if (arguments.Count < 2)
            {
                response = GetUsage();
                return false;
            }

            var roleArg = arguments.At(0).ToLower();
            var actionArg = arguments.At(1).ToLower();

            if (roleArg == "list")
            {
                response = GetRoleList();
                return true;
            }

            bool enable;
            if (actionArg == "on" || actionArg == "enable" || actionArg == "true")
                enable = true;
            else if (actionArg == "off" || actionArg == "disable" || actionArg == "false")
                enable = false;
            else
            {
                response = "Invalid action. Use 'on' or 'off'.";
                return false;
            }

            if (roleArg == "all")
            {
                // Process all roles
                int count = 0;
                foreach (var role in SCPBuff.Instance.Config.RoleConfigs.Keys)
                {
                    SCPBuff.Instance.ToggleRole(role, enable);
                    count++;
                }

                response = $"{count} roles have been {(enable ? "enabled" : "disabled")}!";
                return true;
            }

            // Parse role name
            var roleType = ParseRoleType(roleArg);

            if (roleType == RoleTypeId.None)
            {
                response = $"Unknown role: {roleArg}\nUse 'scpbuff list' to see available roles";
                return false;
            }

            SCPBuff.Instance.ToggleRole(roleType, enable);

            response = $"Role {roleType} has been {(enable ? "enabled" : "disabled")}!";
            return true;
        }

        private string GetUsage()
        {
            return "Usage: scpbuff <role/all> <on/off> OR scpbuff list\n" +
                   "Examples:\n" +
                   "  scpbuff scp173 off - Disable SCP-173\n" +
                   "  scpbuff classd on - Enable Class-D\n" +
                   "  scpbuff all on - Enable all roles\n" +
                   "  scpbuff list - List all configurable roles";
        }

        private string GetRoleList()
        {
            var scpRoles = SCPBuff.Instance.Config.RoleConfigs.Keys
                .Where(r => r.ToString().StartsWith("Scp"))
                .Select(r => r.ToString().Replace("Scp", ""))
                .OrderBy(r => r);

            var humanRoles = SCPBuff.Instance.Config.RoleConfigs.Keys
                .Where(r => !r.ToString().StartsWith("Scp"))
                .Select(r => r.ToString())
                .OrderBy(r => r);

            return "Available SCP roles: " + string.Join(", ", scpRoles) + "\n" +
                   "Available human roles: " + string.Join(", ", humanRoles);
        }

        private RoleTypeId ParseRoleType(string input)
        {
            input = input.ToLower();

            // Handle SCP numbers
            if (input.StartsWith("scp"))
                input = input.Substring(3);

            // First try direct parse
            if (Enum.TryParse<RoleTypeId>($"Scp{input}", true, out var roleType))
                return roleType;

            if (Enum.TryParse<RoleTypeId>(input, true, out roleType))
                return roleType;

            // Manual mapping for common names
            switch (input)
            {
                case "173": return RoleTypeId.Scp173;
                case "096": return RoleTypeId.Scp096;
                case "106": return RoleTypeId.Scp106;
                case "049": return RoleTypeId.Scp049;
                case "939": return RoleTypeId.Scp939;
                case "3114": return RoleTypeId.Scp3114;
                case "0492":
                case "zombie": return RoleTypeId.Scp0492;
                case "079": return RoleTypeId.Scp079;

                case "classd":
                case "dclass":
                case "d-class": return RoleTypeId.ClassD;
                case "scientist":
                case "sci": return RoleTypeId.Scientist;
                case "facilityguard":
                case "guard": return RoleTypeId.FacilityGuard;
                case "tutorial": return RoleTypeId.Tutorial;

                case "ntfprivate":
                case "private": return RoleTypeId.NtfPrivate;
                case "ntfsergeant":
                case "sergeant": return RoleTypeId.NtfSergeant;
                case "ntfspecialist":
                case "specialist": return RoleTypeId.NtfSpecialist;
                case "ntfcaptain":
                case "captain": return RoleTypeId.NtfCaptain;

                case "chaosconscript":
                case "conscript": return RoleTypeId.ChaosConscript;
                case "chaosrepressor":
                case "repressor": return RoleTypeId.ChaosRepressor;
                case "chaosmarauder":
                case "marauder": return RoleTypeId.ChaosMarauder;
                case "chaosrifleman":
                case "rifleman": return RoleTypeId.ChaosRifleman;

                default: return RoleTypeId.None;
            }
        }
    }

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class RoleStatusCommand : ICommand
    {
        public string Command { get; } = "rolestatus";
        public string[] Aliases { get; } = new[] { "roleinfo", "buffstatus" };
        public string Description { get; } = "Check status of roles in SCP Buff";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission("scpbuff.admin"))
            {
                response = "You don't have permission to use this command!";
                return false;
            }

            var disabledRoles = SCPBuff.Instance.Config.RoleConfigs.Keys
                .Where(r => !SCPBuff.Instance.GetRoleStatus(r))
                .OrderBy(r => r.ToString())
                .ToList();

            if (disabledRoles.Count == 0)
            {
                response = "All roles are currently enabled.";
            }
            else
            {
                response = $"Disabled roles ({disabledRoles.Count}):\n" +
                          string.Join("\n", disabledRoles.Select(r => $"  • {r}"));
            }

            return true;
        }
    }
}
