using System.Collections;
using System.IO;

using CalApi.Patches;

namespace CalCustomMusic.Patches;

// ReSharper disable once UnusedType.Global
internal class RegisterTracks : IPatch {
    public void Apply() => On.PolyMap.MapManager.CallMapLoadedActions += (orig, self, state, mapName) => {
        Register();
        return orig(self, state, mapName);
    };

    private static void Register() {
        string? mapPackPath = Path.GetDirectoryName(Path.GetDirectoryName(PolyMap.MapManager.LastLoadedPolyMapPath));
        HandleCustomMusicData.updatedFade = true;
        CustomMusic.fade = true;
        HandleCustomMusicData.updatedWaitForEnd = false;
        CustomMusic.waitForEnd = false;
        HandleCustomMusicData.updatedLoop = true;
        CustomMusic.loop = true;
        if(CustomMusic.currentlyLoadedTracksForWorldpack == mapPackPath) return;
        CalCustomMusicPlugin.instance!.StopAllCoroutines();
        CustomMusic.UnregisterAllTracks();
        if(mapPackPath != null) {
            CustomMusic.logger?.LogInfo($"Registering modded tracks for world pack {mapPackPath}");
            foreach(string file in Directory.GetFiles(mapPackPath)) {
                CustomMusic.RegisterTrack(file);
            }
        }
        CustomMusic.UpdateMusicSelectors();
        CustomMusic.currentlyLoadedTracksForWorldpack = mapPackPath;
    }
}