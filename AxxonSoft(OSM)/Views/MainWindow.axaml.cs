using Avalonia.Controls;
using Avalonia.Input;
using AxxonSoft_OSM_.Services;
using AxxonSoft_OSM_.ViewModels;
using System;

namespace AxxonSoft_OSM_.Views
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel? _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            var mapService = new MapService(MyMapControl);
            _viewModel = new MainWindowViewModel(mapService);
            DataContext = _viewModel;

            this.Closing += MainWindow_Closing;
        }

        private void MapPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            _viewModel?.HandleMapPointerPressed(e, sender);
        }

        private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                try
                {
                    await viewModel.SaveBeforeExitAsync();
                    Console.WriteLine("Данные сохранены перед закрытием приложения");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при сохранении: {ex.Message}");
                }
            }
        }
    }
}