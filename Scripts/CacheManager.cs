// #define DO_NOT_LOAD_CACHE

using System;
using System.IO;

using UnityEngine;

namespace ModIO
{
    // TODO(@jackson): Remove after writing own store/load code
    [System.Serializable]
    public struct ResourceLocationMapping
    {
        public string[] urls;
        public string[] filePaths;
    }

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
        public static string userFilePath
        { get { return CacheManager._cacheDirectory + "user.data"; } }
        
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

        // ---------[ GAME PROFILE ]---------
        public static string gameProfileFilePath
        { get { return CacheManager._cacheDirectory + "gameProfile.data"; } }
        
        public static void GetGameProfile(Action<GameProfile> onSuccess,
                                          Action<WebRequestError> onError)
        {
            // - Attempt load from cache -
            try
            {
                if(File.Exists(gameProfileFilePath))
                {
                    GameProfile profile
                    = JsonUtility.FromJson<GameProfile>(File.ReadAllText(gameProfileFilePath));

                    onSuccess(profile);

                    return;
                }
            }
            catch(Exception e)
            {
                Debug.LogWarning("[mod.io] Failed to read game profile from " + gameProfileFilePath
                                 + "\n" + e.Message);
            }

            // - Fetch from Server -
            Action<API.GameObject> cacheGameProfile = (gameObject) =>
            {
                GameProfile profile = GameProfile.CreateFromGameObject(gameObject);

                try
                {
                    File.WriteAllText(gameProfileFilePath, JsonUtility.ToJson(profile));
                }
                catch(Exception e)
                {
                    Debug.LogError("[mod.io] Failed to write game profile to " + gameProfileFilePath
                                   + "\n" + e.Message);
                }

                onSuccess(profile);
            };

            API.Client.GetGame(cacheGameProfile, onError);
        }

        // ---------[ IMAGE MANAGEMENT ]---------
        public static string GenerateModLogoFilePath(int modId, ModLogoVersion version)
        {
            return (CacheManager._cacheDirectory
                    + "mod_logos/"
                    + modId + "/"
                    + version.ToString() + ".png");
        }


        // TODO(@jackson): Look at reconfiguring params
        public static void GetModLogo(ModProfile profile, ModLogoVersion version,
                                      Action<Texture2D> onSuccess,
                                      Action<WebRequestError> onError)
        {
            int modId = profile.id;

            // - Attempt load from cache -
            string logoFilePath = GenerateModLogoFilePath(modId, version);
            try
            {
                if(File.Exists(logoFilePath))
                {
                    Texture2D logoTexture = new Texture2D(0,0);
                    logoTexture.LoadImage(File.ReadAllBytes(logoFilePath));
                    onSuccess(logoTexture);
                    return;
                }
            }
            catch(Exception e)
            {
                Debug.LogWarning("[mod.io] Failed to read the mod logo from " + logoFilePath
                                 + "\n" + e.Message);
            }

            // - Fetch from Server -
            // GetModProfile(modId)
            DownloadAndSaveImageAsPNG(profile.logoLocator.GetVersionURL(version),
                                      GenerateModLogoFilePath(modId, version),
                                      onSuccess,
                                      onError);
        }

        // ---------[ FILE DOWNLOADING ]---------
        public static void DownloadAndSaveImageAsPNG(string serverURL,
                                                     string destinationFilePath,
                                                     Action<Texture2D> onSuccess,
                                                     Action<WebRequestError> onError)
        {
            Debug.Assert(Path.GetExtension(destinationFilePath).Equals(".png"),
                         "[mod.io] Images can only be saved in PNG format."
                         + "\n" + destinationFilePath
                         + " is an invalid file path.");

            var download = new TextureDownload();

            download.sourceURL = serverURL;
            download.OnCompleted += (d) =>
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath));
                    File.WriteAllBytes(destinationFilePath, download.texture.EncodeToPNG());
                }
                catch(Exception e)
                {
                    Debug.LogError("[mod.io] Failed to write image to " + destinationFilePath
                                   + "\n" + e.Message);
                }

                onSuccess(download.texture);
            };
            download.OnFailed += (d, e) =>
            {
                onError(e);
            };

            DownloadManager.StartDownload(download);
        }
    }
}