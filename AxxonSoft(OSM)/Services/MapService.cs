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

        public IFeature? FindPointAtLocation(double lat, double lon, double pixelTolerance = 10)
        {
            var targetPoint = SphericalMercator.FromLonLat(lon, lat);
            var targetScreen = _mapControl.Map.Navigator.Viewport.WorldToScreen(targetPoint.ToMPoint());

            foreach (var feature in _pointsLayer.GetFeatures())
            {
                if (feature is PointFeature pointFeature)
                {
                    var point = pointFeature.Point;
                    var pointScreen = _mapControl.Map.Navigator.Viewport.WorldToScreen(point);

                    var screenDistance = Math.Sqrt(
                        Math.Pow(pointScreen.X - targetScreen.X, 2) +
                        Math.Pow(pointScreen.Y - targetScreen.Y, 2));

                    if (screenDistance < pixelTolerance)
                    {
                        return feature;
                    }
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

        public void CenterOn(MapPoint point)
        {
            if (point == null) return;
            var mercator = SphericalMercator.FromLonLat(point.Longitude, point.Latitude);
            //_mapControl.Map.Navigator.CenterOn(mercator.x, mercator.y);
            _mapControl.Map.Navigator.CenterOnAndZoomTo(new MPoint(mercator.x, mercator.y), _mapControl.Map.Navigator.Viewport.Resolution, 1000);
        }
    }
}