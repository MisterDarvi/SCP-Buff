# SCP-Buff
SCPBuff is a plugin for SCP: Secret Laboratory servers that allows you to fine-tune the parameters of SCP objects, including health (HP) and humus shield (HS). The plugin provides administrators with flexible control over the game's balance.

## 🍵Main features
📊 Configure HP and HS for each SCP via the config

🔒 Automatically replace disabled SCPs with other roles (scientist, D-class, security)

⚡ Instantly apply settings when spawning

👑 Special rights for administrators (the ability to spawn any SCP if set to true in the config, if set to false, the administrator will not be able to spawn a specific SCP)

🦴 Special settings for SCP-3114 (skeleton) with HS restriction

## 💿Installation
 Download the latest version of **`SCPBuff.dll`** from the releases

 Place the file in the **`Plugins`** folder of your server

 Configure the **`SCPBuff.yml`** configuration file




# Configuration
## Configuration example:
    is_enabled: true
    debug: false
    scp_configs:
      Scp173:
        health: 5000
        hume_shield: 1238
        is_enabled: true
      Scp096:
        health: 3000
        hume_shield: 550
        is_enabled: true
      Scp3114:
        health: 550
        hume_shield: 100
        is_enabled: true
        
# Work features

- SCP-3114 has a hard limit of 550 HP and a maximum of 100 HS
- When SCP is disabled, regular players receive a random alternative role

# Requirements
**EXILED 9.6.1** or later

# An example in the photo
![scpbuff](https://github.com/user-attachments/assets/08bda5bb-d846-441f-9b08-e3070a33834f)
