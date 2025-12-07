using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AxxonSoft_OSM_.Models.DataModels
{
    public class SaveData
    {
        public CameraData Camera { get; set; } = new CameraData();
        public List<PointData> Points { get; set; } = new List<PointData>();
        public List<AreaData> Areas { get; set; } = new List<AreaData>();
    }
    public class CameraData
    {
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double Resolution { get; set; } // Уровень масштабирования (zoom)
        public double Rotation { get; set; }
    }

    public class PointData
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#FFFF0000";
        public double Size { get; set; } = 1.0;
    }

    public class AreaPointData
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }

    public class AreaData
    {
        public string Name { get; set; } = string.Empty;
        public List<AreaPointData> Points { get; set; } = new List<AreaPointData>();
        public string FillColor { get; set; } = "#80FF0000"; // Полупрозрачный красный
        public string BorderColor { get; set; } = "#FFFF0000"; // Красный
    }

    public class ColorConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var colorString = reader.GetString();
            return Color.Parse(colorString ?? "#FFFF0000");
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}