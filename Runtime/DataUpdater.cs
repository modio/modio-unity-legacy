using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Debug = UnityEngine.Debug;

#pragma warning disable 0618 // Obsolete Detection
namespace ModIO
{
    /// <summary>Performs the operations necessary to update data from older versions of the
    /// plugin.</summary>
    public static class DataUpdater
    {
        /// <summary>Runs the update functionality depending on the lastRunVersion.</summary>
        public static void UpdateFromVersion(ModIOVersion lastRunVersion)
        {
            if(lastRunVersion < new ModIOVersion(2, 1))
            {
                Update_2_0_to_2_1_UserData();
            }
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

        // ---------[ 2019 ]---------
        /// <summary>Moves the data from the UserAuthenticationData and ModManager caches to
        /// UserAccountManagement.</summary>
        private static void Update_2_0_to_2_1_UserData()
        {
            Debug.Log("[mod.io] Attempting 2.0->2.1 UserData update.");

            // check if the file already exists
            byte[] fileData = null;

            UserDataStorage.ReadFile(LocalUser.FILENAME, (path, success, data) => fileData = data);

            if(fileData != null && fileData.Length > 0)
            {
                Debug.Log("[mod.io] Aborting UserData update. FileExists: \'" + LocalUser.FILENAME
                          + "\' [" + ValueFormatting.ByteCount(fileData.Length, null) + "]");
            }

            // update
            GenericJSONObject dataWrapper;
            LocalUser userData = new LocalUser();
            string filePath = null;

            // - copy enabled/subbed -
            filePath = ModManager.PERSISTENTDATA_FILEPATH;

            if(IOUtilities.TryReadJsonObjectFile(filePath, out dataWrapper))
            {
                int[] modIds = null;

                if(DataUpdater.TryGetArrayField(dataWrapper, "subscribedModIds", out modIds))
                {
                    userData.subscribedModIds = new List<int>(modIds);
                }

                if(DataUpdater.TryGetArrayField(dataWrapper, "enabledModIds", out modIds))
                {
                    userData.enabledModIds = new List<int>(modIds);
                }
            }

            // - copy queued subs/unsubs -
            filePath = IOUtilities.CombinePath(CacheClient.cacheDirectory,
                                               ModIO.UI.ModBrowser.MANIFEST_FILENAME);

            if(IOUtilities.TryReadJsonObjectFile(filePath, out dataWrapper))
            {
                List<int> modIds = null;

                if(DataUpdater.TryGetArrayField(dataWrapper, "queuedSubscribes", out modIds))
                {
                    userData.queuedSubscribes = new List<int>(modIds);
                }

                if(DataUpdater.TryGetArrayField(dataWrapper, "queuedUnsubscribes", out modIds))
                {
                    userData.queuedUnsubscribes = new List<int>(modIds);
                }
            }

            // - copy UAD -
            filePath = UserAuthenticationData.FILE_LOCATION;

            if(IOUtilities.TryReadJsonObjectFile(filePath, out dataWrapper))
            {
                // user profile
                int userId = UserProfile.NULL_ID;
                if(dataWrapper.data.ContainsKey("userId"))
                {
                    userId = (int)dataWrapper.data["userId"];
                }

                userData.profile = null;
                if(userId != UserProfile.NULL_ID)
                {
                    userData.profile = CacheClient.LoadUserProfile(userId);
                }

                // token data
                if(dataWrapper.data.ContainsKey("token"))
                {
                    userData.oAuthToken = (string)dataWrapper.data["token"];
                }
                if(dataWrapper.data.ContainsKey("wasTokenRejected"))
                {
                    userData.wasTokenRejected = (bool)dataWrapper.data["wasTokenRejected"];
                }

                // NOTE(@jackson): External Authentication is no longer saved to disk and is thus
                // ignored.

                IOUtilities.DeleteFile(filePath);
            }

            // - set and save -
            LocalUser.instance = userData;
            LocalUser.isLoaded = true;
            LocalUser.Save();

            Debug.Log("[mod.io] UserData updated completed.");
        }

        // ---------[ UTILITY ]---------
        /// <summary>Attempts to fetch an array-type field from the data-wrapper object.</summary>
        private static bool TryGetArrayField<T>(GenericJSONObject jsonObject, string fieldName,
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
#pragma warning restore 0618 // Obsolete Detection
