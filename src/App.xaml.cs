using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace TrackpadCompanion;

public partial class App : Application
{
    public static Config Cfg = Config.Load();
    public static BatteryMonitor Monitor = new();
    public static TrayIcon Tray = null!;
    public static MainWindow MainWin = null!;
    public static DispatcherQueue UI = null!;
    public static bool Exiting;
    public static bool Paused;
    static Mutex? mutex;

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, e) => { Log.Fatal("xaml", e.Exception); e.Handled = true; };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try { Launch(); }
        catch (Exception e) { Log.Fatal("OnLaunched", e); Environment.Exit(1); }
    }

    void Launch()
    {
        Log.Write("OnLaunched");
        bool first;
        mutex = new Mutex(true, "TrackpadCompanion.Single", out first);
        var showEvt = new EventWaitHandle(false, EventResetMode.AutoReset, "TrackpadCompanion.Show");
        if (!first) { showEvt.Set(); Environment.Exit(0); return; }

        UI = DispatcherQueue.GetForCurrentThread();
        MainWin = new MainWindow();
        Tray = new TrayIcon(MainWin);
        Task.Run(() => { while (true) { showEvt.WaitOne(); UI.TryEnqueue(MainWin.ShowWindow); } });
        Log.Write("window and tray created");
        Monitor.Start();
        if (!Environment.GetCommandLineArgs().Contains("--tray")) MainWin.ShowWindow();
        Log.Write("started");
    }

    public static void Quit()
    {
        Exiting = true;
        try { Tray.Dispose(); } catch { }
        Environment.Exit(0);
    }
}
