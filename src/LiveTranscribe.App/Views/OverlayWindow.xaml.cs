using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
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
    private readonly OverlayViewModel _viewModel;

    public OverlayWindow(OverlayViewModel viewModel, ISettingsService settings, Func<SettingsWindow> settingsWindowFactory)
    {
        InitializeComponent();
        DataContext = viewModel;
        _settings = settings;
        _settingsWindowFactory = settingsWindowFactory;
        _viewModel = viewModel;

        // The waveform animation must be started in code: a Storyboard launched from a Style cannot
        // use TargetName, but our equalizer targets the named bars + glow. Begin(this, …) resolves
        // those names against this window's namescope, so TargetName stays valid.
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        var o = settings.Current.Overlay;
        Left = o.Left;
        Top = o.Top;
        Topmost = o.AlwaysOnTop;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(OverlayViewModel.IsRecording)) return;

        var storyboard = (Storyboard)FindResource("WaveAnim");
        if (_viewModel.IsRecording) storyboard.Begin(this, isControllable: true);
        else storyboard.Stop(this);
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
            existing.ShowAndFocus();
            return;
        }

        var window = _settingsWindowFactory();
        window.ShowAndFocus();
    }
}
