using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace TrackpadCompanion;

// Small helpers so each page reads as a list of Windows 11 settings rows.
public static class Ui
{
    public static Style? S(string key)
    {
        try { return (Style)Application.Current.Resources[key]; }
        catch (Exception e) { Log.Write("style lookup failed for " + key + ": " + e.Message); return null; }
    }

    public static ScrollViewer NewPage(string title, out StackPanel root)
    {
        root = new StackPanel { Spacing = 4, MaxWidth = 920, HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(36, 8, 36, 40) };
        Log.Write("NewPage: " + title);
        root.Children.Add(new TextBlock { Text = title, Style = S("TitleTextBlockStyle"), Margin = new Thickness(0, 8, 0, 12) });
        return new ScrollViewer { Content = root };
    }

    public static TextBlock Header(string text) =>
        new TextBlock { Text = text, Style = S("BodyStrongTextBlockStyle"), Margin = new Thickness(1, 16, 0, 4) };

    public static TextBlock Caption(string text) => new TextBlock { Text = text, Style = S("CaptionText") };

    public static Border Card(string title, string? description, FrameworkElement? control)
    {
        var grid = new Grid { ColumnSpacing = 16 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(new TextBlock { Text = title, TextWrapping = TextWrapping.Wrap });
        if (description != null) text.Children.Add(Caption(description));
        grid.Children.Add(text);
        if (control != null)
        {
            control.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(control, 1);
            grid.Children.Add(control);
        }
        return new Border { Style = S("CardStyle"), Child = grid };
    }

    public static Border CardWith(FrameworkElement content) => new Border { Style = S("CardStyle"), Child = content };

    public static ToggleSwitch Toggle(bool on, Action<bool> changed)
    {
        var t = new ToggleSwitch { IsOn = on, MinWidth = 0 };
        t.Toggled += (_, _) => changed(t.IsOn);
        return t;
    }
}
