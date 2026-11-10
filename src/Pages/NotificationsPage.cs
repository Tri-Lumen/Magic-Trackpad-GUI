using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace TrackpadCompanion;

public sealed class NotificationsPage : Page
{
    public NotificationsPage()
    {
        Log.Write("NotificationsPage: ctor start");
        var cfg = App.Cfg;
        var page = Ui.NewPage(Loc.T("nav.notifications"), out var root);
        Content = page;

        root.Children.Add(Ui.Card(Loc.T("nt.alerts.title"), Loc.T("nt.alerts.desc"),
            Ui.Toggle(cfg.NotifyEnabled, v => { cfg.NotifyEnabled = v; cfg.Save(); })));

        var lowVal = new TextBlock { Text = cfg.Low + "%", Width = 44, TextAlignment = Microsoft.UI.Xaml.TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        var critVal = new TextBlock { Text = cfg.Critical + "%", Width = 44, TextAlignment = Microsoft.UI.Xaml.TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        var low = new Slider { Minimum = 5, Maximum = 50, Value = cfg.Low, Width = 220 };
        var crit = new Slider { Minimum = 1, Maximum = 25, Value = cfg.Critical, Width = 220 };
        low.ValueChanged += (_, _) =>
        {
            cfg.Low = (int)low.Value; lowVal.Text = cfg.Low + "%";
            if (cfg.Critical >= cfg.Low) { crit.Value = Math.Max(1, cfg.Low - 1); }
            cfg.Save(); App.Tray.Refresh();
        };
        crit.ValueChanged += (_, _) =>
        {
            cfg.Critical = (int)crit.Value; critVal.Text = cfg.Critical + "%";
            if (cfg.Critical >= cfg.Low) { low.Value = Math.Min(50, cfg.Critical + 1); }
            cfg.Save(); App.Tray.Refresh();
        };
        root.Children.Add(Ui.Card(Loc.T("nt.low.title"), Loc.T("nt.low.desc"),
            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { low, lowVal } }));
        root.Children.Add(Ui.Card(Loc.T("nt.crit.title"), Loc.T("nt.crit.desc"),
            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { crit, critVal } }));

        var repeat = new ComboBox { MinWidth = 180 };
        int[] mins = { 0, 15, 60, 180 };
        foreach (var n in new[] { Loc.T("nt.never"), Loc.T("nt.r15"), Loc.T("nt.r60"), Loc.T("nt.r180") }) repeat.Items.Add(n);
        repeat.SelectedIndex = Math.Max(0, Array.IndexOf(mins, cfg.RepeatMinutes));
        repeat.SelectionChanged += (_, _) => { cfg.RepeatMinutes = mins[Math.Max(0, repeat.SelectedIndex)]; cfg.Save(); };
        root.Children.Add(Ui.Card(Loc.T("nt.repeat.title"), Loc.T("nt.repeat.desc"), repeat));

        root.Children.Add(Ui.Card(Loc.T("nt.disc.title"), Loc.T("nt.disc.desc"),
            Ui.Toggle(cfg.DisconnectAlert, v => { cfg.DisconnectAlert = v; cfg.Save(); })));
        root.Children.Add(Ui.Card(Loc.T("nt.sound"), null, Ui.Toggle(cfg.Sound, v => { cfg.Sound = v; cfg.Save(); })));

        root.Children.Add(Ui.Header(Loc.T("nt.hdr.msg")));
        var msgLow = new TextBox { Header = Loc.T("nt.msg.low"), Text = cfg.LowText() };
        var msgCrit = new TextBox { Header = Loc.T("nt.msg.crit"), Text = cfg.CriticalText() };
        msgLow.TextChanged += (_, _) => { cfg.MsgLow = msgLow.Text == Loc.T("msg.low") ? "" : msgLow.Text; cfg.Save(); };
        msgCrit.TextChanged += (_, _) => { cfg.MsgCritical = msgCrit.Text == Loc.T("msg.crit") ? "" : msgCrit.Text; cfg.Save(); };
        var testLow = new Button { Content = Loc.T("nt.preview.low") };
        var testCrit = new Button { Content = Loc.T("nt.preview.crit") };
        testLow.Click += (_, _) => App.Monitor.Notify(false, cfg.Low);
        testCrit.Click += (_, _) => App.Monitor.Notify(true, cfg.Critical);
        var box = new StackPanel { Spacing = 10 };
        box.Children.Add(msgLow); box.Children.Add(msgCrit);
        box.Children.Add(Ui.Caption(Loc.T("nt.placeholder")));
        box.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { testLow, testCrit } });
        root.Children.Add(Ui.CardWith(box));
    }
}
