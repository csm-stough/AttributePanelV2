
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Editing.Controls;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public ObservableCollection<FeatureLayerViewModel> FeatureClasses { get; }
        public ICommand ApplyCommand { get; }
        public object _selectedItem;

        public object SelectedItem
        {
            get => _selectedItem;
            set
            {
                if(Equals(_selectedItem, value)) { return; }
                _selectedItem = value;
                NotifyPropertyChanged();
            }
        }

        protected Dockpane1ViewModel() 
        {
            MapSelectionChangedEvent.Subscribe(OnSelectionChanged);
            FeatureClasses = new ObservableCollection<FeatureLayerViewModel>();
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

                        var feature = LoadFeature(featureLayer, row);

                        featureLayerViewModel.Features.Add(feature);
                    }

                    result.Add(featureLayerViewModel);
                }

                return result;
            });

            foreach(var featureClass in featureClasses)
            {
                FeatureClasses.Add(featureClass);
            }

            System.Console.Write("Done!");
        }

        private SelectedFeatureViewModel LoadFeature(FeatureLayer featureLayer, Row row)
        {
            var selectedFeature = new SelectedFeatureViewModel(featureLayer, row.GetObjectID());

            foreach (var field in row.GetFields())
            {
                var attribute = new AttributeFieldViewModel(field, row[field.Name]);

                selectedFeature.Attributes.Add(attribute);
            }

            return selectedFeature;
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
