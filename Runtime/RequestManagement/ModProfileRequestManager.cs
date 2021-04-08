using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModIO.UI
{
    /// <summary>Manages requests made for ModProfiles.</summary>
    public class ModProfileRequestManager : MonoBehaviour, IModSubscriptionsUpdateReceiver
    {
        // ---------[ SINGLETON ]---------
        /// <summary>Singleton instance.</summary>
        private static ModProfileRequestManager _instance = null;
        /// <summary>Singleton instance.</summary>
        public static ModProfileRequestManager instance
        {
            get
            {
                if(ModProfileRequestManager._instance == null)
                {
                    ModProfileRequestManager._instance = UIUtilities.FindComponentInAllScenes<ModProfileRequestManager>(true);

                    if(ModProfileRequestManager._instance == null)
                    {
                        GameObject go = new GameObject("Mod Profile Request Manager");
                        ModProfileRequestManager._instance = go.AddComponent<ModProfileRequestManager>();
                    }
                }

                return ModProfileRequestManager._instance;
            }
        }

        // ---------[ FIELDS ]---------
        /// <summary>Minimum profile count to request from the API.</summary>
        public int minimumFetchSize = APIPaginationParameters.LIMIT_MAX;

        // ---------[ INITIALIZATION ]---------
        protected virtual void Awake()
        {
            if(ModProfileRequestManager._instance == null)
            {
                ModProfileRequestManager._instance = this;
            }
            #if DEBUG
            else if(ModProfileRequestManager._instance != this)
            {
                Debug.LogWarning("[mod.io] Second instance of a ModProfileRequestManager"
                                 + " component enabled simultaneously."
                                 + " Only one instance of a ModProfileRequestManager"
                                 + " component should be active at a time.");
                this.enabled = false;
            }
            #endif
        }

        // ---------[ FUNCTIONALITY ]---------
        /// <summary>Fetches page of ModProfiles grabbing from the cache where possible.</summary>
        public virtual void FetchModProfilePage(RequestFilter filter, int resultOffset, int profileCount,
                                                Action<RequestPage<ModProfile>> onSuccess,
                                                Action<WebRequestError> onError)
        {
            Debug.Assert(this.minimumFetchSize <= APIPaginationParameters.LIMIT_MAX);

            // early out if onSuccess == null
            if(onSuccess == null) { return; }

            if(profileCount > APIPaginationParameters.LIMIT_MAX)
            {
                Debug.LogWarning("[mod.io] FetchModProfilePage has been called with a profileCount"
                                 + " larger than the APIPaginationParameters.LIMIT_MAX."
                                 + "\nAs such, results may not be as expected.");

                profileCount = APIPaginationParameters.LIMIT_MAX;
            }

            // ensure indicies are positive
            if(resultOffset < 0) { resultOffset = 0; }
            if(profileCount < 0) { profileCount = 0; }

            // setup request structures
            List<ModProfile> results = new List<ModProfile>(profileCount);

            // PaginationParameters
            APIPaginationParameters pagination = new APIPaginationParameters();

            int pageIndex = resultOffset / this.minimumFetchSize;
            pagination.offset = pageIndex * this.minimumFetchSize;
            pagination.limit = this.minimumFetchSize;

            APIClient.GetAllMods(filter, pagination,
            (r01) =>
            {
                int pageOffset = resultOffset % this.minimumFetchSize;

                for(int i = pageOffset;
                    i < r01.items.Length
                    && i < pageOffset + profileCount;
                    ++i)
                {
                    results.Add(r01.items[i]);
                }

                if(pageOffset + profileCount > r01.size
                   && r01.items.Length == r01.size)
                {
                    pagination.offset += pagination.limit;
                    APIClient.GetAllMods(filter, pagination,
                    (r02) =>
                    {
                        for(int i = 0;
                            i < r02.items.Length
                            && i < pageOffset + profileCount - r02.size;
                            ++i)
                        {
                            results.Add(r02.items[i]);
                            OnModsReceived(resultOffset, profileCount, r02.resultTotal,
                                           results, onSuccess);
                        }
                    }, onError);
                }
                else
                {
                    OnModsReceived(resultOffset, profileCount, r01.resultTotal,
                                   results, onSuccess);
                }
            }, onError);
        }

        /// <summary>Processes the received mod collection and generates a request page.</summary>
        private void OnModsReceived(int resultOffset, int pageSize, int resultTotal,
                                    List<ModProfile> results,
                                    Action<RequestPage<ModProfile>> onSuccess)
        {
            if(onSuccess == null) { return; }

            this.CacheModProfiles(results);

            RequestPage<ModProfile> page = new RequestPage<ModProfile>()
            {
                size = pageSize,
                resultOffset = resultOffset,
                resultTotal = resultTotal,
                items = results.ToArray(),
            };

            Debug.Log("RequestPage Generated:"
                      + "\n.size=" + page.size.ToString()
                      + "\n.resultOffset=" + page.resultOffset.ToString()
                      + "\n.resultTotal=" + page.resultTotal.ToString()
                      + "\n.items.Length=" + page.items.Length.ToString());

            onSuccess.Invoke(page);
        }

        /// <summary>Updates the cache - both on disk and in this object.</summary>
        public virtual void CacheModProfiles(IEnumerable<ModProfile> modProfiles)
        {
            if(modProfiles == null) { return; }

            // store
                IList<int> subMods = LocalUser.SubscribedModIds;
                foreach(ModProfile profile in modProfiles)
                {
                    if(profile != null
                       && subMods.Contains(profile.id))
                    {
                        CacheClient.SaveModProfile(profile, null);
                    }
                }
        }

        /// <summary>Requests an individual ModProfile by id.</summary>
        public virtual void RequestModProfile(int id,
                                              Action<ModProfile> onSuccess,
                                              Action<WebRequestError> onError)
        {
            ModManager.GetModProfile(id, onSuccess, onError);
        }

        /// <summary>Requests a collection of ModProfiles by id.</summary>
        public virtual void RequestModProfiles(IList<int> orderedIdList,
                                               Action<ModProfile[]> onSuccess,
                                               Action<WebRequestError> onError)
        {
            Debug.Assert(orderedIdList != null);
            Debug.Assert(onSuccess != null);

            ModProfile[] results = new ModProfile[orderedIdList.Count];
            List<int> missingIds = new List<int>(orderedIdList.Count);

            // grab from cache
            for(int i = 0; i < orderedIdList.Count; ++i)
            {
                int modId = orderedIdList[i];
                ModProfile profile = null;

                if(profile == null)
                {
                    missingIds.Add(modId);
                }
            }

            CacheClient.RequestFilteredModProfiles(missingIds, (cachedProfiles) =>
            {
                // check disk for any missing profiles
                foreach(ModProfile profile in cachedProfiles)
                {
                    int index = orderedIdList.IndexOf(profile.id);
                    if(index >= 0 && results[index] == null)
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
                Action<List<ModProfile>> onFetchProfiles = (modProfiles) =>
                {
                    if(this != null)
                    {
                        this.CacheModProfiles(modProfiles);
                    }

                    foreach(ModProfile profile in modProfiles)
                    {
                        int i = orderedIdList.IndexOf(profile.id);
                        if(i >= 0)
                        {
                            results[i] = profile;
                        }
                    }

                    onSuccess(results);
                };

                this.StartCoroutine(this.FetchAllModProfiles(missingIds.ToArray(),
                                                             onFetchProfiles,
                                                             onError));
            });
        }

        // ---------[ UTILITY ]---------
        /// <summary>Recursively fetches all of the mod profiles in the array.</summary>
        protected System.Collections.IEnumerator FetchAllModProfiles(int[] modIds,
                                                                     Action<List<ModProfile>> onSuccess,
                                                                     Action<WebRequestError> onError)
        {
            List<ModProfile> modProfiles = new List<ModProfile>();

            // pagination
            APIPaginationParameters pagination = new APIPaginationParameters()
            {
                limit = APIPaginationParameters.LIMIT_MAX,
                offset = 0,
            };

            // filter
            RequestFilter filter = new RequestFilter();
            filter.AddFieldFilter(API.GetAllModsFilterFields.id,
                new InArrayFilter<int>() { filterArray = modIds, });

            bool isDone = false;

            while(!isDone)
            {
                RequestPage<ModProfile> page = null;
                WebRequestError error = null;

                APIClient.GetAllMods(filter, pagination,
                                     (r) => page = r,
                                     (e) => error = e);

                while(page == null && error == null) { yield return null;}

                if(error != null)
                {
                    if(onError != null)
                    {
                        onError(error);
                    }

                    modProfiles = null;
                    isDone = true;
                }
                else
                {
                    modProfiles.AddRange(page.items);

                    if(page.resultTotal <= (page.resultOffset + page.size))
                    {
                        isDone = true;
                    }
                    else
                    {
                        pagination.offset = page.resultOffset + page.size;
                    }
                }
            }

            if(isDone && modProfiles != null)
            {
                onSuccess(modProfiles);
            }
        }

        protected static RequestPage<ModProfile> CreatePageSubset(RequestPage<ModProfile> sourcePage,
                                                                  int resultOffset,
                                                                  int profileCount)
        {
            Debug.Assert(sourcePage != null);

            RequestPage<ModProfile> subPage = new RequestPage<ModProfile>()
            {
                size = profileCount,
                resultOffset = resultOffset,
                resultTotal = sourcePage.resultTotal,
            };

            if(subPage.resultOffset < sourcePage.resultOffset)
            {
                int difference = sourcePage.resultOffset = subPage.resultOffset;

                subPage.resultOffset = sourcePage.resultOffset;
                subPage.size -= difference;
            }

            // early out for 0
            if(subPage.size <= 0)
            {
                subPage.size = 0;
                subPage.items = new ModProfile[0];
                return subPage;
            }

            int sourcePageOffset = subPage.resultOffset - sourcePage.resultOffset;

            int subPageItemCount = subPage.size;
            if(sourcePageOffset + subPageItemCount > sourcePage.items.Length)
            {
                subPageItemCount = sourcePage.items.Length - sourcePageOffset;
            }

            subPage.items = new ModProfile[subPageItemCount];

            Array.Copy(sourcePage.items, sourcePageOffset,
                       subPage.items, 0,
                       subPage.items.Length);

            return subPage;
        }

        protected ModProfile[] PullProfilesFromCache(IList<int> modIds)
        {
            Debug.Assert(modIds != null);

            ModProfile[] result = new ModProfile[modIds.Count];

            for(int i = 0; i < result.Length; ++i)
            {
                ModProfile profile = null;
                result[i] = profile;
            }

            return result;
        }

        // ---------[ EVENTS ]---------
        /// <summary>Stores any cached profiles when the mod subscriptions are updated.</summary>
        public void OnModSubscriptionsUpdated(IList<int> addedSubscriptions,
                                              IList<int> removedSubscriptions)
        {
            if(addedSubscriptions.Count > 0)
            {
                foreach(int modId in addedSubscriptions)
                {
                }
            }
        }
    }
}
