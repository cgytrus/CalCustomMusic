using BepInEx;
using BepInEx.Configuration;

using CalApi.API;

using CalCustomMusic.Patches;

namespace CalCustomMusic;

[BepInPlugin("mod.cgytrus.plugins.calCustomMusic", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("mod.cgytrus.plugins.calapi", "0.2.3")]
public class CalCustomMusicPlugin : BaseUnityPlugin {
    public static CalCustomMusicPlugin? instance { get; private set; }

    private readonly ConfigEntry<bool> _fadeMusicOnQuit;

    public CalCustomMusicPlugin() {
        instance = this;
        Patches.CustomMusic.logger = Logger;

        Logger.LogInfo("Loading settings");

        ConfigEntry<int> menusMusic = Config.Bind("General", "MenusMusic", 7, "");
        menusMusic.SettingChanged += (_, _) => RegisterTracks.startingTrack = menusMusic.BoxedValue;
        RegisterTracks.startingTrack = menusMusic.BoxedValue;

        _fadeMusicOnQuit = Config.Bind("General", "FadeMusicOnQuit", false, "");
    }

    private void Awake() {
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
        }
        On.PolyMap.MapManager.UnloadAllPolymaps += orig => {
            orig();
            PolyMapsUnloaded();
        };
        On.PolyMap.MapManager.UnloadPolyMap += (orig, self) => {
            orig(self);
            PolyMapsUnloaded();
        };

        On.ExitScreen.InvokeDelayedQuitGame += (orig, self) => {
            orig(self);
            if(_fadeMusicOnQuit.Value) Music.QueueTrack(0);
        };
    }
}