using System;

namespace ModIO
{
    // ------[ INTERFACE ]------
    public interface IRequestFieldFilter
    {
        string GenerateFilterString(string fieldName);
    }

    public interface IRequestFieldFilter<T> : IRequestFieldFilter {}

    // ------[ GENERIC FILTERS ]------
    public class EqualToFilter<T> : IRequestFieldFilter, IRequestFieldFilter<T>
    {
        public T filterValue;

        public string GenerateFilterString(string fieldName)
        {
            return fieldName + "=" + filterValue.ToString();
        }
    }

    public class NotEqualToFilter<T> : IRequestFieldFilter, IRequestFieldFilter<T>
    {
        public T filterValue;

        public string GenerateFilterString(string fieldName)
        {
            return fieldName + "-not=" + filterValue.ToString();
        }
    }

    public class MatchesArrayFilter<T> : IRequestFieldFilter, IRequestFieldFilter<T>
    {
        public T[] filterArray;

        public string GenerateFilterString(string fieldName)
        {
            string valueList = filterArray[0].ToString();
            for(int i = 1;
                i < filterArray.Length;
                ++i)
            {
                valueList += "," + filterArray[i];
            }
            return fieldName + "=" + valueList;
        }
    }

    public class InArrayFilter<T> : IRequestFieldFilter, IRequestFieldFilter<T>
    {
        public T[] filterArray;

        public string GenerateFilterString(string fieldName)
        {
            string valueList = filterArray[0].ToString();
            for(int i = 1;
                i < filterArray.Length;
                ++i)
            {
                valueList += "," + filterArray[i];
            }
            return fieldName + "-in=" + valueList;
        }
    }

    public class NotInArrayFilter<T> : IRequestFieldFilter, IRequestFieldFilter<T>
    {
        public T[] filterArray;

        public string GenerateFilterString(string fieldName)
        {
            string valueList = filterArray[0].ToString();
            for(int i = 1;
                i < filterArray.Length;
                ++i)
            {
                valueList += "," + filterArray[i];
            }
            return fieldName + "-not-in=" + valueList;
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
            return fieldName + (isInclusive ? "-min=" : "-gt=") + minimum;
        }
    }

    public class MaximumFilter<T> : IRequestFieldFilter, IRequestFieldFilter<T>
        where T : IComparable<T>
    {
        public T maximum;
        public bool isInclusive;

        public string GenerateFilterString(string fieldName)
        {
            return fieldName + (isInclusive ? "-max=" : "-lt=") + maximum;
        }
    }

    public class RangeFilter<T> : IRequestFieldFilter, IRequestFieldFilter<T>
        where T : IComparable<T>
    {
        public T min;
        public bool isMinInclusive;
        public T max;
        public bool isMaxInclusive;

        public string GenerateFilterString(string fieldName)
        {
            return (fieldName + (isMinInclusive ? "-min=" : "-gt=") + min
                    + "&" + fieldName + (isMaxInclusive ? "-max=" : "-lt=") + max);
        }
    }

    // ------[ INT FILTERS ]------
    public class BitwiseAndFilter : IRequestFieldFilter, IRequestFieldFilter<int>
    {
        public int filterValue;

        public string GenerateFilterString(string fieldName)
        {
            return fieldName + "-bitwise-and=" + filterValue;
        }
    }

    // ------[ STRING FILTERS ]------
    public class StringLikeFilter : IRequestFieldFilter, IRequestFieldFilter<string>
    {
        public string likeValue;

        public string GenerateFilterString(string fieldName)
        {
            return fieldName + "-lk=" + likeValue;
        }
    }
    public class StringNotLikeFilter : IRequestFieldFilter, IRequestFieldFilter<string>
    {
        public string notLikeValue;

        public string GenerateFilterString(string fieldName)
        {
            return fieldName + "-not-lk=" + notLikeValue;
        }
    }
}
