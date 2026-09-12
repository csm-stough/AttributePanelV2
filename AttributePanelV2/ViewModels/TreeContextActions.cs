using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AttributePanelV2.ViewModels
{
    // Shared implementation for the tree view's right-click actions (Unselect / Only Select
    // This / Zoom To / Delete) and for Dockpane1ViewModel.GoToReference, which needs the same
    // "select and zoom to a set of object IDs on a layer" behavior once it resolves a GUID
    // reference to a target feature. Kept as one static helper instead of duplicating this
    // logic across FeatureLayerViewModel and SelectedFeatureViewModel (one feature vs. a whole
    // batch is just a different object ID list to the same methods).
    internal static class TreeContextActions
    {
        // Drops the given features from the map's current selection; every other layer's
        // (and this layer's other) selected features are left untouched.
        public static Task UnselectAsync(FeatureLayer layer, IReadOnlyList<long> objectIds)
        {
            if (layer == null || objectIds == null || objectIds.Count == 0)
            {
                return Task.CompletedTask;
            }

            return QueuedTask.Run(() =>
            {
                layer.Select(new QueryFilter { ObjectIDs = objectIds }, SelectionCombinationMethod.Subtract);
            });
        }

        // Clears the ENTIRE map selection (every layer, not just this one), then selects just
        // the given features on this layer -- "only" meaning nothing else stays selected.
        public static Task SelectOnlyThisAsync(FeatureLayer layer, IReadOnlyList<long> objectIds)
        {
            if (layer == null || objectIds == null || objectIds.Count == 0)
            {
                return Task.CompletedTask;
            }

            return QueuedTask.Run(() =>
            {
                layer.Map?.ClearSelection();
                layer.Select(new QueryFilter { ObjectIDs = objectIds }, SelectionCombinationMethod.New);
            });
        }

        // Zooms the active map view to the combined extent of the given features. Builds the
        // combined extent with plain Envelope.Union rather than a full geometric union of the
        // shapes themselves -- we only need a bounding box to zoom to, not the actual merged
        // geometry.
        public static async Task ZoomToAsync(FeatureLayer layer, IReadOnlyList<long> objectIds)
        {
            if (layer == null || objectIds == null || objectIds.Count == 0)
            {
                return;
            }

            Envelope extent = await QueuedTask.Run(() =>
            {
                Envelope combined = null;

                using var cursor = layer.Search(new QueryFilter { ObjectIDs = objectIds });
                while (cursor.MoveNext())
                {
                    using var row = cursor.Current;
                    var shape = (row as Feature)?.GetShape();

                    if (shape == null || shape.IsEmpty)
                    {
                        continue;
                    }

                    combined = combined == null ? shape.Extent : combined.Union(shape.Extent);
                }

                return combined;
            });

            if (extent != null && MapView.Active != null)
            {
                await MapView.Active.ZoomToAsync(extent);
            }
        }

        // Deletes the given features outright (no undo of our own -- Pro's own Undo stack is
        // what would bring them back). Per Cody: a single-feature delete runs immediately, a
        // multi-feature delete (e.g. from a layer/batch node) asks for confirmation first.
        public static async Task<bool> DeleteAsync(FeatureLayer layer, IReadOnlyList<long> objectIds)
        {
            if (layer == null || objectIds == null || objectIds.Count == 0)
            {
                return false;
            }

            if (objectIds.Count > 1)
            {
                // ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show reuses the standard WPF
                // MessageBoxButton/MessageBoxResult enums (it doesn't define its own) -- fully
                // qualified here since this file has no `using System.Windows;` to pull them in
                // unqualified.
                var answer = ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                    $"Delete {objectIds.Count} features from \"{layer.Name}\"? This cannot be undone from within this add-in.",
                    "Delete Features",
                    System.Windows.MessageBoxButton.YesNo);

                if (answer != System.Windows.MessageBoxResult.Yes)
                {
                    return false;
                }
            }

            string errorMessage = null;

            bool success = await QueuedTask.Run(() =>
            {
                var edit = new EditOperation { Name = "Delete Features" };
                edit.Delete(layer, objectIds);

                bool result = edit.Execute();
                if (!result)
                {
                    errorMessage = edit.ErrorMessage;
                }
                return result;
            });

            if (!success)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                    string.IsNullOrEmpty(errorMessage) ? "The delete could not be completed." : errorMessage,
                    "Delete Features");
            }

            return success;
        }
    }
}
