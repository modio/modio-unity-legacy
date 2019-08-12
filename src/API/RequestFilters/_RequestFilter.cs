using System.Collections.Generic;

namespace ModIO
{
    public class RequestFilter
    {
        public static readonly RequestFilter None = new RequestFilter();

        public string sortFieldName = string.Empty;
        public bool isSortAscending = true;
        public Dictionary<string, List<IRequestFieldFilter>> fieldFilterMap = new Dictionary<string, List<IRequestFieldFilter>>();

        public string GenerateFilterString()
        {
            var filterStringBuilder = new System.Text.StringBuilder();

            if(!System.String.IsNullOrEmpty(sortFieldName))
            {
                filterStringBuilder.Append("_sort=" + (isSortAscending ? "" : "-") + sortFieldName + "&");
            }

            foreach(KeyValuePair<string, List<IRequestFieldFilter>> kvp in this.fieldFilterMap)
            {
                if(kvp.Value != null)
                {
                    foreach(IRequestFieldFilter fieldFilter in kvp.Value)
                    {
                        if(fieldFilter != null)
                        {
                            filterStringBuilder.Append(fieldFilter.GenerateFilterString(kvp.Key) + "&");
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

        // ---------[ OBSOLETE ]---------
        [System.Obsolete("Use RequestFilter.fieldFilterMap instead.", true)]
        public Dictionary<string, IRequestFieldFilter> fieldFilters;
    }
}
