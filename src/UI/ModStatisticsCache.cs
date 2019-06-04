using System.Collections.Generic;
using UnityEngine;

namespace ModIO.UI
{
    /// <summary>A simple component for caching ModStatistics objects.</summary>
    public class ModStatisticsCache : MonoBehaviour
    {
        // ---------[ FIELDS ]---------
        /// <summary>Cached ModStatistics to id map.</summary>
        public Dictionary<int, ModStatistics> cache = new Dictionary<int, ModStatistics>();

        /// <summary>Should GetForId return null if the ModStatistics object is expired.</summary>
        public bool returnNullIfExpired = false;

        // ---------[ ACCESSOR FUNCTIONS ]---------
        /// <summary>Attempts to retrieve a cached ModStatistics object.</summary>
        public virtual ModStatistics GetForId(int modId)
        {
            ModStatistics stats = null;

            if(!cache.TryGetValue(modId, out stats))
            {
                cache[modId] = CacheClient.LoadModStatistics(modId);
            }

            if(returnNullIfExpired
               && stats != null
               && stats.dateExpires < ServerTimeStamp.Now)
            {
                stats = null;
            }

            return stats;
        }

        /// <summary>Stores a ModStatistics object in the cache.</summary>
        public virtual void Store(IEnumerable<ModStatistics> statistics)
        {
            foreach(ModStatistics statData in statistics)
            {
                if(statData != null)
                {
                    cache[statData.modId] = statData;
                }
            }
        }
    }
}
