using System;

namespace BuildBot.Helpers;

internal static class StartupBanner
{
    public static void Show()
    {
        const string banner = @"
oooooooooo.               o8o  oooo        .o8  oooooooooo.                .
`888'   `Y8b              `""'  `888       ""888  `888'   `Y8b             .o8
 888     888 oooo  oooo  oooo   888   .oooo888   888     888  .ooooo.  .o888oo
 888oooo888' `888  `888  `888   888  d88' `888   888oooo888' d88' `88b   888
 888    `88b  888   888   888   888  888   888   888    `88b 888   888   888
 888    .88P  888   888   888   888  888   888   888    .88P 888   888   888 .
o888bood8P'   `V88V""V8P' o888o o888o `Y8bod88P"" o888bood8P'  `Y8bod8P'   ""888""


";

        Console.WriteLine(banner);

        string version = VersionDetection.ProgramVersion;
        Console.WriteLine(value: $"Starting version {version}...");
    }
}