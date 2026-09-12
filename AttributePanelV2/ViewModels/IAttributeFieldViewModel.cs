using ArcGIS.Core.Data;
using System.Collections.Generic;

namespace AttributePanelV2.ViewModels
{
    // Common shape shared by AttributeFieldViewModel (one field on one feature) and
    // BatchAttributeFieldViewModel (one field across every feature currently loaded for a
    // layer, used for batch editing). AttributeTemplateSelector and every DataTemplate in
    // AttributeTemplates.xaml are written against this shape rather than either concrete
    // type, so the same per-type templates render both single-feature and batch editing.
    public interface IAttributeFieldViewModel
    {
        string FieldName { get; }
        string Alias { get; }
        object CurrentValue { get; set; }
        FieldType FieldType { get; }
        int Length { get; }
        bool IsEditable { get; }
        bool HasDomain { get; }
        Domain CurrentDomain { get; }
        SortedList<object, string> DomainValues { get; }
        bool IsCodedValue { get; }
        bool IsDirty { get; }
    }
}
