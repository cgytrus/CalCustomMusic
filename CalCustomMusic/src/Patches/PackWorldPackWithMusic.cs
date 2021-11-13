using System;
using System.IO;

using CalApi.Patches;

using UnityEngine;

namespace CalCustomMusic.Patches;

// ReSharper disable once UnusedType.Global
internal class PackWorldPackWithMusic : IPatch {
    private const string MusicMarker = "---MUSIC---";
    private const string MusicTrackMarker = "\n---MUSIC TRACK---\n";
    private const string MusicTrackDataMarker = "\n---MUSIC TRACK DATA---\n";

    public void Apply() {
        ApplyPackPatch();
        ApplyUnpackPatch();
    }

    private static void ApplyPackPatch() => On.WorldPacker.PackWorldPack += (orig, packFolder) => {
        string result = orig(packFolder);
        CustomMusic.logger?.LogInfo("Packing modded music");

        string str = MusicMarker;
        foreach(string filePath in Directory.GetFiles(packFolder)) {
            if(!Path.HasExtension(filePath) ||
               CustomMusic.ExtensionToAudioType(Path.GetExtension(filePath)) == AudioType.UNKNOWN) continue;
            byte[] bytes = File.ReadAllBytes(filePath);
            string data = Convert.ToBase64String(bytes);
            str += MusicTrackMarker + Path.GetFileName(filePath) + MusicTrackDataMarker + data;
        }

        return result + str;
    };

    private static void ApplyUnpackPatch() => On.WorldPacker.UnpackPack += (orig, pack, version) => {
        orig(pack, version);
        CustomMusic.logger?.LogInfo("Unpacking modded music");

        string contents = pack.Substring(WorldPacker.packStartMarker.Length,
            pack.IndexOf(WorldPacker.packDataStartMarker, StringComparison.Ordinal) -
            WorldPacker.packStartMarker.Length - 1);
        WorldPackSettings.Settings settings = JsonUtility.FromJson<WorldPackSettings.Settings>(contents);
        string packFolder = Path.Combine(Application.persistentDataPath, WorldPacker.packDirectory,
            WorldPacker.importDirectory, settings.worldPackGUID, settings.worldPackName);

        string[] music = pack.Split(new[] { MusicMarker }, StringSplitOptions.RemoveEmptyEntries);
        for(int i = 1; i < music.Length; i++) {
            string musicData = music[i];
            string[] musicTracks =
                musicData.Split(new[] { MusicTrackMarker }, StringSplitOptions.RemoveEmptyEntries);
            CustomMusic.logger?.LogInfo($"Found {musicTracks.Length.ToString()} tracks");
            foreach(string musicTrack in musicTracks) {
                string[] musicTrackData = musicTrack.Split(new[] { MusicTrackDataMarker },
                    StringSplitOptions.RemoveEmptyEntries);
                string trackName = musicTrackData[0];
                string filePath = Path.Combine(packFolder, trackName);
                CustomMusic.logger?.LogInfo($"Unpacking track {trackName} to {filePath}");
                string trackData = musicTrackData[1];
                byte[] track = Convert.FromBase64String(trackData);
                File.WriteAllBytes(filePath, track);
            }
        }
    };
}