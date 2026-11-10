using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace TrackpadCompanion;

public sealed class TrackpadPage : Page
{
    TrackpadModel m;
    bool loading;

    readonly ComboBox presetBox = new() { MinWidth = 180 };
    readonly RadioButtons strength = new() { MaxColumns = 3 };
    readonly ToggleSwitch silent = new();
    readonly RadioButtons mode = new();
    readonly RadioButtons stop = new();
    readonly NumberBox sizeBox = new() { Minimum = 0, Value = 7, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline, Width = 140, Header = "Touch size" };
    readonly NumberBox pressureBox = new() { Minimum = 0, Value = 0, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline, Width = 140, Header = "Pressure" };
    readonly ToggleSwitch palm = new(), ignoreBtn = new(), ignoreNear = new();
    readonly InfoBar info = new() { IsClosable = true, IsOpen = false };
    readonly Button applyBtn = new() { Content = "Apply", Style = Ui.S("AccentButtonStyle") };

    public TrackpadPage()
    {
        Log.Write("TrackpadPage: ctor start");
        try { m = TrackpadModel.FromRaw(Driver.ReadRaw()); } catch { m = new TrackpadModel(); }

        var page = Ui.NewPage("Trackpad", out var root);
        Content = page;

        // presets
        foreach (var p in App.Cfg.Presets) presetBox.Items.Add(p.Name);
        presetBox.PlaceholderText = "Current settings";
        presetBox.SelectionChanged += (_, _) =>
        {
            int i = presetBox.SelectedIndex;
            if (loading || i < 0 || i >= App.Cfg.Presets.Count) return;
            m = App.Cfg.Presets[i].Model.Clone();
            SyncUi();
        };
        var saveBtn = new Button { Content = "Save as…" };
        var nameBox = new TextBox { PlaceholderText = "Preset name", Width = 220 };
        var okBtn = new Button { Content = "Save", Style = Ui.S("AccentButtonStyle"), Margin = new Thickness(0, 8, 0, 0) };
        var fly = new Flyout { Content = new StackPanel { Children = { nameBox, okBtn } } };
        saveBtn.Flyout = fly;
        okBtn.Click += (_, _) =>
        {
            var name = nameBox.Text.Trim();
            if (name.Length == 0) return;
            App.Cfg.Presets.RemoveAll(p => p.Name == name);
            App.Cfg.Presets.Add(new Preset { Name = name, Model = m.Clone() });
            App.Cfg.Save();
            loading = true;
            presetBox.Items.Clear();
            foreach (var p in App.Cfg.Presets) presetBox.Items.Add(p.Name);
            presetBox.SelectedItem = name;
            loading = false;
            nameBox.Text = ""; fly.Hide();
        };
        var delBtn = new Button { Content = "Delete" };
        delBtn.Click += (_, _) =>
        {
            int i = presetBox.SelectedIndex;
            if (i < 0 || i >= App.Cfg.Presets.Count) return;
            App.Cfg.Presets.RemoveAt(i); App.Cfg.Save();
            loading = true; presetBox.Items.Clear();
            foreach (var p in App.Cfg.Presets) presetBox.Items.Add(p.Name);
            loading = false;
        };
        var presetRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { presetBox, saveBtn, delBtn } };
        root.Children.Add(Ui.Card("Preset", "Save the current settings under a name. Presets also appear in the tray menu.", presetRow));

        // click feedback
        root.Children.Add(Ui.Header("Click feedback"));
        foreach (var s in new[] { "Light", "Medium", "Firm" }) strength.Items.Add(s);
        strength.SelectionChanged += (_, _) => { if (!loading && strength.SelectedIndex >= 0) m.Strength = strength.SelectedIndex; };
        root.Children.Add(Ui.Card("Click strength", "How firm the haptic click feels.", strength));

        silent.Toggled += (_, _) => { if (!loading) m.Silent = silent.IsOn; };
        root.Children.Add(Ui.Card("Silent clicking", "Removes the click sound from the haptic motor.", silent));

        mode.Items.Add("Use macOS click options");
        mode.Items.Add("Maximum haptic feedback (very clicky and loud)");
        mode.Items.Add("Disable haptics and the force touch button completely");
        mode.SelectionChanged += (_, _) => { if (!loading && mode.SelectedIndex >= 0) { m.Mode = mode.SelectedIndex; UpdateEnabled(); } };
        root.Children.Add(Ui.CardWith(mode));

