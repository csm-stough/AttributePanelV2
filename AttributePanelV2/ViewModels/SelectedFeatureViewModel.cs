using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace AttributePanelV2.ViewModels
{
    public class SelectedFeatureViewModel : PropertyChangedBase
    {

        public FeatureLayerViewModel ParentLayer { get; }
        public long ObjectID { get; }
        public ObservableCollection<AttributeFieldViewModel> Attributes { get; }
        public IEnumerable<AttributeFieldViewModel> DirtyAttributes => Attributes.Where(attr => attr.IsDirty);
        public bool IsDirty => DirtyAttributes.Any();

        // Right-click tree actions (Dockpane1.xaml's context menus bind to these same four
        // command names on both this class and FeatureLayerViewModel). Every one of these acts
        // on just this single feature, so Delete never hits TreeContextActions' multi-feature
        // confirmation prompt from here.
        public ICommand UnselectCommand { get; }
        public ICommand SelectOnlyThisCommand { get; }
        public ICommand ZoomToCommand { get; }
        public ICommand DeleteCommand { get; }

        // What the TreeView shows for this feature. Defaults to the OID so every row is at
        // least distinguishable; Dockpane1ViewModel.LoadFeature overwrites this once the
        // attributes are loaded, using the layer's display field if one is configured.
        // TODO: this is a one-time snapshot -- it won't update live if the user edits the
        // display-field attribute itself before Applying.
        private string _displayName;
        public string DisplayName
        {
            get => _displayName;
            set
            {
                if (_displayName == value)
                {
                    return;
                }
                _displayName = value;
                NotifyPropertyChanged();
            }
        }

        public SelectedFeatureViewModel(FeatureLayerViewModel parentLayer, long objectID)
        {
            ParentLayer = parentLayer;
            ObjectID = objectID;
            Attributes = new ObservableCollection<AttributeFieldViewModel>();
            _displayName = $"OID {objectID}";

            UnselectCommand = new RelayCommand(async () =>
                await TreeContextActions.UnselectAsync(ParentLayer.Layer, new[] { ObjectID }));
            SelectOnlyThisCommand = new RelayCommand(async () =>
                await TreeContextActions.SelectOnlyThisAsync(ParentLayer.Layer, new[] { ObjectID }));
            ZoomToCommand = new RelayCommand(async () =>
                await TreeContextActions.ZoomToAsync(ParentLayer.Layer, new[] { ObjectID }));
            DeleteCommand = new RelayCommand(async () =>
                await TreeContextActions.DeleteAsync(ParentLayer.Layer, new[] { ObjectID }));
        }

        public override string ToString() => DisplayName;

        public void CommitChanges()
        {
            foreach(var attribute in DirtyAttributes)
            {
                attribute.Commit();
            }
        }
    }
}
