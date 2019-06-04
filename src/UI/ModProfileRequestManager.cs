using System;
using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Manages requests made for ModProfiles.</summary>
    public class ModProfileRequestManager : MonoBehaviour
    {
        // ---------[ FUNCTIONALITY ]---------
        /// <summary>Fetchs page of ModProfiles grabbing from the cache where possible.</summary>
        public virtual void FetchPage(RequestFilter filter,
                                      int offsetIndex,
                                      int profileCount,
                                      Action<RequestPage<ModProfile>> onSuccess,
                                      Action<WebRequestError> onError)
        {
            // PaginationParameters
            APIPaginationParameters pagination = new APIPaginationParameters();
            pagination.limit = profileCount;
            pagination.offset = offsetIndex;

            // Send Request
            APIClient.GetAllMods(filter, pagination,
                                 onSuccess, onError);
        }
    }
}
