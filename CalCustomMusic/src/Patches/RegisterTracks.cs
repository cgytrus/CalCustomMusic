using System.IO;

using CalApi.API;
using CalApi.Patches;

namespace CalCustomMusic.Patches;

// ReSharper disable once UnusedType.Global
// ReSharper disable once ClassNeverInstantiated.Global
internal class RegisterTracks : IPatch {
    private const string RootName = "Menus";
    private const string ReadmeName = "CustomMusic-README.txt";
    private const string StartingTrackName = "startingTrack.txt";

    private static string? _currentlyLoadedTracksForWorldpack;

    public void Apply() {
        On.PolyMap.MapManager.CallMapLoadedActions += (orig, self, state, mapName) => {
            string? worldpackPath =
                Path.GetDirectoryName(Path.GetDirectoryName(PolyMap.MapManager.LastLoadedPolyMapPath));
            if(_currentlyLoadedTracksForWorldpack == worldpackPath) return orig(self, state, mapName);
            CalCustomMusic.CustomMusic.ReregisterTracks();
            CalCustomMusic.CustomMusic.RegisterCustomTracks(worldpackPath);
            _currentlyLoadedTracksForWorldpack = worldpackPath;
            return orig(self, state, mapName);
        };

        CalCustomMusic.CustomMusic.initialized += (_, _) => {
            _currentlyLoadedTracksForWorldpack = null;
            UpdateProfile();
        };
        CustomizationProfiles.profileChanged += (_, _) => {
            UpdateProfile();
            CalCustomMusic.CustomMusic.TryQueueStartingTrack();
        };

        Directory.CreateDirectory(Path.Combine(CustomizationProfiles.defaultPath, RootName));
        CreateReadme(Path.Combine(CustomizationProfiles.defaultPath, RootName, ReadmeName));
    }

    private static void CreateReadme(string path) {
        if(File.Exists(path)) return;
        File.WriteAllText(path, @"All custom music with supported formats is registered when entering the main menu.
startingTrack.txt contains the music ID or name that would be played in the menus.
Custom tracks don't have numeric IDs, they may have any file name, so an example configuration would be:
2 files in the `Menus` folder:
`startingTrack.txt`, which has *only* 'myCustomMusic' written in it
and `myCustomMusic.ogg`, this track would be played in the menus");
    }

    private static void UpdateProfile() {
        string customMenusMusicPath = Path.Combine(CustomizationProfiles.currentPath!, RootName);
        string startingTrackPath = Path.Combine(customMenusMusicPath, StartingTrackName);
        if(File.Exists(startingTrackPath)) CalCustomMusic.CustomMusic.startingTrack = File.ReadAllText(startingTrackPath);
        CalCustomMusic.CustomMusic.ReregisterTracks();
        CalCustomMusic.CustomMusic.RegisterCustomTracks(customMenusMusicPath);
    }
}
