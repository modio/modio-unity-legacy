using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModIO.UI
{
    /// <summary>A simple component for caching ModStatistics objects.</summary>
    public class ModStatisticsRequestManager : MonoBehaviour
    {
        // ---------[ SINGLETON ]---------
        /// <summary>Singleton instance.</summary>
        private static ModStatisticsRequestManager _instance = null;
        /// <summary>Singleton instance.</summary>
        public static ModStatisticsRequestManager instance
        {
            get
            {
                if(ModStatisticsRequestManager._instance == null)
                {
                    ModStatisticsRequestManager._instance = UIUtilities.FindComponentInAllScenes<ModStatisticsRequestManager>(true);

                    if(ModStatisticsRequestManager._instance == null)
                    {
                        GameObject go = new GameObject("Mod Statistics Request Manager");
                        ModStatisticsRequestManager._instance = go.AddComponent<ModStatisticsRequestManager>();
                    }
                }

                return ModStatisticsRequestManager._instance;
            }
        }

        // ---------[ FIELDS ]---------
        /// <summary>Should the cache be cleared on disable</summary>
        public bool clearCacheOnDisable = true;

        /// <summary>Cached ModStatistics to id map.</summary>
        public Dictionary<int, ModStatistics> cache = new Dictionary<int, ModStatistics>();

        /// <summary>Should the statistics be refetched if expired.</summary>
        public bool refetchIfExpired = true;

        // ---------[ INITIALIZATION ]---------
        protected virtual void Awake()
        {
            if(ModStatisticsRequestManager._instance == null)
            {
                ModStatisticsRequestManager._instance = this;
            }
            #if DEBUG
            else if(ModStatisticsRequestManager._instance != this)
            {
                Debug.LogWarning("[mod.io] Second instance of a ModStatisticsRequestManager"
                                 + " component enabled simultaneously."
                                 + " Only one instance of a ModStatisticsRequestManager"
                                 + " component should be active at a time.");
                this.enabled = false;
            }
            #endif
        }

        protected virtual void OnDisable()
        {
            if(this.clearCacheOnDisable)
            {
                this.cache.Clear();
            }
        }

        // ---------[ ACCESSOR FUNCTIONS ]---------
        /// <summary>Requests an individual ModStatistics by id.</summary>
        public virtual void RequestModStatistics(int modId,
                                                 Action<ModStatistics> onSuccess,
                                                 Action<WebRequestError> onError)
        {
            ModStatistics stats = null;

            if(!this.cache.TryGetValue(modId, out stats))
            {
                stats = CacheClient.LoadModStatistics(modId);
                this.cache.Add(modId, stats);
            }

            if(this.IsValid(stats))
            {
                onSuccess(stats);
            }
            else
            {
                APIClient.GetModStats(modId, (s) =>
                {
                    if(this != null)
                    {
                        this.cache[modId] = s;
                    }

                    onSuccess(s);
                },
                onError);
            }
        }

        /// <summary>Requests a collection of ModStatistcs by id.</summary>
        public virtual void RequestModStatistics(IList<int> orderedIdList,
                                                 Action<ModStatistics[]> onSuccess,
                                                 Action<WebRequestError> onError)
        {
            ModStatistics[] results = new ModStatistics[orderedIdList.Count];
            List<int> missingIds = new List<int>(orderedIdList.Count);

            // grab from cache
            for(int i = 0; i < orderedIdList.Count; ++i)
            {
                int modId = orderedIdList[i];
                ModStatistics stats = null;
                this.cache.TryGetValue(modId, out stats);
                results[i] = stats;

                if(!this.IsValid(stats))
                {
                    missingIds.Add(modId);
                }
            }

            // check disk for any missing stats
            int missingIndex = 0;
            while(missingIndex < missingIds.Count)
            {
                int id = missingIds[missingIndex];
                ModStatistics stats = CacheClient.LoadModStatistics(id);

                if(this.IsValid(stats))
                {
                    int resultIndex = orderedIdList.IndexOf(id);
                    results[resultIndex] = stats;
                    missingIds.RemoveAt(missingIndex);
                }
                else
                {
                    ++missingIndex;
                }
            }

            // if no missing profiles, early out
            if(missingIds.Count == 0)
            {
                onSuccess(results);
                return;
            }

            // fetch missing statistics
            Action<List<ModStatistics>> onFetchStats = (modStatistics) =>
            {
                if(this != null)
                {
                    foreach(ModStatistics stats in modStatistics)
                    {
                        this.cache[stats.modId] = stats;
                    }
                }

                foreach(ModStatistics stats in modStatistics)
                {
                    int i = orderedIdList.IndexOf(stats.modId);
                    if(i >= 0)
                    {
                        results[i] = stats;
                    }
                }

                onSuccess(results);
            };

            this.StartCoroutine(this.FetchAllModStatistics(missingIds.ToArray(), onFetchStats, onError));
        }

        /// <summary>Returns the a ModStatistics if the object is cached and valid.</summary>
        public virtual ModStatistics TryGetValid(int modId)
        {
            ModStatistics stats = null;
            this.cache.TryGetValue(modId, out stats);

            if(this.IsValid(stats))
            {
                return stats;
            }
            else
            {
                return null;
            }
        }

        // ---------[ UTILITY ]---------
        /// <summary>A convenience function for checking if a stats object should be refetched.</summary>
        protected virtual bool IsValid(ModStatistics statistics)
        {
            return (statistics != null
                    && (!this.refetchIfExpired
                        || ServerTimeStamp.Now < statistics.dateExpires));
        }

        /// <summary>Recursively fetches all of the mod statistics in the array.</summary>
        protected System.Collections.IEnumerator FetchAllModStatistics(int[] modIds,
                                                                       Action<List<ModStatistics>> onSuccess,
                                                                       Action<WebRequestError> onError)
        {
            List<ModStatistics> modProfiles = new List<ModStatistics>();

            APIPaginationParameters pagination = new APIPaginationParameters()
            {
                limit = APIPaginationParameters.LIMIT_MAX,
                offset = 0,
            };
            RequestFilter filter = new RequestFilter();
            filter.fieldFilters.Add(API.GetAllModStatsFilterFields.modId,
                new InArrayFilter<int>() { filterArray = modIds });

            bool isDone = false;

            while(!isDone)
            {
                RequestPage<ModStatistics> page = null;
                WebRequestError error = null;

                APIClient.GetAllModStats(filter, pagination,
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
    }
}
