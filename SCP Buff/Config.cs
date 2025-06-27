using System.Collections.Generic;
using Exiled.API.Interfaces;
using PlayerRoles;

namespace SCPBuff
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;

        public Dictionary<RoleTypeId, ScpConfig> ScpConfigs { get; set; } = new Dictionary<RoleTypeId, ScpConfig>
        {
            [RoleTypeId.Scp173] = new ScpConfig { Health = 4500, HumeShield = 750, IsEnabled = true },
            [RoleTypeId.Scp096] = new ScpConfig { Health = 3000, HumeShield = 500, IsEnabled = true },
            [RoleTypeId.Scp106] = new ScpConfig { Health = 2300, HumeShield = 350, IsEnabled = true },
            [RoleTypeId.Scp049] = new ScpConfig { Health = 2500, HumeShield = 300, IsEnabled = true },
            [RoleTypeId.Scp939] = new ScpConfig { Health = 2700, HumeShield = 350, IsEnabled = true },
            [RoleTypeId.Scp3114] = new ScpConfig { Health = 1250, HumeShield = 350, IsEnabled = true },
            [RoleTypeId.Scp0492] = new ScpConfig { Health = 400, HumeShield = 0, IsEnabled = true },
            [RoleTypeId.Scp079] = new ScpConfig { Health = 0, HumeShield = 0, IsEnabled = true } // SCP-079 was added to the plugin in version V1.0.1
        };
    }

    public class ScpConfig
    {
        public float Health { get; set; }
        public float HumeShield { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}