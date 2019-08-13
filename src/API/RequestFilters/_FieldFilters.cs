using System;
using StringBuilder = System.Text.StringBuilder;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    // ------[ INTERFACE ]------
    public interface IRequestFieldFilter
    {
        FieldFilterMethod FilterMethod { get; }
        string GenerateFilterString(string fieldName);
    }

    public interface IRequestFieldFilter<T> : IRequestFieldFilter {}

    // ------[ GENERIC FILTERS ]------
    public class EqualToFilter<T> : IRequestFieldFilter, IRequestFieldFilter<T>
    {
        public T filterValue;

        public string GenerateFilterString(string fieldName)
        {
            Debug.Assert(!string.IsNullOrEmpty(fieldName));
            Debug.Assert(this.filterValue != null);

            return fieldName + "=" + filterValue.ToString();
        }

        public FieldFilterMethod FilterMethod { get { return FieldFilterMethod.Equal; } }

        // --- Initialization ---
        public EqualToFilter(T filterValue = default(T))
        {
            this.filterValue = filterValue;
        }
    }

    public class NotEqualToFilter<T> : IRequestFieldFilter, IRequestFieldFilter<T>
    {
        public T filterValue;

        public string GenerateFilterString(string fieldName)
        {
            Debug.Assert(!string.IsNullOrEmpty(fieldName));
            Debug.Assert(this.filterValue != null);

            return fieldName + "-not=" + filterValue.ToString();
        }

        public FieldFilterMethod FilterMethod { get { return FieldFilterMethod.NotEqual; } }

        // --- Initialization ---
        public NotEqualToFilter(T filterValue = default(T))
        {
            this.filterValue = filterValue;
        }
    }

    public class MatchesArrayFilter<T> : IRequestFieldFilter, IRequestFieldFilter<T[]>
    {
        public T[] filterArray;

        public string GenerateFilterString(string fieldName)
        {
            Debug.Assert(!string.IsNullOrEmpty(fieldName));
            Debug.Assert(this.filterArray != null);

            StringBuilder valueList = new StringBuilder();

            if(filterArray.Length > 0)
            {
                foreach(T filterValue in this.filterArray)
                {
                    if(filterValue != null)
                    {
                        valueList.Append(filterValue.ToString() + ",");
                    }
                }

                if(valueList.Length > 0)
                {
                    // Remove trailing comma
                    valueList.Length -= 1;
                }
            }

            return fieldName + "=" + valueList.ToString();
        }

        public FieldFilterMethod FilterMethod { get { return FieldFilterMethod.EquivalentCollection; } }

        // --- Initialization ---
        public MatchesArrayFilter(T[] filterArray = null)
        {
            this.filterArray = filterArray;
        }
    }

    public class InArrayFilter<T> : IRequestFieldFilter, IRequestFieldFilter<T[]>
    {
        public T[] filterArray;

        public string GenerateFilterString(string fieldName)
        {
            Debug.Assert(!string.IsNullOrEmpty(fieldName));
            Debug.Assert(this.filterArray != null);

            StringBuilder valueList = new StringBuilder();

            if(filterArray.Length > 0)
            {
                foreach(T filterValue in this.filterArray)
                {
                    if(filterValue != null)
                    {
                        valueList.Append(filterValue.ToString() + ",");
                    }
                }

                if(valueList.Length > 0)
                {
                    // Remove trailing comma
                    valueList.Length -= 1;
                }
            }

            return fieldName + "-in=" + valueList.ToString();
        }

        public FieldFilterMethod FilterMethod { get { return FieldFilterMethod.InCollection; } }

        // --- Initialization ---
        public InArrayFilter(T[] filterArray = null)
        {
            this.filterArray = filterArray;
        }
    }

    public class NotInArrayFilter<T> : IRequestFieldFilter, IRequestFieldFilter<T[]>
    {
        public T[] filterArray;

        public string GenerateFilterString(string fieldName)
        {
            Debug.Assert(!string.IsNullOrEmpty(fieldName));
            Debug.Assert(this.filterArray != null);

            StringBuilder valueList = new StringBuilder();

            if(filterArray.Length > 0)
            {
                foreach(T filterValue in this.filterArray)
                {
                    if(filterValue != null)
                    {
                        valueList.Append(filterValue.ToString() + ",");
                    }
                }

                if(valueList.Length > 0)
                {
                    // Remove trailing comma
                    valueList.Length -= 1;
                }
            }

            return fieldName + "-not-in=" + valueList.ToString();
        }

        public FieldFilterMethod FilterMethod { get { return FieldFilterMethod.NotInCollection; } }

        // --- Initialization ---
        public NotInArrayFilter(T[] filterArray = null)
        {
            this.filterArray = filterArray;
        }
    }

    // ------[ NUMERIC FILTERS ]------
    public class MinimumFilter<T> : IRequestFieldFilter, IRequestFieldFilter<T>
        where T : IComparable<T>
    {
        public T minimum;
        public bool isInclusive;

        public string GenerateFilterString(string fieldName)
        {
            Debug.Assert(!string.IsNullOrEmpty(fieldName));
            Debug.Assert(this.minimum != null);

            return fieldName + (isInclusive ? "-min=" : "-gt=") + minimum;
        }

        public FieldFilterMethod FilterMethod
        {
            get
            {
                if(this.isInclusive)
                {
                    return FieldFilterMethod.Minimum;
                }
                else
                {
                    return FieldFilterMethod.GreaterThan;
                }
            }
        }

        // --- Initialization ---
        public MinimumFilter(T filterValue = default(T), bool isInclusive = false)
        {
            this.minimum = filterValue;
            this.isInclusive = isInclusive;
        }
    }

    public class MaximumFilter<T> : IRequestFieldFilter, IRequestFieldFilter<T>
        where T : IComparable<T>
    {
        public T maximum;
        public bool isInclusive;

        public string GenerateFilterString(string fieldName)
        {
            Debug.Assert(!string.IsNullOrEmpty(fieldName));
            Debug.Assert(this.maximum != null);

            return fieldName + (isInclusive ? "-max=" : "-st=") + maximum;
        }

        public FieldFilterMethod FilterMethod
        {
            get
            {
                if(this.isInclusive)
                {
                    return FieldFilterMethod.Maximum;
                }
                else
                {
                    return FieldFilterMethod.LessThan;
                }
            }
        }

        // --- Initialization ---
        public MaximumFilter(T filterValue = default(T), bool isInclusive = false)
        {
            this.maximum = filterValue;
            this.isInclusive = isInclusive;
        }
    }

    // ------[ INT FILTERS ]------
    public class BitwiseAndFilter : IRequestFieldFilter, IRequestFieldFilter<int>
    {
        public int filterValue;

        public string GenerateFilterString(string fieldName)
        {
            Debug.Assert(!string.IsNullOrEmpty(fieldName));

            return fieldName + "-bitwise-and=" + filterValue;
        }

        public FieldFilterMethod FilterMethod { get { return FieldFilterMethod.BitwiseAnd; } }

        // --- Initialization ---
        public BitwiseAndFilter(int filterValue = -1)
        {
            this.filterValue = filterValue;
        }
    }

    // ------[ STRING FILTERS ]------
    public class StringLikeFilter : IRequestFieldFilter, IRequestFieldFilter<string>
    {
        public string likeValue;

        public string GenerateFilterString(string fieldName)
        {
            Debug.Assert(!string.IsNullOrEmpty(fieldName));
            Debug.Assert(!string.IsNullOrEmpty(this.likeValue));

            return fieldName + "-lk=" + likeValue;
        }

        public FieldFilterMethod FilterMethod { get { return FieldFilterMethod.LikeString; } }

        // --- Initialization ---
        public StringLikeFilter(string likeValue = null)
        {
            this.likeValue = likeValue;
        }
    }
    public class StringNotLikeFilter : IRequestFieldFilter, IRequestFieldFilter<string>
    {
        public string notLikeValue;

        public string GenerateFilterString(string fieldName)
        {
            Debug.Assert(!string.IsNullOrEmpty(fieldName));
            Debug.Assert(!string.IsNullOrEmpty(this.notLikeValue));

            return fieldName + "-not-lk=" + notLikeValue;
        }

        public FieldFilterMethod FilterMethod { get { return FieldFilterMethod.NotLikeString; } }

        // --- Initialization ---
        public StringNotLikeFilter(string notLikeValue = null)
        {
            this.notLikeValue = notLikeValue;
        }
    }

    // ---------[ OBSOLETE ]---------
    [Obsolete("Combine a MinimumFilter and MaximumFilter instead.")]
    public class RangeFilter<T> : IRequestFieldFilter, IRequestFieldFilter<T>
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

            return (fieldName + (isMinInclusive ? "-min=" : "-gt=") + min
                    + "&" + fieldName + (isMaxInclusive ? "-max=" : "-st=") + max);
        }

        public FieldFilterMethod FilterMethod { get { throw new System.NotImplementedException(); } }
    }

}
