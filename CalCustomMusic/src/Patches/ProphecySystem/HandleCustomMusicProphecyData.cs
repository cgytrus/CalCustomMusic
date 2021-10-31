using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

using CalApi.Patches;

using HarmonyLib;

using ProphecySystem;

using UnityEngine;

using CalMusic = Music;

namespace CalCustomMusic.Patches.ProphecySystem {
    // too lazy to figure out the monomod way (although, should be pretty easy, idk)
    [HarmonyPatch("ProphecySystem.Prophet+<Tell>d__42,Assembly-CSharp", "MoveNext", MethodType.Normal)]
    internal class HandleCustomMusicProphecyData : IPatch {
        public void Apply() => Harmony.CreateAndPatchAll(GetType());

        // ReSharper disable once UnusedMember.Local
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            List<CodeInstruction> codes = new(instructions);
            Label yield14Label = new();

            bool foundSwitch = false;
            int copyIndex = -1;
            int insertIndex = -1;
            int ifEndIndex = -1;
            for(int i = 0; i < codes.Count; i++) {
                CodeInstruction code = codes[i];

                if(!foundSwitch && code.opcode == OpCodes.Switch) {
                    foundSwitch = true;
                    Label[] labels = (Label[])code.operand;
                    Array.Resize(ref labels, labels.Length + 1);
                    labels[labels.Length - 1] = yield14Label;
                    code.operand = labels;
                }

                if(copyIndex == -1 && code.opcode == OpCodes.Newobj &&
                   code.OperandIs(AccessTools.Constructor(typeof(WaitForEndOfFrame)))) {
                    copyIndex = i + 1;
                }

                if(insertIndex != -1 || code.opcode != OpCodes.Isinst || !code.OperandIs(typeof(MusicProphecy)))
                    continue;
                insertIndex = i + 1;
                ifEndIndex = i - 6;
            }

            if(!foundSwitch || copyIndex == -1 || insertIndex == -1) return codes.AsEnumerable();

            codes.RemoveAt(insertIndex);
            codes[insertIndex] = CodeInstruction.Call(typeof(HandleCustomMusicProphecyData), nameof(PerformMusic));
            List<CodeInstruction> codesToInsert = new(10);
            for(int i = copyIndex; i < copyIndex + 9; i++) codesToInsert.Add(codes[i]);
            codesToInsert[2] = new CodeInstruction(OpCodes.Ldc_I4_S, 14);
            codesToInsert[6] = new CodeInstruction(OpCodes.Ldarg_0);
            codesToInsert[6].labels.Add(yield14Label);
            
            codes.InsertRange(insertIndex + 1, codesToInsert);
            codes.Insert(insertIndex + 1,
                CodeInstruction.Call(typeof(MonoBehaviour), nameof(MonoBehaviour.StartCoroutine),
                    new[] { typeof(IEnumerator) }));
            codes.Insert(ifEndIndex + 1, new CodeInstruction(OpCodes.Ldarg_0));
            codes.Insert(ifEndIndex + 2, new CodeInstruction(OpCodes.Ldloc_1));

            return codes.AsEnumerable();
        }
        
        // ReSharper disable once UnusedMember.Global
        public static IEnumerator PerformMusic(MusicProphecy prophecy) {
            if(CustomMusic.waitForEnd) yield return new WaitUntil(() => CustomMusic.musicStoppedThisFrame);
            MusicPerformer(prophecy);
            if(!CalCustomMusic.Patches.HandleCustomMusicData.updatedWaitForEnd) yield break;
            yield return new WaitUntil(() => CustomMusic.musicStoppedThisFrame);
            yield return new WaitWhile(() => CustomMusic.musicStoppedThisFrame);
            yield return new WaitUntil(() => CustomMusic.musicStoppedThisFrame);
        }

        private static void MusicPerformer(MusicProphecy prophecy) {
            CalCustomMusic.Patches.HandleCustomMusicData.updatedFade =
                !(bool)AccessTools.Field(typeof(MusicProphecy), "noFade").GetValue(prophecy);
            CalCustomMusic.Patches.HandleCustomMusicData.updatedWaitForEnd =
                (bool)AccessTools.Field(typeof(MusicProphecy), "waitForEnd").GetValue(prophecy);
            CalCustomMusic.Patches.HandleCustomMusicData.updatedLoop =
                !(bool)AccessTools.Field(typeof(MusicProphecy), "noLoop").GetValue(prophecy);
            CalMusic.QueueTrack(prophecy.musicID);
            CustomMusic.musicStoppedThisFrame = false;
        }
    }
}
