using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace CalCustomMusicPatcher;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
public static class MusicProphecyPatcher {
    // ReSharper disable once InconsistentNaming
    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

    public static void Patch(AssemblyDefinition assembly) {
        ModuleDefinition module = assembly.MainModule;

        MethodReference dropdownAttributeConstructor = module.GetType("DataEditorDropdown").Methods[0];
        MethodReference toggleAttributeConstructor = module.GetType("DataEditorToggle").Methods[0];

        FieldDefinition customMusicIdField = new("customMusicID", FieldAttributes.Public, module.TypeSystem.Int32);
        CustomAttribute customMusicIdAttribute = new(dropdownAttributeConstructor);
        customMusicIdAttribute.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String,
            "Custom Music"));
        customMusicIdAttribute.ConstructorArguments.Add(new CustomAttributeArgument(
            module.TypeSystem.String.MakeArrayType(), new string[] { }));
        customMusicIdField.CustomAttributes.Add(customMusicIdAttribute);

        FieldDefinition useCustomMusicField = new("useCustomMusic", FieldAttributes.Public, module.TypeSystem.Boolean);
        CustomAttribute useCustomMusicAttribute = new(toggleAttributeConstructor);
        useCustomMusicAttribute.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String,
            "Use Custom Music"));
        useCustomMusicField.CustomAttributes.Add(useCustomMusicAttribute);

        FieldDefinition noFadeField = new("noFade", FieldAttributes.Public, module.TypeSystem.Boolean);
        CustomAttribute noFadeAttribute = new(toggleAttributeConstructor);
        noFadeAttribute.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, "No Fade"));
        noFadeField.CustomAttributes.Add(noFadeAttribute);

        FieldDefinition waitForEndField = new("waitForEnd", FieldAttributes.Public, module.TypeSystem.Boolean);
        CustomAttribute waitForEndAttribute = new(toggleAttributeConstructor);
        waitForEndAttribute.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String,
            "Wait For End"));
        waitForEndField.CustomAttributes.Add(waitForEndAttribute);

        FieldDefinition noLoopField = new("noLoop", FieldAttributes.Public, module.TypeSystem.Boolean);
        CustomAttribute noLoopAttribute = new(toggleAttributeConstructor);
        noLoopAttribute.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, "No Loop"));
        noLoopField.CustomAttributes.Add(noLoopAttribute);

        TypeDefinition musicProphecyType = module.GetType("ProphecySystem.MusicProphecy");
        musicProphecyType.Fields.Add(customMusicIdField);
        musicProphecyType.Fields.Add(useCustomMusicField);
        musicProphecyType.Fields.Add(noFadeField);
        musicProphecyType.Fields.Add(waitForEndField);
        musicProphecyType.Fields.Add(noLoopField);
    }
}