using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModIO.UI
{
    /// <summary>A simple component for caching ModStatistics objects.</summary>
    public class ModStatisticsRequestManager : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        /// <summary>Should the cache be cleared on disable</summary>
        public bool clearCacheOnDisable = true;

        /// <summary>Cached ModStatistics to id map.</summary>
        public Dictionary<int, ModStatistics> cache = new Dictionary<int, ModStatistics>();

        /// <summary>Should the statistics be refetched if expired.</summary>
        public bool refetchIfExpired = true;

        // ---------[ INITIALIZATION ]---------
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

            // fetch missing profiles
            RequestFilter filter = new RequestFilter();
            filter.fieldFilters.Add(API.GetAllModStatsFilterFields.modId,
                new InArrayFilter<int>() { filterArray = missingIds.ToArray(), });

            APIClient.GetAllModStats(filter, null, (r) =>
            {
                if(this != null)
                {
                    foreach(ModStatistics stats in r.items)
                    {
                        this.cache[stats.modId] = stats;
                    }
                }

                foreach(ModStatistics stats in r.items)
                {
                    int i = orderedIdList.IndexOf(stats.modId);
                    if(i >= 0)
                    {
                        results[i] = stats;
                    }
                }

                onSuccess(results);

            },
            onError);
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

        /// <summary>A convenience function for checking if a stats object should be refetched.</summary>
        protected virtual bool IsValid(ModStatistics statistics)
        {
            return (statistics != null
                    && (!this.refetchIfExpired
                        || ServerTimeStamp.Now < statistics.dateExpires));
        }
    }
}
