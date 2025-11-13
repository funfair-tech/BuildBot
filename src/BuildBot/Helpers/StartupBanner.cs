using System;
using Figgle;

namespace BuildBot.Helpers;

// https://www.figlet.org/examples.html
[GenerateFiggleText("Banner", "basic", "BuildBot")]
internal static partial class StartupBanner
{
    public static void Show()
    {
        Console.WriteLine(Banner);
        Console.WriteLine();
        Console.WriteLine();

        Console.WriteLine(value: "Starting version " + VersionInformation.Version + "...");
    }
}
