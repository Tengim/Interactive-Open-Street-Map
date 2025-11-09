using Mapsui;

namespace AxxonSoft_OSM_.Models
{
    public class MapPoint
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public IFeature? Feature { get; set; }

        public string DisplayText => $"{Latitude:F4}, {Longitude:F4}";

        public MapPoint() { }

        public MapPoint(double lng, double lat, IFeature? feature = null)
        {
            Longitude = lng;
            Latitude = lat;
            Feature = feature;
        }
    }
}