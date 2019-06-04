using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Manages requests made for ModProfiles.</summary>
    public class ModProfileRequestManager : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        /// <summary>Cached requests.</summary>
        public Dictionary<string, RequestPage<ModProfile>> cachedRequests = new Dictionary<string, RequestPage<ModProfile>>();

        // ---------[ FUNCTIONALITY ]---------
        /// <summary>Fetchs page of ModProfiles grabbing from the cache where possible.</summary>
        public virtual void FetchPage(RequestFilter filter, int offsetIndex, int profileCount,
                                      Action<RequestPage<ModProfile>> onSuccess,
                                      Action<WebRequestError> onError)
        {
            // ensure indicies are positive
            if(offsetIndex < 0) { offsetIndex = 0; }
            if(profileCount < 0) { profileCount = 0; }

            // check if results already cached
            string filterString = filter.GenerateFilterString();
            RequestPage<ModProfile> cachedPage = null;
            if(this.cachedRequests.TryGetValue(filterString, out cachedPage))
            {
                // early out if no results
                if(cachedPage.resultTotal == 0)
                {
                    if(onSuccess != null)
                    {
                        cachedPage.size = profileCount;
                        onSuccess(cachedPage);
                    }
                    return;
                }

                // early out if index is beyond results
                if(offsetIndex >= cachedPage.resultTotal)
                {
                    if(onSuccess != null)
                    {
                        RequestPage<ModProfile> requestPage = new RequestPage<ModProfile>();
                        requestPage.size = profileCount;
                        requestPage.resultOffset = offsetIndex;
                        requestPage.resultTotal = cachedPage.resultTotal;
                        requestPage.items = new ModProfile[0];

                        onSuccess(requestPage);
                    }
                    return;
                }

                // clamp last index
                int clampedLastIndex = offsetIndex + profileCount-1;
                if(clampedLastIndex >= cachedPage.resultTotal)
                {
                    // NOTE(@jackson): cachedPage.resultTotal > 0
                    clampedLastIndex = cachedPage.resultTotal - 1;
                }

                // check if entire result set encompassed by cache
                int cachedLastIndex = cachedPage.resultOffset + cachedPage.items.Length;
                if(cachedPage.resultOffset <= offsetIndex
                   && clampedLastIndex <= cachedLastIndex)
                {
                    // check for nulls
                    bool nullFound = false;
                    for(int arrayIndex = offsetIndex - cachedPage.resultOffset;
                        arrayIndex < cachedPage.items.Length
                        && arrayIndex <= clampedLastIndex - cachedPage.resultOffset
                        && !nullFound;
                        ++arrayIndex)
                    {
                        nullFound = (cachedPage.items[arrayIndex] == null);
                    }

                    // return if no nulls found
                    if(!nullFound)
                    {
                        if(onSuccess != null)
                        {
                            RequestPage<ModProfile> requestPage = new RequestPage<ModProfile>();
                            requestPage.size = profileCount;
                            requestPage.resultOffset = offsetIndex;
                            requestPage.resultTotal = cachedPage.resultTotal;

                            // fill array
                            requestPage.items = new ModProfile[clampedLastIndex - offsetIndex + 1];
                            Array.Copy(cachedPage.items, offsetIndex - cachedPage.resultOffset,
                                       requestPage.items, 0, requestPage.items.Length);

                            onSuccess(requestPage);
                        }
                        return;
                    }
                }
            }

            // PaginationParameters
            APIPaginationParameters pagination = new APIPaginationParameters();
            pagination.offset = offsetIndex;
            pagination.limit = profileCount;

            // Send Request
            APIClient.GetAllMods(filter, pagination,
            (r) =>
            {
                if(this != null)
                {
                    this.AppendCachedPage(filter, r);
                }

                if(onSuccess != null)
                {
                    onSuccess(r);
                }
            }, onError);
        }

        /// <summary>Append the response page to the cached data.</summary>
        public virtual void AppendCachedPage(RequestFilter filter, RequestPage<ModProfile> page)
        {
            Debug.Assert(filter != null);
            Debug.Assert(page != null);

            string filterString = filter.GenerateFilterString();
            RequestPage<ModProfile> cachedPage = null;
            if(this.cachedRequests.TryGetValue(filterString, out cachedPage))
            {
                ModProfile[] cachedPageArray = cachedPage.items;
                int cachedPageArrayOffset = 0;

                int newPageResultOffset = cachedPage.resultOffset;
                int newPageArrayLength = cachedPage.items.Length;

                // check for page.resultOffset stretching to lower indicies
                if(page.resultOffset < newPageResultOffset)
                {
                    cachedPageArrayOffset = cachedPage.resultOffset - page.resultOffset;
                    newPageResultOffset = page.resultOffset;
                    newPageArrayLength = cachedPageArrayOffset + cachedPageArray.Length;
                }

                // check for page stretching to higher indicies
                // p.ro = 35, p.i.l = 10 : npro = 10, npal = 30
                if(page.resultOffset + page.items.Length > newPageResultOffset + newPageArrayLength)
                {
                    newPageArrayLength = (page.resultOffset - newPageResultOffset) + page.items.Length;
                }

                // create new array
                ModProfile[] newItemArray = new ModProfile[newPageArrayLength];
                Array.Copy(cachedPageArray, 0, newItemArray, cachedPageArrayOffset, cachedPageArray.Length);
                Array.Copy(page.items, 0, newItemArray, page.resultOffset - newPageResultOffset, page.items.Length);

                // update page
                cachedPage.items = newItemArray;
                cachedPage.size = newPageArrayLength;
                cachedPage.resultOffset = newPageResultOffset;
                // TODO(@jackson): CHECK THIS
                cachedPage.resultTotal = page.resultTotal;
            }
            else
            {
                this.cachedRequests.Add(filterString, page);
            }
        }
    }
}
