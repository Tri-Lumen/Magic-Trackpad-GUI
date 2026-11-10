using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TrackpadCompanion;

// Writes %APPDATA%\TrackpadCompanion\app.log and shows a message box on a fatal error,
// because a windowed app that crashes at startup otherwise disappears without a trace.
static class Log
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int MessageBoxW(IntPtr h, string text, string caption, uint type);

    static string LogPath => Path.Combine(Config.Dir, "app.log");

    public static void Write(string s)
    {
        try
        {
            Directory.CreateDirectory(Config.Dir);
            File.AppendAllText(LogPath, DateTime.Now.ToString("s") + " " + s + Environment.NewLine);
        }
        catch { }
    }

    public static int Step(string s) { Write(s); return 0; }

    public static void Fatal(string where, Exception e)
    {
        Write("FATAL in " + where + ": " + e);
        try { MessageBoxW(IntPtr.Zero, e.GetType().Name + ": " + e.Message + "\n\nDetails were saved to:\n" + LogPath, "Trackpad Companion", 0x10); } catch { }
    }

    [ModuleInitializer]
    public static void Init()
    {
        Write("--- process start " + Environment.ProcessPath);
        AppDomain.CurrentDomain.UnhandledException += (_, a) =>
            Fatal("unhandled", a.ExceptionObject as Exception ?? new Exception(a.ExceptionObject?.ToString() ?? "unknown"));
        TaskScheduler.UnobservedTaskException += (_, a) => Write("task exception: " + a.Exception);
    }
}
