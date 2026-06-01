using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LiveTranscribe.Core.Models;

namespace LiveTranscribe.App.Views;

/// <summary>
/// A button that captures a single global-hotkey combination. Click it, then press the desired
/// key (with optional modifiers); pressing Escape cancels. The captured combo is exposed as a
/// fresh <see cref="HotkeyConfig"/> via <see cref="Hotkey"/> so two-way bindings push back.
/// </summary>
public sealed class HotkeyBox : Button
{
    private bool _capturing;

    public static readonly DependencyProperty HotkeyProperty = DependencyProperty.Register(
        nameof(Hotkey), typeof(HotkeyConfig), typeof(HotkeyBox),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHotkeyChanged));

    public HotkeyConfig? Hotkey
    {
        get => (HotkeyConfig?)GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    public HotkeyBox()
    {
        Focusable = true;
        UpdateContent();
    }

    private static void OnHotkeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((HotkeyBox)d).UpdateContent();

    protected override void OnClick()
    {
        _capturing = true;
        Content = "Taste drücken…";
        Focus();
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnLostKeyboardFocus(e);
        if (_capturing) { _capturing = false; UpdateContent(); }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (!_capturing) { base.OnPreviewKeyDown(e); return; }
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape) { _capturing = false; UpdateContent(); return; }
        if (IsModifierKey(key)) return; // wait for a real key

        var vk = KeyInterop.VirtualKeyFromKey(key);
        if (vk == 0) return;

        _capturing = false;
        Hotkey = new HotkeyConfig(vk, FromKeyboard(Keyboard.Modifiers));
    }

    private void UpdateContent() => Content = HotkeyDisplay.Describe(Hotkey);

    private static bool IsModifierKey(Key k) => k is Key.LeftCtrl or Key.RightCtrl
        or Key.LeftShift or Key.RightShift or Key.LeftAlt or Key.RightAlt
        or Key.LWin or Key.RWin or Key.System;

    private static HotkeyModifiers FromKeyboard(ModifierKeys m)
    {
        var r = HotkeyModifiers.None;
        if (m.HasFlag(ModifierKeys.Control)) r |= HotkeyModifiers.Control;
        if (m.HasFlag(ModifierKeys.Shift)) r |= HotkeyModifiers.Shift;
        if (m.HasFlag(ModifierKeys.Alt)) r |= HotkeyModifiers.Alt;
        if (m.HasFlag(ModifierKeys.Windows)) r |= HotkeyModifiers.Win;
        return r;
    }
}

/// <summary>Renders a <see cref="HotkeyConfig"/> as readable German text, e.g. "Strg+Umschalt+F10".</summary>
public static class HotkeyDisplay
{
    public static string Describe(HotkeyConfig? cfg)
    {
        if (cfg is null || !cfg.IsSet) return "Nicht festgelegt";

        var sb = new StringBuilder();
        if (cfg.Modifiers.HasFlag(HotkeyModifiers.Control)) sb.Append("Strg+");
        if (cfg.Modifiers.HasFlag(HotkeyModifiers.Shift)) sb.Append("Umschalt+");
        if (cfg.Modifiers.HasFlag(HotkeyModifiers.Alt)) sb.Append("Alt+");
        if (cfg.Modifiers.HasFlag(HotkeyModifiers.Win)) sb.Append("Win+");
        sb.Append(KeyName(cfg.VirtualKey));
        return sb.ToString();
    }

    private static string KeyName(int virtualKey)
    {
        var key = KeyInterop.KeyFromVirtualKey(virtualKey);
        return key == Key.None ? $"0x{virtualKey:X2}" : key.ToString();
    }
}
