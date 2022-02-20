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

#pragma warning disable CS0649
#pragma warning disable CS0169
    [DataEditorDropdown("Fallback Music",
        new string[] {
            "EDITOR_ROOMSETTINGS_MUSIC_DROPDOWN_NOMUSIC", "Alone in a Hall", "Casual Physics", "Company",
            "Just Add Circles", "Less Cropping Required", "Lighter Atmosphere", "Machines Can Hope", "Neon Lines",
            "Not Enough Shampoo", "Not Fast Enough", "Platforming Cats", "Raycats", "There is no Alteration", "Wind",
            "Editor", "Don't change"
        })]
    // ReSharper disable once NotAccessedField.Local
    [SerializeField] private int musicID;
#pragma warning restore CS0649
#pragma warning restore CS0169

    public override IEnumerator Performer(Prophet prophet, int index) {
        bool finished = false;
        CustomMusic.TryQueueTrack(musicName, loop, fade, waitForEnd, () => finished = true);
        if(!runParallel) yield return new WaitUntil(() => finished);
    }
}
