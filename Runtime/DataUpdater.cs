using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Debug = UnityEngine.Debug;

namespace ModIO
{
    /// <summary>Performs the operations necessary to update data from older versions of the plugin.</summary>
    public static class DataUpdater
    {
        /// <summary>Runs the update functionality depending on the lastRunVersion.</summary>
        public static void UpdateFromVersion(ModIOVersion lastRunVersion)
        {
        }

        /// <summary>Generic object wrapper for retrieving JSON Data from files.</summary>
        [System.Serializable]
        private struct GenericJSONObject
        {
            #pragma warning disable 0649 // Never assigned to

            [JsonExtensionData]
            public IDictionary<string, JToken> data;

            #pragma warning restore 0649 // Never assigned to
        }

        // ---------[ UTILITY ]---------
        /// <summary>Attempts to fetch an array-type field from the data-wrapper object.</summary>
        private static bool TryGetArrayField<T>(GenericJSONObject jsonObject,
                                                string fieldName,
                                                out T fieldData)
        {
            fieldData = default(T);

            JArray jArray;

            if(jsonObject.data.ContainsKey(fieldName)
               && (jArray = jsonObject.data[fieldName] as JArray) != null)
            {
                fieldData = jArray.ToObject<T>();
                return true;
            }

            return false;
        }
    }
}
