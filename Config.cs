using BepInEx.Configuration;
using RiftOfTheNecroManager;
using UnityEngine;

namespace SearchMod;

//straight up yonked this from skip game over get owned
public static class Config
{
    public static class Keybinds
    {
        const string GROUP = "Keybinds";
        public static Setting<KeyCode> ToggleSearchBar { get; } = new(GROUP, "Toggle Search Bar", KeyCode.Slash, "Press to toggle the search bar when viewing custom tracks.");
    }
}