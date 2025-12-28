using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System.Threading.Tasks;

public interface IDialogService
{
    Task<bool> ShowConfirmationDialogAsync(string title, string message, string confirmText = "Да", string cancelText = "Нет");
}

public class DialogService : IDialogService
{
    private Window? _ownerWindow;

    public void SetOwnerWindow(Window owner)
    {
        _ownerWindow = owner;
    }

    public async Task<bool> ShowConfirmationDialogAsync(string title, string message, string confirmText = "Да", string cancelText = "Нет")
    {
        if (_ownerWindow == null) return false;

        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false
        };

        bool? result = null;

        var stackPanel = new StackPanel
        {
            Margin = new Thickness(20),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        var textBlock = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 20)
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 10
        };

        var confirmButton = new Button
        {
            Content = confirmText,
            MinWidth = 100,
            Classes = { "btn-cancel" }
        };

        var cancelButton = new Button
        {
            Content = cancelText,
            MinWidth = 100,
            Classes = { "btn-success" }
        };

        confirmButton.Click += (s, e) =>
        {
            result = true;
            dialog.Close();
        };

        cancelButton.Click += (s, e) =>
        {
            result = false;
            dialog.Close();
        };

        buttonPanel.Children.Add(confirmButton);
        buttonPanel.Children.Add(cancelButton);

        stackPanel.Children.Add(textBlock);
        stackPanel.Children.Add(buttonPanel);

        dialog.Content = stackPanel;

        await dialog.ShowDialog(_ownerWindow);

        return result ?? false;
    }
}