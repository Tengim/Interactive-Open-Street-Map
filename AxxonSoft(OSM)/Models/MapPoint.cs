using Avalonia.Media;
using Mapsui;

namespace AxxonSoft_OSM_.Models
{
    public class MapPoint
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public string Name { get; set; } = string.Empty;
        public Color PointColor { get; set; } = Colors.Red;
        public double PointSize { get; set; } = 0.5;
        public IFeature? Feature { get; set; }
        public MapPoint(double lat, double lon) {
            Longitude = lat; 
            Latitude = lon;
        }

        public string DisplayText => string.IsNullOrEmpty(Name)
            ? $"{Latitude:F4}, {Longitude:F4}"
            : $"{Name} ({Latitude:F4}, {Longitude:F4})";
    }
}