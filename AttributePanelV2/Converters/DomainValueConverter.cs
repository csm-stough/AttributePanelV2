using AttributePanelV2.ViewModels;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System;

namespace AttributePanelV2.Converters
{
    // NOTE: not currently referenced by any DataTemplate in AttributeTemplates.xaml --
    // CodedValueAttributeTemplate binds its ComboBox straight to DomainValues/CurrentValue
    // instead of going through a converter. This class previously cast `value` to
    // ArcGIS.Desktop.Editing.Attributes.Attribute, which nothing in this app ever produces
    // (the actual bound type is AttributeFieldViewModel) -- fixed here so it's at least
    // correct if you wire it into a read-only display (e.g. a summary/tooltip) later.
    public class DomainValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not AttributeFieldViewModel attribute)
            {
                return value;
            }

            if (attribute.CurrentValue == null)
            {
                return "Null";
            }

            if (attribute.IsCodedValue && attribute.DomainValues.TryGetValue(attribute.CurrentValue, out var displayValue))
            {
                return displayValue;
            }

            return attribute.CurrentValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
