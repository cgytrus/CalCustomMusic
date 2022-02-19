using BepInEx;
using BepInEx.Configuration;

using CalApi.API;

namespace CalCustomMusic;

[BepInPlugin("mod.cgytrus.plugins.calCustomMusic", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("mod.cgytrus.plugins.calapi", "0.2.5")]
public class CalCustomMusicPlugin : BaseUnityPlugin {
    private readonly ConfigEntry<bool> _fadeMusicOnQuit;

    public CalCustomMusicPlugin() {
        Patches.PackWorldPackWithMusic.logger = Logger;
        _fadeMusicOnQuit = Config.Bind("General", "FadeMusicOnQuit", false, "");
    }

    private void Awake() {
        CustomMusic.Setup(Logger);

        Logger.LogInfo("Applying patches");
        Util.ApplyAllPatches();

        On.ExitScreen.InvokeDelayedQuitGame += (orig, self) => {
            orig(self);
            if(_fadeMusicOnQuit.Value) Music.QueueTrack(0);
        };

        Logger.LogInfo("Registering prophecies");
        Prophecies.RegisterProphecy<CustomMusicProphecy>("cgytrus.music", "MUSIC");
    }
}
