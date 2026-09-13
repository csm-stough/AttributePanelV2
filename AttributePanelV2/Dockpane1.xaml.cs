using AttributePanelV2.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace AttributePanelV2
{
    /// <summary>
    /// Interaction logic for Dockpane1View.xaml
    /// </summary>
    public partial class Dockpane1View : UserControl
    {
        public Dockpane1View()
        {
            InitializeComponent();
        }

        public void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var viewModel = DataContext as Dockpane1ViewModel;

            if (viewModel != null)
            {
                viewModel.SelectedItem = e.NewValue;
            }
        }

        // Every feature-row click -- plain, Ctrl, or Shift alike -- is handled here instead of
        // through the TreeView's own native single-selection, so a single clicked feature
        // renders with exactly the same highlighted-background visual as a Ctrl/Shift
        // multi-selected one. Previously a plain click fell through to the TreeView's native
        // selection (via TreeView_SelectedItemChanged above), which drew WPF's own blue/gray
        // selection box -- a different look from the custom IsChecked-triggered background used
        // for Ctrl/Shift picks, which is the inconsistency Cody flagged. Marking every click
        // Handled here (not just modified ones) stops a TreeViewItem from ever selecting itself
        // via the mouse, so that native visual never appears for a feature row at all; layer
        // nodes aren't touched by any of this and keep using the TreeView's native selection,
        // same as before.
        private void FeatureRow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element ||
                element.DataContext is not SelectedFeatureViewModel feature ||
                DataContext is not Dockpane1ViewModel viewModel)
            {
                return;
            }

            e.Handled = true;

            bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            bool shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

            if (shift)
            {
                viewModel.ExtendCheckedRange(feature);
            }
            else if (ctrl)
            {
                viewModel.ToggleChecked(feature);
            }
            else
            {
                viewModel.SelectOnlyFeature(feature);
            }
        }
    }
}
