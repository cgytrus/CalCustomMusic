using System.Collections;
using System.IO;

using CaLAPI.Patches;

namespace CalCustomMusic.Patches {
    // ReSharper disable once UnusedType.Global
    internal class RegisterTracks : IPatch {
        public void Apply() => On.PolyMap.MapManager.CallMapLoadedActions += (orig, self, state, mapName) => {
            IEnumerator original = orig(self, state, mapName);
            return new[] { Prefix(), original }.GetEnumerator();
                
            IEnumerator Prefix() {
                string mapPackPath =
                    Path.GetDirectoryName(Path.GetDirectoryName(PolyMap.MapManager.LastLoadedPolyMapPath));
                HandleCustomMusicData.updatedFade = true;
                CustomMusic.fade = true;
                HandleCustomMusicData.updatedWaitForEnd = false;
                CustomMusic.waitForEnd = false;
                HandleCustomMusicData.updatedLoop = true;
                CustomMusic.loop = true;
                if(CustomMusic.currentlyLoadedTracksForWorldpack != mapPackPath) {
                    CalCustomMusicPlugin.instance.StopAllCoroutines();
                    CustomMusic.UnregisterAllTracks();
                    if(mapPackPath != null) {
                        CustomMusic.logger.LogInfo($"Registering modded tracks for world pack {mapPackPath}");
                        foreach(string file in Directory.GetFiles(mapPackPath)) {
                            yield return CalCustomMusicPlugin.instance.StartCoroutine(CustomMusic.RegisterTrack(file));
                        }
                    }
                    CustomMusic.UpdateMusicSelectors();
                    CustomMusic.currentlyLoadedTracksForWorldpack = mapPackPath;
                }
            }
        };
    }
}
