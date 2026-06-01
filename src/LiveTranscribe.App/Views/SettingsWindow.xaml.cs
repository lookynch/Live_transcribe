using System.Windows;
using LiveTranscribe.App.ViewModels;

namespace LiveTranscribe.App.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
    }

    /// <summary>Shows (or un-minimizes) the window and forces it to the foreground, even past the topmost overlay.</summary>
    public void ShowAndFocus()
    {
        if (!IsVisible) Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;

        // Brief Topmost pulse so the window jumps above the always-on-top overlay, then settles back.
        Topmost = true;
        Activate();
        Topmost = false;
        Focus();
    }

    private void OnSaveApiKey(object sender, RoutedEventArgs e)
    {
        _viewModel.ApiKeyInput = ApiKeyBox.Password;
        _viewModel.SaveApiKeyCommand.Execute(null);
        ApiKeyBox.Clear();
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        _viewModel.Persist();
        Close();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _viewModel.Persist();
        base.OnClosing(e);
    }
}
