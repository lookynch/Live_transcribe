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
