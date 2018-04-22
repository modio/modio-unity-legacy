using System.Collections.Generic;

namespace ModIO.API
{
    public class RequestFilter
    {
        public static readonly RequestFilter None = new RequestFilter();

        public string sortFieldName = string.Empty;
        public bool isSortAscending = true;
        public Dictionary<string, IRequestFieldFilter> fieldFilters = new Dictionary<string, IRequestFieldFilter>();

        public string GenerateFilterString()
        {
            var filterStringBuilder = new System.Text.StringBuilder();

            if(!System.String.IsNullOrEmpty(sortFieldName))
            {
                filterStringBuilder.Append("_sort=" + (isSortAscending ? "" : "-") + sortFieldName + "&");
            }

            foreach(KeyValuePair<string, IRequestFieldFilter> kvp in fieldFilters)
            {
                filterStringBuilder.Append(kvp.Value.GenerateFilterString(kvp.Key) + "&");
            }

            if(filterStringBuilder.Length > 1)
            {
                // Remove trailing '&'
                filterStringBuilder.Length -= 1;
            }

            return filterStringBuilder.ToString();
        }
    }
}
