using System.Windows;
using LiveTranscribe.App.ViewModels;

namespace LiveTranscribe.App.Views;

public partial class PreviewWindow : Window
{
    public PreviewWindow(PreviewViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += _ => Close();
    }
}
