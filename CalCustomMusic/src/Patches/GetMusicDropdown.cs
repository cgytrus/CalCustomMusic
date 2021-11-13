using CalApi.Patches;

using HarmonyLib;

using UnityEngine.UI;

using CalRoomSettingsUI = RSSystem.RoomSettingsUI;

namespace CalCustomMusic.Patches;

// ReSharper disable once ClassNeverInstantiated.Global
internal class GetMusicDropdown : IPatch {
    public static Dropdown? musicDropdown { get; private set; }
    public static int vanillaMusicDropdownOptionsCount { get; private set; }

    public void Apply() => On.RSSystem.RoomSettingsUI.Awake += (orig, self) => {
        orig(self);
        musicDropdown =
            (Dropdown)AccessTools.Field(typeof(RSSystem.RoomSettingsUI), "musicDropdown").GetValue(self);
        vanillaMusicDropdownOptionsCount = musicDropdown.options.Count;
    };
}