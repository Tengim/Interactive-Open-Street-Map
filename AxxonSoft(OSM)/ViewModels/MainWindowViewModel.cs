using Avalonia.Input;
using AxxonSoft_OSM_.Models;
using AxxonSoft_OSM_.Services;
using AxxonSoft_OSM_.Views;
using Mapsui;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AxxonSoft_OSM_.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly MapService _mapService;
        private MapPoint? _selectedPoint;
        private MapArea? _selectedArea;
        private bool _isInAreaMode;
        private List<MapPoint> _tempAreaPoints = new List<MapPoint>();

        public ObservableCollection<MapPoint> Points { get; } = new ObservableCollection<MapPoint>();
        public ObservableCollection<MapArea> Areas { get; } = new ObservableCollection<MapArea>();

        public MapPoint? SelectedPoint
        {
            get => _selectedPoint;
            set
            {
                _selectedPoint = value;
                OnPropertyChanged();
                DeleteSelectedPointCommand.RaiseCanExecuteChanged();
            }
        }

        public MapArea? SelectedArea
        {
            get => _selectedArea;
            set
            {
                _selectedArea = value;
                OnPropertyChanged();
                DeleteSelectedAreaCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsInAreaMode
        {
            get => _isInAreaMode;
            set
            {
                _isInAreaMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotInAreaMode));
                StartAreaCommand.RaiseCanExecuteChanged();
                FinishAreaCommand.RaiseCanExecuteChanged();
                CancelAreaCommand.RaiseCanExecuteChanged();
            }
        }
        
        public bool IsNotInAreaMode => !IsInAreaMode;

        //Коммнады для UI
        public RelayCommand DeleteSelectedPointCommand { get; }
        public RelayCommand ClearAllPointsCommand { get; }
        public ICommand AddPointCommand { get; }

        public RelayCommand StartAreaCommand { get; }
        public RelayCommand FinishAreaCommand { get; }
        public RelayCommand CancelAreaCommand { get; }
        public RelayCommand DeleteSelectedAreaCommand { get; }
        public RelayCommand ClearAllAreasCommand { get; }

        public MainWindowViewModel(MapService mapService)
        {
            _mapService = mapService;

            AddPointCommand = new RelayCommand<double[]>((coords) => AddPoint(coords[0], coords[1]));
            DeleteSelectedPointCommand = new RelayCommand(
                execute: DeleteSelectedPoint,
                canExecute: () => SelectedPoint != null
            );
            ClearAllPointsCommand = new RelayCommand(
                execute: ClearAllPoints,
                canExecute: () => Points.Count > 0
            );

            StartAreaCommand = new RelayCommand(
                execute: StartAreaMode,
                canExecute: () => !IsInAreaMode
            );
            FinishAreaCommand = new RelayCommand(
                execute: FinishArea,
                canExecute: () => IsInAreaMode && _tempAreaPoints.Count >= 3
            );
            CancelAreaCommand = new RelayCommand(
                execute: CancelArea,
                canExecute: () => IsInAreaMode
            );
            DeleteSelectedAreaCommand = new RelayCommand(
                execute: DeleteSelectedArea,
                canExecute: () => SelectedArea != null
            );
            ClearAllAreasCommand = new RelayCommand(
                execute: ClearAllAreas,
                canExecute: () => Areas.Count > 0
            );

            Points.CollectionChanged += (s, e) => ClearAllPointsCommand.RaiseCanExecuteChanged();
            Areas.CollectionChanged += (s, e) => ClearAllAreasCommand.RaiseCanExecuteChanged();
        }

        public void HandleMapPointerPressed(PointerPressedEventArgs e, object? mapControl)
        {
            if (mapControl is Mapsui.UI.Avalonia.MapControl myMapControl)
            {
                var screenPosition = e.GetPosition(myMapControl);
                var (lon, lat) = _mapService.ScreenToWorldCoordinates(screenPosition.X, screenPosition.Y);

                if (IsInAreaMode)
                {
                    AddTempAreaPoint(lon, lat);
                }
                else
                {
                    var existingFeature = _mapService.FindPointAtLocation(lon, lat);

                    if (existingFeature != null)
                    {
                        RemovePointByFeature(existingFeature);
                        Debug.WriteLine("Point removed.");
                    }
                    else
                    {
                        AddPoint(lon, lat);
                        Debug.WriteLine("Point added.");
                    }
                }
            }
        }

        private async void AddPoint(double lng, double lat)
        {
            var tempPoint = new MapPoint(lng, lat);
            var result = await ShowPointEditDialogAsync(tempPoint);
            
            if (result != null)
            {
                _mapService.AddPoint(result);
                var newFeature = _mapService.FindPointAtLocation(result.Longitude, result.Latitude);
                result.Feature = newFeature;
                Points.Add(result);
            }
        }

        private void RemovePointByFeature(IFeature feature)
        {
            _mapService.RemovePoint(feature);

            var pointToRemove = Points.FirstOrDefault(p => p.Feature == feature);
            if (pointToRemove != null)
            {
                Points.Remove(pointToRemove);

                if (SelectedPoint == pointToRemove)
                {
                    SelectedPoint = null;
                }
            }
        }

        private void DeleteSelectedPoint()
        {
            if (SelectedPoint?.Feature != null)
            {
                RemovePointByFeature(SelectedPoint.Feature);
            }
        }

        private void ClearAllPoints()
        {
            _mapService.ClearAllPoints();
            Points.Clear();
            SelectedPoint = null;
        }

        private void StartAreaMode()
        {
            IsInAreaMode = true;
            _tempAreaPoints.Clear();
            Debug.WriteLine("Режим создания области активирован");
        }

        private void AddTempAreaPoint(double lng, double lat)
        {
            var point = new MapPoint(lng, lat);
            _mapService.AddPoint(point);

            var newFeature = _mapService.FindPointAtLocation(lng, lat);
            point.Feature = newFeature;

            _tempAreaPoints.Add(point);
            FinishAreaCommand.RaiseCanExecuteChanged();

            Debug.WriteLine($"Добавлена точка области: {lng}, {lat} (всего: {_tempAreaPoints.Count})");
        }

        private async void FinishArea()
        {
            if (_tempAreaPoints.Count < 3) return;

            var tempArea = new MapArea();
            tempArea.Points.AddRange(_tempAreaPoints);

            var result = await ShowAreaEditDialogAsync(tempArea);

            if (result != null)
            {
                _mapService.AddArea(_tempAreaPoints, result);
                Areas.Add(result);
                _mapService.ClearTempAreaPoints(_tempAreaPoints);
                _tempAreaPoints.Clear();
                IsInAreaMode = false;
            }
            else
            {
                CancelArea();
            }
        }

        private void CancelArea()
        {
            _mapService.ClearTempAreaPoints(_tempAreaPoints);
            _tempAreaPoints.Clear();
            IsInAreaMode = false;
            Debug.WriteLine("Создание области отменено");
        }

        private void DeleteSelectedArea()
        {
            if (SelectedArea != null)
            {
                _mapService.RemoveArea(SelectedArea);
                Areas.Remove(SelectedArea);
                SelectedArea = null;
            }
        }

        private void ClearAllAreas()
        {
            foreach (var area in Areas.ToList())
            {
                _mapService.RemoveArea(area);
            }
            Areas.Clear();
            SelectedArea = null;
        }

        private async Task<MapPoint?> ShowPointEditDialogAsync(MapPoint point)
        {
            var tcs = new TaskCompletionSource<MapPoint?>();

            var viewModel = new PointEditDialogViewModel(point);
            var window = new PointEditDialog
            {
                DataContext = viewModel
            };

            viewModel.CloseDialog += (result) =>
            {
                tcs.SetResult(result);
                window.Close();
            };

            window.Show();

            return await tcs.Task;
        }
        private async Task<MapArea?> ShowAreaEditDialogAsync(MapArea area)
        {
            var tcs = new TaskCompletionSource<MapArea?>();

            var viewModel = new AreaEditDialogViewModel(area);
            var window = new AreaEditDialog { DataContext = viewModel };

            viewModel.CloseDialog += (result) =>
            {
                tcs.SetResult(result);
                window.Close();
            };

            window.Show();
            return await tcs.Task;
        }
    }
}