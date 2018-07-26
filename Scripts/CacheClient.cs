// #define DO_NOT_LOAD_CACHE

using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;

using Newtonsoft.Json;

using ModIO.API;

namespace ModIO
{
    public static class CacheClient
    {
        // ---------[ MEMBERS ]---------
        private static string _cacheDirectory = null;

        // ---------[ INITIALIZATION ]---------
        static CacheClient()
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


        /// <summary>
        /// Attempts to set the directory used for the cache by the cache client
        /// </summary>
        /// <param name="directory"> The absolute or relative directory to use. (Created if non-existent) </param>
        /// <returns> True if successful </returns>
        public static bool TrySetCacheDirectory(string directory)
        {
            try
            {
                Directory.CreateDirectory(directory);

                Debug.Log("[mod.io] Successfully set cache directory to  " + directory);
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to set cache directory."
                                      + "\nDirectory: " + directory + "\n\n");

                Debug.LogError(warningInfo
                               + Utility.GenerateExceptionDebugString(e));

                return false;
            }

            CacheClient._cacheDirectory = directory;
            return true;
        }

        // ---------[ GET DIRECTORIES ]---------
        public static string GetCacheDirectory()
        {
            return CacheClient._cacheDirectory;
        }

        public static string GenerateModDirectoryPath(int modId)
        {
            return(CacheClient._cacheDirectory
                   + "mods/"
                   + modId.ToString() + "/");
        }

