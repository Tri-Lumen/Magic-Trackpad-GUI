using System.Text.Json;

namespace TrackpadCompanion;

// Trackpad settings in the same terms the UI uses. Converts to and from the driver's registry values.
public class TrackpadModel
{
    public int Mode { get; set; }          // 0 macOS click options, 1 maximum, 2 disabled
    public int Strength { get; set; } = 1; // 0 light, 1 medium, 2 firm
    public bool Silent { get; set; }
    public int StopMode { get; set; }      // 0 do nothing, 1 touch size, 2 pressure
    public int SizeValue { get; set; } = 7;
    public int PressureValue { get; set; } = 0;
    public bool IgnoreButtonFinger { get; set; } = true;
    public bool IgnoreNearFingers { get; set; } = true;
    public bool PalmRejection { get; set; } = true;

    public TrackpadModel Clone() => (TrackpadModel)MemberwiseClone();

    public Driver.Raw ToRaw()
    {
        var r = new Driver.Raw();
        if (Mode == 2) { r.ButtonDisabled = 1; r.FeedbackClick = 0; r.FeedbackRelease = 0; }
        else if (Mode == 1) { r.ButtonDisabled = 0; r.FeedbackClick = 16777215; r.FeedbackRelease = 16777215; }
        else
        {
            r.ButtonDisabled = 0;
            r.FeedbackClick = Strength == 0 ? 263189 : Strength == 1 ? 394775 : 526366;
            r.FeedbackRelease = Strength == 0 ? 16 : Strength == 1 ? 20 : 131608;
            if (Silent) { r.FeedbackClick &= 255; r.FeedbackRelease &= 255; }
        }
        r.StopPressure = -1; r.StopSize = -1;
        if (StopMode == 1) r.StopSize = SizeValue;
        else if (StopMode == 2) r.StopPressure = PressureValue;
        r.IgnoreButtonFinger = IgnoreButtonFinger ? 1 : 0;
        r.IgnoreNearFingers = IgnoreNearFingers ? 1 : 0;
        r.PalmRejection = PalmRejection ? 1 : 0;
        return r;
    }

    public static TrackpadModel FromRaw(Driver.Raw r)
    {
        var m = new TrackpadModel();
        if (r.ButtonDisabled != 0) m.Mode = 2;
        else if (r.FeedbackClick == 16777215 && r.FeedbackRelease == 16777215) m.Mode = 1;
        else
        {
            m.Mode = 0;
            m.Silent = (r.FeedbackClick & 0xFFFF00) == 0 && (r.FeedbackRelease & 0xFFFF00) == 0;
            int c = r.FeedbackClick & 255, f = r.FeedbackRelease & 255;
            m.Strength = (c == 21 && f == 16) ? 0 : (c == 30 && f == 24) ? 2 : 1;
        }
        if (r.StopPressure == -1 && r.StopSize == -1) m.StopMode = 0;
        else if (r.StopPressure != -1) { m.StopMode = 2; m.PressureValue = r.StopPressure; }
        else { m.StopMode = 1; m.SizeValue = r.StopSize; }
        m.IgnoreButtonFinger = r.IgnoreButtonFinger != 0;
        m.IgnoreNearFingers = r.IgnoreNearFingers != 0;
        m.PalmRejection = r.PalmRejection != 0;
        return m;
    }

    // Writes the registry, restarts the touchpad device, then asks the driver to reload.
    public Task ApplyAsync()
    {
        var raw = ToRaw();
        return Task.Run(() => { Driver.WriteRaw(raw); Driver.RestartDevices(); Driver.Reload(); });
    }
}

public class Preset
{
    public string Name { get; set; } = "";
    public TrackpadModel Model { get; set; } = new();
}

public class Config
{
    public int IntervalMinutes { get; set; } = 15;
    public bool NotifyEnabled { get; set; } = true;
    public int Low { get; set; } = 20;
    public int Critical { get; set; } = 10;
    public int RepeatMinutes { get; set; } = 60;
    public bool DisconnectAlert { get; set; }
    public bool Sound { get; set; } = true;
    // Empty means "use the default text in the current language".
    public string MsgLow { get; set; } = "";
    public string MsgCritical { get; set; } = "";
    public string Language { get; set; } = "auto";
    public bool RunInTray { get; set; } = true;
    public List<Preset> Presets { get; set; } = new();

    public string LowText() => string.IsNullOrEmpty(MsgLow) || MsgLow == Loc.En("msg.low") ? Loc.T("msg.low") : MsgLow;
    public string CriticalText() => string.IsNullOrEmpty(MsgCritical) || MsgCritical == Loc.En("msg.crit") ? Loc.T("msg.crit") : MsgCritical;

    public static string Dir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TrackpadCompanion");
    static string FilePath => Path.Combine(Dir, "config.json");

    public static Config Load()
    {
        try { if (File.Exists(FilePath)) return JsonSerializer.Deserialize<Config>(File.ReadAllText(FilePath)) ?? new Config(); }
        catch { }
        return new Config();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
