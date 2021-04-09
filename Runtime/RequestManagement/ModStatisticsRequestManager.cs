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
            ModManager.GetModProfile(modId,
            (profile) =>
            {
                if(onSuccess != null)
                {
                    onSuccess.Invoke(profile.statistics);
                }
            }, onError);
        }

        /// <summary>Requests a collection of ModStatistcs by id.</summary>
        public virtual void RequestModStatistics(IList<int> orderedIdList,
                                                 Action<ModStatistics[]> onSuccess,
                                                 Action<WebRequestError> onError)
        {
            ModManager.GetModProfiles(orderedIdList,
            (profiles) =>
            {
                // early outs
                if(onSuccess == null) { return; }
                if(profiles == null) { onSuccess.Invoke(null); }

                // collect stats objects
                ModStatistics[] retVal = new ModStatistics[profiles.Length];
                for(int i = 0; i < profiles.Length; ++i)
                {
                    ModStatistics s = null;
                    if(profiles[i] != null)
                    {
                        s = profiles[i].statistics;
                    }

                    retVal[i] = s;
                }

                onSuccess.Invoke(retVal);

            }, onError);
        }
    }
}
