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
    public class DoubleValueConverter : IValueConverter
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
            if (value is double dbl)
            {
                return dbl.ToString();
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
                if(Double.TryParse(str, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, culture, out double result))
                {
                    return result;
                }
            }

            // Not a valid double (or empty) -- tell the binding to leave CurrentValue alone
            // rather than overwriting it with a throwaway value while the user is typing.
            return Binding.DoNothing;
        }
    }
}
