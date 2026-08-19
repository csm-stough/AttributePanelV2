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
    public class FeatureLayerViewModel : PropertyChangedBase
    {

        public FeatureLayer Layer { get; }
        public ObservableCollection<SelectedFeatureViewModel> Features { get; }
        public IEnumerable<SelectedFeatureViewModel> DirtyFeatures => Features.Where(feature => feature.DirtyAttributes.Any());

        public FeatureLayerViewModel(FeatureLayer featureLayer)
        {
            Layer = featureLayer;
            Features = new ObservableCollection<SelectedFeatureViewModel>();
        }

    }
}
