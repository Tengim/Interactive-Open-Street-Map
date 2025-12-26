using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace AxxonSoft_OSM_;

public partial class AreaEditDialog : Window
{
    public AreaEditDialog()
    {
        InitializeComponent();
    }
    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is AreaEditDialogViewModel viewModel)
        {
            var result = viewModel.GetResult();
            Close(result);
        }
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        Close(null);
    }
}