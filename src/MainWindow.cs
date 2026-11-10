using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System.Runtime.InteropServices;

namespace TrackpadCompanion;

public sealed class MainWindow : Window
{
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);

    readonly ContentControl host = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
    readonly NavigationView nav;

    public MainWindow()
    {
        Log.Write("MainWindow: start");
        Title = "Trackpad Companion";
        try { AppWindow.Resize(new Windows.Graphics.SizeInt32(1080, 780)); Log.Write("MainWindow: resized"); }
        catch (Exception e) { Log.Write("resize failed: " + e.Message); }

        var titleBar = new Grid { Height = 32 };
        titleBar.Children.Add(new TextBlock
        {
            Text = "Trackpad Companion", FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0, 0, 0)
        });
        Log.Write("MainWindow: titlebar built");

        nav = new NavigationView
        {
            PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
            IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed,
            IsSettingsVisible = false,
            OpenPaneLength = 260,
            Content = host
        };
        nav.MenuItems.Add(Item("Battery", "battery", ""));
        nav.MenuItems.Add(Item("Trackpad", "trackpad", ""));
        nav.MenuItems.Add(Item("Notifications", "notify", ""));
        nav.FooterMenuItems.Add(Item("App settings", "app", ""));
        nav.SelectionChanged += (_, e) =>
        {
            var tag = (e.SelectedItem as NavigationViewItem)?.Tag as string;
            Log.Write("navigate: " + tag);
            Type? page = tag switch
            {
                "battery" => typeof(BatteryPage),
                "trackpad" => typeof(TrackpadPage),
                "notify" => typeof(NotificationsPage),
                "app" => typeof(AppPage),
                _ => null
            };
            try { if (page != null) host.Content = (UIElement)Activator.CreateInstance(page)!; }
            catch (Exception ex) { Log.Write("page creation threw: " + ex); }
        };
        Log.Write("MainWindow: nav built");

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.Children.Add(titleBar);
        Grid.SetRow(nav, 1);
        root.Children.Add(nav);
        root.Loaded += (_, _) =>
        {
            Log.Write("MainWindow: content loaded");
            if (nav.SelectedItem == null) nav.SelectedItem = nav.MenuItems[0];
        };
        Content = root;
        Log.Write("MainWindow: content set");

        try { ExtendsContentIntoTitleBar = true; SetTitleBar(titleBar); Log.Write("MainWindow: title bar set"); }
        catch (Exception e) { Log.Write("title bar failed: " + e.Message); }
        try { SystemBackdrop = new MicaBackdrop(); Log.Write("MainWindow: mica set"); }
        catch (Exception e) { Log.Write("mica failed: " + e.Message); }

        AppWindow.Closing += (s, a) =>
        {
            if (App.Cfg.RunInTray && !App.Exiting) { a.Cancel = true; s.Hide(); }
            else App.Quit();
        };
        Log.Write("MainWindow: done");
    }

    static NavigationViewItem Item(string text, string tag, string glyph) =>
        new() { Content = text, Tag = tag, Icon = new FontIcon { Glyph = glyph } };

    public void ShowWindow()
    {
        AppWindow.Show();
        Activate();
        SetForegroundWindow(WinRT.Interop.WindowNative.GetWindowHandle(this));
    }
}
