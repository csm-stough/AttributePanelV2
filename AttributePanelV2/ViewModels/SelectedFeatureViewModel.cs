using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AttributePanelV2.ViewModels
{
    public class SelectedFeatureViewModel : PropertyChangedBase
    {

        public FeatureLayer FeatureLayer { get; }
        public long ObjectID { get; }
        public ObservableCollection<AttributeFieldViewModel> Attributes { get; }

        public SelectedFeatureViewModel(FeatureLayer featureLayer, long objectID)
        {
            FeatureLayer = featureLayer;
            ObjectID = objectID;
            Attributes = new ObservableCollection<AttributeFieldViewModel>();
        }

    }
}
