namespace TrackpadCompanion;

public static class History
{
    static string FilePath => Path.Combine(Config.Dir, "history.csv");
    static DateTime lastWritten = DateTime.MinValue;
    static int lastLevel = -1;

    public static void Add(int level)
    {
        var now = DateTime.UtcNow;
        if (level == lastLevel && (now - lastWritten).TotalMinutes < 60) return;
        lastLevel = level; lastWritten = now;
        try
        {
            Directory.CreateDirectory(Config.Dir);
            File.AppendAllText(FilePath, new DateTimeOffset(now).ToUnixTimeSeconds() + "," + level + Environment.NewLine);
        }
        catch { }
    }

    public static List<(DateTime Time, int Level)> Load(int days)
    {
        var list = new List<(DateTime, int)>();
        try
        {
            if (!File.Exists(FilePath)) return list;
            var cutoff = DateTime.UtcNow.AddDays(-days);
            foreach (var line in File.ReadAllLines(FilePath))
            {
                var p = line.Split(',');
                if (p.Length != 2 || !long.TryParse(p[0], out var s) || !int.TryParse(p[1], out var l)) continue;
                var t = DateTimeOffset.FromUnixTimeSeconds(s).UtcDateTime;
                if (t >= cutoff) list.Add((t, l));
            }
            // keep the file from growing forever
            if (days >= 14 && list.Count > 0 && new FileInfo(FilePath).Length > 400_000)
                File.WriteAllLines(FilePath, list.Select(x => new DateTimeOffset(x.Item1).ToUnixTimeSeconds() + "," + x.Item2));
        }
        catch { }
        return list;
    }

    // Days left at the current drain rate, from the readings since the last recharge. Null when there is not enough data.
    public static double? EstimateDaysLeft(int currentLevel)
    {
        var pts = Load(14);
        if (pts.Count < 3) return null;
        int start = 0;
        for (int i = 1; i < pts.Count; i++) if (pts[i].Level - pts[i - 1].Level > 3) start = i;
        var seg = pts.Skip(start).ToList();
        if (seg.Count < 3 || (seg[^1].Time - seg[0].Time).TotalHours < 3) return null;
        double t0 = seg[0].Time.Ticks;
        double n = seg.Count, sx = 0, sy = 0, sxy = 0, sxx = 0;
        foreach (var p in seg)
        {
            double x = (p.Time.Ticks - t0) / TimeSpan.TicksPerHour, y = p.Level;
            sx += x; sy += y; sxy += x * y; sxx += x * x;
        }
        double den = n * sxx - sx * sx;
        if (Math.Abs(den) < 1e-9) return null;
        double slope = (n * sxy - sx * sy) / den; // percent per hour
        if (slope >= -0.01) return null;
        return currentLevel / -slope / 24.0;
    }
}

public class BatteryMonitor
{
    public int Level = -1;
    public DateTime? Updated;
    public event Action? Changed;

    Timer? timer;
    bool firedLow, firedCrit, wasReadable, discNotified;
    int fails;
    DateTime lastRemind = DateTime.MinValue;

    public void Start() { ApplyInterval(); _ = CheckAsync(); }

    public void ApplyInterval()
    {
        timer?.Dispose();
        var span = TimeSpan.FromMinutes(Math.Max(1, App.Cfg.IntervalMinutes));
        timer = new Timer(_ => _ = CheckAsync(), null, span, span);
    }

    public async Task CheckAsync()
    {
        int l = await Task.Run(Driver.ReadBattery);
        App.UI.TryEnqueue(() => Apply(l));
    }

    void Apply(int l)
    {
        var cfg = App.Cfg;
        if (l < 0)
        {
            fails++;
            Level = -1;
            if (wasReadable && fails >= 3 && cfg.DisconnectAlert && !discNotified)
            {
                discNotified = true;
                App.Tray.Notify(Loc.T("mon.disc.title"), Loc.T("mon.disc.text"), false);
            }
        }
        else
        {
            fails = 0; wasReadable = true; discNotified = false;
            Level = l; Updated = DateTime.Now;
            History.Add(l);
            if (l > cfg.Low + 4) { firedLow = false; firedCrit = false; }
            if (cfg.NotifyEnabled && !App.Paused)
            {
                bool crit = l <= cfg.Critical, low = l <= cfg.Low;
                var due = (DateTime.Now - lastRemind).TotalMinutes >= cfg.RepeatMinutes;
                if (crit && !firedCrit) { firedCrit = firedLow = true; Notify(true); }
                else if (low && !crit && !firedLow) { firedLow = true; Notify(false); }
                else if (low && cfg.RepeatMinutes > 0 && due) Notify(crit);
            }
        }
        Changed?.Invoke();
    }

    public void Notify(bool critical, int? levelOverride = null)
    {
        int l = levelOverride ?? Level;
        var cfg = App.Cfg;
        var text = (critical ? cfg.CriticalText() : cfg.LowText()).Replace("{level}", l.ToString());
        lastRemind = DateTime.Now;
        App.Tray.Notify(critical ? Loc.T("mon.title.crit") : Loc.T("mon.title.low"), text, critical);
    }
}
