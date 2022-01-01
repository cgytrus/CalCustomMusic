using System.IO;

using CalApi.API;
using CalApi.Patches;

using HarmonyLib;

namespace CalCustomMusic.Patches;

// ReSharper disable once UnusedType.Global
// ReSharper disable once ClassNeverInstantiated.Global
internal class RegisterTracks : IPatch {
    private const string RootName = "Menus";
    private const string ReadmeName = "CustomMusic-README.txt";
    private const string StartingTrackName = "startingTrack.txt";

    private const int DefaultStartingTrack = 7;

    public void Apply() {
        On.PolyMap.MapManager.CallMapLoadedActions += (orig, self, state, mapName) => {
            Register(Path.GetDirectoryName(Path.GetDirectoryName(PolyMap.MapManager.LastLoadedPolyMapPath)), false);
            return orig(self, state, mapName);
        };

        CustomMusic.initialized += (_, _) => UpdateProfile();
        CustomizationProfiles.profileChanged += (_, _) => Music.QueueTrack(UpdateProfile());

        Directory.CreateDirectory(Path.Combine(CustomizationProfiles.defaultPath, RootName));
        CreateReadme(Path.Combine(CustomizationProfiles.defaultPath, RootName, ReadmeName));
    }

    private static void CreateReadme(string path) {
        if(File.Exists(path)) return;
        File.WriteAllText(path, @"All custom music with supported formats is registered when entering the main menu.
startingTrack.txt contains the music ID that would be played in the menus.
Custom music IDs start at 16, custom music may have any file name, so an example configuration would be:
2 files in the `Menus` folder:
`startingTrack.txt`, which has *only* the number 16 written in it
and `myCustomMusic.ogg`, this track would be loaded under music ID 16 and played in the menus");
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