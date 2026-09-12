using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace AttributePanelV2.Converters
{
    // FieldType.Single (a "Float" field in Pro's UI) maps to System.Single at the row level.
    public class SingleValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                // Let the binding's TargetNullValue ("<Null>" in AttributeTemplates.xaml) show
                // instead -- returning UnsetValue here (the fallback below) would leave the
                // TextBox showing whatever it last displayed instead of a null indicator.
                return null;
            }
            if (value is float val)
            {
                return val.ToString();
            }
            if (value is string str)
            {
                // Batch editing: see IntegerValueConverter's Convert for why.
                return str;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                if (float.TryParse(str, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, culture, out float result))
                {
                    return result;
                }
            }

            // Not a valid float (or empty) -- tell the binding to leave CurrentValue alone
            // rather than overwriting it with a throwaway value while the user is typing.
            return Binding.DoNothing;
        }
    }
}
