using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace TrackpadCompanion;

// Notification-area icon and balloon toasts through Shell_NotifyIcon. This path keeps working
// when the app runs elevated, which the driver's control device requires.
public sealed class TrayIcon : IDisposable
{
    const uint NIM_ADD = 0, NIM_MODIFY = 1, NIM_DELETE = 2;
    const uint NIF_MESSAGE = 1, NIF_ICON = 2, NIF_TIP = 4, NIF_INFO = 0x10;
    const uint NIIF_WARNING = 2, NIIF_ERROR = 3, NIIF_INFO = 1, NIIF_NOSOUND = 0x10;
    const uint WM_APP_TRAY = 0x8001, WM_LBUTTONUP = 0x202, WM_RBUTTONUP = 0x205, NIN_BALLOONUSERCLICK = 0x405;
    const uint MF_STRING = 0, MF_SEPARATOR = 0x800, MF_CHECKED = 8;
    const int ID_OPEN = 1, ID_CHECK = 2, ID_PAUSE = 3, ID_EXIT = 4, ID_PRESET = 100;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct NOTIFYICONDATA
    {
        public int cbSize; public IntPtr hWnd; public uint uID; public uint uFlags; public uint uCallbackMessage; public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
        public uint dwState; public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
        public uint dwInfoFlags; public Guid guidItem; public IntPtr hBalloonIcon;
    }
    [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }
    delegate IntPtr SUBCLASSPROC(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, IntPtr id, IntPtr data);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] static extern bool Shell_NotifyIconW(uint msg, ref NOTIFYICONDATA d);
    [DllImport("comctl32.dll")] static extern bool SetWindowSubclass(IntPtr h, SUBCLASSPROC p, IntPtr id, IntPtr data);
    [DllImport("comctl32.dll")] static extern IntPtr DefSubclassProc(IntPtr h, uint msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] static extern IntPtr CreatePopupMenu();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern bool AppendMenuW(IntPtr m, uint flags, UIntPtr id, string? text);
    [DllImport("user32.dll")] static extern int TrackPopupMenuEx(IntPtr m, uint flags, int x, int y, IntPtr h, IntPtr p);
    [DllImport("user32.dll")] static extern bool DestroyMenu(IntPtr m);
    [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT p);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr h);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern uint RegisterWindowMessage(string s);

    readonly IntPtr hwnd;
    readonly SUBCLASSPROC proc;
    readonly uint taskbarCreated;
    IntPtr hIcon = IntPtr.Zero;
    bool added;

    public TrayIcon(MainWindow w)
    {
        Log.Write("Tray: start");
        hwnd = WinRT.Interop.WindowNative.GetWindowHandle(w);
        Log.Write("Tray: hwnd " + hwnd);
        taskbarCreated = RegisterWindowMessage("TaskbarCreated");
        proc = WndProc;
        SetWindowSubclass(hwnd, proc, (IntPtr)1, IntPtr.Zero);
        Log.Write("Tray: subclassed");
        Refresh();
        Log.Write("Tray: icon added");
        App.Monitor.Changed += Refresh;
    }

    NOTIFYICONDATA Base() => new NOTIFYICONDATA
    {
        cbSize = Marshal.SizeOf<NOTIFYICONDATA>(), hWnd = hwnd, uID = 1,
        szTip = "", szInfo = "", szInfoTitle = ""
    };

    public void Refresh()
    {
        int l = App.Monitor.Level;
        var old = hIcon;
        hIcon = MakeIcon(l);
        var d = Base();
        d.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP;
        d.uCallbackMessage = WM_APP_TRAY;
        d.hIcon = hIcon;
        d.szTip = l < 0 ? "Magic Trackpad: battery unavailable" : "Magic Trackpad: " + l + "%";
        Shell_NotifyIconW(added ? NIM_MODIFY : NIM_ADD, ref d);
        added = true;
        if (old != IntPtr.Zero) DestroyIcon(old);
    }

    public void Notify(string title, string text, bool critical)
    {
        var d = Base();
        d.uFlags = NIF_INFO;
        d.szInfoTitle = title.Length > 63 ? title[..63] : title;
        d.szInfo = text.Length > 255 ? text[..255] : text;
        d.dwInfoFlags = (critical ? NIIF_ERROR : NIIF_WARNING) | (App.Cfg.Sound ? 0u : NIIF_NOSOUND);
        Shell_NotifyIconW(NIM_MODIFY, ref d);
    }

    static IntPtr MakeIcon(int level)
    {
        var cfg = App.Cfg;
        Color c = level < 0 ? Color.FromArgb(110, 110, 110)
            : level <= cfg.Critical ? Color.FromArgb(196, 43, 28)
            : level <= cfg.Low ? Color.FromArgb(202, 80, 16)
            : Color.FromArgb(16, 124, 16);
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            using (var path = new GraphicsPath())
            {
                int r = 8, s = 32;
                path.AddArc(0, 0, r, r, 180, 90); path.AddArc(s - r, 0, r, r, 270, 90);
                path.AddArc(s - r, s - r, r, r, 0, 90); path.AddArc(0, s - r, r, r, 90, 90);
                path.CloseFigure();
                using var br = new SolidBrush(c);
                g.FillPath(br, path);
            }
            string t = level < 0 ? "--" : level.ToString();
            using var f = new Font("Segoe UI", t.Length >= 3 ? 14f : 19f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(t, f, Brushes.White, new RectangleF(0, 1, 32, 32), sf);
        }
        return bmp.GetHicon();
    }

    IntPtr WndProc(IntPtr h, uint msg, IntPtr w, IntPtr l, IntPtr id, IntPtr data)
    {
        if (msg == WM_APP_TRAY)
        {
            uint ev = (uint)((long)l & 0xFFFF);
            if (ev == WM_LBUTTONUP || ev == NIN_BALLOONUSERCLICK) App.UI.TryEnqueue(App.MainWin.ShowWindow);
            else if (ev == WM_RBUTTONUP) ShowMenu();
            return IntPtr.Zero;
        }
        if (msg == taskbarCreated && msg != 0) { added = false; Refresh(); }
        return DefSubclassProc(h, msg, w, l);
    }

    void ShowMenu()
    {
        var menu = CreatePopupMenu();
        AppendMenuW(menu, MF_STRING, (UIntPtr)ID_OPEN, "Open Trackpad Companion");
        AppendMenuW(menu, MF_STRING, (UIntPtr)ID_CHECK, "Check battery now");
        AppendMenuW(menu, MF_STRING | (App.Paused ? MF_CHECKED : 0), (UIntPtr)ID_PAUSE, "Pause battery alerts");
        var presets = App.Cfg.Presets;
        if (presets.Count > 0)
        {
            AppendMenuW(menu, MF_SEPARATOR, UIntPtr.Zero, null);
            for (int i = 0; i < presets.Count; i++) AppendMenuW(menu, MF_STRING, (UIntPtr)(ID_PRESET + i), "Preset: " + presets[i].Name);
        }
        AppendMenuW(menu, MF_SEPARATOR, UIntPtr.Zero, null);
        AppendMenuW(menu, MF_STRING, (UIntPtr)ID_EXIT, "Exit");
        GetCursorPos(out var p);
        SetForegroundWindow(hwnd);
        int cmd = TrackPopupMenuEx(menu, 0x100 | 0x2, p.X, p.Y, hwnd, IntPtr.Zero);
        PostMessage(hwnd, 0, IntPtr.Zero, IntPtr.Zero);
        DestroyMenu(menu);
        if (cmd == 0) return;
        App.UI.TryEnqueue(async () =>
        {
            if (cmd == ID_OPEN) App.MainWin.ShowWindow();
            else if (cmd == ID_CHECK) await App.Monitor.CheckAsync();
            else if (cmd == ID_PAUSE) App.Paused = !App.Paused;
            else if (cmd == ID_EXIT) App.Quit();
            else if (cmd >= ID_PRESET && cmd - ID_PRESET < presets.Count)
            {
                try { await presets[cmd - ID_PRESET].Model.ApplyAsync(); Notify("Preset applied", presets[cmd - ID_PRESET].Name, false); }
                catch (Exception e) { Notify("Could not apply preset", e.Message, true); }
            }
        });
    }

    public void Dispose()
    {
        var d = Base();
        Shell_NotifyIconW(NIM_DELETE, ref d);
        if (hIcon != IntPtr.Zero) DestroyIcon(hIcon);
    }
}
