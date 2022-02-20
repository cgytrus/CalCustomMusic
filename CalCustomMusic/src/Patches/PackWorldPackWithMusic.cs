using System;
using System.IO;
using System.IO.Compression;

using BepInEx.Logging;

using CalApi.Patches;

using HarmonyLib;

using Mono.Cecil.Cil;

using MonoMod.Cil;

using UnityEngine;

namespace CalCustomMusic.Patches;

// ReSharper disable once ClassNeverInstantiated.Global
internal class PackWorldPackWithMusic : IPatch {
    private const string MusicMarker = "---MUSIC---";
    private const string MusicTrackMarker = "\n---MUSIC TRACK---\n";
    private const string MusicTrackDataMarker = "\n---MUSIC TRACK DATA---\n";
    private const string GzipFlag = "gzip\n";

    internal static ManualLogSource? logger { get; set; }

    private static string _lastWorldPackPath = "";

    // ReSharper disable once UnusedMember.Global
    public void Apply() {
        ApplyPackPatch();
        ApplyUnpackPatch();
    }

    private static void ApplyPackPatch() => On.WorldPacker.PackWorldPack += (orig, packFolder) => {
        string result = orig(packFolder);
        logger?.LogInfo("Packing modded music");

        string str = MusicMarker;
        foreach(string filePath in Directory.GetFiles(packFolder)) {
            if(!Path.HasExtension(filePath) ||
               CustomMusic.ExtensionToAudioType(Path.GetExtension(filePath)) == AudioType.UNKNOWN) continue;
            byte[] bytes = File.ReadAllBytes(filePath);
            bytes = Compress(bytes);
            string data = Convert.ToBase64String(bytes);
            str = $"{str}{MusicTrackMarker}{Path.GetFileName(filePath)}{MusicTrackDataMarker}{GzipFlag}{data}";
        }

        return result + str;
    };

    private static void ApplyUnpackPatch() {
        // what the fuck what the fuck what the fuck what the fuck what the fuck what the fuck what the fuck what the
        // fuck what the fuck what the fuck what the fuck what the fuck what the fuck what the fuck what the fuck what
        // the fuck what the fuck what the fuck what the fuck what the fuck what the fuck what the fuck what the fuck
        On.WorldPacker.UnpackWorld += (orig, pathToWorldPackFolder, world, version) => {
            _lastWorldPackPath = pathToWorldPackFolder;
            orig(pathToWorldPackFolder, world, version);
        };
        IL.WorldPacker.UnpackPack += il => {
            ILCursor cursor = new(il);
            cursor.GotoNext(code => code.MatchCall<WorldPacker>("UnpackWorld"));
            cursor.GotoNext(code => code.MatchLeaveS(out _));

            // UnpackMusic(pack);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Call, AccessTools.Method(typeof(PackWorldPackWithMusic), nameof(UnpackMusic)));
        };
    }

    private static void UnpackMusic(string pack) {
        string packFolder = _lastWorldPackPath;
        logger?.LogInfo("Unpacking modded music");

        string[] music = pack.Split(new[] { MusicMarker }, StringSplitOptions.RemoveEmptyEntries);
        for(int i = 1; i < music.Length; i++) {
            string musicData = music[i];
            string[] musicTracks =
                musicData.Split(new[] { MusicTrackMarker }, StringSplitOptions.RemoveEmptyEntries);

            logger?.LogInfo($"Found {musicTracks.Length.ToString()} tracks");
            foreach(string musicTrack in musicTracks) {
                string[] musicTrackData = musicTrack.Split(new[] { MusicTrackDataMarker },
                    StringSplitOptions.RemoveEmptyEntries);
                string trackName = musicTrackData[0];
                string filePath = Path.Combine(packFolder, trackName);

                logger?.LogInfo($"Unpacking track {trackName} to {filePath}");
                string trackData = musicTrackData[1];
                bool gzip = trackData.StartsWith(GzipFlag, StringComparison.Ordinal);
                if(gzip) trackData = trackData.Substring(GzipFlag.Length);
                byte[] track = Convert.FromBase64String(trackData);
                if(gzip) track = Decompress(track);
                File.WriteAllBytes(filePath, track);
            }
        }
    }

    // https://stackoverflow.com/a/25135871/10484146
    private static byte[] Compress(byte[] input) {
        using MemoryStream result = new();
        byte[] lengthBytes = BitConverter.GetBytes(input.Length);
        result.Write(lengthBytes, 0, 4);

        using GZipStream compressionStream = new(result, CompressionMode.Compress);
        compressionStream.Write(input, 0, input.Length);
        compressionStream.Flush();
        return result.ToArray();
    }

    private static byte[] Decompress(byte[] input) {
        using MemoryStream source = new(input);
        byte[] lengthBytes = new byte[4];
        source.Read(lengthBytes, 0, 4);

        int length = BitConverter.ToInt32(lengthBytes, 0);
        using GZipStream decompressionStream = new(source, CompressionMode.Decompress);
        byte[] result = new byte[length];
        decompressionStream.Read(result, 0, length);
        return result;
    }
}
