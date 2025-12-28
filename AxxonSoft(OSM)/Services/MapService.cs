using AxxonSoft_OSM_.Models;
using AxxonSoft_OSM_.Models.DataModels;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Avalonia;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AxxonSoft_OSM_.Services
{
    public class MapService
    {
        private readonly MapControl _mapControl;
        private readonly WritableLayer _pointsLayer;
        private readonly WritableLayer _areasLayer;

        private const double MIN_LATITUDE = -85.05112878;
        private const double MAX_LATITUDE = 85.05112878;
        private const double MIN_LONGITUDE = -180.0;
        private const double MAX_LONGITUDE = 180.0; 

        public MapService(MapControl mapControl)
        {
            _mapControl = mapControl;

            _pointsLayer = new WritableLayer
            {
                Name = "Points",
                Style = new SymbolStyle
                {
                    SymbolType = SymbolType.Ellipse,
                    SymbolScale = 0.1
                }
            };

            _areasLayer = new WritableLayer
            {
                Name = "Areas"
            };

            _areasLayer.Opacity= 0.3;

            var tileLayer = OpenStreetMap.CreateTileLayer("AxxonSoft_OSM/1.0 (1997denic@gmail.com)");
            _mapControl.Map.Layers.Add(tileLayer);
            _mapControl.Map.Layers.Add(_areasLayer);
            _mapControl.Map.Layers.Add(_pointsLayer);

            _mapControl.Map.Widgets.Clear();
        }

        private static IStyle CreatePointStyle(MapPoint point)
        {
            return new SymbolStyle
            {SymbolType = SymbolType.Ellipse,
                Fill = new Brush(Color.FromArgb(point.PointColor.A, point.PointColor.R, point.PointColor.G, point.PointColor.B)),
                Outline = new Pen(Color.Black, 2),
                SymbolScale = point.PointSize
            };
        }

        private static IStyle CreateAreaStyle(MapArea area)
        {
            return new VectorStyle
            {
                Fill = new Brush(new Color(area.FillColor.R, area.FillColor.G, area.FillColor.B)),
                Outline = new Pen(new Color(area.BorderColor.R, area.BorderColor.G, area.BorderColor.B), 2),
            };
        }

        public bool IsValidCoordinate(double latitude, double longitude)
        {
            if (latitude < MIN_LATITUDE || latitude > MAX_LATITUDE)
            {
                Console.WriteLine($"Широта {latitude:F6} выходит за пределы карты ({MIN_LATITUDE} до {MAX_LATITUDE})");
                return false;
            }

            if (longitude < MIN_LONGITUDE || longitude > MAX_LONGITUDE)
            {
                Console.WriteLine($"Долгота {longitude:F6} выходит за пределы карты ({MIN_LONGITUDE} до {MAX_LONGITUDE})");
                return false;
            }

            if (double.IsNaN(latitude) || double.IsNaN(longitude))
            {
                Console.WriteLine($"Координаты содержат NaN значения");
                return false;
            }

            return true;
        }

        public MapPoint AddPoint(MapPoint point)
        {
            if (!IsValidCoordinate(point.Latitude, point.Longitude))
            {
                Console.WriteLine($"Не удалось добавить точку {point.Name}: координаты вне границ карты");
                return null;
            }
            var mpoint = SphericalMercator.FromLonLat(point.Longitude, point.Latitude).ToMPoint();
            
            var feature = new PointFeature(mpoint);

            feature.Styles.Add(CreatePointStyle(point));

            _pointsLayer.Add(feature);
            point.Feature = feature;
            _pointsLayer.DataHasChanged();
            _mapControl.Refresh();

            return point;
        }

        public void UpdatePointStyle(MapPoint point)
        {
            if (point.Feature != null)
            {
                point.Feature.Styles.Clear();
                point.Feature.Styles.Add(CreatePointStyle(point));
                _pointsLayer.DataHasChanged();
                _mapControl.Refresh();
            }
        }

        public void RemovePoint(IFeature pointToRemove)
        {
            _pointsLayer.TryRemove(pointToRemove);
            _pointsLayer.DataHasChanged();
            _mapControl.Refresh();
        }

        public void ClearAllPoints()
        {
            _pointsLayer.Clear();
            _pointsLayer.DataHasChanged();
            _mapControl.Refresh();
        }

        public void ClearAllAreas()
        {
            _areasLayer.Clear();
            _pointsLayer.DataHasChanged();
            _mapControl.Refresh();
        }

        public MapPoint FindPointAtLocation(double lat, double lon, IEnumerable<MapPoint> points, double pixelTolerance = 10)
        {
            var targetPoint = SphericalMercator.FromLonLat(lon, lat);
            var targetScreen = _mapControl.Map.Navigator.Viewport.WorldToScreen(targetPoint.ToMPoint());

            foreach (var point in points)
            {
                var pointWorld = SphericalMercator.FromLonLat(point.Longitude, point.Latitude);
                var pointScreen = _mapControl.Map.Navigator.Viewport.WorldToScreen(pointWorld.ToMPoint());

                var screenDistance = Math.Sqrt(
                    Math.Pow(pointScreen.X - targetScreen.X, 2) +
                    Math.Pow(pointScreen.Y - targetScreen.Y, 2));

                if (screenDistance <= point.PointSize * 15 && point.Feature != null)
                {
                    return point;
                }
            }

            return null;
        }

        public (double lon, double lat) ScreenToWorldCoordinates(double screenX, double screenY)
        {
            var worldPosition = _mapControl.Map.Navigator.Viewport.ScreenToWorld(screenX, screenY);
            return SphericalMercator.ToLonLat(worldPosition.X, worldPosition.Y);
        }

        public void AddArea(List<MapPoint> areaPoints, MapArea mapArea)
        {
            if (areaPoints.Count < 3) return;

            var coordinates = areaPoints.Select(p =>
                SphericalMercator.FromLonLat(p.Longitude, p.Latitude).ToMPoint()
            ).ToList();

            coordinates.Add(coordinates[0]);

            var polygon = new Polygon(new LinearRing(coordinates.Select(p => new Coordinate(p.X, p.Y)).ToArray()));
            var feature = new GeometryFeature(polygon);

            feature.Styles.Add(CreateAreaStyle(mapArea));

            mapArea.Feature = feature;

            _areasLayer.Add(feature);
            _areasLayer.DataHasChanged();
            _mapControl.Refresh();
        }

        public void RemoveArea(MapArea area)
        {
            if (area.Feature is IFeature feature)
            {
                _areasLayer.TryRemove(feature);
                _areasLayer.DataHasChanged();
                _mapControl.Refresh();
            }
        }

        public void ClearTempAreaPoints(List<MapPoint> tempPoints)
        {
            foreach (var point in tempPoints)
            {
                if (point.Feature is IFeature feature)
                {
                    _pointsLayer.TryRemove(feature);
                }
            }
            _pointsLayer.DataHasChanged();
            _mapControl.Refresh();
        }

        public CameraData GetCameraState()
        {
            var viewport = _mapControl.Map.Navigator.Viewport;
            return new CameraData
            {
                CenterX = viewport.CenterX,
                CenterY = viewport.CenterY,
                Resolution = viewport.Resolution
            };
        }

        public void SetCameraState(CameraData camera)
        {
            if (camera == null) return;
            if (camera.Resolution == 0) camera.Resolution = 40000;
            _mapControl.Map.Navigator.CenterOn(camera.CenterX, camera.CenterY);
            _mapControl.Map.Navigator.ZoomTo(camera.Resolution);
            _mapControl.Refresh();

            Console.WriteLine($"Камера восстановлена: X={camera.CenterX:F2}, Y={camera.CenterY:F2}, Zoom={camera.Resolution:F2}");
        }

        public void CenterOnPoint(MapPoint point)
        {
            if (point == null) return;
            var mercator = SphericalMercator.FromLonLat(point.Longitude, point.Latitude);
            _mapControl.Map.Navigator.CenterOnAndZoomTo(new MPoint(mercator.x, mercator.y), _mapControl.Map.Navigator.Viewport.Resolution, 1000);
        }
        public void CenterOnArea(MapArea area, bool fitToView = true)
        {
            if (area == null || area.Points == null || area.Points.Count == 0) return;

            double minLon = area.Points[0].Longitude;
            double maxLon = area.Points[0].Longitude;
            double minLat = area.Points[0].Latitude;
            double maxLat = area.Points[0].Latitude;

            foreach (var point in area.Points)
            {
                if (point.Longitude < minLon) minLon = point.Longitude;
                if (point.Longitude > maxLon) maxLon = point.Longitude;
                if (point.Latitude < minLat) minLat = point.Latitude;
                if (point.Latitude > maxLat) maxLat = point.Latitude;
            }

            double centerLon = (minLon + maxLon) / 2;
            double centerLat = (minLat + maxLat) / 2;

            var mercator = SphericalMercator.FromLonLat(centerLon, centerLat);

            if (fitToView)
            {
                var minPoint = SphericalMercator.FromLonLat(minLon, minLat);
                var maxPoint = SphericalMercator.FromLonLat(maxLon, maxLat);

                double width = Math.Abs(maxPoint.x - minPoint.x);
                double height = Math.Abs(maxPoint.y - minPoint.y);

                double viewportWidth = _mapControl.Map.Navigator.Viewport.Width;
                double viewportHeight = _mapControl.Map.Navigator.Viewport.Height;

                double resolutionX = width / viewportWidth;
                double resolutionY = height / viewportHeight;

                double resolution = Math.Max(resolutionX, resolutionY) * 1.2; // Добавляем отступ 20%

                _mapControl.Map.Navigator.CenterOnAndZoomTo(
                    new MPoint(mercator.x, mercator.y),
                    resolution,
                    1000);
            }
            else
            {
                _mapControl.Map.Navigator.CenterOnAndZoomTo(
                    new MPoint(mercator.x, mercator.y),
                    _mapControl.Map.Navigator.Viewport.Resolution,
                    1000);
            }
        }
        public void CenterAndZoomOn(MapPoint point,double Resolution)
        {
            if (point == null) return;
            var mercator = SphericalMercator.FromLonLat(point.Longitude, point.Latitude);
            _mapControl.Map.Navigator.CenterOnAndZoomTo(new MPoint(mercator.x, mercator.y), Resolution, 1000);
        }
    }
}