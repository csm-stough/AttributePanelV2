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
    public class IntegerValueConverter : IValueConverter
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
            if (value is int val)
            {
                return val.ToString();
            }
            if (value is string str)
            {
                // Batch editing: BatchAttributeFieldViewModel.CurrentValue is the literal
                // "<Varies>" placeholder (a string) when the selected features disagree on
                // this field -- pass it through as-is instead of falling to UnsetValue.
                return str;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                if (int.TryParse(str, NumberStyles.AllowLeadingSign, culture, out int result))
                {
                    return result;
                }
            }

            // Not a valid int (or empty) -- tell the binding to leave CurrentValue alone
            // rather than overwriting it with a throwaway value while the user is typing.
            return Binding.DoNothing;
        }
    }
}
