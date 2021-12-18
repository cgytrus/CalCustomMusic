using System.IO;

using CalApi.Patches;

using HarmonyLib;

using UnityEngine;

namespace CalCustomMusic.Patches;

// ReSharper disable once UnusedType.Global
// ReSharper disable once ClassNeverInstantiated.Global
internal class RegisterTracks : IPatch {
    private static readonly string menusMusicPath =
        Path.Combine(Application.streamingAssetsPath, "Modded", "Menus");

    // ReSharper disable once HeapView.BoxingAllocation
    public static object startingTrack = 7;

    public void Apply() {
        On.PolyMap.MapManager.CallMapLoadedActions += (orig, self, state, mapName) => {
            Register(Path.GetDirectoryName(Path.GetDirectoryName(PolyMap.MapManager.LastLoadedPolyMapPath)));
            return orig(self, state, mapName);
        };

        CustomMusic.initialized += (_, _) => {
            AccessTools.Field(typeof(Music), "startingTrack").SetValue(CustomMusic.vanillaInstance, startingTrack);
            Register(menusMusicPath);
        };
    }

    private static void Register(string? mapPackPath) {
        if(!Directory.Exists(mapPackPath)) return;
        HandleCustomMusicData.updatedFade = true;
        CustomMusic.fade = true;
        HandleCustomMusicData.updatedWaitForEnd = false;
        CustomMusic.waitForEnd = false;
        HandleCustomMusicData.updatedLoop = true;
        CustomMusic.loop = true;
        if(CustomMusic.currentlyLoadedTracksForWorldpack == mapPackPath) return;
        CalCustomMusicPlugin.instance!.StopAllCoroutines();
        CustomMusic.UnregisterAllTracks();
        CustomMusic.logger?.LogInfo($"Registering modded tracks for world pack {mapPackPath}");
        foreach(string file in Directory.GetFiles(mapPackPath)) CustomMusic.RegisterTrack(file);
        CustomMusic.UpdateMusicSelectors();
        CustomMusic.currentlyLoadedTracksForWorldpack = mapPackPath;
    }
}