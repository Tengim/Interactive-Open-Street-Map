using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace AxxonSoft_OSM_;

public partial class PointEditDialog : Window
{
    public PointEditDialog()
    {
        InitializeComponent();
    }
    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is PointEditDialogViewModel viewModel)
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