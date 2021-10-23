using System.Reflection;

using CaLAPI.Patches;

using UnityEngine;

using CalDataEditor = DataEditor;

namespace CalCustomMusic.Patches {
    // ReSharper disable once UnusedType.Global
    internal class DataEditor : IPatch {
        public void Apply() => On.DataEditor.AddDropdown += (On.DataEditor.orig_AddDropdown orig, CalDataEditor self,
            RectTransform content, MonoBehaviour component, FieldInfo field, string nameTranslationString,
            string[] itemTranslationStrings, ref float yPosition) => {
            if(nameTranslationString == "GENERIC_MUSIC")
                return orig(self, content, component, field, nameTranslationString, CustomMusic.musicProphecyItems,
                    ref yPosition);
            return orig(self, content, component, field, nameTranslationString, itemTranslationStrings, ref yPosition);
        };
    }
}
