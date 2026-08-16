
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AttributePanelV2.ViewModels
{
    internal class Dockpane1ViewModel : DockPane
    {
        private readonly Inspector _inspector = new Inspector();
        private const string _dockPaneID = "AttributePanelV2_Dockpane1";
        public ObservableCollection<FeatureClassViewModel> FeatureClasses { get; }
        public ICommand ApplyCommand { get; }

        protected Dockpane1ViewModel() 
        {
            MapSelectionChangedEvent.Subscribe(OnSelectionChanged);
            FeatureClasses = new ObservableCollection<FeatureClassViewModel>();
            ApplyCommand = new RelayCommand(ApplyChanges);
        }

        private async void OnSelectionChanged(MapSelectionChangedEventArgs args)
        {
            FeatureClasses.Clear();

            if (args.Selection.Count == 0)
            {
                return;
            }

            await LoadSelection(args);
        }

        private async Task LoadSelection(MapSelectionChangedEventArgs args)
        {
            var selection = args.Selection.ToDictionary();

            var featureClasses = await QueuedTask.Run(async () =>
            {
                var result = new List<FeatureClassViewModel>();

                foreach (var selectedLayer in selection)
                {
                    if (selectedLayer.Key is not FeatureLayer featureLayer)
                    {
                        continue;
                    }

                    var featureClassViewModel = new FeatureClassViewModel(featureLayer);

                    foreach (var objectId in selectedLayer.Value)
                    {
                        var feature = await LoadFeature(featureLayer, objectId);

                        featureClassViewModel.Features.Add(feature);
                    }

                    result.Add(featureClassViewModel);
                }

                return result;
            });

            foreach(var featureClass in featureClasses)
            {
                FeatureClasses.Add(featureClass);
            }
        }

        private async Task<SelectedFeatureViewModel> LoadFeature(FeatureLayer featureLayer, long objectId)
        {
            await _inspector.LoadAsync(featureLayer, objectId);

            var feature = new SelectedFeatureViewModel(featureLayer, objectId);

            foreach(var attribute in _inspector)
            {
                feature.Attributes.Add(new AttributeFieldViewModel(attribute));
            }

            return feature;
        }

        public async void ApplyChanges()
        {
            await QueuedTask.Run(async () =>
            {
                return await _inspector.ApplyAsync();
            });
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

    internal class RelayCommand : ICommand
    {
        private readonly System.Action _execute;
        public event System.EventHandler CanExecuteChanged;

        public RelayCommand(System.Action execute)
        {
            this._execute = execute;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _execute.Invoke();
        }
    }
}
