using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace BuildBot.Helpers;

public static class ApplicationConfigLocator
{
    public static string ConfigurationFilesPath { get; } = LookupConfigurationFilesPath(AppContext.BaseDirectory);

    public static string LookupConfigurationFilesPath(string baseDirectory)
    {
        string? path = LookupAppSettingsLocationByAssemblyName(baseDirectory);

        if (path is null)
        {
            // https://stackoverflow.com/questions/57222718/how-to-configure-self-contained-single-file-program
            return Environment.CurrentDirectory;
        }

        return path;
    }

    private static string? LookupAppSettingsLocationByAssemblyName(string baseDirectory)
    {
        string? path = Path.GetDirectoryName(baseDirectory);

        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string appSettings = Path.Combine(path1: path, path2: "appsettings.json");

        return File.Exists(appSettings) ? path : null;
    }
}
