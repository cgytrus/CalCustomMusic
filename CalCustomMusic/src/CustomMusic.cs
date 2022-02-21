using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

using BepInEx.Logging;

using CalCustomMusic.Patches;

using HarmonyLib;

using Mono.Cecil.Cil;

using MonoMod.Cil;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace CalCustomMusic;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public static class CustomMusic {
    public struct QueuedTrack {
        public AudioClip? clip { get; }
        public bool loop { get; }
        public bool fade { get; }
        public bool waitForEnd { get; }
        public Action? onLoop { get; }

        public QueuedTrack(AudioClip clip, bool loop, bool fade, bool waitForEnd, Action? onLoop) {
            this.clip = clip;
            this.loop = loop;
            this.fade = fade;
            this.waitForEnd = waitForEnd;
            this.onLoop = onLoop;
        }
    }

    public static event EventHandler? initialized;

    public static string startingTrack { get; internal set; } = "";

    private static readonly Dictionary<string, AudioClip?> tracks = new();
    private static readonly Queue<QueuedTrack> queue = new();

    private static ManualLogSource? _logger;
    private static Music? _music;
    private static AudioSource? _source;
    private static QueuedTrack? _currentTrack;
    private static bool _forceSkipCurrentTrack;
    private static float _fadeSpeed;

    internal static void Setup(ManualLogSource logger) {
        _logger = logger;
        _logger.LogInfo("Setting up");

        IL.Music.Start += il => {
            ILCursor cursor = new(il);
            cursor.GotoNext(code => code.MatchCall<Music>(nameof(Music.QueueTrack)));
            cursor.Index -= 2;
            cursor.RemoveRange(3);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate((Music music) => {
                _music = music;
                Initialize();
                initialized?.Invoke(null, EventArgs.Empty);
                TryQueueStartingTrack();
            });
        };
        On.Music.CheckQueue += (_, _) => { };
        On.Music.QueueTrack += (_, id) => { TryQueueTrack(id); };

        On.PolyMap.MapManager.UnloadAllPolymaps += orig => {
            orig();
            _forceSkipCurrentTrack = true;
        };
        On.PolyMap.MapManager.UnloadPolyMap += (orig, mapParent) => {
            orig(mapParent);
            _forceSkipCurrentTrack = true;
        };
    }

    private static void Initialize() {
        _logger?.LogInfo("Initializing");

        tracks.Clear();
        queue.Clear();
        startingTrack = "";
        _currentTrack = null;

        _source = (AudioSource)AccessTools.Field(typeof(Music), "audioSource").GetValue(null);
        _fadeSpeed = (float)AccessTools.Field(typeof(Music), "musicFadeSpeed").GetValue(null);
        if(!_music) return;
        _music!.StopAllCoroutines();
        _music.StartCoroutine(ManageQueue());
        _music.StartCoroutine(WatchCurrentTrackLoop());
    }

    private static void RegisterVanillaTracks() {
        _logger?.LogInfo("Registering vanilla tracks");

        AudioClip[] tracks = (AudioClip[])AccessTools.Field(typeof(Music), "tracks").GetValue(_music);
        foreach(AudioClip? audioClip in tracks) RegisterTrack(audioClip);
    }

    public static void RegisterCustomTracks(string? path) {
        if(!Directory.Exists(path)) return;
        _logger?.LogInfo($"Registering custom tracks at {path}");
        foreach(string file in Directory.GetFiles(path)) RegisterTrack(file);
    }

    public static void ReregisterTracks() {
        _logger?.LogInfo("Reregistering tracks");
        UnregisterAllTracks();
        RegisterVanillaTracks();
    }

    private static void UnregisterAllTracks() {
        _logger?.LogInfo("Unregistering tracks");
        tracks.Clear();
        UpdateProphecyDropdown();
    }

    public static void RegisterTrack(string path) {
        string fullPath = $"file://{path}";
        string extension = Path.GetExtension(path);
        AudioType audioType = ExtensionToAudioType(extension);
        if(audioType == AudioType.UNKNOWN) return;
        using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(fullPath, audioType);
        UnityWebRequestAsyncOperation requestOperation = request.SendWebRequest();
        while(!requestOperation.isDone) { }
        if(request.isNetworkError || request.isHttpError) _logger?.LogError(request.error);
        else {
            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            clip.name = Path.GetFileNameWithoutExtension(path);
            RegisterTrack(clip);
        }
    }

    public static void RegisterTrack(AudioClip? clip) => RegisterTrack(clip ? clip!.name : null, clip);
    public static void RegisterTrack(string? name, AudioClip? clip) {
        _logger?.LogInfo(name is null ? "Registering nothing" : $"Registering track {name}");
        tracks.Add(name ?? "No music", clip);
        UpdateProphecyDropdown();
    }

    // ReSharper disable once UnusedMethodReturnValue.Global
    public static bool TryQueueStartingTrack() => TryQueueTrack(startingTrack) ||
                                                  TryQueueTrack((int)AccessTools.Field(typeof(Music), "startingTrack")
                                                      .GetValue(_music));

    public static bool TryQueueTrack(string name, bool loop = true, bool fade = true, bool waitForEnd = false,
        Action? onLoop = null) {
        if(!TryGetTrack(name, out AudioClip? clip)) {
            _logger?.LogWarning($"Track {name} not found!");
            return false;
        }
        QueueTrack(new QueuedTrack(clip!, loop, fade, waitForEnd, onLoop));
        return true;
    }

    public static bool TryQueueTrack(int id, bool loop = true, bool fade = true, bool waitForEnd = false,
        Action? onLoop = null) {
        if(!TryGetTrack(id, out AudioClip? clip)) {
            _logger?.LogWarning($"Track {id.ToString()} not found!");
            return false;
        }
        QueueTrack(new QueuedTrack(clip!, loop, fade, waitForEnd, onLoop));
        return true;
    }

    public static void QueueTrack(QueuedTrack track) => queue.Enqueue(track);

    public static bool TryGetTrack(string name, out AudioClip? clip) =>
        tracks.TryGetValue(name, out clip) || int.TryParse(name, out int id) && TryGetTrack(id, out clip);

    public static bool TryGetTrack(int id, out AudioClip? clip) {
        AudioClip[] tracks = (AudioClip[])AccessTools.Field(typeof(Music), "tracks").GetValue(_music);
        if(id >= 0 && id < tracks.Length) {
            clip = tracks[id];
            return true;
        }

        clip = default;
        return false;
    }

    private static IEnumerator ManageQueue() {
        while(true) {
            yield return WaitUntilQueueReady(_currentTrack);
            _currentTrack = queue.Dequeue();
            bool same = _source!.clip == _currentTrack.Value.clip;
            if(!same && _currentTrack.Value.fade) yield return Fade(0f);
            PlayTrack(_currentTrack.Value, same);
            if(!same && _currentTrack.Value.fade) yield return Fade(1f);
            yield return null;
        }
        // ReSharper disable once IteratorNeverReturns
    }

    private static IEnumerator WaitUntilQueueReady(QueuedTrack? currentTrack) {
        yield return new WaitUntil(() => queue.Count > 0);
        if(currentTrack is not { waitForEnd: true }) yield break;
        yield return WaitUntilCurrentTrackLoop();
    }

    private static void PlayTrack(QueuedTrack track, bool same) {
        if(!_source) return;
        string logBeginning = same ? "Updating" : "Playing";
        _logger?.LogInfo(track.clip ? $"{logBeginning} track {track.clip!.name}" : $"{logBeginning} nothing");

        _source!.loop = track.loop;
        if(same) return;
        _source.clip = track.clip;
        _source.Play();
    }

    private static IEnumerator WatchCurrentTrackLoop() {
        if(!_source) yield return null;
        while(true) {
            yield return WaitUntilCurrentTrackLoop();
            _currentTrack?.onLoop?.Invoke();
        }
        // ReSharper disable once IteratorNeverReturns
    }

    private static IEnumerator WaitUntilCurrentTrackLoop() {
        if(!_source) yield break;
        float prevTime = 0f;
        yield return new WaitUntil(() => {
            if(!_source!.loop) return !_source.isPlaying || _forceSkipCurrentTrack;
            if(_source.time < prevTime) return true;
            prevTime = _source.time;
            return _forceSkipCurrentTrack;
        });
        _forceSkipCurrentTrack = false;
    }

    private static IEnumerator Fade(float endVolume) {
        if(!_source) yield break;

        float startTime = Time.time;
        float startVolume = _source!.volume / Music.volumeMultiplier;

        FieldInfo fading = AccessTools.Field(typeof(Music), "fading");
        fading.SetValue(_music, true);

        float t = 0f;
        while(t < 1f) {
            t = (Time.time - startTime) * _fadeSpeed;
            _source.volume = Mathf.Lerp(startVolume, endVolume, t) * Music.volumeMultiplier;
            yield return null;
        }

        AccessTools.Field(typeof(Music), "lastFadeTarget").SetValue(_music, endVolume);
        fading.SetValue(_music, false);

        _source.volume = endVolume * Music.volumeMultiplier;
    }

    private static void UpdateProphecyDropdown() {
        CustomMusicProphecy.musicName_Options = tracks.Keys.ToArray();
        CustomMusicProphecy.musicName_DisplayValues = CustomMusicProphecy.musicName_Options;
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
