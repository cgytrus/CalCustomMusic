using BepInEx;

using CaLAPI.API;

using CalCustomMusic.Patches;

namespace CalCustomMusic {
    [BepInPlugin("mod.cgytrus.plugins.calCustomMusic", "Custom Music", "0.1.0")]
    [BepInDependency("mod.cgytrus.plugins.calapi", "0.2.0")]
    public class CalCustomMusicPlugin : BaseUnityPlugin {
        public static CalCustomMusicPlugin instance { get; private set; }

        private void Awake() {
            instance = this;
            Patches.CustomMusic.logger = Logger;

            Logger.LogInfo("Applying patches");
            Util.ApplyAllPatches();

            Logger.LogInfo("Applying hooks");
            void PolyMapsUnloaded() {
                HandleCustomMusicData.updatedFade = true;
                Patches.CustomMusic.fade = true;
                HandleCustomMusicData.updatedWaitForEnd = false;
                Patches.CustomMusic.waitForEnd = false;
                HandleCustomMusicData.updatedLoop = true;
                Patches.CustomMusic.loop = true;
                // i don't remember why this is commented out
                /*API.Music.UnregisterAllTracks();
                API.Music.UpdateMusicSelectors();*/
            }
            On.PolyMap.MapManager.UnloadAllPolymaps += orig => {
                orig();
                PolyMapsUnloaded();
            };
            On.PolyMap.MapManager.UnloadPolyMap += (orig, self) => {
                orig(self);
                PolyMapsUnloaded();
            };
        }
    }
}
