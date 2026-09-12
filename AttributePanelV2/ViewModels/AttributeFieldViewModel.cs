using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Framework.Contracts;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AttributePanelV2.ViewModels
{
    // Implements IAttributeFieldViewModel so AttributeTemplateSelector and the shared
    // DataTemplates in AttributeTemplates.xaml also work for BatchAttributeFieldViewModel
    // (batch editing across every feature loaded for a layer) without any duplication.
    public class AttributeFieldViewModel : PropertyChangedBase, IAttributeFieldViewModel
    {

        public AttributeFieldViewModel(Field field, object value)
        {
            FieldName = field.Name;
            Alias = field.AliasName;
            OriginalValue = value;
            CurrentValue = value;
            FieldType = field.FieldType;
            Length = field.Length;
            IsEditable = field.IsEditable;
            HasDomain = field.GetDomain() != null;
            CurrentDomain = field.GetDomain();
            IsCodedValue = CurrentDomain is ArcGIS.Core.Data.CodedValueDomain;
            DomainValues = (CurrentDomain as ArcGIS.Core.Data.CodedValueDomain)?.GetCodedValuePairs() ?? new SortedList<object, string>();
        }

        public string FieldName { get; }
        public string Alias { get; }
        private object _currentValue;
        public object OriginalValue { get; private set; }
        public object CurrentValue
        {
            get => _currentValue;
            set
            {
                if (Equals(_currentValue, value))
                {
                    return;
                }
                _currentValue = value;
                // IsDirty must be updated BEFORE the CurrentValue change notification fires:
                // Dockpane1ViewModel's auto-apply hook runs synchronously off that same
                // notification (PropertyChanged is a plain synchronous event), and checks
                // whether anything is dirty before applying. With NotifyPropertyChanged()
                // called first, that check would run while this field's own IsDirty was
                // still false -- so the very first edit to an otherwise-clean feature (the
                // most common case) would silently fail to auto-apply every time.
                IsDirty = !Equals(OriginalValue, _currentValue);
                NotifyPropertyChanged();
            }
        }
        public FieldType FieldType { get; }
        public int Length { get; }
        public bool IsEditable { get; }
        public bool HasDomain { get; }
        private bool _isDirty;
        public bool IsDirty
        {
            get => _isDirty;
            private set
            {
                if(_isDirty == value)
                {
                    return;
                }

                _isDirty = value;
                NotifyPropertyChanged();
            }
        }
        public ArcGIS.Core.Data.Domain CurrentDomain { get; }
        public SortedList<object, string> DomainValues { get; }
        public bool IsCodedValue { get; }
        public void Commit()
        {
            OriginalValue = CurrentValue;
            IsDirty = false;

        }

        public void Discard()
        {
            // Routes through the CurrentValue setter (not a direct field write) so the usual
            // equality/IsDirty/notification logic runs the same way an ordinary edit would --
            // if CurrentValue already equals OriginalValue this is a no-op.
            CurrentValue = OriginalValue;
        }
    }
}
