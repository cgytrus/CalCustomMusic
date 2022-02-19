using System;
using System.Collections;

using CalApi.API;

using PolyMap;

using ProphecySystem;

using UnityEngine;

namespace CalCustomMusic;

[DataEditorComponent("Custom Music Prophecy")]
[DataEditorComponentControlButtons(
    new[] { nameof(MoveProphecyUp), nameof(MoveProphecyDown), nameof(DeleteProphecy) },
    new[] { "ARROW_UP", "ARROW_DOWN", "REMOVE" })]
public class CustomMusicProphecy : BaseProphecy {
    // DataEditorStringDropdown requires the fields to be public ._.
    [DataEditorStringDropdown("GENERIC_MUSIC")]
    [SerializeField] private string musicName = "";
    // ReSharper disable once InconsistentNaming
    [IgnoreWhenSaving] public static string[] musicName_Options = Array.Empty<string>();
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once NotAccessedField.Global
    [IgnoreWhenSaving] public static string[] musicName_DisplayValues = Array.Empty<string>();

    [DataEditorToggle("Loop")]
    [SerializeField] public bool loop = true;
    [DataEditorToggle("Fade")]
    [SerializeField] public bool fade = true;
    [DataEditorToggle("Wait For End")]
    [SerializeField] public bool waitForEnd;

    [DataEditorToggle("EDITOR_DATAEDITOR_PROPHECYSYSTEM_PROPHECY_RUNPARALLEL_TOGGLE_LABEL")]
    [SerializeField] private bool runParallel = true;

    [DataEditorStringDisplay("Tip:")]
    [IgnoreWhenSaving] public string fallbackTip =
        "To select music that's going to play for vanilla players, add a vanilla music prophecy after this one";

    public override bool skipNext => _skipNext;
    private bool _skipNext;

    public override IEnumerator Performer(Prophet prophet, int index) {
        bool finished = false;
        CustomMusic.TryQueueTrack(musicName, loop, fade, waitForEnd, () => finished = true);
        _skipNext = index + 1 < prophet.prophecies.Count && prophet.prophecies[index + 1] is MusicProphecy;
        if(!runParallel) yield return new WaitUntil(() => finished);
    }
}
