using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModIO
{
    public static class VersionUpdater
    {
        [System.Serializable]
        public struct GenericJSONObject
        {
            [JsonExtensionData]
            public IDictionary<string, JToken> data;
        }

        public static void Run(SimpleVersion lastRunVersion)
        {
            GenericJSONObject dataWrapper;

            if(lastRunVersion < new SimpleVersion(2, 1))
            {
                // - copy enabled/subbed -
                string filepath = IOUtilities.CombinePath(PluginSettings.data.cacheDirectory,
                                                          "mod_manager.data");

                if(IOUtilities.TryReadJsonObjectFile(filepath, out dataWrapper))
                {
                    int[] subscribedModIds = null;
                    if(dataWrapper.data.ContainsKey("subscribedModIds"))
                    {
                        JArray array = dataWrapper.data["subscribedModIds"] as JArray;
                        if(array != null)
                        {
                            subscribedModIds = array.ToObject<int[]>();
                        }
                    }
                    int[] enabledModIds = null;
                    if(dataWrapper.data.ContainsKey("enabledModIds"))
                    {
                        JArray array = dataWrapper.data["enabledModIds"] as JArray;
                        if(array != null)
                        {
                            enabledModIds = array.ToObject<int[]>();
                        }
                    }

                    UserAccountManagement.SetSubscribedMods(subscribedModIds);
                    UserAccountManagement.SetEnabledMods(enabledModIds);
                }
            }
        }
    }
}
