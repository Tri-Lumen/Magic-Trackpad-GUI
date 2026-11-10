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
    readonly NumberBox sizeBox = new() { Minimum = 0, Value = 7, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline, Width = 140, Header = Loc.T("tp.size") };
    readonly NumberBox pressureBox = new() { Minimum = 0, Value = 0, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline, Width = 140, Header = Loc.T("tp.pressure") };
    readonly ToggleSwitch palm = new(), ignoreBtn = new(), ignoreNear = new();
    readonly InfoBar info = new() { IsClosable = true, IsOpen = false };
    readonly Button applyBtn = new() { Content = Loc.T("tp.apply"), Style = Ui.S("AccentButtonStyle") };

    public TrackpadPage()
    {
        Log.Write("TrackpadPage: ctor start");
        try { m = TrackpadModel.FromRaw(Driver.ReadRaw()); } catch { m = new TrackpadModel(); }

        var page = Ui.NewPage(Loc.T("nav.trackpad"), out var root);
        Content = page;

        // presets
        foreach (var p in App.Cfg.Presets) presetBox.Items.Add(p.Name);
        presetBox.PlaceholderText = Loc.T("tp.preset.placeholder");
        presetBox.SelectionChanged += (_, _) =>
        {
            int i = presetBox.SelectedIndex;
            if (loading || i < 0 || i >= App.Cfg.Presets.Count) return;
            m = App.Cfg.Presets[i].Model.Clone();
            SyncUi();
        };
        var saveBtn = new Button { Content = Loc.T("tp.preset.saveas") };
        var nameBox = new TextBox { PlaceholderText = Loc.T("tp.preset.name"), Width = 220 };
        var okBtn = new Button { Content = Loc.T("tp.save"), Style = Ui.S("AccentButtonStyle"), Margin = new Thickness(0, 8, 0, 0) };
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
        var delBtn = new Button { Content = Loc.T("tp.delete") };
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
        root.Children.Add(Ui.Card(Loc.T("tp.preset.title"), Loc.T("tp.preset.desc"), presetRow));

        // click feedback
        root.Children.Add(Ui.Header(Loc.T("tp.hdr.click")));
        foreach (var s in new[] { Loc.T("tp.light"), Loc.T("tp.medium"), Loc.T("tp.firm") }) strength.Items.Add(s);
        strength.SelectionChanged += (_, _) => { if (!loading && strength.SelectedIndex >= 0) m.Strength = strength.SelectedIndex; };
        root.Children.Add(Ui.Card(Loc.T("tp.strength.title"), Loc.T("tp.strength.desc"), strength));

        silent.Toggled += (_, _) => { if (!loading) m.Silent = silent.IsOn; };
        root.Children.Add(Ui.Card(Loc.T("tp.silent.title"), Loc.T("tp.silent.desc"), silent));

        mode.Items.Add(Loc.T("tp.mode.mac"));
        mode.Items.Add(Loc.T("tp.mode.max"));
        mode.Items.Add(Loc.T("tp.mode.off"));
        mode.SelectionChanged += (_, _) => { if (!loading && mode.SelectedIndex >= 0) { m.Mode = mode.SelectedIndex; UpdateEnabled(); } };
        root.Children.Add(Ui.CardWith(mode));

        // lift-off
        root.Children.Add(Ui.Header(Loc.T("tp.hdr.lift")));
        stop.Items.Add(Loc.T("tp.stop.none"));
        stop.Items.Add(Loc.T("tp.stop.size"));
        stop.Items.Add(Loc.T("tp.stop.pressure"));
        stop.SelectionChanged += (_, _) => { if (!loading && stop.SelectedIndex >= 0) { m.StopMode = stop.SelectedIndex; UpdateEnabled(); } };
        sizeBox.ValueChanged += (_, _) => { if (!loading && !double.IsNaN(sizeBox.Value)) m.SizeValue = (int)sizeBox.Value; };
        pressureBox.ValueChanged += (_, _) => { if (!loading && !double.IsNaN(pressureBox.Value)) m.PressureValue = (int)pressureBox.Value; };
        var stopPanel = new StackPanel { Spacing = 10 };
        stopPanel.Children.Add(stop);
        stopPanel.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Children = { sizeBox, pressureBox } });
        stopPanel.Children.Add(Ui.Caption(Loc.T("tp.stop.hint")));
        root.Children.Add(Ui.CardWith(stopPanel));

        // other
        root.Children.Add(Ui.Header(Loc.T("tp.hdr.other")));
        palm.Toggled += (_, _) => { if (!loading) m.PalmRejection = palm.IsOn; };
        ignoreBtn.Toggled += (_, _) => { if (!loading) m.IgnoreButtonFinger = ignoreBtn.IsOn; };
        ignoreNear.Toggled += (_, _) => { if (!loading) m.IgnoreNearFingers = ignoreNear.IsOn; };
        root.Children.Add(Ui.Card(Loc.T("tp.palm"), null, palm));
        root.Children.Add(Ui.Card(Loc.T("tp.ignorebtn.title"), Loc.T("tp.ignorebtn.desc"), ignoreBtn));
        root.Children.Add(Ui.Card(Loc.T("tp.ignorenear.title"), null, ignoreNear));

        // actions
        var revert = new Button { Content = Loc.T("tp.revert") };
        revert.Click += (_, _) => { try { m = TrackpadModel.FromRaw(Driver.ReadRaw()); } catch { } SyncUi(); info.IsOpen = false; };
        var winTp = new HyperlinkButton { Content = Loc.T("tp.wintp") };
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
        applyBtn.IsEnabled = false; applyBtn.Content = Loc.T("tp.applying");
        try
        {
            await m.ApplyAsync();
            info.Severity = InfoBarSeverity.Success;
            info.Title = Loc.T("tp.ok.title");
            info.Message = Loc.T("tp.ok.msg");
        }
        catch (Exception e)
        {
            info.Severity = InfoBarSeverity.Error;
            info.Title = Loc.T("tp.err.title");
            info.Message = e.Message;
        }
        info.IsOpen = true;
        applyBtn.Content = Loc.T("tp.apply"); applyBtn.IsEnabled = true;
    }
}
