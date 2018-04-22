using System;

namespace ModIO.API
{
    // ------[ INTERFACE ]------
    public interface IRequestFieldFilter
    {
        string GenerateFilterString(string fieldName);
    }

    // ------[ GENERIC FILTERS ]------
    public class EqualToFilter<T> : IRequestFieldFilter
    {
        T filterValue;

        public string GenerateFilterString(string fieldName)
        {
            return fieldName + "=" + filterValue.ToString();
        }
    }

    public class NotEqualToFilter<T> : IRequestFieldFilter
    {
        T filterValue;

        public string GenerateFilterString(string fieldName)
        {
            return fieldName + "-not=" + filterValue.ToString();
        }
    }

    public class MatchesArrayFilter<T> : IRequestFieldFilter
    {
        T[] filterArray;

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

    public class InArrayFilter<T> : IRequestFieldFilter
    {
        T[] filterArray;

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

    public class NotInArrayFilter<T> : IRequestFieldFilter
    {
        T[] filterArray;

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
    public class MinimumFilter<T> : IRequestFieldFilter
        where T : IComparable<T>
    {
        public T minimum;
        public bool isInclusive;

        public string GenerateFilterString(string fieldName)
        {
            return fieldName + (isInclusive ? "-min=" : "-gt") + minimum;
        }
    }

    public class MaximumFilter<T> : IRequestFieldFilter
        where T : IComparable<T>
    {
        public T maximum;
        public bool isInclusive;

        public string GenerateFilterString(string fieldName)
        {
            return fieldName + (isInclusive ? "-max=" : "-lt") + maximum;
        }
    }

    public class RangeFilter<T> : IRequestFieldFilter
        where T : IComparable<T>
    {
        public T minimum;
        public bool isMinInclusive;
        public T maximum;
        public bool isMaxInclusive;

        public string GenerateFilterString(string fieldName)
        {
            return (fieldName + (isMinInclusive ? "-min=" : "-gt") + minimum
                    + "&" + fieldName + (isMaxInclusive ? "-max=" : "-lt") + maximum);
        }
    }

    // ------[ INT FILTERS ]------
    public class BitwiseAndFilter : IRequestFieldFilter
    {
        public int filterValue;

        public string GenerateFilterString(string fieldName)
        {
            return fieldName + "-bitwise-and=" + filterValue;
        }
    }

    // ------[ STRING FILTERS ]------
    public class StringLikeFilter : IRequestFieldFilter
    {
        public string likeValue;

        public string GenerateFilterString(string fieldName)
        {
            return fieldName + "-lk=" + likeValue;
        }
    }
    public class StringNotLikeFilter : IRequestFieldFilter
    {
        public string notLikeValue;

        public string GenerateFilterString(string fieldName)
        {
            return fieldName + "-not-lk=" + notLikeValue;
        }
    }
}
