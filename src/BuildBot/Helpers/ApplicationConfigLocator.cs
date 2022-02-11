using System;
using System.IO;

namespace BuildBot.Helpers;

/// <summary>Locator of the application configuration</summary>
internal static class ApplicationConfigLocator
{
    /// <summary>
    ///     The base path of the folder with the configuration files in them.
    /// </summary>
    public static string ConfigurationFilesPath { get; } = LookupConfigurationFilesPath();

    private static string LookupConfigurationFilesPath()
    {
        return LookupAppSettingsLocationByAssemblyName() ?? Environment.CurrentDirectory;
    }

    private static string? LookupAppSettingsLocationByAssemblyName()
    {
        string? directoryName = Path.GetDirectoryName(AppContext.BaseDirectory);

        if (string.IsNullOrWhiteSpace(directoryName))
        {
            return null;
        }

        return !File.Exists(Path.Combine(path1: directoryName, path2: "appsettings.json"))
            ? null
            : directoryName;
    }
}