using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using AxxonSoft_OSM_.Models;
using AxxonSoft_OSM_.Models.DataModels;
using AxxonSoft_OSM_.Services;
using AxxonSoft_OSM_.Views;
using Mapsui.Projections;
using Mapsui.UI.Avalonia;
using System;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
        private bool _isDarkTheme;
        private List<MapPoint> _tempAreaPoints = new List<MapPoint>();

        private readonly DataService _dataService;
        private AppSettings _appSettings;

        public ObservableCollection<MapPoint> Points { get; } = new ObservableCollection<MapPoint>();
        public ObservableCollection<MapArea> Areas { get; } = new ObservableCollection<MapArea>();

        public MapPoint? SelectedPoint
        {
            get => _selectedPoint;
            set
            {
                _selectedPoint = value;
                _mapService.CenterOn(SelectedPoint);
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
                StartAreaCommand.RaiseCanExecuteChanged();
                FinishAreaCommand.RaiseCanExecuteChanged();
                CancelAreaCommand.RaiseCanExecuteChanged();
            }
        }
        public Window OwnerWindow;

        //Коммнады для UI
        public RelayCommand DeleteSelectedPointCommand { get; }
        public RelayCommand ClearAllPointsCommand { get; }
        public ICommand AddPointCommand { get; }

        public RelayCommand StartAreaCommand { get; }
        public RelayCommand FinishAreaCommand { get; }
        public RelayCommand CancelAreaCommand { get; }
        public RelayCommand DeleteSelectedAreaCommand { get; }
        public RelayCommand ClearAllAreasCommand { get; }
        public RelayCommand SaveDataCommand { get; }
        public RelayCommand NewFile { get; }
        public RelayCommand OpenFile {  get; }
        public RelayCommand SaveFile { get; }
        public RelayCommand SaveFileToPath {  get; }
        public RelayCommand ToDarkTheme { get; }
        public RelayCommand ToLightTheme { get; }

        public MainWindowViewModel(MapService mapService)
        {
            _mapService = mapService;
            _dataService = new DataService();

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
            ToDarkTheme = new RelayCommand(
                execute: SetDarkTheme,
                canExecute: () => !_isDarkTheme
            );
            ToLightTheme = new RelayCommand(
                execute: SetLightTheme,
                canExecute: () => _isDarkTheme
            );
            NewFile = new RelayCommand(
                execute: CreateNewFile,
                canExecute: () => true
            );
            OpenFile = new RelayCommand(
                execute: ShowFindFileDialog,
                canExecute: () => true
            );
            SaveFile = new RelayCommand(
                execute: SaveDataAsync,
                canExecute: () => true
            );
            SaveFileToPath = new RelayCommand(
                execute: ShowSaveFileDialog,
                canExecute: () => true
            );

            Points.CollectionChanged += (s, e) => ClearAllPointsCommand.RaiseCanExecuteChanged();
            Areas.CollectionChanged += (s, e) => ClearAllAreasCommand.RaiseCanExecuteChanged();

            LoadSettings();
        }
        //События карты
        public void HandleMapPointerPressed(PointerPressedEventArgs e, object? mapControl)
        {
            if (mapControl is Mapsui.UI.Avalonia.MapControl myMapControl)
            {
                var screenPosition = e.GetPosition(myMapControl);
                var (lon, lat) = _mapService.ScreenToWorldCoordinates(screenPosition.X, screenPosition.Y);

                if (IsInAreaMode)
                {
                    AddTempAreaPoint(lat, lon);
                }
                else
                {
                    var existingFeature = _mapService.FindPointAtLocation(lat, lon);

                    if (existingFeature != null)
                    {
                        var pointToRemove = Points.FirstOrDefault(p => p.Feature == existingFeature);
                        RemovePointByFeature(pointToRemove);
                        Debug.WriteLine("Point removed.");
                    }
                    else
                    {
                        AddPoint(lat, lon);
                        Debug.WriteLine("Point added.");
                    }
                }
            }
        }
        public async Task SaveBeforeExitAsync()
        {
            SaveDataAsync();
            await SaveSettingsAsync();
        }
        //Точки
        private async void AddPoint(double lat, double lon)
        {
            if (!_mapService.IsValidCoordinate(lat, lon)) 
                return;

            var tempPoint = new MapPoint(lat, lon);
            var result = await ShowPointEditDialogAsync(tempPoint);
            
            if (result != null)
            {
                _mapService.AddPoint(result);
                Points.Add(result);
            }
        }

        private void RemovePointByFeature(MapPoint point)
        {
            _mapService.RemovePoint(point.Feature);

            var pointToRemove = Points.FirstOrDefault(p => p.Feature == point.Feature);
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
                RemovePointByFeature(SelectedPoint);
            }
        }

        private void ClearAllPoints()
        {
            _mapService.ClearAllPoints();
            Points.Clear();
            SelectedPoint = null;
        }
        //Области
        private void StartAreaMode()
        {
            IsInAreaMode = true;
            _tempAreaPoints.Clear();
            Debug.WriteLine("Режим создания области активирован");
        }

        private void AddTempAreaPoint(double lat, double lon)
        {
            if (!_mapService.IsValidCoordinate(lat, lon))
                return;

            var point = new MapPoint(lat, lon);
            _mapService.AddPoint(point);

            var newFeature = _mapService.FindPointAtLocation(lat, lon);
            point.Feature = newFeature;

            _tempAreaPoints.Add(point);
            FinishAreaCommand.RaiseCanExecuteChanged();

            Debug.WriteLine($"Добавлена точка области: {lat}, {lon} (всего: {_tempAreaPoints.Count})");
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
            _mapService.ClearAllAreas();
            Areas.Clear();
            SelectedArea = null;
        }
        //Диалоги
        private async Task<MapPoint?> ShowPointEditDialogAsync(MapPoint point)
        {
            if (OwnerWindow == null)
                return null;

            var viewModel = new PointEditDialogViewModel(point);
            var window = new PointEditDialog
            {
                DataContext = viewModel
            };

            // Используем ShowDialog для модального диалога
            var result = await window.ShowDialog<MapPoint?>(OwnerWindow);
            return result;
        }
        private async Task<MapArea?> ShowAreaEditDialogAsync(MapArea area)
        {
            if (OwnerWindow == null)
                return null;

            var viewModel = new AreaEditDialogViewModel(area);
            var window = new AreaEditDialog { DataContext = viewModel };

            // Используем ShowDialog для модального диалога
            var result = await window.ShowDialog<MapArea?>(OwnerWindow);
            return result;
        }
        
        public async Task LoadSettings()
        {
            _appSettings = await _dataService.LoadSettingsAsync();

            if (_appSettings.Theme == "Dark")
                SetDarkTheme();
            else SetLightTheme();

            _ = LoadDataFromFileAsync(_appSettings.LastSaveDirectory);
        }
        public async Task SaveSettingsAsync()
        {
            _appSettings.Theme = _isDarkTheme ? "Dark" : "Light";

            await _dataService.SaveSettingsAsync(_appSettings);
        }
        private async void SaveDataAsync()
        {
            try
            {
                Console.WriteLine($"Начато сохранение {Points.Count} точек...");

                // Сохраняем точки
                var pointsToSave = Points.Select(p => new PointData
                {
                    Longitude = p.Longitude,
                    Latitude = p.Latitude,
                    Name = p.Name,
                    Color = p.PointColor.ToString(),
                    Size = p.PointSize
                }).ToList();
                
                // Сохраняем области
                var areasToSave = Areas.Select(a => new AreaData
                {
                    Name = a.Name,
                    Points = a.Points.Select(p => new AreaPointData
                    {
                        Longitude = p.Longitude,
                        Latitude = p.Latitude
                    }).ToList(),
                    FillColor = a.FillColor.ToString(),
                    BorderColor = a.BorderColor.ToString()
                }).ToList();

                // Сохраняем камеру
                var cameraToSave = _mapService.GetCameraState();

                // Создаем полный объект сохранения
                var saveData = new SaveData
                {
                    Camera = cameraToSave,
                    Points = pointsToSave,
                    Areas = areasToSave
                };

                string savePath;
                if (!string.IsNullOrEmpty(_appSettings.LastSaveDirectory))
                {
                    savePath = _appSettings.LastSaveDirectory;
                }
                else
                {
                    // Если путь не указан, открываем диалог сохранения
                    ShowSaveFileDialog();
                    return;
                }

                _appSettings.LastSaveDirectory = savePath;

                await _dataService.SaveDataAsync(saveData,_appSettings.LastSaveDirectory);

                Console.WriteLine($"Успешно сохранено {pointsToSave.Count} точек");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в SaveDataAsync: {ex.Message}");
            }
        }
        //Файлы
        private async void ShowFindFileDialog()
        {
            try
            {
                if (OwnerWindow == null)
                {
                    Debug.WriteLine("OwnerWindow is null");
                    return;
                }

                // Получаем TopLevel из окна
                var topLevel = TopLevel.GetTopLevel(OwnerWindow);
                if (topLevel == null)
                {
                    Debug.WriteLine("TopLevel is null");
                    return;
                }

                // Настройки диалога
                var options = new FilePickerOpenOptions
                {
                    Title = "Открыть файл проекта",
                    AllowMultiple = false,
                    FileTypeFilter = new FilePickerFileType[]
                    {
                new("JSON файлы") { Patterns = new[] { "*.json" } },
                new("Все файлы") { Patterns = new[] { "*.*" } }
                    }
                };

                // Открываем диалог выбора файла
                var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);

                if (files.Count >= 1)
                {
                    var selectedFile = files[0];
                    var filePath = selectedFile.Path.LocalPath;

                    Debug.WriteLine($"Выбран файл: {filePath}");

                    // Проверяем расширение файла
                    if (!Path.GetExtension(filePath).Equals(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine("Выбранный файл не является JSON файлом");
                        return;
                    }

                    // Загружаем данные из выбранного файла
                    await LoadDataFromFileAsync(filePath);

                    // Обновляем настройки
                    _appSettings.LastSaveDirectory = filePath;
                    await SaveSettingsAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при открытии диалога файла: {ex.Message}");
            }
        }
        private async void ShowSaveFileDialog()
        {
            if (OwnerWindow == null)
                return;

            var topLevel = TopLevel.GetTopLevel(OwnerWindow);
            if (topLevel == null)
                return;

            var options = new FilePickerSaveOptions
            {
                Title = "Сохранить проект",
                FileTypeChoices = new FilePickerFileType[]
                {
            new("JSON файлы") { Patterns = new[] { "*.json" } },
            new("Все файлы") { Patterns = new[] { "*.*" } }
                },
                DefaultExtension = "json",
                ShowOverwritePrompt = true
            };

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(options);
            if (file == null) return;
            SaveDataToPathAsync(file.Path.LocalPath);
        }
        private async Task LoadDataFromFileAsync(String Path)
        {
            try
            {
                Console.WriteLine("Загрузка данных...");

                var saveData = await _dataService.LoadDataAsync(Path);
                if (saveData == null)
                {
                    Console.WriteLine("Данные не загружены (null)");
                    return;
                }

                // Очищаем текущие данные
                ClearAllAreas();
                ClearAllPoints();

                Console.WriteLine($"Загрузка {saveData.Points.Count} точек...");

                // Восстанавливаем точки
                foreach (var savedPoint in saveData.Points)
                {
                    try
                    {
                        var point = new MapPoint(
                            latitude: savedPoint.Latitude,
                            longitude: savedPoint.Longitude)
                        {
                            Name = savedPoint.Name,
                            PointColor = Color.Parse(savedPoint.Color),
                            PointSize = savedPoint.Size
                        };

                        if (!_mapService.IsValidCoordinate(point.Latitude, point.Longitude))
                            return;

                        _mapService.AddPoint(point);
                        Points.Add(point);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при загрузке точки: {ex.Message}");
                    }
                }

                // Восстанавливаем области
                Console.WriteLine($"Загрузка {saveData.Areas.Count} областей...");
                foreach (var savedArea in saveData.Areas)
                {
                    try
                    {
                        var areaPoints = savedArea.Points.Select(p =>
                            new MapPoint(p.Latitude, p.Longitude)).ToList();

                        var area = new MapArea
                        {
                            Name = savedArea.Name,
                            FillColor = Color.Parse(savedArea.FillColor),
                            BorderColor = Color.Parse(savedArea.BorderColor)
                        };

                        area.Points.AddRange(areaPoints);
                        _mapService.AddArea(areaPoints, area);
                        Areas.Add(area);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при загрузке области: {ex.Message}");
                    }
                }

                // Восстанавливаем камеру
                Console.WriteLine("Восстанавливаем состояние камеры...");
                _mapService.SetCameraState(saveData.Camera);

                Console.WriteLine($"Успешно загружено {Points.Count} точек");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в LoadDataAsync: {ex.Message}");
            }
        }
        private async Task SaveDataToPathAsync(string filePath)
        {
            try
            {
                var pointsToSave = Points.Select(p => new PointData
                {
                    Longitude = p.Longitude,
                    Latitude = p.Latitude,
                    Name = p.Name,
                    Color = p.PointColor.ToString(),
                    Size = p.PointSize
                }).ToList();

                var areasToSave = Areas.Select(a => new AreaData
                {
                    Name = a.Name,
                    Points = a.Points.Select(p => new AreaPointData
                    {
                        Longitude = p.Longitude,
                        Latitude = p.Latitude
                    }).ToList(),
                    FillColor = a.FillColor.ToString(),
                    BorderColor = a.BorderColor.ToString()
                }).ToList();

                var cameraToSave = _mapService.GetCameraState();

                var saveData = new SaveData
                {
                    Camera = cameraToSave,
                    Points = pointsToSave,
                    Areas = areasToSave
                };

                await _dataService.SaveDataAsync(saveData, filePath);

                // Обновляем настройки
                _appSettings.LastSaveDirectory = filePath;
                await SaveSettingsAsync();

                Console.WriteLine($"Проект сохранен как: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения: {ex.Message}");
            }
        }
        public async Task HandleMapClickAsync(double screenX, double screenY, MapControl mapControl)
        {
            var (lon, lat) = _mapService.ScreenToWorldCoordinates(screenX, screenY);

            if (IsInAreaMode)
            {
                AddTempAreaPoint(lat, lon);
            }
            else
            {
                var existingFeature = _mapService.FindPointAtLocation(lat, lon);

                if (existingFeature != null)
                {
                    var pointToRemove = Points.FirstOrDefault(p => p.Feature == existingFeature);
                    RemovePointByFeature(pointToRemove);
                    Debug.WriteLine("Point removed.");
                }
                else
                {
                    AddPoint(lat, lon);
                    Debug.WriteLine("Point added.");
                }
            }
        }
        private void CreateNewFile()
        {
            _mapService.CenterAndZoomOn(new MapPoint(0,0),80000);
            _appSettings.LastSaveDirectory = "";
            ClearAllAreas();
            ClearAllPoints();
        }
        //Установка Темы приложения
        private void SetDarkTheme()
        {
            Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
            _isDarkTheme = true;
        }
        private void SetLightTheme()
        {
            Application.Current.RequestedThemeVariant = ThemeVariant.Light;
            _isDarkTheme = false;
        }
    }
}