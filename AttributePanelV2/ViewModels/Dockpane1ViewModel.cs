
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Editing.Controls;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Mapping.Locate;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AttributePanelV2.ViewModels
{
    internal class Dockpane1ViewModel : DockPane
    {
        private readonly Inspector _inspector = new Inspector();
        private const string _dockPaneID = "AttributePanelV2_Dockpane1";
        public ObservableCollection<FeatureLayerViewModel> FeatureLayers { get; }
        public ICommand ApplyCommand { get; }
        public ICommand DiscardCommand { get; }
        public ICommand GoToReferenceCommand { get; }
        public object _selectedItem;

        public object SelectedItem
        {
            get => _selectedItem;
            set
            {
                // This setter is only ever driven by clicking a layer node now -- a click on an
                // individual feature row (plain, Ctrl, or Shift) is fully intercepted in
                // Dockpane1View's code-behind and goes straight through CheckedFeatures /
                // SelectOnlyFeature below instead, so that a single clicked feature renders with
                // exactly the same highlighted-background visual as a Ctrl/Shift multi-selected
                // one (rather than the TreeView's own native selection color, which was the
                // inconsistency Cody flagged). A plain layer click still always means "start
                // fresh" -- clear any active Ctrl/Shift multi-select first, even if the
                // newly-clicked item happens to already equal _selectedItem (e.g. after a run of
                // Ctrl-clicks, which never touch SelectedItem at all, so it can be stale by the
                // time a layer click comes in).
                bool hadCheckedFeatures = CheckedFeatures.Count > 0;
                if (hadCheckedFeatures)
                {
                    ClearCheckedFeatures();
                    _rangeAnchor = null;
                }

                if (Equals(_selectedItem, value) && !hadCheckedFeatures)
                {
                    return;
                }

                _selectedItem = value;
                NotifyPropertyChanged();
                RefreshActiveAttributes();
                UpdateHighlight();
            }
        }

        // The pane's own Ctrl/Shift-click multi-select of individual feature rows under one
        // layer -- a tree concept independent of the real map selection, added because the
        // default ArcGIS attribute pane supports it and the plain WPF TreeView underneath this
        // one doesn't have any multi-select of its own to build on. Always scoped to a single
        // layer: starting to check a feature under a different layer than whatever's already
        // checked clears the old set first (same as clicking a different layer/feature node
        // outright does via SelectedItem above), matching how batch editing is already scoped
        // to one layer at a time.
        public ObservableCollection<SelectedFeatureViewModel> CheckedFeatures { get; } = new ObservableCollection<SelectedFeatureViewModel>();

        // Anchor for Shift-click range selection -- the last feature a Ctrl or Shift click
        // touched. Deliberately not moved by ExtendCheckedRange itself, so repeated Shift-clicks
        // keep extending from the same starting point, same as Explorer/ListBox range-select.
        private SelectedFeatureViewModel _rangeAnchor;

        // Plain click on a feature row: same "start fresh" semantics as clicking a layer node
        // (see SelectedItem above), just scoped to one feature instead of the whole layer.
        // Routing plain feature clicks through here -- rather than through SelectedItem, which
        // a feature row's click handler no longer ever touches -- is what makes a single
        // clicked feature visually identical to a Ctrl/Shift-picked one: both are just
        // CheckedFeatures with one or more entries, rendered by the same IsChecked-triggered
        // background in Dockpane1.xaml.
        public void SelectOnlyFeature(SelectedFeatureViewModel feature)
        {
            ClearCheckedFeatures();
            feature.IsChecked = true;
            CheckedFeatures.Add(feature);
            _rangeAnchor = feature;
            OnCheckedFeaturesChanged();
        }

        // Ctrl-click: toggles just this one feature's checked state without touching any of the
        // others.
        public void ToggleChecked(SelectedFeatureViewModel feature)
        {
            if (CheckedFeatures.Count > 0 && CheckedFeatures[0].ParentLayer != feature.ParentLayer)
            {
                ClearCheckedFeatures();
            }

            if (feature.IsChecked)
            {
                feature.IsChecked = false;
                CheckedFeatures.Remove(feature);
            }
            else
            {
                feature.IsChecked = true;
                CheckedFeatures.Add(feature);
            }

            _rangeAnchor = feature;
            OnCheckedFeaturesChanged();
        }

        // Shift-click: replaces the checked set with the contiguous range between the last
        // anchor and this feature (inclusive), in the order Features already lists them.
        public void ExtendCheckedRange(SelectedFeatureViewModel feature)
        {
            var anchor = _rangeAnchor;

            if (anchor == null || anchor.ParentLayer != feature.ParentLayer)
            {
                // No usable anchor (nothing checked yet, or the last anchor was under a
                // different layer) -- nothing sensible to range between, so just check this one
                // feature and start fresh from here.
                SelectOnlyFeature(feature);
                return;
            }

            var siblings = feature.ParentLayer.Features;
            int anchorIndex = siblings.IndexOf(anchor);
            int targetIndex = siblings.IndexOf(feature);

            if (anchorIndex < 0 || targetIndex < 0)
            {
                return;
            }

            int start = System.Math.Min(anchorIndex, targetIndex);
            int end = System.Math.Max(anchorIndex, targetIndex);

            ClearCheckedFeatures();
            for (int i = start; i <= end; i++)
            {
                siblings[i].IsChecked = true;
                CheckedFeatures.Add(siblings[i]);
            }

            OnCheckedFeaturesChanged();
        }

        private void ClearCheckedFeatures()
        {
            foreach (var feature in CheckedFeatures)
            {
                feature.IsChecked = false;
            }
            CheckedFeatures.Clear();
        }

        private void OnCheckedFeaturesChanged()
        {
            RefreshActiveAttributes();
            UpdateHighlight();
        }

        // What the attribute editor (Dockpane1.xaml's ItemsControl) actually shows. A checked
        // multi-select always wins when one is active, batching just those features exactly
        // like a whole-layer selection already batches every feature loaded under it; otherwise
        // this falls back to today's SelectedItem-based single-feature/whole-layer behavior.
        public ObservableCollection<IAttributeFieldViewModel> ActiveAttributes { get; } = new ObservableCollection<IAttributeFieldViewModel>();

        private void RefreshActiveAttributes()
        {
            ActiveAttributes.Clear();

            IEnumerable<IAttributeFieldViewModel> source = CheckedFeatures.Count > 0
                ? BatchAttributeFieldViewModel.BuildRows(CheckedFeatures)
                : SelectedItem switch
                {
                    SelectedFeatureViewModel feature => feature.Attributes,
                    FeatureLayerViewModel layer => layer.Attributes,
                    _ => Enumerable.Empty<IAttributeFieldViewModel>()
                };

            foreach (var attribute in source)
            {
                ActiveAttributes.Add(attribute);
            }
        }

        // Every map-overlay highlight currently on screen for whatever's currently active in
        // this pane -- see FeatureHighlighter for why this is a separate graphic instead of a
        // second selection color. A checked multi-select highlights exactly that set; otherwise
        // a single feature node highlights just itself and a layer/batch node highlights every
        // feature currently loaded under it (per Cody's ask that picking the layer node light
        // up the whole selection rather than nothing); nothing selected clears the list.
        private readonly List<System.IDisposable> _currentHighlights = new List<System.IDisposable>();

        private void UpdateHighlight()
        {
            IEnumerable<Geometry> geometries = CheckedFeatures.Count > 0
                ? CheckedFeatures.Select(GetGeometry)
                : SelectedItem switch
                {
                    SelectedFeatureViewModel feature => new[] { GetGeometry(feature) },
                    FeatureLayerViewModel layer => layer.Features.Select(GetGeometry),
                    _ => Enumerable.Empty<Geometry>()
                };

            // Reads each feature's geometry straight off its already-loaded Geometry-type
            // AttributeFieldViewModel -- no extra query needed.
            var geometryList = geometries.Where(geometry => geometry != null).ToList();
            var mapView = MapView.Active;

            // Disposing the old overlays and building/adding the new ones are all ArcGIS.Core
            // calls (CIM symbols, MapView.AddOverlay) -- they need to run on the MCT, same as
            // every other ArcGIS.Core call in this file. Every caller of UpdateHighlight fires
            // on the UI thread (a plain WPF binding, or the tree's click handlers), so doing
            // this inline there throws ArcGIS.Core.CalledOnWrongThreadException. Not awaited:
            // QueuedTask.Run calls queue up in order on the MCT, so a quick run of clicks still
            // resolves in the order the user made them.
            QueuedTask.Run(() =>
            {
                foreach (var highlight in _currentHighlights)
                {
                    highlight?.Dispose();
                }
                _currentHighlights.Clear();

                if (mapView == null)
                {
                    return;
                }

                foreach (var geometry in geometryList)
                {
                    var highlight = FeatureHighlighter.Highlight(mapView, geometry);
                    if (highlight != null)
                    {
                        _currentHighlights.Add(highlight);
                    }
                }
            });
        }

        private static Geometry GetGeometry(SelectedFeatureViewModel feature)
        {
            return feature.Attributes
                .FirstOrDefault(attribute => attribute.FieldType == FieldType.Geometry)?.CurrentValue as Geometry;
        }

        // A checked multi-select is always scoped to one layer (enforced by ToggleChecked /
        // ExtendCheckedRange above), so its first entry's ParentLayer always resolves the whole
        // set correctly -- this is what ApplyChanges/DiscardChanges walk DirtyFeatures from, so
        // it has to keep working even when SelectedItem itself is stale (Ctrl/Shift clicks never
        // touch SelectedItem).
        public FeatureLayerViewModel CurrentFeatureLayer =>
            CheckedFeatures.Count > 0
                ? CheckedFeatures[0].ParentLayer
                : SelectedItem switch
                {
                    FeatureLayerViewModel layer => layer,
                    SelectedFeatureViewModel feature => feature.ParentLayer,
                    _ => null
                };

        // Toggle for the "Auto Apply" checkbox: when on, every attribute edit applies itself
        // immediately (via OnAttributeValueChanged below) instead of waiting for the Apply
        // button. No debounce/timer -- each committed field edit (TextBox default
        // UpdateSourceTrigger is LostFocus, so this fires once per field, not per keystroke)
        // triggers one ApplyChanges call, same as clicking Apply would.
        private bool _isAutoApplyEnabled;
        public bool IsAutoApplyEnabled
        {
            get => _isAutoApplyEnabled;
            set => SetProperty(ref _isAutoApplyEnabled, value);
        }

        protected Dockpane1ViewModel()
        {
            MapSelectionChangedEvent.Subscribe(OnSelectionChanged);
            FeatureLayers = new ObservableCollection<FeatureLayerViewModel>();
            ApplyCommand = new RelayCommand(ApplyChanges);
            DiscardCommand = new RelayCommand(DiscardChanges);
            GoToReferenceCommand = new RelayCommand<object>(GoToReference);
        }

        private async void OnSelectionChanged(MapSelectionChangedEventArgs args)
        {
            FeatureLayers.Clear();

            // The real map selection just moved out from under whatever this pane had
            // singled out (if anything) -- clear SelectedItem so the highlight overlay clears
            // with it (via the setter above) rather than pointing at a now-stale feature.
            SelectedItem = null;

            if (args.Selection.Count == 0)
            {
                UpdateSelectionSummary();
                return;
            }

            await LoadSelection(args);
            UpdateSelectionSummary();
        }

        // Short, human-readable description of what's currently loaded, shown in the header
        // above the tree view. Recomputed after every selection change (both the "nothing
        // selected" early-out above and once LoadSelection has finished populating
        // FeatureLayers) rather than kept incrementally in sync with it.
        private string _selectionSummary = "No selection";
        public string SelectionSummary
        {
            get => _selectionSummary;
            set => SetProperty(ref _selectionSummary, value);
        }

        private void UpdateSelectionSummary()
        {
            if (FeatureLayers.Count == 0)
            {
                SelectionSummary = "No selection";
                return;
            }

            int totalFeatures = FeatureLayers.Sum(layer => layer.Features.Count);

            if (FeatureLayers.Count == 1)
            {
                var layer = FeatureLayers[0];
                SelectionSummary = totalFeatures == 1
                    ? $"{layer.Layer.Name}: 1 feature selected"
                    : $"{layer.Layer.Name}: {totalFeatures} features selected";
            }
            else
            {
                SelectionSummary = $"{totalFeatures} features selected across {FeatureLayers.Count} layers";
            }
        }

        private async Task LoadSelection(MapSelectionChangedEventArgs args)
        {
            var selection = args.Selection.ToDictionary();

            var featureClasses = await QueuedTask.Run(async () =>
            {
                var result = new List<FeatureLayerViewModel>();

                foreach (var selectedLayer in selection)
                {
                    if (selectedLayer.Key is not FeatureLayer featureLayer)
                    {
                        continue;
                    }

                    var featureLayerViewModel = new FeatureLayerViewModel(featureLayer);

                    var queryFilter = new QueryFilter
                    {
                        ObjectIDs = selectedLayer.Value
                    };

                    using var cursor = featureLayer.Search(queryFilter);

                    while (cursor.MoveNext())
                    {
                        using var row = cursor.Current;

                        var feature = LoadFeature(featureLayerViewModel, row);

                        featureLayerViewModel.Features.Add(feature);
                    }

                    // Needs every feature loaded above first -- builds the batch-edit rows
                    // shown when the layer node itself (rather than one feature under it) is
                    // selected in the tree.
                    featureLayerViewModel.BuildBatchAttributes();

                    result.Add(featureLayerViewModel);
                }

                return result;
            });

            foreach(var featureClass in featureClasses)
            {
                FeatureLayers.Add(featureClass);
            }
        }

        private SelectedFeatureViewModel LoadFeature(FeatureLayerViewModel featureLayerViewModel, Row row)
        {
            var selectedFeature = new SelectedFeatureViewModel(featureLayerViewModel, row.GetObjectID());

            foreach (var field in row.GetFields())
            {
                var attribute = new AttributeFieldViewModel(field, row[field.Name]);
                attribute.PropertyChanged += OnAttributeValueChanged;

                selectedFeature.Attributes.Add(attribute);
            }

            // Prefer the layer's configured display field for the TreeView label; SelectedFeatureViewModel
            // already defaults to "OID <n>" in its constructor, so leave that alone if there's no match.
            var displayAttribute = selectedFeature.Attributes.FirstOrDefault(attribute =>
                string.Equals(attribute.FieldName, featureLayerViewModel.DisplayFieldName, System.StringComparison.OrdinalIgnoreCase));

            if (displayAttribute?.CurrentValue != null)
            {
                selectedFeature.DisplayName = displayAttribute.CurrentValue.ToString();
            }

            return selectedFeature;
        }

        // Fires for every AttributeFieldViewModel this dockpane creates (subscribed in
        // LoadFeature), whether the edit came from single-feature editing or from a batch row
        // (BatchAttributeFieldViewModel.CurrentValue's setter writes through to each
        // underlying AttributeFieldViewModel in a synchronous loop, which raises this same
        // event once per feature). The _isApplying guard below matters here: without it, a
        // batch edit touching 50 features would fire 50 overlapping ApplyChanges calls.
        private void OnAttributeValueChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(AttributeFieldViewModel.CurrentValue))
            {
                return;
            }

            if (!IsAutoApplyEnabled || _isApplying)
            {
                return;
            }

            ApplyChanges();
        }

        // Guards against overlapping ApplyChanges calls -- both re-entrant auto-apply calls
        // during a batch edit (see OnAttributeValueChanged) and a stray double-click on the
        // Apply button while one is already in flight. Set synchronously before the first
        // `await` below, so every call that arrives while one is running is skipped rather
        // than starting a second, overlapping EditOperation; anything that comes in dirty
        // while skipped isn't lost, just picked up by the next call that actually proceeds.
        private bool _isApplying;

        public async void ApplyChanges()
        {
            if (_isApplying)
            {
                return;
            }

            // Capture the layer once: SelectedItem (and so CurrentFeatureLayer) could change while
            // we're awaiting QueuedTask.Run, and we want the commit-back below to match whatever we
            // actually sent to the edit operation, not whatever happens to be selected afterward.
            var currentLayer = CurrentFeatureLayer;

            if (currentLayer == null || !currentLayer.DirtyFeatures.Any())
            {
                return;
            }

            _isApplying = true;
            try
            {
                string errorMessage = null;

                bool success = await QueuedTask.Run(() =>
                {
                    EditOperation edit = new EditOperation
                    {
                        Name = "Update Attributes"
                    };

                    foreach (var feature in currentLayer.DirtyFeatures)
                    {
                        var updates = feature.DirtyAttributes
                            .Where(attribute => attribute.IsEditable)
                            .ToDictionary(
                                attribute => attribute.FieldName,
                                attribute => attribute.CurrentValue);

                        edit.Modify(feature.ParentLayer.Layer, feature.ObjectID, updates);
                    }

                    bool result = edit.Execute();
                    if (!result)
                    {
                        errorMessage = edit.ErrorMessage;
                    }
                    return result;
                });

                if (success)
                {
                    foreach (var feature in currentLayer.DirtyFeatures)
                    {
                        feature.CommitChanges();
                    }
                }
                else
                {
                    // Previously: silent no-op on failure (e.g. a required field left null, a domain
                    // violation) -- at least surface *something* now. Fully-qualified to avoid clashing
                    // with the System.Windows.MessageBox this file already brings in via `using System.Windows;`.
                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                        string.IsNullOrEmpty(errorMessage) ? "The edit could not be applied." : errorMessage,
                        "Apply Attributes");
                }
            }
            finally
            {
                _isApplying = false;
            }

            // Auto-apply catch-up: an edit that arrived while the call above was in flight
            // got skipped by the _isApplying guard (OnAttributeValueChanged just returns
            // without queuing anything for later), and nothing else would ever re-trigger it
            // -- it would just sit there marked dirty until the user happened to edit another
            // field. Recheck the same layer now that we're clear and, if auto-apply is still
            // on and something's still dirty, run it again. Each pass calls CommitChanges() on
            // everything it just applied, so this converges in one or two extra rounds rather
            // than looping indefinitely.
            if (IsAutoApplyEnabled && currentLayer.DirtyFeatures.Any())
            {
                ApplyChanges();
            }
        }

        // Reverts every dirty attribute in the current selection back to its last-committed
        // value, without touching the database. Reuses the _isApplying guard so that, with
        // Auto Apply on, resetting CurrentValue back to OriginalValue doesn't itself look like
        // an edit and immediately re-trigger ApplyChanges via OnAttributeValueChanged.
        public void DiscardChanges()
        {
            var currentLayer = CurrentFeatureLayer;

            if (currentLayer == null || !currentLayer.DirtyFeatures.Any())
            {
                return;
            }

            _isApplying = true;
            try
            {
                // Snapshot both levels before discarding: discarding a feature's attributes
                // changes its IsDirty as we go, which would otherwise mutate the very
                // DirtyFeatures/DirtyAttributes queries we're enumerating.
                foreach (var feature in currentLayer.DirtyFeatures.ToList())
                {
                    foreach (var attribute in feature.DirtyAttributes.ToList())
                    {
                        attribute.Discard();
                    }
                }
            }
            finally
            {
                _isApplying = false;
            }
        }

        // Bound to the "→" button next to a GUID attribute's value (see GUIDAttributeTemplate
        // in AttributeTemplates.xaml). Treats the GUID as if it were another feature's
        // GlobalID and scans every feature layer in the active map for a match -- there's no
        // formal relationship class involved, just a plain "does any row's GlobalID equal
        // this value" search, per Cody's call on how this should resolve. If found, clears the
        // map selection down to just that feature and zooms to it, reusing the same
        // select/zoom logic as the tree's "Only Select This" + "Zoom To" actions.
        public async void GoToReference(object rawValue)
        {
            if (rawValue == null || rawValue is not string guidText || string.IsNullOrWhiteSpace(guidText) ||
                guidText == BatchAttributeFieldViewModel.VariesPlaceholder)
            {
                // Nothing to resolve: no value, or a batch row where the selected features
                // don't even agree on this GUID.
                return;
            }

            // ArcGIS's SQL layer expects GUID/GlobalID literals wrapped in curly braces.
            if (!guidText.StartsWith("{"))
            {
                guidText = "{" + guidText.Trim('{', '}') + "}";
            }

            var map = MapView.Active?.Map;
            if (map == null)
            {
                return;
            }

            (FeatureLayer layer, long objectId) match = await QueuedTask.Run(() =>
            {
                foreach (var candidateLayer in map.GetLayersAsFlattenedList().OfType<FeatureLayer>())
                {
                    var globalIdField = candidateLayer.GetTable()?.GetDefinition()?.GetFields()
                        .FirstOrDefault(field => field.FieldType == FieldType.GlobalID);

                    if (globalIdField == null)
                    {
                        continue;
                    }

                    var queryFilter = new QueryFilter { WhereClause = $"{globalIdField.Name} = '{guidText}'" };

                    using var cursor = candidateLayer.Search(queryFilter);
                    if (cursor.MoveNext())
                    {
                        using var row = cursor.Current;
                        return (candidateLayer, row.GetObjectID());
                    }
                }

                return (null, -1L);
            });

            if (match.layer == null)
            {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(
                    "No feature with that GlobalID was found in the current map.",
                    "Go To Reference");
                return;
            }

            var objectIds = new List<long> { match.objectId };
            await TreeContextActions.SelectOnlyThisAsync(match.layer, objectIds);
            await TreeContextActions.ZoomToAsync(match.layer, objectIds);
        }

        /// <summary>
        /// Show the DockPane.
        /// </summary>
        internal static void Show()
        {
            DockPane pane = FrameworkApplication.DockPaneManager.Find(_dockPaneID);
            if (pane == null)
                return;

            pane.Activate();
        }

        /// <summary>
        /// Text shown near the top of the DockPane.
        /// </summary>
        private string _heading = "Attributes Extended";
        public string Heading
        {
            get => _heading;
            set => SetProperty(ref _heading, value);
        }
    }

    /// <summary>
    /// Button implementation to show the DockPane.
    /// </summary>
    internal class Dockpane1_ShowButton : Button
    {
        protected override void OnClick()
        {
            Dockpane1ViewModel.Show();
        }
    }

    // RelayCommand / RelayCommand<T> now live in RelayCommand.cs -- they're shared by
    // FeatureLayerViewModel and SelectedFeatureViewModel too, not just this class.
}
