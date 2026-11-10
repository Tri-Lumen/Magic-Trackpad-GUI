using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace TrackpadCompanion;

public sealed class AppPage : Page
{
    const string TaskName = "Trackpad Companion";

    public AppPage()
    {
        Log.Write("AppPage: ctor start");
        var cfg = App.Cfg;
        var page = Ui.NewPage("App settings", out var root);
        Content = page;

        var startup = Ui.Toggle(TaskExists(), on =>
        {
            var exe = Environment.ProcessPath ?? "";
            if (on) Run("schtasks", $"/Create /TN \"{TaskName}\" /TR \"\\\"{exe}\\\" --tray\" /SC ONLOGON /RL HIGHEST /F");
            else Run("schtasks", $"/Delete /TN \"{TaskName}\" /F");
        });
        root.Children.Add(Ui.Card("Start with Windows", "Runs in the tray at sign-in without a UAC prompt.", startup));
        root.Children.Add(Ui.Card("Keep running in the tray when closed", "Needed for battery alerts when this window is closed.",
            Ui.Toggle(cfg.RunInTray, v => { cfg.RunInTray = v; cfg.Save(); })));

        bool driver = Driver.CanOpen();
        root.Children.Add(Ui.Card("Driver", @"AmtPtpHidFilter · control device \\.\AmtPtpControlDeviceUm",
            new TextBlock { Text = driver ? "Driver found" : "Driver not found (error " + Driver.LastError + "). Run as administrator and check the driver is installed." }));
        root.Children.Add(Ui.Card("Data folder", Config.Dir, null));
    }

    static bool TaskExists() => Run("schtasks", $"/Query /TN \"{TaskName}\"") == 0;

    static int Run(string file, string args)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo(file, args) { CreateNoWindow = true, UseShellExecute = false });
            p!.WaitForExit(8000);
            return p.ExitCode;
        }
        catch { return -1; }
    }
}
