using System.Collections;

using CalApi.Patches;

using HarmonyLib;

using UnityEngine;

namespace CalCustomMusic.Patches {
    internal class HandleCustomMusicData : IPatch {
        public static bool updatedFade { get; set; }
        public static bool updatedWaitForEnd { get; set; }
        public static bool updatedLoop { get; set; }
        private static float _prevTime;

        public void Apply() {
            // don't fade
            On.Music.Fade += (orig, self, fadeTo) => {
                if(CustomMusic.fade) return orig(self, fadeTo);
                AccessTools.Field(typeof(Music), "lastFadeTarget").SetValue(self, fadeTo);
                IEnumerator Break() { yield break; }
                return Break();
            };

            // don't switch
            On.Music.CheckQueue += (orig, self) => {
                AudioSource audioSource = (AudioSource)AccessTools.Field(typeof(Music), "audioSource").GetValue(self);
                float time = audioSource.time;
                AudioClip clip = audioSource.clip;
                CustomMusic.musicStoppedThisFrame = time == 0f || time != _prevTime &&
                    (clip && time * 2 - _prevTime >= clip.length || time < _prevTime);
                _prevTime = time;
                bool canContinue = !CustomMusic.waitForEnd || CustomMusic.musicStoppedThisFrame;
                if(!canContinue) return;
                CustomMusic.fade = updatedFade;
                CustomMusic.waitForEnd = updatedWaitForEnd;
                CustomMusic.loop = updatedLoop;
                orig(self);
            };
        }
    }
}
