using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using BepInEx.Logging;

using CalApi.Patches;

using HarmonyLib;

using Mono.Cecil.Cil;

using MonoMod.Cil;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

using Object = UnityEngine.Object;

namespace CalCustomMusic.Patches;

// ReSharper disable once ClassNeverInstantiated.Global
internal class CustomMusic : IPatch {
    internal static ManualLogSource? logger { get; set; }

    public static event EventHandler? initialized;

    public static Music? vanillaInstance { get; private set; }
    public static AudioSource? audioSource { get; private set; }
    public static int vanillaTracksCount { get; private set; }
    public static bool fade { get; set; } = true;
    public static bool waitForEnd { get; set; }
    public static bool loop {
        get => !audioSource || audioSource!.loop;
        set { if(audioSource) audioSource!.loop = value; }
    }

    public static bool musicStoppedThisFrame { get; set; }
    public static string? currentlyLoadedTracksForWorldpack { get; set; }

    public static IReadOnlyList<AudioClip?>? queue => _queue;

    public static string?[] musicProphecyItems => _musicProphecyItems;
    private static string?[] _musicProphecyItems = { };

    private static List<AudioClip?>? _queue;

    private static readonly FieldInfo tracks = AccessTools.Field(typeof(Music), "tracks");

    public void Apply() => IL.Music.Start += il => {
        ILCursor cursor = new(il);
        cursor.GotoNext(code => code.MatchCall<Music>(nameof(Music.QueueTrack)));
        cursor.Index -= 2;
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit<CustomMusic>(OpCodes.Call, nameof(Initialize));
    };

    private static void Initialize(Music vanillaInstance) {
        CustomMusic.vanillaInstance = vanillaInstance;
        audioSource = (AudioSource)AccessTools.Field(typeof(Music), "audioSource").GetValue(vanillaInstance);
        _queue = (List<AudioClip?>)AccessTools.Field(typeof(Music), "queue").GetValue(vanillaInstance);
        vanillaTracksCount = ((AudioClip[])tracks.GetValue(vanillaInstance)).Length;
        initialized?.Invoke(null, EventArgs.Empty);
    }

    public static void RegisterTrack(string path) {
        string fullPath = $"file://{path}";
        string extension = Path.GetExtension(path);
        AudioType audioType = ExtensionToAudioType(extension);
        if(audioType == AudioType.UNKNOWN) return;
        using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(fullPath, audioType);
        UnityWebRequestAsyncOperation requestOperation = request.SendWebRequest();
        while(!requestOperation.isDone) { }
        if(request.isNetworkError || request.isHttpError) logger?.LogError(request.error);
        else {
            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            clip.name = Path.GetFileNameWithoutExtension(path);
            RegisterTrack(clip);
        }
    }

    private static void RegisterTrack(AudioClip clip) {
        logger?.LogInfo($"Registering modded track {clip.name}");

        AudioClip[] tracks = (AudioClip[])CustomMusic.tracks.GetValue(vanillaInstance);
        Array.Resize(ref tracks, tracks.Length + 1);
        tracks[tracks.Length - 1] = clip;
        CustomMusic.tracks.SetValue(vanillaInstance, tracks);

        UpdateMusicSelectors(tracks);
    }

    public static void UnregisterAllTracks() {
        logger?.LogInfo("Unregistering all modded tracks");

        AudioClip[] tracks = (AudioClip[])CustomMusic.tracks.GetValue(vanillaInstance);
        for(int i = vanillaTracksCount; i < tracks.Length; i++) {
            AudioClip track = tracks[i];
            if(track != null) Object.Destroy(track);
        }
        Array.Resize(ref tracks, vanillaTracksCount);
        CustomMusic.tracks.SetValue(vanillaInstance, tracks);

        currentlyLoadedTracksForWorldpack = null;
    }

    public static void UpdateMusicSelectors() =>
        UpdateMusicSelectors((AudioClip[])tracks.GetValue(vanillaInstance));

    private static void UpdateMusicSelectors(IReadOnlyList<AudioClip> tracks) {
        if(!GetMusicDropdown.musicDropdown) return;

        logger?.LogInfo("Updating music selectors");

        string emptyTrackName = GetMusicDropdown.musicDropdown!.options[0].text;

        Array.Resize(ref _musicProphecyItems, tracks.Count);
        GetMusicDropdown.musicDropdown.ClearOptions();

        for(int i = 0; i < tracks.Count; i++) {
            string trackName = i == 0 ? emptyTrackName : tracks[i].name;
            _musicProphecyItems[i] = trackName;
            GetMusicDropdown.musicDropdown.options.Add(new Dropdown.OptionData(trackName));
        }

        GetMusicDropdown.musicDropdown.RefreshShownValue();
    }

    public static AudioType ExtensionToAudioType(string extension) {
        switch(extension) {
            case ".acc": return AudioType.ACC;
            case ".aiff": return AudioType.AIFF;
            case ".it": return AudioType.IT;
            case ".mod": return AudioType.MOD;
            case ".mp2":
            case ".mp3": return AudioType.MPEG;
            case ".ogg": return AudioType.OGGVORBIS;
            case ".s3m": return AudioType.S3M;
            case ".wav": return AudioType.WAV;
            case ".xm": return AudioType.XM;
            case ".xma": return AudioType.XMA;
            case ".vag": return AudioType.VAG;
        }

        return AudioType.UNKNOWN;
    }
}