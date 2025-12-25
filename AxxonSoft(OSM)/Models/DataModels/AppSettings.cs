using System;

namespace AxxonSoft_OSM_.Models.DataModels
{
    public class AppSettings
    {
        public string Theme { get; set; } = "Light"; // Light, Dark или System
        public string? LastSaveDirectory { get; set; }
    }
}