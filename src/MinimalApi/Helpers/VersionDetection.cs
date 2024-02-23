using System.Diagnostics.CodeAnalysis;

namespace MinimalApi.Helpers;

[SuppressMessage(category: "ReSharper", checkId: "UnusedType.Global", Justification = "Used in exe code. Not possible to unit test.")]
internal static class VersionDetection
{
    [SuppressMessage(category: "ReSharper", checkId: "UnusedMember.Global", Justification = "Used in exe code. Not possible to unit test.")]
    public const string ProgramVersion = ThisAssembly.Info.FileVersion;
}