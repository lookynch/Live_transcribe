using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using LiveTranscribe.App.ViewModels;
using LiveTranscribe.Core.Abstractions;

namespace LiveTranscribe.App.Views;

/// <summary>
/// Frameless, always-on-top mic overlay. It is given WS_EX_NOACTIVATE | TOPMOST |
/// TOOLWINDOW so it never steals focus, never becomes the paste target, and stays
/// out of Alt-Tab. Position is persisted to settings.
/// </summary>
public partial class OverlayWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private readonly ISettingsService _settings;
    private readonly Func<SettingsWindow> _settingsWindowFactory;

    public OverlayWindow(OverlayViewModel viewModel, ISettingsService settings, Func<SettingsWindow> settingsWindowFactory)
    {
        InitializeComponent();
        DataContext = viewModel;
        _settings = settings;
        _settingsWindowFactory = settingsWindowFactory;

        var o = settings.Current.Overlay;
        Left = o.Left;
        Top = o.Top;
        Topmost = o.AlwaysOnTop;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        var ex = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, ex | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
    }

    private void OnDragHandle(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed) return;
        DragMove();
        _settings.Current.Overlay.Left = Left;
        _settings.Current.Overlay.Top = Top;
        _settings.Save();
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        var existing = Application.Current.Windows.OfType<SettingsWindow>().FirstOrDefault();
        if (existing is not null)
        {
            existing.Activate();
            return;
        }

        var window = _settingsWindowFactory();
        window.Show();
    }
}
