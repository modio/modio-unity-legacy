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

        // ---------[ ACCESSOR FUNCTIONS ]---------
        /// <summary>Attempts to retrieve a cached ModStatistics object.</summary>
        public ModStatistics GetForId(int modId)
        {
            ModStatistics stats = null;
            cache.TryGetValue(modId, out stats);
            return stats;
        }

        /// <summary>Stores a ModStatistics object in the cache.</summary>
        public void Store(IEnumerable<ModStatistics> statistics)
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
