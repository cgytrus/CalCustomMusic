using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Mono.Cecil;

namespace CalCustomMusicPatcher {
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    public static class MusicProphecyPatcher {
        // ReSharper disable once InconsistentNaming
        public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };
        
        public static void Patch(AssemblyDefinition assembly) {
            ModuleDefinition module = assembly.MainModule;
            
            MethodReference toggleAttributeConstructor = module.GetType("DataEditorToggle").Methods[0];

            FieldDefinition noFadeField = new FieldDefinition("noFade", FieldAttributes.Public, module.TypeSystem.Boolean);
            CustomAttribute noFadeAttribute = new CustomAttribute(toggleAttributeConstructor);
            noFadeAttribute.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, "No Fade"));
            noFadeField.CustomAttributes.Add(noFadeAttribute);

            FieldDefinition waitForEndField =
                new FieldDefinition("waitForEnd", FieldAttributes.Public, module.TypeSystem.Boolean);
            CustomAttribute waitForEndAttribute = new CustomAttribute(toggleAttributeConstructor);
            waitForEndAttribute.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String,
                "Wait For End"));
            waitForEndField.CustomAttributes.Add(waitForEndAttribute);

            FieldDefinition noLoopField =
                new FieldDefinition("noLoop", FieldAttributes.Public, module.TypeSystem.Boolean);
            CustomAttribute noLoopAttribute = new CustomAttribute(toggleAttributeConstructor);
            noLoopAttribute.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String,
                "No Loop"));
            noLoopField.CustomAttributes.Add(noLoopAttribute);

            TypeDefinition musicProphecyType = module.GetType("ProphecySystem.MusicProphecy");
            musicProphecyType.Fields.Add(noFadeField);
            musicProphecyType.Fields.Add(waitForEndField);
            musicProphecyType.Fields.Add(noLoopField);
        }
    }
}
