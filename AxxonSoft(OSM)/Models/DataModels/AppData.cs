using System.Collections.Generic;

namespace AxxonSoft_OSM_.Models.DataModels
{
    public class AppData
    {
        public CameraData Camera { get; set; } = new CameraData();
        public List<PointData> Points { get; set; } = new List<PointData>();
        public List<AreaData> Areas { get; set; } = new List<AreaData>();
    }
}