        public static string GenerateModBuildsDirectoryPath(int modId)
        {
            return(CacheClient.GenerateModDirectoryPath(modId)
                   + "builds/");
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
                    string warningInfo = ("[mod.io] Failed to read json object from file."
                                          + "\nFile: " + filePath + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));
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
                string warningInfo = ("[mod.io] Failed to write json object to file."
                                      + "\nFile: " + filePath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }
        }

        public static byte[] LoadBinaryFile(string filePath)
        {
            byte[] fileData = null;

            if(File.Exists(filePath))
            {
                try
                {
                    fileData = File.ReadAllBytes(filePath);
                }
                catch(Exception e)
                {
                    string warningInfo = ("[mod.io] Failed to read binary file."
                                          + "\nFile: " + filePath + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));

                    fileData = null;
                }
            }

            return fileData;
        }

        public static void WriteBinaryFile(string filePath,
                                           byte[] data)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllBytes(filePath, data);
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to write binary file."
                                      + "\nFile: " + filePath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }
        }

        public static Texture2D ReadImageFile(string filePath)
        {
            Texture2D texture = null;

            if(File.Exists(filePath))
            {
                byte[] imageData = CacheClient.LoadBinaryFile(filePath);

                if(imageData != null)
                {
                    texture = new Texture2D(0,0);
                    texture.LoadImage(imageData);
                }
            }

            return texture;
        }

        public static void WritePNGFile(string filePath,
                                        Texture2D texture)
        {
            Debug.Assert(Path.GetExtension(filePath).Equals(".png"),
                         "[mod.io] Images can only be saved in PNG format."
                         + "\n" + filePath
                         + " is an invalid file path.");

            CacheClient.WriteBinaryFile(filePath,
                                        texture.EncodeToPNG());
        }

        public static void DeleteFile(string filePath)
        {
            try
            {
                if(File.Exists(filePath)) { File.Delete(filePath); }
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to delete file."
                                      + "\nFile: " + filePath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }
        }

        public static void DeleteDirectory(string directoryPath)
        {
            try
            {
                if(Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, true);
                }
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to delete directory."
                                      + "\nDirectory: " + directoryPath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }
        }


        // ---------[ AUTHENTICATED USER ]---------
        [Serializable]
        private class AuthenticatedUser
        {
            public string oAuthToken;
            public int userId;
            public List<int> modIds;
            public List<int> subscribedModIds;
        }

        public static string userFilePath
        { get { return CacheClient._cacheDirectory + "user.data"; } }

        public static void SaveAuthenticatedUserToken(string oAuthToken)
        {
            AuthenticatedUser au = CacheClient.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);

            if(au == null)
            {
                au = new AuthenticatedUser();
            }

            au.oAuthToken = oAuthToken;

            CacheClient.WriteJsonObjectFile(userFilePath, au);
        }

        public static string LoadAuthenticatedUserToken()
        {
            AuthenticatedUser au = CacheClient.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);

            if(au != null)
            {
                return au.oAuthToken;
            }
            return null;
        }

        public static void SaveAuthenticatedUserProfile(UserProfile userProfile)
        {
            CacheClient.SaveUserProfile(userProfile);

            AuthenticatedUser au = CacheClient.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);

            if(au == null)
            {
                au = new AuthenticatedUser();
            }

            au.userId = userProfile.id;

            CacheClient.WriteJsonObjectFile(userFilePath, au);
        }

        public static UserProfile LoadAuthenticatedUserProfile()
        {
            AuthenticatedUser au = CacheClient.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);

            if(au != null
               && au.userId > 0)
            {
                return LoadUserProfile(au.userId);
            }
            return null;
        }

        public static void SaveAuthenticatedUserSubscriptions(List<int> subscribedModIds)
        {
            AuthenticatedUser au = CacheClient.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);

            if(au == null)
            {
                au = new AuthenticatedUser();
            }

            au.subscribedModIds = subscribedModIds;

            CacheClient.WriteJsonObjectFile(userFilePath, au);
        }

        public static List<int> LoadAuthenticatedUserSubscriptions()
        {
            AuthenticatedUser au = CacheClient.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);

            if(au != null)
            {
                return au.subscribedModIds;
            }
            return null;
        }

        public static void SaveAuthenticatedUserMods(List<int> modIds)
        {
            AuthenticatedUser au = CacheClient.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);

            if(au == null)
            {
                au = new AuthenticatedUser();
            }

            au.modIds = modIds;

            CacheClient.WriteJsonObjectFile(userFilePath, au);

        }

        public static List<int> LoadAuthenticatedUserMods()
        {
            AuthenticatedUser au = CacheClient.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);

            if(au != null)
            {
                return au.modIds;
            }

            return null;
        }

        public static void DeleteAuthenticatedUser()
        {
            try
            {
                if(File.Exists(userFilePath)) { File.Delete(userFilePath); }
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to delete user data save file."
                                      + "\nFile: " + userFilePath + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }
        }


        // ---------[ GAME PROFILE ]---------
        public static string gameProfileFilePath
        { get { return CacheClient._cacheDirectory + "game_profile.data"; } }

        public static GameProfile LoadGameProfile()
        {
            return CacheClient.ReadJsonObjectFile<GameProfile>(gameProfileFilePath);
        }

        public static void SaveGameProfile(GameProfile profile)
        {
            CacheClient.WriteJsonObjectFile(gameProfileFilePath, profile);
        }


        // ---------[ MOD PROFILES ]---------
        public static string GenerateModProfileFilePath(int modId)
        {
            return (CacheClient.GenerateModDirectoryPath(modId)
                    + "profile.data");
        }

        public static ModProfile LoadModProfile(int modId)
        {
            string profileFilePath = GenerateModProfileFilePath(modId);
            ModProfile profile = CacheClient.ReadJsonObjectFile<ModProfile>(profileFilePath);
            return(profile);
        }

        public static IEnumerable<ModProfile> AllModProfiles()
        {
            string profileDirectory = CacheClient._cacheDirectory + "mods/";

            if(Directory.Exists(profileDirectory))
            {
                string[] modDirectories;
                try
                {
                    modDirectories = Directory.GetDirectories(profileDirectory);
                }
                catch(Exception e)
                {
                    string warningInfo = ("[mod.io] Failed to read mod profile directory."
                                          + "\nDirectory: " + profileDirectory + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));

                    modDirectories = new string[0];
                }

                foreach(string modDirectory in modDirectories)
                {
                    ModProfile profile = CacheClient.ReadJsonObjectFile<ModProfile>(modDirectory + "/profile.data");

                    if(profile != null)
                    {
                        yield return profile;
                    }
                }
            }
        }

        public static void SaveModProfile(ModProfile profile)
        {
            Debug.Assert(profile.id > 0,
                         "[mod.io] Cannot cache a mod without a mod id");

            CacheClient.WriteJsonObjectFile(GenerateModProfileFilePath(profile.id),
                                             profile);
        }

        public static void SaveModProfiles(IEnumerable<ModProfile> modProfiles)
        {
            foreach(ModProfile profile in modProfiles)
            {
                CacheClient.SaveModProfile(profile);
            }
        }

        public static void DeleteMod(int modId)
        {
            string modDir = CacheClient.GenerateModDirectoryPath(modId);
            CacheClient.DeleteDirectory(modDir);
        }

        // ---------[ MODFILES ]---------
        public static string GenerateModfileFilePath(int modId, int modfileId)
        {
            return(CacheClient.GenerateModBuildsDirectoryPath(modId)
                   + modfileId + ".data");
        }
        public static string GenerateModBinaryZipFilePath(int modId, int modfileId)
        {
            return(CacheClient.GenerateModBuildsDirectoryPath(modId)
                   + modfileId + ".zip");
        }

        public static Modfile LoadModfile(int modId, int modfileId)
        {
            string modfileFilePath = GenerateModfileFilePath(modId, modfileId);
            var modfile = CacheClient.ReadJsonObjectFile<Modfile>(modfileFilePath);
            return modfile;
        }

        public static void SaveModfile(Modfile modfile)
        {
            Debug.Assert(modfile.modId > 0,
                         "[mod.io] Cannot cache a modfile without a mod id");
            Debug.Assert(modfile.id > 0,
                         "[mod.io] Cannot cache a modfile without a modfile id");

            CacheClient.WriteJsonObjectFile(GenerateModfileFilePath(modfile.modId, modfile.id),
                                            modfile);
        }

        public static byte[] LoadModBinaryZip(int modId, int modfileId)
        {
            string filePath = GenerateModBinaryZipFilePath(modId, modfileId);
            byte[] zipData = CacheClient.LoadBinaryFile(filePath);
            return zipData;
        }

        public static void SaveModBinaryZip(int modId, int modfileId,
                                            byte[] modBinary)
        {
            Debug.Assert(modId > 0,
                         "[mod.io] Cannot cache a mod binary without a mod id");
            Debug.Assert(modfileId > 0,
                         "[mod.io] Cannot cache a mod binary without a modfile id");

            string filePath = GenerateModBinaryZipFilePath(modId, modfileId);
            CacheClient.WriteBinaryFile(filePath, modBinary);
        }

        public static void DeleteModfileAndBinaryZip(int modId, int modfileId)
        {
            CacheClient.DeleteFile(CacheClient.GenerateModfileFilePath(modId, modfileId));
            CacheClient.DeleteFile(CacheClient.GenerateModBinaryZipFilePath(modId, modfileId));
        }

        // ---------[ MOD MEDIA ]---------
        public static string GenerateModLogoDirectoryPath(int modId)
        {
            return(CacheClient.GenerateModDirectoryPath(modId)
                   + "logo/");
        }
        public static string GenerateModLogoFilePath(int modId, LogoSize size)
        {
            return (GenerateModLogoDirectoryPath(modId)
                    + size.ToString() + ".png");
        }
        public static string GenerateModLogoVersionInfoFilePath(int modId)
        {
            return(CacheClient.GenerateModLogoDirectoryPath(modId)
                   + "versionInfo.data");
        }

        public static string GenerateModGalleryImageDirectoryPath(int modId)
        {
            return(Application.temporaryCachePath
                   + "/mod_images/"
                   + modId + "/");
        }
        public static string GenerateModGalleryImageFilePath(int modId,
                                                              string imageFileName,
                                                              ModGalleryImageSize size)
        {
            return(GenerateModGalleryImageDirectoryPath(modId)
                   + size.ToString() + "/"
                   + Path.GetFileNameWithoutExtension(imageFileName) +
                   ".png");
        }

        public static Texture2D LoadModLogo(int modId, LogoSize size)
        {
            string logoFilePath = CacheClient.GenerateModLogoFilePath(modId, size);
            Texture2D logoTexture = CacheClient.ReadImageFile(logoFilePath);
            return(logoTexture);
        }

        public static Dictionary<LogoSize, string> LoadModLogoVersionInfo(int modId)
        {
            return CacheClient.ReadJsonObjectFile<Dictionary<LogoSize, string>>(CacheClient.GenerateModLogoVersionInfoFilePath(modId));
        }

        public static void SaveModLogo(int modId, string fileName,
                                       LogoSize size, Texture2D logoTexture)
        {
            Debug.Assert(modId > 0,
                         "[mod.io] Cannot cache a mod logo without a mod id");
            Debug.Assert(!String.IsNullOrEmpty(fileName),
                         "[mod.io] Cannot cache a mod logo without file name as it"
                         + " is used for versioning purposes");

            string logoFilePath = CacheClient.GenerateModLogoFilePath(modId, size);
            CacheClient.WritePNGFile(logoFilePath, logoTexture);

            // - Version Info -
            var versionInfo = CacheClient.LoadModLogoVersionInfo(modId);
            if(versionInfo == null)
            {
                versionInfo = new Dictionary<LogoSize, string>();
            }

            versionInfo[size] = fileName;
            CacheClient.WriteJsonObjectFile(GenerateModLogoVersionInfoFilePath(modId),
                                            versionInfo);
        }

        public static Texture2D LoadModGalleryImage(int modId,
                                                    string imageFileName,
                                                    ModGalleryImageSize size)
        {
            string imageFilePath = CacheClient.GenerateModGalleryImageFilePath(modId,
                                                                               imageFileName,
                                                                               size);
            Texture2D imageTexture = CacheClient.ReadImageFile(imageFilePath);

            return(imageTexture);
        }

        public static void SaveModGalleryImage(int modId,
                                               string imageFileName,
                                               ModGalleryImageSize size,
                                               Texture2D imageTexture)
        {
            Debug.Assert(modId > 0,
                         "[mod.io] Cannot cache a mod image without a mod id");

            string imageFilePath = CacheClient.GenerateModGalleryImageFilePath(modId,
                                                                               imageFileName,
                                                                               size);
            CacheClient.WritePNGFile(imageFilePath, imageTexture);
        }

        // ---------[ MOD TEAM ]---------
        public static string GenerateModTeamFilePath(int modId)
        {
            return(CacheClient.GenerateModDirectoryPath(modId)
                   + "team.data");
        }

        public static List<ModTeamMember> LoadModTeam(int modId)
        {
            string filePath = CacheClient.GenerateModTeamFilePath(modId);
            var modTeam = CacheClient.ReadJsonObjectFile<List<ModTeamMember>>(filePath);
            return modTeam;
        }

        public static void SaveModTeam(int modId,
                                       List<ModTeamMember> modTeam)
        {
            Debug.Assert(modId > 0,
                         "[mod.io] Cannot cache a mod team without a mod id");

            string filePath = CacheClient.GenerateModTeamFilePath(modId);
            CacheClient.WriteJsonObjectFile(filePath, modTeam);
        }

        public static void DeleteModTeam(int modId)
        {
            CacheClient.DeleteFile(CacheClient.GenerateModTeamFilePath(modId));
        }

        // ---------[ USERS ]---------
        public static string GenerateUserProfileFilePath(int userId)
        {
            return(CacheClient.GetCacheDirectory()
                   + "users/"
                   + userId + ".data");
        }

        public static UserProfile LoadUserProfile(int userId)
        {
            string filePath = CacheClient.GenerateUserProfileFilePath(userId);
            var userProfile = CacheClient.ReadJsonObjectFile<UserProfile>(filePath);
            return(userProfile);
        }

        public static IEnumerable<UserProfileStub> IterateAllUserProfiles()
        {
            string profileDirectory = CacheClient.GetCacheDirectory() + "users/";

            if(Directory.Exists(profileDirectory))
            {
                string[] userFiles;
                try
                {
                    userFiles = Directory.GetFiles(profileDirectory);
                }
                catch(Exception e)
                {
                    string warningInfo = ("[mod.io] Failed to read user profile directory."
                                          + "\nDirectory: " + profileDirectory + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));

                    userFiles = new string[0];
                }

                foreach(string profileFilePath in userFiles)
                {
                    var profile = CacheClient.ReadJsonObjectFile<UserProfileStub>(profileFilePath);
                    if(profile != null)
                    {
                        yield return profile;
                    }
                }
            }
        }

        public static void SaveUserProfile(UserProfile userProfile)
        {
            Debug.Assert(userProfile.id > 0,
                         "[mod.io] Cannot cache a user profile without a user id");

            string filePath = CacheClient.GenerateUserProfileFilePath(userProfile.id);
            CacheClient.WriteJsonObjectFile(filePath, userProfile);
        }

        public static void DeleteUserProfile(int userId)
        {
            CacheClient.DeleteFile(CacheClient.GenerateUserProfileFilePath(userId));
        }
    }
}
