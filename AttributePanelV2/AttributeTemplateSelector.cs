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

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var attribute = item as Attribute;

            if (attribute.HasDomain && attribute.CurrentDomain is ArcGIS.Desktop.Editing.Attributes.CodedValueDomain)
            {
                return CodedValueTemplate;
            }

            if (attribute.FieldType == FieldType.String)
            {
                return StringTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }
}
