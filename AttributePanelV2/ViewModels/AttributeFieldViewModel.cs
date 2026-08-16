using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Framework.Contracts;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.Data;

namespace AttributePanelV2.ViewModels
{
    public class AttributeFieldViewModel : PropertyChangedBase
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
        public object _currentValue;
        public object OriginalValue { get; }
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
                NotifyPropertyChanged();
                IsDirty = !Equals(OriginalValue, _currentValue);
            }
        }
        public FieldType FieldType { get; }
        public int Length { get; }
        public bool IsEditable { get; }
        public bool HasDomain { get; }
        public bool _isDirty;
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

        //private new void PropertyChanged(object sender, PropertyChangedEventArgs args)
        //{
        //    switch(args.PropertyName)
        //    {
        //        case "CurrentValue":
        //            NotifyPropertyChanged(nameof(CurrentValue));
        //            break;
        //        case "IsDirty":
        //            NotifyPropertyChanged(nameof(IsDirty));
        //            break;
        //        case "CurrentDomain":
        //            NotifyPropertyChanged(nameof(CurrentDomain));
        //            NotifyPropertyChanged(nameof(IsCodedValue));
        //            break;
        //        case "IsEditable":
        //            NotifyPropertyChanged(nameof(IsEditable));
        //            break;
        //    }
        //}
    }
}
