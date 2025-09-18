using BepInEx.Configuration;
using UnityEngine;

namespace SearchMod;


//straight up yonked this from skip game over get owned
public static class Config
{
    public class ConfigGroup(ConfigFile config, string group)
    {
        public ConfigEntry<T> Bind<T>(string key, T defaultValue, string description)
        {
            return config.Bind(group, key, defaultValue, description);
        }

        public ConfigEntry<T> Bind<T>(string key, T defaultValue, ConfigDescription description = null)
        {
            return config.Bind(group, key, defaultValue, description);
        }
    }

    
    public static class General
    {
        public static ConfigEntry<bool> DisableVersionCheck;

        public static void Initialize(ConfigGroup config)
        {
            DisableVersionCheck = config.Bind("Disable Version Check", false, "If you think it'll run on current patch without an update.");
        }
    }

    public static class Keybinds
    {
        public static ConfigEntry<Keys> ToggleKeyCode;
        public static void Initialize(ConfigGroup config)
        {
            ToggleKeyCode = config.Bind("ToggleKeyCode", Keys.Slash, "Keycode for toggling between searching / not searching");
        }
    }

    public static void Initialize(ConfigFile config)
    {
        General.Initialize(new(config, "General"));
        Keybinds.Initialize(new(config, "Keybinds"));
    }
}
