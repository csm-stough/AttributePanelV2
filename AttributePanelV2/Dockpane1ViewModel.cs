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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ArcGIS.Desktop.Internal.Mapping.Controls.FloorFilter.FloorFilterListControlVM;

namespace AttributePanelV2
{
    internal class Dockpane1ViewModel : DockPane
    {
        private readonly Inspector _inspector = new Inspector();
        private const string _dockPaneID = "AttributePanelV2_Dockpane1";
        private string _attributeText = "No selection";

        public string AttributeText
        {
            get => _attributeText;
            set => SetProperty(ref _attributeText, value);
        }

        protected Dockpane1ViewModel() 
        {
            MapSelectionChangedEvent.Subscribe(OnSelectionChanged);
        }

        private void OnSelectionChanged(MapSelectionChangedEventArgs args)
        {
            QueuedTask.Run(async () =>
            {
                if (args.Selection.Count == 0)
                {
                    AttributeText = "No selection";
                    return;
                }

                AttributeText = "";

                var selection = args.Selection.ToDictionary();

                var firstSelection = selection.First();

                MapMember mapMember = firstSelection.Key;
                List<long> objectIds = firstSelection.Value;

                if (mapMember is not FeatureLayer featureLayer)
                    return;

                long oid = objectIds.First();

                await _inspector.LoadAsync(featureLayer, oid);

                foreach(var attribute in _inspector)
                {
                    AttributeText += $"{attribute.FieldName} : {attribute.CurrentValue}\n";
                }

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
}
