using System.IO;

using CalApi.API;
using CalApi.Patches;

using HarmonyLib;

namespace CalCustomMusic.Patches;

// ReSharper disable once UnusedType.Global
// ReSharper disable once ClassNeverInstantiated.Global
internal class RegisterTracks : IPatch {
    private const string RootName = "Menus";
    private const string StartingTrackName = "startingTrack.txt";

    private const int DefaultStartingTrack = 7;

    public void Apply() {
        On.PolyMap.MapManager.CallMapLoadedActions += (orig, self, state, mapName) => {
            Register(Path.GetDirectoryName(Path.GetDirectoryName(PolyMap.MapManager.LastLoadedPolyMapPath)), false);
            return orig(self, state, mapName);
        };

        CustomMusic.initialized += (_, _) => UpdateProfile();
        CustomizationProfiles.profileChanged += (_, _) => Music.QueueTrack(UpdateProfile());
    }

    private static int UpdateProfile() {
        string customMenusMusicPath = Path.Combine(CustomizationProfiles.currentPath!, RootName);
        string startingTrackPath = Path.Combine(customMenusMusicPath, StartingTrackName);
        int startingTrack = File.Exists(startingTrackPath) &&
            int.TryParse(File.ReadAllText(startingTrackPath), out int parsedStartingTrack) ?
                parsedStartingTrack : DefaultStartingTrack;

        AccessTools.Field(typeof(Music), "startingTrack").SetValue(CustomMusic.vanillaInstance, startingTrack);
        Register(customMenusMusicPath, true);

        return startingTrack;
    }

    private static void Register(string? mapPackPath, bool force) {
        if(!Directory.Exists(mapPackPath)) return;
        HandleCustomMusicData.updatedFade = true;
        CustomMusic.fade = true;
        HandleCustomMusicData.updatedWaitForEnd = false;
        CustomMusic.waitForEnd = false;
        HandleCustomMusicData.updatedLoop = true;
        CustomMusic.loop = true;
        if(!force && CustomMusic.currentlyLoadedTracksForWorldpack == mapPackPath) return;
        CalCustomMusicPlugin.instance!.StopAllCoroutines();
        CustomMusic.UnregisterAllTracks();
        CustomMusic.logger?.LogInfo($"Registering modded tracks for world pack {mapPackPath}");
        foreach(string file in Directory.GetFiles(mapPackPath)) CustomMusic.RegisterTrack(file);
        CustomMusic.UpdateMusicSelectors();
        CustomMusic.currentlyLoadedTracksForWorldpack = mapPackPath;
    }
}