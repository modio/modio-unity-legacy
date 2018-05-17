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
        // TODO(@jackson): Sort Initialization interface/timing
        // public static void Initialize()
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
                                      + "\nDirectory: " + directory + "\n");

                Utility.LogExceptionAsWarning(warningInfo, e);

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
                                          + "\nFile: " + filePath + "\n");

                    Utility.LogExceptionAsWarning(warningInfo, e);
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
                                      + "\nFile: " + filePath + "\n");

                Utility.LogExceptionAsWarning(warningInfo, e);
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
                                          + "\nFile: " + filePath + "\n");

                    Utility.LogExceptionAsWarning(warningInfo, e);

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
                                      + "\nFile: " + filePath + "\n");

                Utility.LogExceptionAsWarning(warningInfo, e);
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

        public static void WriteImageFile(string filePath,
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
                                      + "\nFile: " + filePath + "\n");
                Utility.LogExceptionAsWarning(warningInfo, e);
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
                                      + "\nDirectory: " + directoryPath + "\n");

                Utility.LogExceptionAsWarning(warningInfo, e);
            }
        }


        // ---------[ USER MANAGEMENT ]---------
        public static string userFilePath
        { get { return CacheClient._cacheDirectory + "user.data"; } }

        public static void SaveAuthenticatedUser(AuthenticatedUser user)
        {
            CacheClient.WriteJsonObjectFile(userFilePath, user);
        }

        public static void LoadAuthenticatedUser(Action<AuthenticatedUser> callback)
        {
            AuthenticatedUser user
            = CacheClient.ReadJsonObjectFile<AuthenticatedUser>(userFilePath);
            callback(user);
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
                                      + "\nFile: " + userFilePath + "\n");

                Utility.LogExceptionAsWarning(warningInfo, e);
            }
        }


        // ---------[ GAME PROFILE ]---------
        public static string gameProfileFilePath
        { get { return CacheClient._cacheDirectory + "game_profile.data"; } }

        public static void LoadGameProfile(Action<GameProfile> callback)
        {
            GameProfile profile = CacheClient.ReadJsonObjectFile<GameProfile>(gameProfileFilePath);
            callback(profile);
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

        public static void LoadModProfile(int modId,
                                          Action<ModProfile> callback)
        {
            string profileFilePath = GenerateModProfileFilePath(modId);
            ModProfile profile = CacheClient.ReadJsonObjectFile<ModProfile>(profileFilePath);
            callback(profile);
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
                                          + "\nDirectory: " + profileDirectory + "\n");

                    Utility.LogExceptionAsWarning(warningInfo, e);

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
            return(CacheClient.GenerateModDirectoryPath(modId)
                   + "builds/"
                   + modfileId + ".data");
        }
        public static string GenerateModBinaryZipFilePath(int modId, int modfileId)
        {
            return(CacheClient.GenerateModDirectoryPath(modId)
                   + "builds/"
                   + modfileId + ".zip");
        }

        public static void LoadModfile(int modId, int modfileId,
                                       Action<Modfile> callback)
        {
            string modfileFilePath = GenerateModfileFilePath(modId, modfileId);
            var modfile = CacheClient.ReadJsonObjectFile<Modfile>(modfileFilePath);
            callback(modfile);
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

        public static void LoadModBinaryZip(int modId, int modfileId,
                                            Action<byte[]> callback)
        {
            string filePath = GenerateModBinaryZipFilePath(modId, modfileId);
            byte[] zipData = CacheClient.LoadBinaryFile(filePath);
            callback(zipData);
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
        public static string GenerateModLogoFilePath(int modId, LogoVersion version)
        {
            return (GenerateModLogoDirectoryPath(modId)
                    + version.ToString() + ".png");
        }

        public static string GenerateModGalleryImageDirectoryPath(int modId)
        {
            return(Application.temporaryCachePath
                   + "/mod_images/"
                   + modId + "/");
        }
        public static string GenerateModGalleryImageFilePath(int modId,
                                                              string imageFileName,
                                                              ModGalleryImageVersion version)
        {
            return(GenerateModGalleryImageDirectoryPath(modId)
                   + version.ToString() + "/"
                   + Path.GetFileNameWithoutExtension(imageFileName) +
                   ".png");
        }

        public static void LoadModLogo(int modId, LogoVersion version,
                                       Action<Texture2D> callback)
        {
            string logoFilePath = CacheClient.GenerateModLogoFilePath(modId, version);
            Texture2D logoTexture = CacheClient.ReadImageFile(logoFilePath);
            callback(logoTexture);
        }

        public static void SaveModLogo(int modId, LogoVersion version,
                                       Texture2D logoTexture)
        {
            Debug.Assert(modId > 0,
                         "[mod.io] Cannot cache a mod logo without a mod id");

            string logoFilePath = CacheClient.GenerateModLogoFilePath(modId, version);
            CacheClient.WriteImageFile(logoFilePath, logoTexture);
        }

        public static void LoadModGalleryImage(int modId,
                                               string imageFileName,
                                               ModGalleryImageVersion version,
                                               Action<Texture2D> callback)
        {
            string imageFilePath = CacheClient.GenerateModGalleryImageFilePath(modId,
                                                                               imageFileName,
                                                                               version);
            Texture2D imageTexture = CacheClient.ReadImageFile(imageFilePath);

            callback(imageTexture);
        }

        public static void SaveModGalleryImage(int modId,
                                               string imageFileName,
                                               ModGalleryImageVersion version,
                                               Texture2D imageTexture)
        {
            Debug.Assert(modId > 0,
                         "[mod.io] Cannot cache a mod image without a mod id");

            string imageFilePath = CacheClient.GenerateModGalleryImageFilePath(modId,
                                                                               imageFileName,
                                                                               version);
            CacheClient.WriteImageFile(imageFilePath, imageTexture);
        }

        // ---------[ MOD TEAM ]---------
        public static string GenerateModTeamFilePath(int modId)
        {
            return(CacheClient.GenerateModDirectoryPath(modId)
                   + "team.data");
        }

        public static void LoadModTeam(int modId,
                                       Action<List<ModTeamMember>> callback)
        {
            string filePath = CacheClient.GenerateModTeamFilePath(modId);
            var modTeam = CacheClient.ReadJsonObjectFile<List<ModTeamMember>>(filePath);
            callback(modTeam);
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

        public static void LoadUserProfile(int userId,
                                           Action<UserProfileStub> callback)
        {
            string filePath = CacheClient.GenerateUserProfileFilePath(userId);
            var userProfile = CacheClient.ReadJsonObjectFile<UserProfile>(filePath);
            callback(userProfile);
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
                                          + "\nDirectory: " + profileDirectory + "\n");

                    Utility.LogExceptionAsWarning(warningInfo, e);

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

        public static void SaveUserProfile(UserProfileStub userProfile)
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
