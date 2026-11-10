using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace TrackpadCompanion;

public sealed class BatteryPage : Page
{
    readonly TextBlock pct = new() { FontSize = 56, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
    readonly int _s1 = Log.Step("BatteryPage field 1");
    readonly TextBlock est = new() { FontSize = 16 };
    readonly int _s2 = Log.Step("BatteryPage field 2");
    readonly TextBlock updated = Ui.Caption("");
    readonly TextBlock pillText = new() { FontSize = 12, Foreground = new SolidColorBrush(Colors.White), FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
    readonly int _s3 = Log.Step("BatteryPage field 3");
    readonly Border pill = new() { CornerRadius = new CornerRadius(10), Padding = new Thickness(10, 2, 10, 2), HorizontalAlignment = HorizontalAlignment.Left };
    readonly int _s4 = Log.Step("BatteryPage field 4");
    readonly FontIcon glyph = new() { FontSize = 44 };
    readonly int _s5 = Log.Step("BatteryPage field 5");
    readonly Canvas chart = new() { Height = 220 };
    readonly int _s6 = Log.Step("BatteryPage field 6");
    readonly Button checkBtn = new() { Content = Loc.T("battery.check.btn"), Style = Ui.S("AccentButtonStyle") };
    readonly int _s7 = Log.Step("BatteryPage field 7");

    public BatteryPage()
    {
        Log.Write("BatteryPage: ctor start");
        var page = Ui.NewPage(Loc.T("nav.battery"), out var root);
        Content = page;

        var hero = new Grid { ColumnSpacing = 24 };
        hero.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        hero.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var left = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14, VerticalAlignment = VerticalAlignment.Center };
        left.Children.Add(glyph); left.Children.Add(pct);
        var right = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        pill.Child = pillText;
        right.Children.Add(pill); right.Children.Add(est); right.Children.Add(updated);
        hero.Children.Add(left); Grid.SetColumn(right, 1); hero.Children.Add(right);
        var heroCard = Ui.CardWith(hero);
        heroCard.Padding = new Thickness(20);
        root.Children.Add(heroCard);

        checkBtn.Click += async (_, _) =>
        {
            checkBtn.IsEnabled = false; checkBtn.Content = Loc.T("battery.check.busy");
            await App.Monitor.CheckAsync();
            checkBtn.Content = Loc.T("battery.check.btn"); checkBtn.IsEnabled = true;
        };
        root.Children.Add(Ui.Card(Loc.T("battery.check.title"), Loc.T("battery.check.desc"), checkBtn));

        var interval = new ComboBox { MinWidth = 180 };
        int[] mins = { 1, 5, 15, 60 };
        string[] names = { Loc.T("battery.every1"), Loc.T("battery.every5"), Loc.T("battery.every15"), Loc.T("battery.every60") };
        foreach (var n in names) interval.Items.Add(n);
        interval.SelectedIndex = Math.Max(0, Array.IndexOf(mins, App.Cfg.IntervalMinutes));
        interval.SelectionChanged += (_, _) =>
        {
            App.Cfg.IntervalMinutes = mins[Math.Max(0, interval.SelectedIndex)];
            App.Cfg.Save(); App.Monitor.ApplyInterval();
        };
        root.Children.Add(Ui.Card(Loc.T("battery.auto.title"), Loc.T("battery.auto.desc"), interval));

        root.Children.Add(Ui.Header(Loc.T("battery.last7")));
        var chartCard = Ui.CardWith(chart);
        root.Children.Add(chartCard);
        chart.SizeChanged += (_, _) => DrawChart();

        App.Monitor.Changed += Refresh;
        Unloaded += (_, _) => App.Monitor.Changed -= Refresh;
        Refresh();
        Log.Write("BatteryPage: ctor done");
    }

    static Color StateColor(int l)
    {
        var c = App.Cfg;
        return l < 0 ? Color.FromArgb(255, 110, 110, 110)
            : l <= c.Critical ? Color.FromArgb(255, 196, 43, 28)
            : l <= c.Low ? Color.FromArgb(255, 202, 80, 16)
            : Color.FromArgb(255, 16, 124, 16);
    }

    void Refresh()
    {
        int l = App.Monitor.Level;
        pill.Background = new SolidColorBrush(StateColor(l));
        if (l < 0)
        {
            pct.Text = "--";
            pillText.Text = Loc.T("battery.unavailable");
            est.Text = Loc.T("battery.btonly");
            glyph.Glyph = "\uE83F";
        }
        else
        {
            pct.Text = l + "%";
            pillText.Text = l <= App.Cfg.Critical ? Loc.T("battery.critical") : l <= App.Cfg.Low ? Loc.T("battery.low") : Loc.T("battery.good");
            glyph.Glyph = l >= 95 ? "" : l >= 10 ? (char)(0xEBA0 + Math.Min(9, l / 10)) + "" : "";
            var d = History.EstimateDaysLeft(l);
            int hrs = d == null ? 0 : Math.Max(1, (int)Math.Round(d.Value * 24));
            int days = d == null ? 0 : (int)Math.Round(d.Value);
            est.Text = d == null ? Loc.T("battery.est.none")
                : d < 1 ? (hrs == 1 ? Loc.T("battery.est.hour1") : Loc.F("battery.est.hours", hrs))
                : (days <= 1 ? Loc.T("battery.est.day1") : Loc.F("battery.est.days", days));
        }
        updated.Text = App.Monitor.Updated == null ? Loc.T("battery.waiting")
            : Loc.F("battery.updated", App.Monitor.Updated.Value.ToString("t", Loc.Culture));
        DrawChart();
    }

    void DrawChart()
    {
        chart.Children.Clear();
        double w = chart.ActualWidth, h = chart.Height;
        if (w < 80) return;
        double L = 34, R = 8, T = 8, B = 22, pw = w - L - R, ph = h - T - B;
        var grid = new SolidColorBrush(Color.FromArgb(60, 128, 128, 128));
        Func<double, double> Y = v => T + (1 - v / 100.0) * ph;
        foreach (var g in new[] { 0, 25, 50, 75, 100 })
        {
            chart.Children.Add(new Line { X1 = L, X2 = w - R, Y1 = Y(g), Y2 = Y(g), Stroke = grid, StrokeThickness = 1 });
            var t = new TextBlock { Text = g.ToString(), Style = Ui.S("CaptionText"), FontSize = 11 };
            Canvas.SetLeft(t, 4); Canvas.SetTop(t, Y(g) - 8); chart.Children.Add(t);
        }
        var now = DateTime.UtcNow; var start = now.AddDays(-7);
        for (int d = 0; d < 7; d++)
        {
            var day = start.AddDays(d + 1).ToLocalTime();
            var t = new TextBlock { Text = day.ToString("ddd", Loc.Culture), Style = Ui.S("CaptionText"), FontSize = 11 };
            Canvas.SetLeft(t, L + (d + 0.5) / 7 * pw - 12); Canvas.SetTop(t, h - B + 4); chart.Children.Add(t);
        }
        void Threshold(int v, Color c) => chart.Children.Add(new Line
        {
            X1 = L, X2 = w - R, Y1 = Y(v), Y2 = Y(v), Stroke = new SolidColorBrush(c), StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection { 4, 4 }
        });
        Threshold(App.Cfg.Low, Color.FromArgb(255, 202, 80, 16));
        Threshold(App.Cfg.Critical, Color.FromArgb(255, 196, 43, 28));

        var pts = History.Load(7);
        if (pts.Count == 0)
        {
            var t = new TextBlock { Text = Loc.T("battery.history.empty"), Style = Ui.S("CaptionText") };
            Canvas.SetLeft(t, L + 12); Canvas.SetTop(t, T + 12); chart.Children.Add(t);
            return;
        }
        var accent = new UISettings().GetColorValue(UIColorType.Accent);
        var line = new Polyline { Stroke = new SolidColorBrush(accent), StrokeThickness = 2, StrokeLineJoin = PenLineJoin.Round };
        Windows.Foundation.Point last = default;
        foreach (var (time, lvl) in pts)
        {
            double x = L + (time - start).TotalDays / 7.0 * pw;
            last = new Windows.Foundation.Point(x, Y(lvl));
            line.Points.Add(last);
        }
        chart.Children.Add(line);
        var dot = new Ellipse { Width = 9, Height = 9, Fill = new SolidColorBrush(accent) };
        Canvas.SetLeft(dot, last.X - 4.5); Canvas.SetTop(dot, last.Y - 4.5);
        chart.Children.Add(dot);
    }
}
