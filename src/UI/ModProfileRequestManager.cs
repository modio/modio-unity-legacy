using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Manages requests made for ModProfiles.</summary>
    public class ModProfileRequestManager : MonoBehaviour
    {
        // ---------[ NESTED DATA-TYPES ]--------
        public struct RequestPageData
        {
            public int resultOffset;
            public int resultTotal;
            public int[] modIds;

            /// <summary>Appends a collection of ids to a RequestPageData.</summary>
            public static RequestPageData Append(RequestPageData pageData,
                                                 int appendCollectionOffset,
                                                 int[] appendCollection)
            {
                if(appendCollection == null
                   || appendCollection.Length == 0)
                {
                    return pageData;
                }

                // asserts
                Debug.Assert(appendCollectionOffset >= 0);
                Debug.Assert(appendCollectionOffset + appendCollection.Length <= pageData.resultTotal);

                // calc last indicies
                int newOffset = (appendCollectionOffset < pageData.resultOffset
                                 ? appendCollectionOffset
                                 : pageData.resultOffset);

                int oldLastIndex = pageData.modIds.Length + pageData.resultOffset - 1;
                int appendingLastIndex = appendCollection.Length + appendCollectionOffset - 1;

                int newLastIndex = (appendingLastIndex > oldLastIndex
                                    ? appendingLastIndex
                                    : oldLastIndex);

                // fill array
                int[] newArray = new int[newLastIndex - newOffset + 1];
                for(int i = 0; i < newArray.Length; ++i)
                {
                    newArray[i] = ModProfile.NULL_ID;
                }

                Array.Copy(pageData.modIds, 0,
                           newArray, pageData.resultOffset - newOffset,
                           pageData.modIds.Length);
                Array.Copy(appendCollection, 0,
                           newArray, appendCollectionOffset - newOffset,
                           appendCollection.Length);

                // Create appended page data
                RequestPageData retData = new RequestPageData()
                {
                    resultOffset = newOffset,
                    resultTotal = pageData.resultTotal,
                    modIds = newArray,
                };

                return retData;
            }
        }

        // ---------[ FIELDS ]---------
        /// <summary>Should the cache be cleared on disable</summary>
        public bool clearCacheOnDisable = true;

        /// <summary>Cached requests.</summary>
        public Dictionary<string, RequestPageData> requestCache = new Dictionary<string, RequestPageData>();

        /// <summary>Cached profiles.</summary>
        public Dictionary<int, ModProfile> profileCache = new Dictionary<int, ModProfile>()
        {
            { ModProfile.NULL_ID, null },
        };

        // ---------[ INITIALIZATION ]---------
        protected virtual void OnDisable()
        {
            if(this.clearCacheOnDisable)
            {
                this.requestCache.Clear();
                this.profileCache.Clear();
                this.profileCache.Add(ModProfile.NULL_ID, null);
            }
        }

        // ---------[ FUNCTIONALITY ]---------
        /// <summary>Fetches page of ModProfiles grabbing from the cache where possible.</summary>
        public virtual void FetchModProfilePage(RequestFilter filter, int offsetIndex, int profileCount,
                                                Action<RequestPage<ModProfile>> onSuccess,
                                                Action<WebRequestError> onError)
        {
            Debug.Assert(onSuccess != null);

            // ensure indicies are positive
            if(offsetIndex < 0) { offsetIndex = 0; }
            if(profileCount < 0) { profileCount = 0; }

            // check if results already cached
            string filterString = filter.GenerateFilterString();
            RequestPageData cachedData;
            if(this.requestCache.TryGetValue(filterString, out cachedData))
            {
                // early out if no results or index beyond resultTotal
                if(offsetIndex >= cachedData.resultTotal || profileCount == 0)
                {

                    RequestPage<ModProfile> requestPage = new RequestPage<ModProfile>();
                    requestPage.size = profileCount;
                    requestPage.resultOffset = offsetIndex;
                    requestPage.resultTotal = cachedData.resultTotal;
                    requestPage.items = new ModProfile[0];

                    onSuccess(requestPage);

                    return;
                }

                // clamp last index
                int clampedLastIndex = offsetIndex + profileCount-1;
                if(clampedLastIndex >= cachedData.resultTotal)
                {
                    // NOTE(@jackson): cachedData.resultTotal > 0
                    clampedLastIndex = cachedData.resultTotal - 1;
                }

                // check if entire result set encompassed by cache
                int cachedLastIndex = cachedData.resultOffset + cachedData.modIds.Length;
                if(cachedData.resultOffset <= offsetIndex
                   && clampedLastIndex <= cachedLastIndex)
                {
                    ModProfile[] resultArray = new ModProfile[clampedLastIndex - offsetIndex + 1];

                    // copy values across
                    bool nullFound = false;
                    for(int cacheIndex = offsetIndex - cachedData.resultOffset;
                        cacheIndex < cachedData.modIds.Length
                        && cacheIndex <= clampedLastIndex - cachedData.resultOffset
                        && !nullFound;
                        ++cacheIndex)
                    {
                        int modId = cachedData.modIds[cacheIndex];
                        ModProfile profile = null;
                        profileCache.TryGetValue(modId, out profile);

                        int arrayIndex = cacheIndex - offsetIndex;
                        resultArray[arrayIndex] = profile;

                        nullFound = (profile == null);
                    }

                    // return if no nulls found
                    if(!nullFound)
                    {
                        RequestPage<ModProfile> requestPage = new RequestPage<ModProfile>();
                        requestPage.size = profileCount;
                        requestPage.resultOffset = offsetIndex;
                        requestPage.resultTotal = cachedData.resultTotal;
                        requestPage.items = resultArray;

                        onSuccess(requestPage);
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
                    this.CacheRequestPage(filter, r);
                }

                if(onSuccess != null)
                {
                    onSuccess(r);
                }
            }, onError);
        }

        /// <summary>Append the response page to the cached data.</summary>
        public virtual void CacheRequestPage(RequestFilter filter, RequestPage<ModProfile> page)
        {
            // asserts
            Debug.Assert(filter != null);
            Debug.Assert(page != null);

            // cache request
            string filterString = filter.GenerateFilterString();
            RequestPageData cachedData;
            if(this.requestCache.TryGetValue(filterString, out cachedData))
            {
                cachedData.resultTotal = page.resultTotal;

                this.requestCache[filterString] = RequestPageData.Append(cachedData,
                                                                         page.resultOffset,
                                                                         Utility.MapProfileIds(page.items));
            }
            else
            {
                cachedData = new RequestPageData()
                {
                    resultOffset = page.resultOffset,
                    resultTotal = page.resultTotal,
                    modIds = Utility.MapProfileIds(page.items),
                };

                this.requestCache.Add(filterString, cachedData);
            }

            // cache profiles
            foreach(ModProfile profile in page.items)
            {
                this.profileCache[profile.id] = profile;
            }
        }

        /// <summary>Requests an individual ModProfile by id.</summary>
        public virtual void RequestModProfile(int id,
                                              Action<ModProfile> onSuccess, Action<WebRequestError> onError)
        {
            ModProfile profile = null;
            if(profileCache.TryGetValue(id, out profile))
            {
                onSuccess(profile);
                return;
            }

            profile = CacheClient.LoadModProfile(id);
            if(profile != null)
            {
                profileCache.Add(id, profile);
                onSuccess(profile);
                return;
            }

            APIClient.GetMod(id, (p) =>
            {
                if(this != null)
                {
                    profileCache.Add(id, p);
                }

                onSuccess(p);
            },
            onError);
        }

        /// <summary>Requests a collection of ModProfiles by id.</summary>
        public virtual void RequestModProfiles(IList<int> orderedIdList,
                                               Action<ModProfile[]> onSuccess,
                                               Action<WebRequestError> onError)
        {
            ModProfile[] results = new ModProfile[orderedIdList.Count];
            List<int> missingIds = new List<int>(orderedIdList.Count);

            // grab from cache
            for(int i = 0; i < orderedIdList.Count; ++i)
            {
                int modId = orderedIdList[i];
                ModProfile profile = null;
                this.profileCache.TryGetValue(modId, out profile);
                results[i] = profile;

                if(profile == null)
                {
                    missingIds.Add(modId);
                }
            }

            // check disk for any missing profiles
            foreach(ModProfile profile in CacheClient.IterateFilteredModProfiles(missingIds))
            {
                int index = orderedIdList.IndexOf(profile.id);
                if(index >= 0)
                {
                    results[index] = profile;
                }

                missingIds.Remove(profile.id);
            }

            // if no missing profiles, early out
            if(missingIds.Count == 0)
            {
                onSuccess(results);
                return;
            }

            // fetch missing profiles
            RequestFilter filter = new RequestFilter();
            filter.fieldFilters.Add(API.GetAllModsFilterFields.id,
                new InArrayFilter<int>() { filterArray = missingIds.ToArray(), });

            APIClient.GetAllMods(filter, null, (r) =>
            {
                if(this != null)
                {
                    foreach(ModProfile profile in r.items)
                    {
                        this.profileCache[profile.id] = profile;
                    }
                }

                foreach(ModProfile profile in r.items)
                {
                    int i = orderedIdList.IndexOf(profile.id);
                    if(i >= 0)
                    {
                        results[i] = profile;
                    }
                }

                onSuccess(results);
            },
            onError);
        }
    }
}