        // lift-off
        root.Children.Add(Ui.Header("When you lift your finger"));
        stop.Items.Add("Do nothing");
        stop.Items.Add("Stop the pointer if the touch size is at or below the value");
        stop.Items.Add("Stop the pointer if the pressure is at or below the value");
        stop.SelectionChanged += (_, _) => { if (!loading && stop.SelectedIndex >= 0) { m.StopMode = stop.SelectedIndex; UpdateEnabled(); } };
        sizeBox.ValueChanged += (_, _) => { if (!loading && !double.IsNaN(sizeBox.Value)) m.SizeValue = (int)sizeBox.Value; };
        pressureBox.ValueChanged += (_, _) => { if (!loading && !double.IsNaN(pressureBox.Value)) m.PressureValue = (int)pressureBox.Value; };
        var stopPanel = new StackPanel { Spacing = 10 };
        stopPanel.Children.Add(stop);
        stopPanel.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Children = { sizeBox, pressureBox } });
        stopPanel.Children.Add(Ui.Caption("A touch size of 7 and a pressure of 0 are good starting values."));
        root.Children.Add(Ui.CardWith(stopPanel));

        // other
        root.Children.Add(Ui.Header("Other"));
        palm.Toggled += (_, _) => { if (!loading) m.PalmRejection = palm.IsOn; };
        ignoreBtn.Toggled += (_, _) => { if (!loading) m.IgnoreButtonFinger = ignoreBtn.IsOn; };
        ignoreNear.Toggled += (_, _) => { if (!loading) m.IgnoreNearFingers = ignoreNear.IsOn; };
        root.Children.Add(Ui.Card("Palm rejection", null, palm));
        root.Children.Add(Ui.Card("Ignore the finger that presses the button", "Helps when you click with your thumb and drag with your index finger.", ignoreBtn));
        root.Children.Add(Ui.Card("Ignore fingers hovering near the surface", null, ignoreNear));

        // actions
        var revert = new Button { Content = "Revert" };
        revert.Click += (_, _) => { try { m = TrackpadModel.FromRaw(Driver.ReadRaw()); } catch { } SyncUi(); info.IsOpen = false; };
        var winTp = new HyperlinkButton { Content = "Windows touchpad settings" };
        winTp.Click += (_, _) => { try { Process.Start(new ProcessStartInfo("ms-settings:devices-touchpad") { UseShellExecute = true }); } catch { } };
        applyBtn.Click += async (_, _) => await Apply();
        info.Margin = new Thickness(0, 12, 0, 0);
        root.Children.Add(info);
        root.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 12, 0, 0), Children = { applyBtn, revert, winTp } });

        SyncUi();
    }

    void SyncUi()
    {
        loading = true;
        strength.SelectedIndex = m.Strength;
        silent.IsOn = m.Silent;
        mode.SelectedIndex = m.Mode;
        stop.SelectedIndex = m.StopMode;
        sizeBox.Value = m.SizeValue;
        pressureBox.Value = m.PressureValue;
        palm.IsOn = m.PalmRejection;
        ignoreBtn.IsOn = m.IgnoreButtonFinger;
        ignoreNear.IsOn = m.IgnoreNearFingers;
        loading = false;
        UpdateEnabled();
    }

    void UpdateEnabled()
    {
        bool mac = m.Mode == 0;
        strength.IsEnabled = mac; silent.IsEnabled = mac;
        sizeBox.IsEnabled = m.StopMode == 1;
        pressureBox.IsEnabled = m.StopMode == 2;
    }

    async Task Apply()
    {
        applyBtn.IsEnabled = false; applyBtn.Content = "Applying…";
        try
        {
            await m.ApplyAsync();
            info.Severity = InfoBarSeverity.Success;
            info.Title = "Settings applied";
            info.Message = "The trackpad driver restarted and reloaded them.";
        }
        catch (Exception e)
        {
            info.Severity = InfoBarSeverity.Error;
            info.Title = "Could not apply settings";
            info.Message = e.Message;
        }
        info.IsOpen = true;
        applyBtn.Content = "Apply"; applyBtn.IsEnabled = true;
    }
}
