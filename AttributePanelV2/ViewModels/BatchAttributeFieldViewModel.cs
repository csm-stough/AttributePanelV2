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

        // CodedValueAttributeTemplate's ComboBox matches CurrentValue against DomainValues by
        // Key (SelectedValuePath="Key") -- the plain VariesPlaceholder string never matches a
        // real domain code, so the combo just shows blank instead of "<Varies>". This sentinel
        // is a synthetic key added to this row's own copy of DomainValues (see below) so the
        // combo has something to match and display against. Internal (not private) so
        // VariesKeyToIsEnabledConverter can recognize it and grey the entry out in the dropdown
        // -- it should be visible as the current selection but not user-selectable.
        internal static readonly object VariesKey = new object();

        // SortedList's default comparer calls the keys' own IComparable.CompareTo, which throws
        // if VariesKey (a plain object) ever gets compared against a real domain code (e.g.
        // short/int/string). Special-case it so it always sorts first without touching the
        // real codes' comparison logic.
        private sealed class VariesAwareComparer : IComparer<object>
        {
            public static readonly VariesAwareComparer Instance = new VariesAwareComparer();

            public int Compare(object x, object y)
            {
                if (ReferenceEquals(x, VariesKey))
                {
                    return ReferenceEquals(y, VariesKey) ? 0 : -1;
                }
                if (ReferenceEquals(y, VariesKey))
                {
                    return 1;
                }
                return Comparer<object>.Default.Compare(x, y);
            }
        }

        private readonly IReadOnlyList<AttributeFieldViewModel> _fields;

        // Exposes the per-feature fields this row wraps -- GeometryThumbnailConverter uses
        // this to merge every selected feature's actual shape into one combined sketch for a
        // batch/layer-node selection, rather than collapsing to CurrentValue's single
        // shared-value-or-"<Varies>" view (which is the right behavior for every other field
        // type, but for Geometry specifically just means "different every time").
        public IReadOnlyList<AttributeFieldViewModel> UnderlyingFields => _fields;

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

            if (IsCodedValue)
            {
                var domainValues = new SortedList<object, string>(VariesAwareComparer.Instance);
                foreach (var pair in first.DomainValues)
                {
                    domainValues.Add(pair.Key, pair.Value);
                }
                domainValues.Add(VariesKey, VariesPlaceholder);
                DomainValues = domainValues;
            }
            else
            {
                DomainValues = first.DomainValues;
            }

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
                if (distinctValues.Count == 1)
                {
                    return distinctValues[0];
                }
                return IsCodedValue ? VariesKey : (object)VariesPlaceholder;
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

        // Groups every given feature's AttributeFieldViewModels by field name into one
        // BatchAttributeFieldViewModel row per field -- the same grouping
        // FeatureLayerViewModel.BuildBatchAttributes uses for "the whole layer", and
        // Dockpane1ViewModel.RefreshActiveAttributes uses for an arbitrary Ctrl/Shift-checked
        // subset of a layer's features. Pulled out here once so both callers share one
        // implementation instead of duplicating the grouping logic.
        public static IEnumerable<BatchAttributeFieldViewModel> BuildRows(IEnumerable<SelectedFeatureViewModel> features)
        {
            var featureList = features as IReadOnlyList<SelectedFeatureViewModel> ?? features.ToList();

            if (featureList.Count == 0)
            {
                return Enumerable.Empty<BatchAttributeFieldViewModel>();
            }

            return featureList
                .SelectMany(feature => feature.Attributes)
                .GroupBy(attribute => attribute.FieldName)
                .Select(group => new BatchAttributeFieldViewModel(group.ToList()))
                .ToList();
        }
    }
}
