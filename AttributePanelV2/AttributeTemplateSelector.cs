using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing.Attributes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AttributePanelV2
{
    public class AttributeTemplateSelector : DataTemplateSelector
    {

        public DataTemplate StringTemplate { get; set; }
        public DataTemplate CodedValueTemplate { get; set; }
        public DataTemplate DoubleTemplate { get; set; }
        public DataTemplate IntegerTemplate { get; set; }
        public DataTemplate SmallIntegerTemplate { get; set; }
        public DataTemplate BigIntegerTemplate { get; set; }
        public DataTemplate OIDTemplate { get; set; }
        public DataTemplate GeometryTemplate { get; set; }
        public DataTemplate GUIDTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var attribute = item as AttributeFieldViewModel;

            if (attribute.IsCodedValue)
            {
                return CodedValueTemplate;
            }

            if (attribute.FieldType == FieldType.String)
            {
                return StringTemplate;
            }

            if (attribute.FieldType == FieldType.Double)
            {
                return DoubleTemplate;
            }

            if (attribute.FieldType == FieldType.Integer) 
            {
                return IntegerTemplate;
            }

            if (attribute.FieldType == FieldType.SmallInteger)
            {
                return SmallIntegerTemplate;
            }

            if (attribute.FieldType == FieldType.BigInteger)
            {
                return BigIntegerTemplate;
            }

            if (attribute.FieldType == FieldType.OID)
            {
                return OIDTemplate;
            }

            if (attribute.FieldType == FieldType.Geometry)
            {
                return GeometryTemplate;
            }

            if (attribute.FieldType == FieldType.GUID)
            {
                return GUIDTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }
}
