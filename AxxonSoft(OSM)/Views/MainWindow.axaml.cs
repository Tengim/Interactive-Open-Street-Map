using Avalonia;
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
        private Point _lastPointerPosition;
        private bool _isDragging;
        private const double DragThreshold = 5.0;

        public MainWindow()
        {
            InitializeComponent();

            var mapService = new MapService(MyMapControl);
            _viewModel = new MainWindowViewModel(mapService , this);
            DataContext = _viewModel;

            this.Closing += MainWindow_Closing;

        }

        private void MapPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var pointer = e.GetCurrentPoint(MyMapControl);
            _lastPointerPosition = pointer.Position;
            _isDragging = false;

            e.Pointer.Capture(MyMapControl);
        }

        private void MapPointerMoved(object? sender, PointerEventArgs e)
        {
            var pointer = e.GetCurrentPoint(MyMapControl);

            if (pointer.Properties.IsLeftButtonPressed)
            {
                var deltaX = pointer.Position.X - _lastPointerPosition.X;
                var deltaY = pointer.Position.Y - _lastPointerPosition.Y;
                var delta = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                if (delta > DragThreshold)
                {
                    _isDragging = true;
                }
            }
        }

        private async void MapPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            e.Pointer.Capture(null);

            if (_isDragging)
            {
                _isDragging = false;
                return;
            }

            var pointer = e.GetCurrentPoint(MyMapControl);
            if (pointer.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                if (_viewModel != null)
                {
                    await _viewModel.HandleMapClickAsync(_lastPointerPosition.X, _lastPointerPosition.Y, MyMapControl);
                }
            }
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