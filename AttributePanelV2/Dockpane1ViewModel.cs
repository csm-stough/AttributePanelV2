using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.KnowledgeGraph;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using static ArcGIS.Desktop.Internal.Mapping.Controls.FloorFilter.FloorFilterListControlVM;

namespace AttributePanelV2
{
    internal class Dockpane1ViewModel : DockPane
    {
        private readonly Inspector _inspector = new Inspector();
        private const string _dockPaneID = "AttributePanelV2_Dockpane1";
        public ObservableCollection<AttributeFieldViewModel> attributes { get; }
        public ICommand ApplyCommand { get; }

        private static bool _isApplyingEdits = false;

        protected Dockpane1ViewModel() 
        {
            MapSelectionChangedEvent.Subscribe(OnSelectionChanged);
            attributes = new ObservableCollection<AttributeFieldViewModel>();
            ApplyCommand = new RelayCommand(ApplyChanges);
        }

        private async void OnSelectionChanged(MapSelectionChangedEventArgs args)
        {

            if (_isApplyingEdits) { return; }

            attributes.Clear();

            if (args.Selection.Count == 0)
            {
                return;
            }

            await QueuedTask.Run(async () =>
            {
                var selection = args.Selection.ToDictionary();

                var firstSelection = selection.First();

                MapMember mapMember = firstSelection.Key;
                List<long> objectIds = firstSelection.Value;

                if (mapMember is not FeatureLayer featureLayer)
                    return;

                long oid = objectIds.First();

                await _inspector.LoadAsync(featureLayer, oid);

                System.Console.Write("Done!");
            });

            foreach (var attribute in _inspector)
            {
                attributes.Add(new AttributeFieldViewModel(attribute));
            }

            System.Console.Write("Done!");
        }

        public async void ApplyChanges()
        {
            Dockpane1ViewModel._isApplyingEdits = true;

            bool result = await QueuedTask.Run(async () =>
            {
                return await _inspector.ApplyAsync();
            });

            Dockpane1ViewModel._isApplyingEdits = false;
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
