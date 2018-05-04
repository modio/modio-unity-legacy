// #define DO_NOT_LOAD_CACHE

using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;

using Newtonsoft.Json;

using ModIO.API;

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

        // ---------[ BASIC FILE I/O ]---------
        public static T ReadJsonObjectFile<T>(string filePath)
        {
            if(File.Exists(filePath))
            {
                try
                {
                    return JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
                }
                catch(Exception e)
                {
                    Debug.LogWarning("[mod.io] Failed to read json object from file " + filePath
                                     + "\nError: " + e.Message);
                }
            }
            return default(T);
        }

        public static void WriteJsonObjectFile<T>(string filePath,
                                                  T jsonObject)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, JsonConvert.SerializeObject(jsonObject));
            }
            catch(Exception e)
            {
                Debug.LogWarning("[mod.io] Failed to write json object to file " + filePath
                                 + "\nError: " + e.Message);
            }
        }

        public static Texture2D ReadImageFile(string filePath)
        {
            Texture2D texture = null;

            if(File.Exists(filePath))
            {
                try
                {
                    texture = new Texture2D(0,0);
                    texture.LoadImage(File.ReadAllBytes(filePath));
                }
                catch(Exception e)
                {
                    Debug.LogWarning("[mod.io] Failed to read image file " + filePath
                                     + "\nError: " + e.Message);
                    texture = null;
                }
            }
            return texture;
        }

        public static void WriteImageFile(string filePath,
                                          Texture2D texture)
        {
            Debug.Assert(Path.GetExtension(filePath).Equals(".png"),
                         "[mod.io] Images can only be saved in PNG format."
                         + "\n" + filePath
                         + " is an invalid file path.");

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllBytes(filePath, texture.EncodeToPNG());
            }
            catch(Exception e)
            {
                Debug.LogWarning("[mod.io] Failed to write image file " + filePath
                                 + "\nError: " + e.Message);
            }
        }

        // ---------[ USER MANAGEMENT ]---------
        public static string userFilePath
        { get { return CacheManager._cacheDirectory + "user.data"; } }
        
        public static void StoreAuthenticatedUser(AuthenticatedUser user)
        {
            CacheManager.WriteJsonObjectFile(userFilePath, user);
        }

        public static AuthenticatedUser LoadAuthenticatedUser()
        {
            AuthenticatedUser user
            = CacheManager.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);
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
            GameProfile profile
            = CacheManager.ReadJsonObjectFile<GameProfile>(gameProfileFilePath);

            if(profile != null)
            {
                onSuccess(profile);
            }
            else
            {
                // - Fetch from Server -
                Action<API.GameObject> cacheGameProfile = (gameObject) =>
                {
                    profile = GameProfile.CreateFromGameObject(gameObject);
                    CacheManager.WriteJsonObjectFile(gameProfileFilePath, profile);
                    onSuccess(profile);
                };

                Client.GetGame(cacheGameProfile, onError);
            }
        }

        // ---------[ MOD PROFILES ]---------
        public static string GenerateModProfileFilePath(int modId)
        {
            return (CacheManager._cacheDirectory
                    + "mod_profiles/"
                    + modId + ".data");
        }

        public static void GetModProfile(int modId,
                                         Action<ModProfile> onSuccess,
                                         Action<WebRequestError> onError)
        {
            string profileFilePath = GenerateModProfileFilePath(modId);
            ModProfile profile = CacheManager.ReadJsonObjectFile<ModProfile>(profileFilePath);
            if(profile != null)
            {
                onSuccess(profile);
            }
            else
            {
                // - Fetch from Server -
                Action<ModObject> cacheModProfile = (modObject) =>
                {
                    profile = ModProfile.CreateFromModObject(modObject);
                    CacheManager.WriteJsonObjectFile(profileFilePath, profile);
                    onSuccess(profile);
                };

                Client.GetMod(modId,
                                  cacheModProfile,
                                  onError);
            }
        }
        
        public static IEnumerable<ModProfile> LoadAllModProfiles()
        {
            string profileDirectory = CacheManager._cacheDirectory + "mod_profiles/";
            
            if(Directory.Exists(profileDirectory))
            {
                string[] profilePaths;
                try
                {
                    profilePaths = Directory.GetFiles(profileDirectory,
                                                      "*.data");
                }
                catch(Exception e)
                {
                    Debug.LogWarning("[mod.io] Failed to read mod profile directory " + profileDirectory
                                     + "\nError: " + e.Message);

                    profilePaths = new string[0];
                }

                foreach(string filePath in profilePaths)
                {
                    ModProfile profile = CacheManager.ReadJsonObjectFile<ModProfile>(filePath);
                    if(profile != null)
                    {
                        yield return profile;
                    }
                }
            }
        }

        // ---------[ IMAGE MANAGEMENT ]---------
        public static string GenerateModLogoFilePath(int modId, ModLogoVersion version)
        {
            return (CacheManager._cacheDirectory
                    + "mod_logos/"
                    + modId + "/"
                    + version.ToString() + ".png");
        }
        public static string GenerateModGalleryImageFilePath(int modId,
                                                             string imageFileName,
                                                             ModGalleryImageVersion version)
        {
            return (Application.temporaryCachePath
                    + "/mod_images/"
                    + modId + "/"
                    + version.ToString() + "/"
                    + Path.GetFileNameWithoutExtension(imageFileName) +
                    ".png");
        }

        public static void GetModLogo(ModObject modObject, ModLogoVersion version,
                                      Action<Texture2D> onSuccess,
                                      Action<WebRequestError> onError)
        {
            CacheManager.GetModLogo(ModProfile.CreateFromModObject(modObject),
                                    version, onSuccess, onError);
        }

        // TODO(@jackson): Look at reconfiguring params
        public static void GetModLogo(ModProfile profile, ModLogoVersion version,
                                      Action<Texture2D> onSuccess,
                                      Action<WebRequestError> onError)
        {
            int modId = profile.id;

            // - Attempt load from cache -
            string logoFilePath = CacheManager.GenerateModLogoFilePath(modId, version);
            Texture2D logoTexture = CacheManager.ReadImageFile(logoFilePath);
            
            if(logoTexture != null)
            {
                onSuccess(logoTexture);
            }
            else
            {
                // - Fetch from Server -
                // GetModProfile(modId)
                DownloadAndSaveImageAsPNG(profile.logoLocator.GetVersionURL(version),
                                          logoFilePath,
                                          onSuccess,
                                          onError);
            }
        }

        public static void GetModGalleryImage(ModProfile profile,
                                              string imageFileName,
                                              ModGalleryImageVersion version,
                                              Action<Texture2D> onSuccess,
                                              Action<WebRequestError> onError)
        {
            int modId = profile.id;

            // - Attempt load from cache -
            string imageFilePath = CacheManager.GenerateModGalleryImageFilePath(modId,
                                                                                imageFileName,
                                                                                version);
            Texture2D imageTexture = CacheManager.ReadImageFile(imageFilePath);
            
            if(imageTexture != null)
            {
                onSuccess(imageTexture);
            }
            else
            {
                // - Fetch from Server -
                // GetModProfile(modId)
                DownloadAndSaveImageAsPNG(profile.GetGalleryImageWithFileName(imageFileName).GetVersionURL(version),
                                          imageFilePath,
                                          onSuccess,
                                          onError);
            }
        }

        // ---------[ FILE DOWNLOADING ]---------
        public static void DownloadAndSaveImageAsPNG(string serverURL,
                                                     string destinationFilePath,
                                                     Action<Texture2D> onSuccess,
                                                     Action<WebRequestError> onError)
        {
            var download = new TextureDownload();

            download.sourceURL = serverURL;
            download.OnCompleted += (d) =>
            {
                CacheManager.WriteImageFile(destinationFilePath,
                                            download.texture);
                onSuccess(download.texture);
            };
            download.OnFailed += (d, e) =>
            {
                onError(e);
            };

            DownloadManager.StartDownload(download);
        }

        // ---------[ FETCH ALL RESULTS ]---------
        private delegate void GetAllObjectsQuery<T>(PaginationParameters pagination,
                                                    Action<ObjectArray<T>> onSuccess,
                                                    Action<WebRequestError> onError);

        private static void FetchAllResultsForQuery<T>(GetAllObjectsQuery<T> query,
                                                       Action<List<T>> onSuccess,
                                                       Action<WebRequestError> onError)
        {
            var pagination = new PaginationParameters()
            {
                limit = PaginationParameters.LIMIT_MAX,
                offset = 0,
            };

            var results = new List<T>();

            query(pagination,
                  (r) => FetchQueryResultsRecursively(query,
                                                      r,
                                                      pagination,
                                                      results,
                                                      onSuccess,
                                                      onError),
                  onError);
        }

        private static void FetchQueryResultsRecursively<T>(GetAllObjectsQuery<T> query,
                                                            ObjectArray<T> queryResult,
                                                            PaginationParameters pagination,
                                                            List<T> culmativeResults,
                                                            Action<List<T>> onSuccess,
                                                            Action<WebRequestError> onError)
        {
            Debug.Assert(pagination.limit > 0);

            culmativeResults.AddRange(queryResult.data);

            if(queryResult.result_count < queryResult.result_limit)
            {
                onSuccess(culmativeResults);
            }
            else
            {
                pagination.offset += pagination.limit;

                query(pagination,
                      (r) => FetchQueryResultsRecursively(query,
                                                          queryResult,
                                                          pagination,
                                                          culmativeResults,
                                                          onSuccess,
                                                          onError),
                      onError);
            }
        }
    }
}