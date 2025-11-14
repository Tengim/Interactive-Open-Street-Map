using Avalonia.Media;
using System.Collections.Generic;
using System.Linq;

namespace AxxonSoft_OSM_.Models
{
    public class MapArea
    {
        public List<MapPoint> Points { get; } = new List<MapPoint>();
        public string Name { get; set; } = "Область";
        public Color FillColor { get; set; } = new Color(255, 0, 0, 128);
        public Color BorderColor { get; set; } = new Color(255, 0, 0, 255);

        public object? Feature { get; set; }

        public string DisplayText => $"{Name} ({Points.Count} точек)";

        public bool IsComplete => Points.Count >= 3;
    }
}