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
        var page = Ui.NewPage(Loc.T("nav.app"), out var root);
        Content = page;

        var startup = Ui.Toggle(TaskExists(), on =>
        {
            var exe = Environment.ProcessPath ?? "";
            if (on) Run("schtasks", $"/Create /TN \"{TaskName}\" /TR \"\\\"{exe}\\\" --tray\" /SC ONLOGON /RL HIGHEST /F");
            else Run("schtasks", $"/Delete /TN \"{TaskName}\" /F");
        });
        root.Children.Add(Ui.Card(Loc.T("ap.startup.title"), Loc.T("ap.startup.desc"), startup));
        root.Children.Add(Ui.Card(Loc.T("ap.tray.title"), Loc.T("ap.tray.desc"),
            Ui.Toggle(cfg.RunInTray, v => { cfg.RunInTray = v; cfg.Save(); })));

        bool driver = Driver.CanOpen();
        root.Children.Add(Ui.Card(Loc.T("ap.driver.title"), @"AmtPtpHidFilter · control device \\.\AmtPtpControlDeviceUm",
            new TextBlock { Text = driver ? Loc.T("ap.driver.ok") : Loc.F("ap.driver.fail", Driver.LastError) }));
        root.Children.Add(Ui.Card(Loc.T("ap.data"), Config.Dir, null));

        var lang = new ComboBox { MinWidth = 220 };
        lang.Items.Add(Loc.T("ap.lang.auto"));
        foreach (var l in Loc.Languages) lang.Items.Add(l.Name);
        lang.SelectedIndex = Math.Max(0, Array.FindIndex(Loc.Languages, l => l.Code == cfg.Language) + 1);
        lang.SelectionChanged += (_, _) =>
        {
            int i = lang.SelectedIndex;
            cfg.Language = i <= 0 ? "auto" : Loc.Languages[i - 1].Code;
            cfg.Save();
        };
        root.Children.Add(Ui.Card(Loc.T("ap.lang.title"), Loc.T("ap.lang.desc"), lang));
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
