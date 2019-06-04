using System.Collections.Generic;
using UnityEngine;

namespace ModIO.UI
{
    public class ModStatisticsCache : MonoBehaviour
    {
        public Dictionary<int, ModStatistics> cache = new Dictionary<int, ModStatistics>();

        public ModStatistics GetForId(int modId)
        {
            ModStatistics stats = null;
            cache.TryGetValue(modId, out stats);
            return stats;
        }

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
