using System;
using StringBuilder = System.Text.StringBuilder;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    // NOTE(@jackson): This struct will eventually replace the various classes below.
    /// <summary>The data necessary for filtering a set of results.</summary>
    public struct FieldFilter<T>
    {
        public T filterValue;
        public FieldFilterMethod filterMethod;
    }

    // ------[ INTERFACE ]------
    public interface IRequestFieldFilter
    {
        object filterValue { get; }
        FieldFilterMethod filterMethod { get; }
        string GenerateFilterString(string fieldName);
    }

    public interface IRequestFieldFilter<T>
    {
        T filterValue { get; }
        FieldFilterMethod filterMethod { get; }
        string GenerateFilterString(string fieldName);
    }

    // ------[ BASE CLASSES ]------
    public abstract class AFieldFilterBase<T> : IRequestFieldFilter, IRequestFieldFilter<T>
    {
        // --- Fields ---
        protected FieldFilter<T> data = new FieldFilter<T>();
        protected string apiStringOperator = string.Empty;

        // Accessor
        public T filterValue
        {
            get {
                return this.data.filterValue;
            }
            set {
                this.data.filterValue = value;
            }
        }

        // --- Initialization ---
        public AFieldFilterBase(FieldFilterMethod filterMethod, string apiStringOperator)
        {
            this.data.filterMethod = filterMethod;
            this.apiStringOperator = apiStringOperator;
        }

        // ---- Interfaces ---
        object IRequestFieldFilter.filterValue
        {
            get {
                return this.data.filterValue;
            }
        }

        T IRequestFieldFilter<T>.filterValue
        {
            get {
                return this.data.filterValue;
            }
        }

        public virtual FieldFilterMethod filterMethod
        {
            get {
                return this.data.filterMethod;
            }
        }

        public virtual string GenerateFilterString(string fieldName)
        {
            Debug.Assert(!string.IsNullOrEmpty(fieldName));
            Debug.Assert(this.filterValue != null);

            return (fieldName + this.apiStringOperator + this.filterValue.ToString());
        }
    }

    public abstract class ArrayFieldFilterBase<T> : AFieldFilterBase<T[]>
    {
        public T[] filterArray
        {
            get {
                return this.filterValue;
            }
            set {
                this.filterValue = value;
            }
        }

        public ArrayFieldFilterBase(FieldFilterMethod filterMethod, string apiStringOperator)
            : base(filterMethod, apiStringOperator)
        {
        }

        public override string GenerateFilterString(string fieldName)
        {
            Debug.Assert(!string.IsNullOrEmpty(fieldName));
            Debug.Assert(this.filterArray != null);

            StringBuilder valueList = new StringBuilder();

            if(this.filterArray.Length > 0)
            {
                foreach(T arrayItem in this.filterArray)
                {
                    if(arrayItem != null)
                    {
                        valueList.Append(arrayItem.ToString() + ",");
                    }
                }

                if(valueList.Length > 0)
                {
                    // Remove trailing comma
                    valueList.Length -= 1;
                }
            }

            return fieldName + this.apiStringOperator + valueList.ToString();
        }
    }

    // ------[ GENERIC FILTERS ]------
    public class EqualToFilter<T> : AFieldFilterBase<T>
    {
        public EqualToFilter(T filterValue = default(T)) : base(FieldFilterMethod.Equal, "=")
        {
            this.filterValue = filterValue;
        }
    }

    public class NotEqualToFilter<T> : AFieldFilterBase<T>
    {
        public NotEqualToFilter(T filterValue = default(T))
            : base(FieldFilterMethod.NotEqual, "-not=")
        {
            this.filterValue = filterValue;
        }
    }

    public class MatchesArrayFilter<T> : ArrayFieldFilterBase<T>
    {
        public MatchesArrayFilter(T[] filterArray = null)
            : base(FieldFilterMethod.EquivalentCollection, "=")
        {
            this.filterArray = filterArray;
        }
    }

    public class InArrayFilter<T> : ArrayFieldFilterBase<T>
    {
        public InArrayFilter(T[] filterArray = null) : base(FieldFilterMethod.InCollection, "-in=")
        {
            this.filterArray = filterArray;
        }
    }

    public class NotInArrayFilter<T> : ArrayFieldFilterBase<T>
    {
        public NotInArrayFilter(T[] filterArray = null)
            : base(FieldFilterMethod.NotInCollection, "-not-in=")
        {
            this.filterArray = filterArray;
        }
    }

    // ------[ NUMERIC FILTERS ]------
    public class MinimumFilter<T> : AFieldFilterBase<T>
        where T : IComparable<T>
    {
        public T minimum
        {
            get {
                return this.filterValue;
            }
            set {
                this.filterValue = value;
            }
        }

        public bool isInclusive = true;

        // --- Initialization ---
        public MinimumFilter(T filterValue = default(T), bool isInclusive = true)
            : base(FieldFilterMethod.Minimum, "-min=")
        {
            this.minimum = filterValue;
            this.isInclusive = isInclusive;
        }

        public override FieldFilterMethod filterMethod
        {
            get {
                this.data.filterMethod =
                    (this.isInclusive ? FieldFilterMethod.Minimum : FieldFilterMethod.GreaterThan);

                return base.filterMethod;
            }
        }

        public override string GenerateFilterString(string fieldName)
        {
            this.apiStringOperator = (isInclusive ? "-min=" : "-gt=");
            return base.GenerateFilterString(fieldName);
        }
    }

    public class MaximumFilter<T> : AFieldFilterBase<T>
        where T : IComparable<T>
    {
        public T maximum
        {
            get {
                return this.filterValue;
            }
            set {
                this.filterValue = value;
            }
        }

        public bool isInclusive = true;

        // --- Initialization ---
        public MaximumFilter(T filterValue = default(T), bool isInclusive = true)
            : base(FieldFilterMethod.Maximum, "-max=")
        {
            this.maximum = filterValue;
            this.isInclusive = isInclusive;
        }

        public override FieldFilterMethod filterMethod
        {
            get {
                this.data.filterMethod =
                    (this.isInclusive ? FieldFilterMethod.Maximum : FieldFilterMethod.LessThan);

                return base.filterMethod;
            }
        }

        public override string GenerateFilterString(string fieldName)
        {
            this.apiStringOperator = (isInclusive ? "-max=" : "-st=");
            return base.GenerateFilterString(fieldName);
        }
    }

    // ------[ INT FILTERS ]------
    public class BitwiseAndFilter : AFieldFilterBase<int>
    {
        public BitwiseAndFilter(int filterValue = -1)
            : base(FieldFilterMethod.BitwiseAnd, "-bitwise-and=")
        {
            this.filterValue = filterValue;
        }
    }

    // ------[ STRING FILTERS ]------
    public class StringLikeFilter : AFieldFilterBase<string>
    {
        public string likeValue
        {
            get {
                return this.filterValue;
            }
            set {
                this.filterValue = value;
            }
        }


        public StringLikeFilter(string filterValue = null)
            : base(FieldFilterMethod.LikeString, "-lk=")
        {
            this.filterValue = filterValue;
        }
    }
    public class StringNotLikeFilter : AFieldFilterBase<string>
    {
        public string notLikeValue
        {
            get {
                return this.filterValue;
            }
            set {
                this.filterValue = value;
            }
        }

        public StringNotLikeFilter(string filterValue = null)
            : base(FieldFilterMethod.NotLikeString, "-not-lk=")
        {
            this.filterValue = filterValue;
        }
    }

    // ---------[ OBSOLETE ]---------
    [Obsolete("Combine a MinimumFilter and MaximumFilter instead.")]
    public class RangeFilter<T> : IRequestFieldFilter<T>, IRequestFieldFilter
        where T : IComparable<T>
    {
        public T min;
        public bool isMinInclusive;
        public T max;
        public bool isMaxInclusive;

        public string GenerateFilterString(string fieldName)
        {
            Debug.Assert(!string.IsNullOrEmpty(fieldName));
            Debug.Assert(this.min != null);
            Debug.Assert(this.max != null);

            return (fieldName + (isMinInclusive ? "-min=" : "-gt=") + min + "&" + fieldName
                    + (isMaxInclusive ? "-max=" : "-st=") + max);
        }

        public FieldFilterMethod filterMethod
        {
            get {
                throw new System.NotImplementedException();
            }
        }
        object IRequestFieldFilter.filterValue
        {
            get {
                throw new System.NotImplementedException();
            }
        }

        T IRequestFieldFilter<T>.filterValue
        {
            get {
                throw new System.NotImplementedException();
            }
        }
    }

}
