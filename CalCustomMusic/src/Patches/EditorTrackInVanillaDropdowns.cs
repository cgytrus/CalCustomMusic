using System;
using System.Reflection;

using CalApi.Patches;

using HarmonyLib;

using Language;

using UnityEngine;
using UnityEngine.UI;

namespace CalCustomMusic.Patches;

// ReSharper disable once UnusedType.Global
internal class EditorTrackInVanillaDropdowns : IPatch {
    public void Apply() {
        On.RSSystem.RoomSettingsUI.Awake += (orig, self) => {
            orig(self);
            Dropdown musicDropdown =
                (Dropdown)AccessTools.Field(typeof(RSSystem.RoomSettingsUI), "musicDropdown").GetValue(self);
            Music music = (Music)AccessTools.Field(typeof(Music), "instance").GetValue(null);
            AudioClip[] tracks = (AudioClip[])AccessTools.Field(typeof(Music), "tracks").GetValue(music);
            if(musicDropdown.options.Count >= tracks.Length + 1) return;
            musicDropdown.options.Add(new Dropdown.OptionData("Editor"));
            // hack: the game will try to set the track but will fail because it doesn't exist
            musicDropdown.options.Add(new Dropdown.OptionData("Don't change"));

            musicDropdown.GetComponent<UILanguageSetter>().keys.Add("");
            musicDropdown.GetComponent<UILanguageSetter>().keys.Add("");
        };

        On.DataEditor.AddDropdown += (On.DataEditor.orig_AddDropdown orig, DataEditor self, RectTransform content,
            MonoBehaviour component, FieldInfo field, string nameTranslationString, string[] itemTranslationStrings,
            ref float yPosition) => {
            if(nameTranslationString != "GENERIC_MUSIC")
                return orig(self, content, component, field, nameTranslationString, itemTranslationStrings,
                    ref yPosition);
            Array.Resize(ref itemTranslationStrings, itemTranslationStrings.Length + 1);
            itemTranslationStrings[itemTranslationStrings.Length - 1] = "Editor";
            return orig(self, content, component, field, nameTranslationString, itemTranslationStrings, ref yPosition);
        };
    }
}
