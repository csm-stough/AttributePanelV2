using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Framework.Contracts;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ArcGIS.Desktop.Editing.Attributes.CodedValueDomain;

namespace AttributePanelV2.ViewModels
{
    public class AttributeFieldViewModel : PropertyChangedBase
    {
        private readonly Attribute _attribute;

        public AttributeFieldViewModel(Attribute attribute)
        {
            _attribute = attribute;
            _attribute.PropertyChanged += PropertyChanged;
        }

        public string FieldName => _attribute.FieldName;

        public string Alias => _attribute.FieldAlias;

        public object CurrentValue
        {
            get => _attribute.CurrentValue;
            set => _attribute.CurrentValue = value;
        }

        public FieldType FieldType => _attribute.FieldType;

        public int Length => _attribute.Length;

        public bool IsEditable => _attribute.IsEditable;

        public bool HasDomain => _attribute.HasDomain;

        public bool IsDirty => _attribute.IsDirty;

        public ArcGIS.Desktop.Editing.Attributes.CodedValueDomain CurrentDomain => _attribute.CurrentDomain as ArcGIS.Desktop.Editing.Attributes.CodedValueDomain;
    
        public IEnumerable<CodedValue> DomainValues => CurrentDomain?.CodedValues ?? Enumerable.Empty<CodedValue>();

        //Properties to make the attrbute template selector's job easier
        public bool IsCodedValue => CurrentDomain != null;

        private new void PropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            switch(args.PropertyName)
            {
                case "CurrentValue":
                    NotifyPropertyChanged(nameof(CurrentValue));
                    break;
                case "IsDirty":
                    NotifyPropertyChanged(nameof(IsDirty));
                    break;
                case "CurrentDomain":
                    NotifyPropertyChanged(nameof(CurrentDomain));
                    NotifyPropertyChanged(nameof(IsCodedValue));
                    break;
                case "IsEditable":
                    NotifyPropertyChanged(nameof(IsEditable));
                    break;
            }

            System.Console.WriteLine("Attribute Update Event!");
        }
    }
}
