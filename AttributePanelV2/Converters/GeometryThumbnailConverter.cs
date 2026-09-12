using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

// Both ArcGIS.Core.Geometry and System.Windows.Media define types named "Geometry" and
// "LineSegment" -- aliasing here instead of fully-qualifying every single usage below.
using ArcGeometry = ArcGIS.Core.Geometry.Geometry;
using WpfGeometry = System.Windows.Media.Geometry;
using WpfLineSegment = System.Windows.Media.LineSegment;
using ArcGIS.Core.Geometry;
using System.Windows.Media;

namespace AttributePanelV2.Converters
{
    // Turns a row's actual geometry (CurrentValue on a Geometry-type field is the real
    // ArcGIS.Core.Geometry.Geometry object, not a string) into a small WPF vector drawing for
    // the inline/hover thumbnail in GeometryAttributeTemplate.
    //
    // This is a schematic sketch, not a true-to-scale map render: it draws the shape's real
    // vertices in its own local (map) coordinate space and leaves all scaling to the bound
    // Path's Stretch="Uniform", which fits whatever bounding box comes out of this converter
    // into the Path's actual on-screen size -- so the same geometry can back both the small
    // inline box and the larger hover popup with no extra math here. The Path itself also
    // carries a ScaleTransform(ScaleY=-1) (not this converter's concern) to flip it right-side
    // up, since map Y increases upward while WPF Y increases downward.
    public class GeometryThumbnailConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ArcGeometry geometry || geometry.IsEmpty)
            {
                return null;
            }

            return geometry switch
            {
                Polygon polygon => BuildOutline(polygon.Parts, closed: true),
                Polyline polyline => BuildOutline(polyline.Parts, closed: false),
                Multipoint multipoint => BuildPointMarkers(multipoint.Points.ToList()),
                MapPoint point => BuildPointMarkers(new List<MapPoint> { point }),
                // Envelopes and other geometry types aren't expected to show up as a row's shape
                // field -- no thumbnail for those.
                _ => null,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Display-only.
            throw new NotSupportedException();
        }

        private static WpfGeometry BuildOutline(ReadOnlyPartCollection parts, bool closed)
        {
            var pathGeometry = new PathGeometry();

            // Each "part" here is a ReadOnlySegmentCollection, not a flat list of points -- a
            // ring/path is actually made of Segments (line, bezier, or arc), each with its own
            // StartPoint/EndPoint. For this schematic sketch, a curved segment's EndPoint is
            // treated as the next straight-line vertex (chording the curve) rather than
            // rendering its true curvature -- plenty for a tiny thumbnail, and avoids needing
            // ArcGIS's bezier/arc math just to draw a rough outline.
            foreach (ReadOnlySegmentCollection part in parts)
            {
                if (part.Count == 0)
                {
                    continue;
                }

                var figure = new PathFigure
                {
                    StartPoint = new Point(part[0].StartPoint.X, part[0].StartPoint.Y),
                    IsClosed = closed,
                    IsFilled = closed
                };

                foreach (var segment in part)
                {
                    figure.Segments.Add(new WpfLineSegment(new Point(segment.EndPoint.X, segment.EndPoint.Y), true));
                }

                pathGeometry.Figures.Add(figure);
            }

            return pathGeometry.Figures.Count > 0 ? pathGeometry : null;
        }

        private static WpfGeometry BuildPointMarkers(IReadOnlyList<MapPoint> points)
        {
            if (points == null || points.Count == 0)
            {
                return null;
            }

            // A lone point has no spatial extent of its own to size a marker against --
            // Stretch="Uniform" normalizes whatever we draw to fill the box regardless, so any
            // fixed radius works identically there. For multiple points, size markers relative
            // to how far apart the points actually are, so the dots stay visible as distinct
            // dots instead of one being swamped by the overall spread.
            double radius = 1.0;

            if (points.Count > 1)
            {
                double minX = points.Min(p => p.X);
                double maxX = points.Max(p => p.X);
                double minY = points.Min(p => p.Y);
                double maxY = points.Max(p => p.Y);
                double spread = Math.Max(maxX - minX, maxY - minY);
                radius = spread > 0 ? spread * 0.04 : 1.0;
            }

            var group = new GeometryGroup();
            foreach (var point in points)
            {
                group.Children.Add(new EllipseGeometry(new Point(point.X, point.Y), radius, radius));
            }

            return group;
        }
    }
}
