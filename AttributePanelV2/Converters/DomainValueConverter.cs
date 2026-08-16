using ArcGIS.Desktop.Editing.Attributes;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System;

namespace AttributePanelV2.Converters
{
    public class DomainValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var attribute = value as ArcGIS.Desktop.Editing.Attributes.Attribute;

            if (attribute.CurrentValue == null)
            {
                return "Null";
            }
            else if (attribute.HasDomain && attribute.CurrentDomain is CodedValueDomain domain)
            {
                return domain[attribute.CurrentValue.ToString()];
            }
            else
            {
                return attribute.CurrentValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
