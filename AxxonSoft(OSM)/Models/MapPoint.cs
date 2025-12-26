using Avalonia.Media;
using Mapsui;

namespace AxxonSoft_OSM_.Models
{
    public class MapPoint
    {
        public double Longitude { get; set; } // Долгота
        public double Latitude { get; set; }   // Широта
        public string Name { get; set; } = "Точка";
        public Color PointColor { get; set; } = Colors.Red;
        public double PointSize { get; set; } = 0.5;
        public IFeature? Feature { get; set; }
        public MapPoint(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public string DisplayText => string.IsNullOrEmpty(Name)
            ? $"Без имени ({Latitude:F4}, {Longitude:F4})"
            : $"{Name} ({Latitude:F4}, {Longitude:F4})";
    }
}