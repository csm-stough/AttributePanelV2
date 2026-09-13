using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using System;

namespace AttributePanelV2.ViewModels
{
    // Draws a single "this is the feature currently selected in the attribute pane" highlight
    // on the map -- deliberately a separate graphic (MapView.AddOverlay) rather than a second
    // selection color, since Pro's real selection is one color per map/layer, not per feature:
    // there's no Select()/Unselect() call that can make one feature within a selection a
    // different color from the rest. This overlay never touches the real selection at all, so
    // it can't interfere with it or with any of the tree's right-click actions.
    // Dockpane1ViewModel owns the single IDisposable handle this hands back and disposes it
    // whenever its own SelectedItem moves on or clears -- see Dockpane1ViewModel.UpdateHighlight.
    internal static class FeatureHighlighter
    {
        // Vivid orange -- clearly distinct from Pro's default selection cyan/blue.
        // NOTE: ColorFactory alpha is 0-100 (a percentage), NOT 0-255 like WPF's Color.FromArgb.
        private static readonly CIMColor OutlineColor = ColorFactory.Instance.CreateRGBColor(255, 85, 0);
        private static readonly CIMColor FillColor = ColorFactory.Instance.CreateRGBColor(255, 85, 0, 35);
        private static readonly CIMColor MarkerFillColor = ColorFactory.Instance.CreateRGBColor(255, 85, 0, 60);

        public static IDisposable Highlight(MapView mapView, Geometry geometry)
        {
            if (mapView == null || geometry == null || geometry.IsEmpty)
            {
                return null;
            }

            CIMSymbolReference symbolRef = geometry switch
            {
                Polygon => SymbolFactory.Instance.ConstructPolygonSymbol(
                    FillColor,
                    SimpleFillStyle.Solid,
                    SymbolFactory.Instance.ConstructStroke(OutlineColor, 2.5, SimpleLineStyle.Solid))
                    .MakeSymbolReference(),

                Polyline => SymbolFactory.Instance.ConstructLineSymbol(
                    OutlineColor, 3.0, SimpleLineStyle.Solid)
                    .MakeSymbolReference(),

                // Points/multipoints get a small dot instead of tracing the geometry. The
                // marker size below is in points (a screen unit), not map units -- unlike the
                // polygon/line symbols above, which are drawn in real map coordinates and so
                // naturally scale with the map, this dot stays a constant, readable size on
                // screen at any zoom level with no manual scale math needed.
                MapPoint => SymbolFactory.Instance.ConstructPointSymbol(
                    MarkerFillColor, 10.0, SimpleMarkerStyle.Circle)
                    .MakeSymbolReference(),

                Multipoint => SymbolFactory.Instance.ConstructPointSymbol(
                    MarkerFillColor, 10.0, SimpleMarkerStyle.Circle)
                    .MakeSymbolReference(),

                _ => null
            };

            if (symbolRef == null)
            {
                return null;
            }

            // NOTE: unverified against a compiler/runtime here (no local ArcGIS toolchain).
            // Most AddOverlay samples call this straight from the UI thread since it's a
            // display-only operation, not a geodatabase/CIM edit -- if this throws a "must run
            // on MCT" exception on rebuild, wrap the call in QueuedTask.Run instead.
            return mapView.AddOverlay(geometry, symbolRef);
        }
    }
}
