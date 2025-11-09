using AxxonSoft_OSM_.Services;
using AxxonSoft_OSM_.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;

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
        }

        private void MapPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            _viewModel?.HandleMapPointerPressed(e, sender);
        }
    }
}