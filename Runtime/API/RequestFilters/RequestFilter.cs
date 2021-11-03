using System.Collections.Generic;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    public class RequestFilter
    {
        public static readonly RequestFilter None = new RequestFilter();

        public string sortFieldName = string.Empty;
        public bool isSortAscending = true;
        public Dictionary<string, List<IRequestFieldFilter>> fieldFilterMap =
            new Dictionary<string, List<IRequestFieldFilter>>();

        public string GenerateFilterString()
        {
            var filterStringBuilder = new System.Text.StringBuilder();

            if(!System.String.IsNullOrEmpty(sortFieldName))
            {
                filterStringBuilder.Append("_sort=" + (isSortAscending ? "" : "-") + sortFieldName
                                           + "&");
            }

            foreach(KeyValuePair<string, List<IRequestFieldFilter>> kvp in this.fieldFilterMap)
            {
                if(kvp.Value != null)
                {
                    foreach(IRequestFieldFilter fieldFilter in kvp.Value)
                    {
                        if(fieldFilter != null)
                        {
                            filterStringBuilder.Append(fieldFilter.GenerateFilterString(kvp.Key)
                                                       + "&");
                        }
                    }
                }
            }

            if(filterStringBuilder.Length > 1)
            {
                // Remove trailing '&'
                filterStringBuilder.Length -= 1;
            }

            return filterStringBuilder.ToString();
        }

        public void AddFieldFilter(string fieldName, IRequestFieldFilter filter)
        {
            Debug.Assert(this.fieldFilterMap != null);

            // early out
            if(string.IsNullOrEmpty(fieldName) || filter == null || filter.filterValue == null)
            {
                Debug.LogWarning(
                    "[mod.io] Attempted to add an invalid field filter to the request filter."
                    + "\nfieldName=\"" + (fieldName == null ? "NULL" : fieldName) + "\""
                    + "\nfilter=" + (filter == null ? "NULL" : filter.GetType().ToString())
                    + (filter == null
                           ? string.Empty
                           : "\nfilterValue="
                                 + (filter.filterValue == null ? "NULL"
                                                               : filter.filterValue.ToString())));

                return;
            }

            List<IRequestFieldFilter> list = null;
            this.fieldFilterMap.TryGetValue(fieldName, out list);

            if(list == null)
            {
                list = new List<IRequestFieldFilter>();
                this.fieldFilterMap[fieldName] = list;
            }

            // remove duplicate from list
            for(int i = 0; i < list.Count; ++i)
            {
                if(list[i] != null && list[i].filterMethod == filter.filterMethod)
                {
                    list.RemoveAt(i);
                    break;
                }
            }

            list.Add(filter);
        }

#pragma warning disable 0618
        public void AddFieldFilter<T>(string fieldName, RangeFilter<T> filter)
            where T : System.IComparable<T>
        {
            if(filter != null)
            {
                MinimumFilter<T> minFilter = new MinimumFilter<T>() {
                    minimum = filter.min,
                    isInclusive = filter.isMinInclusive,
                };
                MaximumFilter<T> maxFilter = new MaximumFilter<T>() {
                    maximum = filter.max,
                    isInclusive = filter.isMaxInclusive,
                };

                this.AddFieldFilter(fieldName, minFilter);
                this.AddFieldFilter(fieldName, maxFilter);
            }
        }
#pragma warning restore 0618

        // ---------[ OBSOLETE ]---------
        [System.Obsolete("Use RequestFilter.fieldFilterMap instead.", true)]
        public Dictionary<string, IRequestFieldFilter> fieldFilters;
    }
}
