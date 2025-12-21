using System.Collections.Generic;
using Exiled.API.Interfaces;
using PlayerRoles;

namespace SCPBuff
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;

        public Dictionary<RoleTypeId, RoleConfig> RoleConfigs { get; set; } = new Dictionary<RoleTypeId, RoleConfig>
        {
            // SCPs
            [RoleTypeId.Scp173] = new RoleConfig { Health = 4500, HumeShield = 750, IsEnabled = true },
            [RoleTypeId.Scp096] = new RoleConfig { Health = 3000, HumeShield = 500, IsEnabled = true },
            [RoleTypeId.Scp106] = new RoleConfig { Health = 2300, HumeShield = 350, IsEnabled = true },
            [RoleTypeId.Scp049] = new RoleConfig { Health = 2500, HumeShield = 300, IsEnabled = true },
            [RoleTypeId.Scp939] = new RoleConfig { Health = 2700, HumeShield = 350, IsEnabled = true },
            [RoleTypeId.Scp3114] = new RoleConfig { Health = 1250, HumeShield = 350, IsEnabled = true },
            [RoleTypeId.Scp0492] = new RoleConfig { Health = 400, HumeShield = 0, IsEnabled = true },
            [RoleTypeId.Scp079] = new RoleConfig { Health = 0, HumeShield = 0, IsEnabled = true },

            // Human Classes
            [RoleTypeId.ClassD] = new RoleConfig { Health = 100, HumeShield = 0, IsEnabled = true },
            [RoleTypeId.Scientist] = new RoleConfig { Health = 100, HumeShield = 0, IsEnabled = true },
            [RoleTypeId.FacilityGuard] = new RoleConfig { Health = 100, HumeShield = 50, IsEnabled = true },

            // Chaos Insurgency
            [RoleTypeId.ChaosConscript] = new RoleConfig { Health = 120, HumeShield = 50, IsEnabled = true },
            [RoleTypeId.ChaosRepressor] = new RoleConfig { Health = 140, HumeShield = 100, IsEnabled = true },
            [RoleTypeId.ChaosMarauder] = new RoleConfig { Health = 160, HumeShield = 150, IsEnabled = true },
            [RoleTypeId.ChaosRifleman] = new RoleConfig { Health = 130, HumeShield = 75, IsEnabled = true },

            // Foundation Forces
            [RoleTypeId.NtfPrivate] = new RoleConfig { Health = 110, HumeShield = 50, IsEnabled = true },
            [RoleTypeId.NtfSergeant] = new RoleConfig { Health = 130, HumeShield = 100, IsEnabled = true },
            [RoleTypeId.NtfSpecialist] = new RoleConfig { Health = 150, HumeShield = 150, IsEnabled = true },
            [RoleTypeId.NtfCaptain] = new RoleConfig { Health = 170, HumeShield = 200, IsEnabled = true },

            // Tutorial & Other
            [RoleTypeId.Tutorial] = new RoleConfig { Health = 100, HumeShield = 0, IsEnabled = true },
        };
    }

    public class RoleConfig
    {
        public float Health { get; set; }
        public float HumeShield { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}