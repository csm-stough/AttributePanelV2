using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AttributePanelV2.ViewModels
{
    public class FeatureLayerViewModel : PropertyChangedBase
    {

        public FeatureLayer Layer { get; }
        public ObservableCollection<SelectedFeatureViewModel> Features { get; }
        public IEnumerable<SelectedFeatureViewModel> DirtyFeatures => Features.Where(feature => feature.DirtyAttributes.Any());

        // Right-click tree actions -- same four command names as SelectedFeatureViewModel, but
        // acting on every feature currently loaded under this layer node (read live off
        // Features when the command runs, not captured at construction time). Delete goes
        // through TreeContextActions' multi-feature confirmation whenever more than one
        // feature is loaded here.
        public ICommand UnselectCommand { get; }
        public ICommand SelectOnlyThisCommand { get; }
        public ICommand ZoomToCommand { get; }
        public ICommand DeleteCommand { get; }

        // Batch-edit rows: one per field, spanning every feature currently loaded for this
        // layer. Named "Attributes" (not "BatchAttributes") on purpose -- Dockpane1.xaml binds
        // the attribute editor to "SelectedItem.Attributes" with no further change needed
        // whether SelectedItem is this layer node (batch) or a single SelectedFeatureViewModel
        // under it (per-feature), since both expose an "Attributes" collection.
        // Built by BuildBatchAttributes() once Features is fully populated -- empty until then.
        public ObservableCollection<BatchAttributeFieldViewModel> Attributes { get; }

        // The layer's configured display-field name (Layer Properties > Display > Display Field),
        // used to give each feature in the TreeView a meaningful label instead of just its OID.
        // Read off the layer's CIM definition rather than a (nonexistent) GetDisplayField()
        // convenience method -- this needs to run on the MCT, so only ever construct a
        // FeatureLayerViewModel from inside QueuedTask.Run, same as
        // Dockpane1ViewModel.LoadSelection already does.
        public string DisplayFieldName { get; }

        public FeatureLayerViewModel(FeatureLayer featureLayer)
        {
            Layer = featureLayer;
            Features = new ObservableCollection<SelectedFeatureViewModel>();
            Attributes = new ObservableCollection<BatchAttributeFieldViewModel>();
            DisplayFieldName = (featureLayer.GetDefinition() as CIMFeatureLayer)?.FeatureTable?.DisplayField;

            UnselectCommand = new RelayCommand(async () =>
                await TreeContextActions.UnselectAsync(Layer, Features.Select(f => f.ObjectID).ToList()));
            SelectOnlyThisCommand = new RelayCommand(async () =>
                await TreeContextActions.SelectOnlyThisAsync(Layer, Features.Select(f => f.ObjectID).ToList()));
            ZoomToCommand = new RelayCommand(async () =>
                await TreeContextActions.ZoomToAsync(Layer, Features.Select(f => f.ObjectID).ToList()));
            DeleteCommand = new RelayCommand(async () =>
                await TreeContextActions.DeleteAsync(Layer, Features.Select(f => f.ObjectID).ToList()));
        }

        // Call once Features has been fully populated for this selection (see
        // Dockpane1ViewModel.LoadSelection) -- groups every feature's AttributeFieldViewModels
        // by field name into one BatchAttributeFieldViewModel per field.
        public void BuildBatchAttributes()
        {
            Attributes.Clear();

            foreach (var row in BatchAttributeFieldViewModel.BuildRows(Features))
            {
                Attributes.Add(row);
            }
        }
    }
}
