using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace AttributePanelV2.ViewModels
{
    // One row in the batch-edit view: shown when a whole FeatureLayerViewModel node (rather
    // than a single feature under it) is selected in the tree. Wraps the same field across
    // every feature currently loaded for that layer.
    //
    // CurrentValue reads back the shared value, or VariesPlaceholder if the loaded features
    // disagree on it. Setting it pushes the new value onto every underlying
    // AttributeFieldViewModel -- which is what Dockpane1ViewModel.ApplyChanges actually acts
    // on, via CurrentFeatureLayer.DirtyFeatures. That method already walks every dirty feature
    // in the layer regardless of which tree node is selected, so nothing else had to change
    // there for batch edits to get applied.
    public class BatchAttributeFieldViewModel : PropertyChangedBase, IAttributeFieldViewModel
    {
        public const string VariesPlaceholder = "<Varies>";

        private readonly IReadOnlyList<AttributeFieldViewModel> _fields;

        public BatchAttributeFieldViewModel(IReadOnlyList<AttributeFieldViewModel> fields)
        {
            _fields = fields;

            // Metadata (alias, type, domain, editability, length) comes from the feature
            // class definition and should be identical across every feature in the same
            // layer -- take it from the first one.
            // NOTE: if this layer has subtypes with per-subtype domain overrides and the
            // loaded features span more than one subtype, HasDomain/CurrentDomain here can
            // be wrong for some of them -- subtype-aware domains aren't handled yet anywhere
            // in this project, not just here.
            var first = fields[0];
            FieldName = first.FieldName;
            Alias = first.Alias;
            FieldType = first.FieldType;
            Length = first.Length;
            IsEditable = first.IsEditable;
            HasDomain = first.HasDomain;
            CurrentDomain = first.CurrentDomain;
            IsCodedValue = first.IsCodedValue;
            DomainValues = first.DomainValues;

            foreach (var field in _fields)
            {
                field.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(AttributeFieldViewModel.CurrentValue) ||
                        args.PropertyName == nameof(AttributeFieldViewModel.IsDirty))
                    {
                        NotifyPropertyChanged(nameof(CurrentValue));
                        NotifyPropertyChanged(nameof(IsDirty));
                    }
                };
            }
        }

        public string FieldName { get; }
        public string Alias { get; }
        public FieldType FieldType { get; }
        public int Length { get; }
        public bool IsEditable { get; }
        public bool HasDomain { get; }
        public Domain CurrentDomain { get; }
        public SortedList<object, string> DomainValues { get; }
        public bool IsCodedValue { get; }
        public bool IsDirty => _fields.Any(field => field.IsDirty);

        public object CurrentValue
        {
            get
            {
                var distinctValues = _fields.Select(field => field.CurrentValue).Distinct().ToList();
                return distinctValues.Count == 1 ? distinctValues[0] : VariesPlaceholder;
            }
            set
            {
                // Covers both "user left the <Varies> placeholder untouched" and "user typed
                // back exactly what was already there" -- either way, nothing to push.
                if (Equals(CurrentValue, value))
                {
                    return;
                }

                foreach (var field in _fields)
                {
                    field.CurrentValue = value;
                }
            }
        }
    }
}
