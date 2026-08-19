using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AttributePanelV2.ViewModels
{
    public class SelectedFeatureViewModel : PropertyChangedBase
    {

        public FeatureLayerViewModel ParentLayer { get; }
        public long ObjectID { get; }
        public ObservableCollection<AttributeFieldViewModel> Attributes { get; }
        public IEnumerable<AttributeFieldViewModel> DirtyAttributes => Attributes.Where(attr => attr.IsDirty);
        public bool IsDirty => DirtyAttributes.Any();

        public SelectedFeatureViewModel(FeatureLayerViewModel parentLayer, long objectID)
        {
            ParentLayer = parentLayer;
            ObjectID = objectID;
            Attributes = new ObservableCollection<AttributeFieldViewModel>();
        }

        public void CommitChanges()
        {
            foreach(var attribute in DirtyAttributes)
            {
                attribute.Commit();
            }
        }
    }
}
