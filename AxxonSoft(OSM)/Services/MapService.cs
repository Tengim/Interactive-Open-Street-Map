using AxxonSoft_OSM_.Models;
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

        public MapService(MapControl mapControl)
        {
            _mapControl = mapControl;

            _pointsLayer = new WritableLayer
            {
                Name = "Points",
                Style = new SymbolStyle
                {
                    SymbolType = SymbolType.Ellipse,
                    Fill = new Brush(Color.Red),
                    Outline = new Pen(Color.Black, 2),
                    SymbolScale = 0.5
                }
            };

            _areasLayer = new WritableLayer
            {
                Name = "Areas",
                Style = CreateAreaStyle()
            };

            var tileLayer = OpenStreetMap.CreateTileLayer("AxxonSoft_OSM/1.0 (1997denic@gmail.com)");
            _mapControl.Map.Layers.Add(tileLayer);
            _mapControl.Map.Layers.Add(_areasLayer);
            _mapControl.Map.Layers.Add(_pointsLayer);

            _mapControl.Map.Widgets.Clear();
            var mercator = SphericalMercator.FromLonLat(28.02, 53.31);//Будет подгружаться из файла
            _mapControl.Map.Navigator.CenterOnAndZoomTo(new MPoint(mercator.x, mercator.y), 2000, 0000);
        }

        private static VectorStyle CreateAreaStyle()
        {
            return new VectorStyle
            {
                Fill = new Brush(new Color(255, 0, 0, 128)),
                Outline = new Pen(Color.Red, 3)
            };
        }

        public void AddPoint(double lng, double lat)
        {
            var point = SphericalMercator.FromLonLat(lng, lat).ToMPoint();
            var feature = new PointFeature(point);
            _pointsLayer.Add(feature);
            _pointsLayer.DataHasChanged();
            _mapControl.Refresh();
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

        public IFeature? FindPointAtLocation(double lng, double lat, double pixelTolerance = 10)
        {
            var targetPoint = SphericalMercator.FromLonLat(lng, lat).ToMPoint();
            var targetScreen = _mapControl.Map.Navigator.Viewport.WorldToScreen(targetPoint);

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

        public void AddArea(List<MapPoint> areaPoints, Models.MapArea mapArea)
        {
            if (areaPoints.Count < 3) return;

            var coordinates = areaPoints.Select(p =>
                SphericalMercator.FromLonLat(p.Longitude, p.Latitude).ToMPoint()
            ).ToList();

            coordinates.Add(coordinates[0]);

            var polygon = new Polygon(new LinearRing(coordinates.Select(p => new Coordinate(p.X, p.Y)).ToArray()));
            var feature = new GeometryFeature(polygon);

            mapArea.Feature = feature;

            _areasLayer.Add(feature);
            _areasLayer.DataHasChanged();
            _mapControl.Refresh();
        }

        public void RemoveArea(Models.MapArea area)
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
    }
}