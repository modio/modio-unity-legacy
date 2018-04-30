// #define DO_NOT_LOAD_CACHE

using System;
using System.IO;

using UnityEngine;

namespace ModIO
{
    public static class CacheManager
    {
        // ---------[ MEMBERS ]---------
        private static string _cacheDirectory = null;

        // ---------[ INITIALIZATION ]---------
        // TODO(@jackson): Sort Initialization interface/timing
        // public static void Initialize()
        static CacheManager()
        {
            string dir;
            #pragma warning disable 0162
            #if DEBUG
            if(GlobalSettings.USE_TEST_SERVER)
            {
                dir = Application.persistentDataPath + "/modio_testServer/";
            }
            else
            #endif
            {
                dir = Application.persistentDataPath + "/modio/";
            }
            #pragma warning restore 0162

            TrySetCacheDirectory(dir);
        }

        public static bool TrySetCacheDirectory(string directory)
        {
            try
            {
                Directory.CreateDirectory(directory);
            }
            catch(Exception e)
            {
                Debug.LogWarning("[mod.io] Failed to set cache directory to " + directory
                                 + "\n" + e.Message);
                return false;
            }

            CacheManager._cacheDirectory = directory;
            return true;
        }
        public static string GetCacheDirectory()
        {
            return CacheManager._cacheDirectory;
        }

        // ---------[ USER MANAGEMENT ]---------
        private static string userFilePath { get { return CacheManager._cacheDirectory + "user.data"; } }
        public static void StoreAuthenticatedUser(AuthenticatedUser user)
        {
            try
            {
                File.WriteAllText(userFilePath, JsonUtility.ToJson(user));
            }
            catch(Exception e)
            {
                Debug.LogError("[mod.io] Failed to write user data save file.\n"
                               + e.Message);
            }
        }

        public static AuthenticatedUser LoadAuthenticatedUser()
        {
            AuthenticatedUser user = null;
            try
            {
                if(File.Exists(userFilePath))
                {
                    user = JsonUtility.FromJson<AuthenticatedUser>(File.ReadAllText(userFilePath));
                }
            }
            catch(Exception e)
            {
                user = null;

                Debug.LogWarning("[mod.io] Unable to read user data save file.\n"
                                 + e.Message);
            }
            return user;
        }

        public static void ClearAuthenticatedUser()
        {
            try
            {
                if(File.Exists(userFilePath)) { File.Delete(userFilePath); }
            }
            catch(Exception e)
            {
                Debug.LogWarning("[mod.io] Failed to delete user data save file.\n"
                                 + e.Message);
            }
        }

    }
}