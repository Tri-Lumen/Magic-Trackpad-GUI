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
    readonly Button checkBtn = new() { Content = "Check now", Style = Ui.S("AccentButtonStyle") };
    readonly int _s7 = Log.Step("BatteryPage field 7");

    public BatteryPage()
    {
        Log.Write("BatteryPage: ctor start");
        var page = Ui.NewPage("Battery", out var root);
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
            checkBtn.IsEnabled = false; checkBtn.Content = "Checking…";
            await App.Monitor.CheckAsync();
            checkBtn.Content = "Check now"; checkBtn.IsEnabled = true;
        };
        root.Children.Add(Ui.Card("Check battery", "The trackpad reports its level over Bluetooth only.", checkBtn));

        var interval = new ComboBox { MinWidth = 180 };
        int[] mins = { 1, 5, 15, 60 };
        string[] names = { "Every minute", "Every 5 minutes", "Every 15 minutes", "Every hour" };
        foreach (var n in names) interval.Items.Add(n);
        interval.SelectedIndex = Math.Max(0, Array.IndexOf(mins, App.Cfg.IntervalMinutes));
        interval.SelectionChanged += (_, _) =>
        {
            App.Cfg.IntervalMinutes = mins[Math.Max(0, interval.SelectedIndex)];
            App.Cfg.Save(); App.Monitor.ApplyInterval();
        };
        root.Children.Add(Ui.Card("Check automatically", "Runs in the background so alerts arrive without opening this window.", interval));

        root.Children.Add(Ui.Header("Last 7 days"));
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
            pillText.Text = "Unavailable";
            est.Text = "Battery can only be read over Bluetooth";
            glyph.Glyph = "\uE83F";
        }
        else
        {
            pct.Text = l + "%";
            pillText.Text = l <= App.Cfg.Critical ? "Critical" : l <= App.Cfg.Low ? "Low" : "Good";
            glyph.Glyph = l >= 95 ? "" : l >= 10 ? (char)(0xEBA0 + Math.Min(9, l / 10)) + "" : "";
            var d = History.EstimateDaysLeft(l);
            est.Text = d == null ? "Estimate appears after a few hours of readings"
                : d < 1 ? "About " + Math.Max(1, (int)Math.Round(d.Value * 24)) + " hours remaining"
                : "About " + (int)Math.Round(d.Value) + " days remaining";
        }
        updated.Text = App.Monitor.Updated == null ? "Waiting for the first reading"
            : "Magic Trackpad · updated " + App.Monitor.Updated.Value.ToString("t");
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
            var t = new TextBlock { Text = day.ToString("ddd"), Style = Ui.S("CaptionText"), FontSize = 11 };
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
            var t = new TextBlock { Text = "History fills in as the app records readings.", Style = Ui.S("CaptionText") };
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
