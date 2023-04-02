using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BuildBot.Helpers;

[SuppressMessage(category: "ReSharper", checkId: "UnusedType.Global", Justification = "Used in exe code. Not possible to unit test.")]
internal static class VersionDetection
{
    [SuppressMessage(category: "ReSharper", checkId: "UnusedMember.Global", Justification = "Used in exe code. Not possible to unit test.")]
    public static string ProgramVersion(Type programType)
    {
        Assembly assembly = programType.Assembly;

        return GetAssemblyFileVersionFile() ?? GetAssemblyFileVersion(assembly) ?? GetAssemblyVersion(assembly);
    }

    private static string? GetAssemblyFileVersionFile()
    {
        IReadOnlyList<string> args = Environment.GetCommandLineArgs();

        if (args.Count == 0)
        {
            return null;
        }

        string location = args[0];

        if (string.IsNullOrWhiteSpace(location) || !File.Exists(location))
        {
            return null;
        }

        FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(location);

        return fileVersionInfo.ProductVersion!;
    }

    private static string? GetAssemblyFileVersion(Assembly assembly)
    {
        AssemblyFileVersionAttribute? fvi = assembly.GetCustomAttributes<AssemblyFileVersionAttribute>()
                                                    .FirstOrDefault();

        if (fvi == null)
        {
            Console.WriteLine("Finding Assembly File Version: No Attribute found");

            return null;
        }

        return fvi.Version;
    }

    private static string GetAssemblyVersion(Assembly assembly)
    {
        Version? assemblyVersion = assembly.GetName()
                                           .Version;

        if (assemblyVersion == null)
        {
            Console.WriteLine("Finding Assembly Version: No Assembly Version");

            throw new VersionNotFoundException();
        }

        return assemblyVersion.ToString();
    }
}