using AttributePanelV2.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;

namespace AttributePanelV2.Converters
{
    // Used on CodedValueAttributeTemplate's ComboBoxItem container style: disables the synthetic
    // "<Varies>" entry BatchAttributeFieldViewModel adds to DomainValues so it still displays as
    // the current selection when values disagree, but can't be re-picked from the dropdown.
    public class VariesKeyToIsEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !ReferenceEquals(value, BatchAttributeFieldViewModel.VariesKey);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
