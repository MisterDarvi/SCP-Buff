using System.Collections.Generic;
using PlayerRoles;

namespace SCPBuff
{
    public class Config
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;

        public Dictionary<RoleTypeId, RoleConfig> RoleConfigs { get; set; } = new Dictionary<RoleTypeId, RoleConfig>
        {
            // SCPs
            [RoleTypeId.Scp173] = new RoleConfig { Health = 4500, HumeShield = 750, IsEnabled = true, IsGod = false },
            [RoleTypeId.Scp096] = new RoleConfig { Health = 3000, HumeShield = 500, IsEnabled = true, IsGod = false },
            [RoleTypeId.Scp106] = new RoleConfig { Health = 2300, HumeShield = 350, IsEnabled = true, IsGod = false },
            [RoleTypeId.Scp049] = new RoleConfig { Health = 2500, HumeShield = 300, IsEnabled = true, IsGod = false },
            [RoleTypeId.Scp939] = new RoleConfig { Health = 2700, HumeShield = 350, IsEnabled = true, IsGod = false },
            [RoleTypeId.Scp3114] = new RoleConfig { Health = 1250, HumeShield = 350, IsEnabled = true, IsGod = false },
            [RoleTypeId.Scp0492] = new RoleConfig { Health = 400, HumeShield = 100, IsEnabled = true, IsGod = false },
            [RoleTypeId.Scp079] = new RoleConfig { Health = 0, HumeShield = 0, IsEnabled = true, IsGod = false },

            // Human Classes
            [RoleTypeId.ClassD] = new RoleConfig { Health = 100, HumeShield = 0, IsEnabled = true, IsGod = false },
            [RoleTypeId.Scientist] = new RoleConfig { Health = 100, HumeShield = 0, IsEnabled = true, IsGod = false },
            [RoleTypeId.FacilityGuard] = new RoleConfig { Health = 100, HumeShield = 0, IsEnabled = true, IsGod = false },

            // Chaos Insurgency
            [RoleTypeId.ChaosConscript] = new RoleConfig { Health = 100, HumeShield = 0, IsEnabled = true, IsGod = false },
            [RoleTypeId.ChaosRepressor] = new RoleConfig { Health = 100, HumeShield = 0, IsEnabled = true, IsGod = false },
            [RoleTypeId.ChaosMarauder] = new RoleConfig { Health = 100, HumeShield = 0, IsEnabled = true, IsGod = false },
            [RoleTypeId.ChaosRifleman] = new RoleConfig { Health = 100, HumeShield = 0, IsEnabled = true, IsGod = false },

            // Foundation Forces
            [RoleTypeId.NtfPrivate] = new RoleConfig { Health = 100, HumeShield = 0, IsEnabled = true, IsGod = false },
            [RoleTypeId.NtfSergeant] = new RoleConfig { Health = 100, HumeShield = 0, IsEnabled = true, IsGod = false },
            [RoleTypeId.NtfSpecialist] = new RoleConfig { Health = 100, HumeShield = 0, IsEnabled = true, IsGod = false },
            [RoleTypeId.NtfCaptain] = new RoleConfig { Health = 100, HumeShield = 0, IsEnabled = true, IsGod = false },

            // Protected / Utility Roles
            [RoleTypeId.Tutorial] = new RoleConfig { Health = 100, HumeShield = 0, IsEnabled = true, IsGod = false },
            [RoleTypeId.Spectator] = new RoleConfig { Health = 0, HumeShield = 0, IsEnabled = true, IsGod = false },
            [RoleTypeId.None] = new RoleConfig { Health = 0, HumeShield = 0, IsEnabled = true, IsGod = false },
            [RoleTypeId.Overwatch] = new RoleConfig { Health = 0, HumeShield = 0, IsEnabled = true, IsGod = false },
            [RoleTypeId.Filmmaker] = new RoleConfig { Health = 0, HumeShield = 0, IsEnabled = true, IsGod = false },
        };
    }

    public class RoleConfig
    {
        public float Health { get; set; }
        public float HumeShield { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool IsGod { get; set; } = false;
    }
}