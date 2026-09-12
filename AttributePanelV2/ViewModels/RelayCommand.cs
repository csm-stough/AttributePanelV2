using System;
using System.Windows.Input;

namespace AttributePanelV2.ViewModels
{
    // Shared ICommand implementation used across the pane's view models: Dockpane1ViewModel
    // (ApplyCommand, DiscardCommand, GoToReferenceCommand), FeatureLayerViewModel and
    // SelectedFeatureViewModel (the tree's Unselect/SelectOnlyThis/ZoomTo/Delete commands).
    // Originally lived at the bottom of Dockpane1ViewModel.cs; moved here once it started
    // getting used by other view models too.
    internal class RelayCommand : ICommand
    {
        private readonly Action _execute;

        // Tied to WPF's CommandManager instead of a plain unused event -- this is what lets
        // bound Buttons/MenuItems re-evaluate CanExecute automatically on focus changes etc.
        // CanExecute always returns true here, so it's not load-bearing yet, but it's the
        // standard ICommand pattern.
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action execute)
        {
            _execute = execute;
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

    // Same as RelayCommand above, but for a bound Button/MenuItem that needs to pass its
    // CommandParameter through (e.g. GoToReferenceCommand receiving the GUID text being
    // clicked on).
    internal class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;

        // See RelayCommand's identical CanExecuteChanged above for why this isn't a plain event.
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<T> execute)
        {
            _execute = execute;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _execute.Invoke((T)parameter);
        }
    }
}
