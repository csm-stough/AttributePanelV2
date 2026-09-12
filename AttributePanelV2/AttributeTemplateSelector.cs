using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing.Attributes;
using AttributePanelV2.ViewModels;
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
        public DataTemplate SingleTemplate { get; set; }
        public DataTemplate IntegerTemplate { get; set; }
        public DataTemplate SmallIntegerTemplate { get; set; }
        public DataTemplate BigIntegerTemplate { get; set; }
        public DataTemplate DateTemplate { get; set; }
        public DataTemplate OIDTemplate { get; set; }
        public DataTemplate GeometryTemplate { get; set; }
        public DataTemplate GUIDTemplate { get; set; }
        public DataTemplate GlobalIDTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            // IAttributeFieldViewModel covers both AttributeFieldViewModel (single-feature
            // editing) and BatchAttributeFieldViewModel (batch editing across every feature
            // loaded for a layer) -- same templates render both.
            if (item is not IAttributeFieldViewModel attribute)
            {
                return base.SelectTemplate(item, container);
            }

            if (attribute.IsCodedValue)
            {
                return CodedValueTemplate;
            }

            return attribute.FieldType switch
            {
                FieldType.String => StringTemplate,
                FieldType.Double => DoubleTemplate,
                FieldType.Single => SingleTemplate,
                FieldType.Integer => IntegerTemplate,
                FieldType.SmallInteger => SmallIntegerTemplate,
                FieldType.BigInteger => BigIntegerTemplate,
                FieldType.Date => DateTemplate,
                FieldType.OID => OIDTemplate,
                FieldType.Geometry => GeometryTemplate,
                FieldType.GUID => GUIDTemplate,
                FieldType.GlobalID => GlobalIDTemplate,
                _ => base.SelectTemplate(item, container)
            };
        }
    }
}
