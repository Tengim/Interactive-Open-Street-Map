using Avalonia.Media;
using AxxonSoft_OSM_.Models;
using AxxonSoft_OSM_.ViewModels;
using System;


public class AreaEditDialogViewModel : ViewModelBase
{
    private MapArea _area;

    public MapArea Area
    {
        get => _area;
        set
        {
            _area = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(FillColor));
            OnPropertyChanged(nameof(BorderColor));
        }
    }

    public string Name
    {
        get => _area.Name;
        set
        {
            _area.Name = value;
            OnPropertyChanged();
        }
    }
    public Color FillColor
    {
        get => _area.FillColor;
        set
        {
            _area.FillColor = value;
            OnPropertyChanged();
        }
    }
    public Color BorderColor
    {
        get => _area.BorderColor;
        set
        {
            _area.BorderColor = value;
            OnPropertyChanged();
        }
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }

    public AreaEditDialogViewModel(MapArea area)
    {
        Area = area;

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
    }

    private void Save()
    {
        Area = _area;
        CloseDialog?.Invoke(Area);
    }

    private void Cancel()
    {
        CloseDialog?.Invoke(null);
    }

    public event Action<MapArea?>? CloseDialog;
}