using Avalonia.Media;
using AxxonSoft_OSM_.Models;
using AxxonSoft_OSM_.ViewModels;
using System;

public class PointEditDialogViewModel : ViewModelBase
{
    private MapPoint _point;

    public MapPoint Point
    {
        get => _point;
        set
        {
            _point = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(PointColor));
            OnPropertyChanged(nameof(PointSize));
            OnPropertyChanged(nameof(Longitude));
            OnPropertyChanged(nameof(Latitude));
        }
    }

    public string Name
    {
        get => _point.Name;
        set
        {
            _point.Name = value;
            OnPropertyChanged();
        }
    }

    public Color PointColor
    {
        get => _point.PointColor;
        set
        {
            _point.PointColor = value;
            OnPropertyChanged();
        }
    }
    public double PointSize
    {
        get => _point.PointSize;
        set
        {
            _point.PointSize = value;
            OnPropertyChanged();
        }
    }
    public double Longitude => _point.Longitude;
    public double Latitude => _point.Latitude;
    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }

    public PointEditDialogViewModel(MapPoint point)
    {
        Point = point;

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
    }

    private void Save()
    {
        CloseDialog?.Invoke(Point);
    }

    private void Cancel()
    {
        CloseDialog?.Invoke(null);
    }

    public event Action<MapPoint?>? CloseDialog;
}