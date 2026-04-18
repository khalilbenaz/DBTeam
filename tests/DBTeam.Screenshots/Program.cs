// FlaUI-based screenshot automation harness (scaffold).
//
// To enable: add package FlaUI.UIA3 and un-comment the block below.
// Then: dotnet run --project tests/DBTeam.Screenshots -- <path-to-DBTeam.App.exe> <output-dir>
//
// The harness launches the app, waits for the main window, captures each
// document tab one by one, and writes PNGs next to the output directory.
// See docs/bmad/DESIGN-NOTES.md#e18 for the full plan.

using System;
using System.IO;

if (args.Length < 2)
{
    Console.Error.WriteLine("usage: DBTeam.Screenshots <DBTeam.App.exe> <output-dir>");
    return 1;
}

var exe = args[0];
var outDir = args[1];
Directory.CreateDirectory(outDir);

if (!File.Exists(exe))
{
    Console.Error.WriteLine($"Executable not found: {exe}");
    return 2;
}

Console.WriteLine($"[scaffold] Would launch {exe}, capture screens to {outDir}");
Console.WriteLine("Enable FlaUI.UIA3 to produce real images.");

/*
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.UIA3;

var app = Application.Launch(exe);
try
{
    using var automation = new UIA3Automation();
    var main = app.GetMainWindow(automation);
    main.WaitUntilEnabled();
    Capture.Element(main).ToFile(Path.Combine(outDir, "welcome.png"));
    // …drive menus / windows and save more screenshots…
}
finally
{
    app.Close();
}
*/
return 0;
