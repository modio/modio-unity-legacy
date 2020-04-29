﻿using System;
using System.Collections.Generic;
using Path = System.IO.Path;

using UnityEngine;

using Newtonsoft.Json;

using ModIO.API;

namespace ModIO
{
    /// <summary>An interface for storing/loading data retrieved for the mod.io servers on disk.</summary>
    public static class CacheClient
    {
        // ---------[ MEMBERS ]---------
        /// <summary>Directory that the CacheClient uses to store data.</summary>
        public static string cacheDirectory;

        // ---------[ INITIALIZATION ]---------
        /// <summary>Initializes the CacheClient settings.</summary>
        static CacheClient()
        {
            PluginSettings.Data settings = PluginSettings.data;
            CacheClient.cacheDirectory = settings.cacheDirectory;
        }

        // ---------[ GAME PROFILE ]---------
        /// <summary>File path for the game profile data.</summary>
        public static string gameProfileFilePath
        { get { return IOUtilities.CombinePath(CacheClient.cacheDirectory, "game_profile.data"); } }

        /// <summary>Stores the game's profile in the cache.</summary>
        public static bool SaveGameProfile(GameProfile profile)
        {
            Debug.Assert(profile != null);

            return LocalDataStorage.WriteJSONFile(gameProfileFilePath, profile);
        }

        /// <summary>Retrieves the game's profile from the cache.</summary>
        public static GameProfile LoadGameProfile()
        {
            GameProfile profile;
            LocalDataStorage.ReadJSONFile(gameProfileFilePath, out profile);

            return profile;
        }

        // ---------[ MODS ]---------
        /// <summary>Generates the path for a mod cache directory.</summary>
        public static string GenerateModDirectoryPath(int modId)
        {
            return IOUtilities.CombinePath(CacheClient.cacheDirectory,
                                           "mods",
                                           modId.ToString());
        }

        // ------[ PROFILES ]------
        /// <summary>Generates the file path for a mod's profile data.</summary>
        public static string GenerateModProfileFilePath(int modId)
        {
            return IOUtilities.CombinePath(CacheClient.GenerateModDirectoryPath(modId),
                                           "profile.data");
        }

        /// <summary>Stores a mod's profile in the cache.</summary>
        public static bool SaveModProfile(ModProfile profile)
        {
            Debug.Assert(profile != null);

            string path = GenerateModProfileFilePath(profile.id);
            return LocalDataStorage.WriteJSONFile(path, profile);
        }

        /// <summary>Retrieves a mod's profile from the cache.</summary>
        public static ModProfile LoadModProfile(int modId)
        {
            string path = GenerateModProfileFilePath(modId);
            ModProfile profile;
            LocalDataStorage.ReadJSONFile(path, out profile);

            return profile;
        }

        /// <summary>Stores a collection of mod profiles in the cache.</summary>
        public static bool SaveModProfiles(IEnumerable<ModProfile> modProfiles)
        {
            Debug.Assert(modProfiles != null);

            bool isSuccessful = true;
            foreach(ModProfile profile in modProfiles)
            {
                isSuccessful = CacheClient.SaveModProfile(profile) && isSuccessful;
            }
            return isSuccessful;
        }

        /// <summary>Iterates through all of the mod profiles in the cache.</summary>
        public static IEnumerable<ModProfile> IterateAllModProfiles()
        {
            return IterateAllModProfilesFromOffset(0);
        }

        /// <summary>Iterates through all of the mod profiles from the given offset.</summary>
        public static IEnumerable<ModProfile> IterateAllModProfilesFromOffset(int offset)
        {
            Debug.Assert(IOUtilities.CombinePath(CacheClient.cacheDirectory, "mods", "0")
                         == CacheClient.GenerateModDirectoryPath(0),
                         "[mod.io] This function relies on mod directory path being a generated in"
                         + " a specific way. Changing CacheClient.GenerateModDirectoryPath()"
                         + " necessitates changes in this function.");

            Debug.Assert(IOUtilities.CombinePath(CacheClient.GenerateModDirectoryPath(0), "profile.data")
                         == CacheClient.GenerateModProfileFilePath(0),
                         "[mod.io] This function relies on mod directory profile file path being a generated in"
                         + " a specific way. Changing CacheClient.GenerateModProfileFilePath()"
                         + " necessitates changes in this function.");

            string profileDirectory = IOUtilities.CombinePath(CacheClient.cacheDirectory, "mods");

            if(LocalDataStorage.GetDirectoryExists(profileDirectory))
            {
                IList<string> modDirectories;
                try
                {
                    modDirectories = LocalDataStorage.GetDirectories(profileDirectory);
                }
                catch(Exception e)
                {
                    string warningInfo = ("[mod.io] Failed to read mod profile directory."
                                          + "\nDirectory: " + profileDirectory + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));

                    modDirectories = new string[0];
                }

                if(modDirectories.Count - offset > 0)
                {
                    for(int i = offset; i < modDirectories.Count; ++i)
                    {
                        string profilePath = IOUtilities.CombinePath(modDirectories[i], "profile.data");
                        ModProfile profile;

                        LocalDataStorage.ReadJSONFile(profilePath, out profile);

                        if(profile != null)
                        {
                            yield return profile;
                        }
                        else
                        {
                            LocalDataStorage.DeleteFile(profilePath);
                        }
                    }
                }
            }
        }

        /// <summary>Iterates through all of the mod profiles returning only those matching the id filter.</summary>
        public static IEnumerable<ModProfile> IterateFilteredModProfiles(IList<int> idFilter)
        {
            if(idFilter == null || idFilter.Count == 0)
            {
                yield break;
            }

            Debug.Assert(IOUtilities.CombinePath(CacheClient.cacheDirectory, "mods", "0")
                         == CacheClient.GenerateModDirectoryPath(0),
                         "[mod.io] This function relies on mod directory path being a generated in"
                         + " a specific way. Changing CacheClient.GenerateModDirectoryPath()"
                         + " necessitates changes in this function.");

            Debug.Assert(IOUtilities.CombinePath(CacheClient.GenerateModDirectoryPath(0), "profile.data")
                         == CacheClient.GenerateModProfileFilePath(0),
                         "[mod.io] This function relies on mod directory profile file path being a generated in"
                         + " a specific way. Changing CacheClient.GenerateModProfileFilePath()"
                         + " necessitates changes in this function.");

            string profileDirectory = IOUtilities.CombinePath(CacheClient.cacheDirectory, "mods");

            if(LocalDataStorage.GetDirectoryExists(profileDirectory))
            {
                IList<string> modDirectories;
                try
                {
                    modDirectories = LocalDataStorage.GetDirectories(profileDirectory);
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
                    string idPart = modDirectory.Substring(profileDirectory.Length + 1);
                    int modId = ModProfile.NULL_ID;
                    if(!int.TryParse(idPart, out modId))
                    {
                        modId = ModProfile.NULL_ID;
                    }

                    if(idFilter.Contains(modId))
                    {
                        string profilePath = IOUtilities.CombinePath(modDirectory, "profile.data");
                        ModProfile profile;

                        LocalDataStorage.ReadJSONFile(profilePath, out profile);

                        if(profile != null)
                        {
                            yield return profile;
                        }
                        else
                        {
                            LocalDataStorage.DeleteFile(profilePath);
                        }
                    }
                }
            }
        }

        /// <summary>Determines how many ModProfiles are currently stored in the cache.</summary>
        public static int CountModProfiles()
        {
            string profileDirectory = IOUtilities.CombinePath(CacheClient.cacheDirectory, "mods");

            if(LocalDataStorage.GetDirectoryExists(profileDirectory))
            {
                IList<string> modDirectories;
                try
                {
                    modDirectories = LocalDataStorage.GetDirectories(profileDirectory);
                }
                catch(Exception e)
                {
                    string warningInfo = ("[mod.io] Failed to read mod profile directory."
                                          + "\nDirectory: " + profileDirectory + "\n\n");

                    Debug.LogWarning(warningInfo
                                     + Utility.GenerateExceptionDebugString(e));

                    modDirectories = new string[0];
                }

                return modDirectories.Count;
            }

            return 0;
        }

        /// <summary>Deletes all of a mod's data from the cache.</summary>
        public static bool DeleteMod(int modId)
        {
            string modDir = CacheClient.GenerateModDirectoryPath(modId);
            return LocalDataStorage.DeleteDirectory(modDir);
        }

        // ------[ STATISTICS ]------
        /// <summary>Generates the file path for a mod's statitics data.</summary>
        public static string GenerateModStatisticsFilePath(int modId)
        {
            return IOUtilities.CombinePath(CacheClient.GenerateModDirectoryPath(modId),
                                           "stats.data");
        }

        /// <summary>Stores a mod's statistics in the cache.</summary>
        public static bool SaveModStatistics(ModStatistics stats)
        {
            Debug.Assert(stats != null);
            string statsFilePath = GenerateModStatisticsFilePath(stats.modId);
            return LocalDataStorage.WriteJSONFile(statsFilePath, stats);
        }

        /// <summary>Retrieves a mod's statistics from the cache.</summary>
        public static ModStatistics LoadModStatistics(int modId)
        {
            string statsFilePath = GenerateModStatisticsFilePath(modId);
            ModStatistics stats;
            LocalDataStorage.ReadJSONFile(statsFilePath, out stats);

            return stats;
        }

        // ------[ MODFILES ]------
        /// <summary>Generates the path for a cached mod build directory.</summary>
        public static string GenerateModBinariesDirectoryPath(int modId)
        {
            return IOUtilities.CombinePath(CacheClient.GenerateModDirectoryPath(modId), "binaries");
        }

        /// <summary>Generates the file path for a modfile.</summary>
        public static string GenerateModfileFilePath(int modId, int modfileId)
        {
            return IOUtilities.CombinePath(CacheClient.GenerateModBinariesDirectoryPath(modId),
                                           modfileId.ToString() + ".data");
        }

        /// <summary>Generates the file path for a mod binary.</summary>
        public static string GenerateModBinaryZipFilePath(int modId, int modfileId)
        {
            return IOUtilities.CombinePath(CacheClient.GenerateModBinariesDirectoryPath(modId),
                                           modfileId.ToString() + ".zip");
        }

        /// <summary>Stores a modfile in the cache.</summary>
        public static bool SaveModfile(Modfile modfile)
        {
            Debug.Assert(modfile != null);

            string path = GenerateModfileFilePath(modfile.modId, modfile.id);
            return LocalDataStorage.WriteJSONFile(path, modfile);
        }

        /// <summary>Retrieves a modfile from the cache.</summary>
        public static Modfile LoadModfile(int modId, int modfileId)
        {
            string modfileFilePath = GenerateModfileFilePath(modId, modfileId);
            Modfile modfile;
            LocalDataStorage.ReadJSONFile(modfileFilePath, out modfile);

            return modfile;
        }

        /// <summary>Stores a mod binary's ZipFile data in the cache.</summary>
        public static bool SaveModBinaryZip(int modId, int modfileId,
                                            byte[] modBinary)
        {
            Debug.Assert(modBinary != null);
            Debug.Assert(modBinary.Length > 0);

            string filePath = GenerateModBinaryZipFilePath(modId, modfileId);
            return LocalDataStorage.WriteFile(filePath, modBinary);
        }

        /// <summary>Retrieves a mod binary's ZipFile data from the cache.</summary>
        public static byte[] LoadModBinaryZip(int modId, int modfileId)
        {
            string filePath = GenerateModBinaryZipFilePath(modId, modfileId);
            byte[] zipData;
            LocalDataStorage.ReadFile(filePath, out zipData);

            return zipData;
        }

        /// <summary>Deletes a modfile and binary from the cache.</summary>
        public static bool DeleteModfileAndBinaryZip(int modId, int modfileId)
        {
            string modfilePath = CacheClient.GenerateModfileFilePath(modId, modfileId);
            string zipPath = CacheClient.GenerateModBinaryZipFilePath(modId, modfileId);

            bool success = true;

            if(!LocalDataStorage.DeleteFile(modfilePath))
            {
                success = false;
            }
            if(!LocalDataStorage.DeleteFile(zipPath))
            {
                success = false;
            }

            return success;
        }

        /// <summary>Deletes all modfiles and binaries from the cache.</summary>
        public static bool DeleteAllModfileAndBinaryData(int modId)
        {
            string path = CacheClient.GenerateModBinariesDirectoryPath(modId);
            return LocalDataStorage.DeleteDirectory(path);
        }

        // ------[ MEDIA ]------
        /// <summary>Generates the directory path for a mod logo collection.</summary>
        public static string GenerateModLogoCollectionDirectoryPath(int modId)
        {
            return IOUtilities.CombinePath(CacheClient.GenerateModDirectoryPath(modId),
                                           "logo");
        }

        /// <summary>Generates the file path for a mod logo.</summary>
        public static string GenerateModLogoFilePath(int modId, LogoSize size)
        {
            return IOUtilities.CombinePath(GenerateModLogoCollectionDirectoryPath(modId),
                                           size.ToString() + ".png");
        }

        /// <summary>Generates the file path for a mod logo's cached version information.</summary>
        public static string GenerateModLogoVersionInfoFilePath(int modId)
        {
            return IOUtilities.CombinePath(CacheClient.GenerateModLogoCollectionDirectoryPath(modId),
                                           "versionInfo.data");
        }

        /// <summary>Generates the directory path for the cached mod media.</summary>
        public static string GenerateModMediaDirectoryPath(int modId)
        {
            return IOUtilities.CombinePath(GenerateModDirectoryPath(modId),
                                           "mod_media");
        }

        /// <summary>Generates the file path for a mod galley image.</summary>
        public static string GenerateModGalleryImageFilePath(int modId,
                                                             string imageFileName,
                                                             ModGalleryImageSize size)
        {
            return IOUtilities.CombinePath(GenerateModMediaDirectoryPath(modId),
                                           "images_" + size.ToString(),
                                           Path.GetFileNameWithoutExtension(imageFileName) + ".png");
        }

        /// <summary>Generates the file path for a YouTube thumbnail.</summary>
        public static string GenerateModYouTubeThumbnailFilePath(int modId,
                                                                 string youTubeId)
        {
            return IOUtilities.CombinePath(GenerateModMediaDirectoryPath(modId),
                                           "youTube",
                                           youTubeId + ".png");
        }

        /// <summary>Retrieves the file paths for the mod logos in the cache.</summary>
        public static Dictionary<LogoSize, string> GetModLogoVersionFileNames(int modId)
        {
            string path = CacheClient.GenerateModLogoVersionInfoFilePath(modId);
            Dictionary<LogoSize, string> retVal;
            LocalDataStorage.ReadJSONFile(path, out retVal);

            return retVal;
        }

        /// <summary>Stores a mod logo in the cache with the given fileName.</summary>
        public static bool SaveModLogo(int modId, string fileName,
                                       LogoSize size, Texture2D logoTexture)
        {
            Debug.Assert(!String.IsNullOrEmpty(fileName));
            Debug.Assert(logoTexture != null);

            bool success = false;
            string logoFilePath = CacheClient.GenerateModLogoFilePath(modId, size);
            byte[] imageData = logoTexture.EncodeToPNG();

            // write file
            if(LocalDataStorage.WriteFile(logoFilePath, imageData))
            {
                success = true;

                // - Update the versioning info -
                var versionInfo = CacheClient.GetModLogoVersionFileNames(modId);
                if(versionInfo == null)
                {
                    versionInfo = new Dictionary<LogoSize, string>();
                }
                versionInfo[size] = fileName;
                LocalDataStorage.WriteJSONFile(GenerateModLogoVersionInfoFilePath(modId), versionInfo);
            }

            return success;
        }

        /// <summary>Retrieves a mod logo from the cache.</summary>
        public static Texture2D LoadModLogo(int modId, LogoSize size)
        {
            string logoFilePath = CacheClient.GenerateModLogoFilePath(modId, size);
            byte[] imageData;

            if(LocalDataStorage.ReadFile(logoFilePath, out imageData)
               && imageData != null)
            {
                return IOUtilities.ParseImageData(imageData);
            }
            else
            {
                return null;
            }
        }

        /// <summary>Retrieves a mod logo from the cache if it matches the given fileName.</summary>
        public static Texture2D LoadModLogo(int modId, string fileName, LogoSize size)
        {
            Debug.Assert(!String.IsNullOrEmpty(fileName));

            string logoFileName = GetModLogoFileName(modId, size);
            if(logoFileName == fileName)
            {
                return CacheClient.LoadModLogo(modId, size);
            }
            else
            {
                return null;
            }
        }

        /// <summary>Retrieves the information for the cached mod logos.</summary>
        public static string GetModLogoFileName(int modId, LogoSize size)
        {
            // - Ensure the logo is the correct version -
            var versionInfo = CacheClient.GetModLogoVersionFileNames(modId);
            if(versionInfo != null)
            {
                string logoFileName = string.Empty;
                if(versionInfo.TryGetValue(size, out logoFileName)
                   && !String.IsNullOrEmpty(logoFileName))
                {
                    return logoFileName;
                }
            }
            return null;
        }

        /// <summary>Stores a mod gallery image in the cache.</summary>
        public static bool SaveModGalleryImage(int modId,
                                               string imageFileName,
                                               ModGalleryImageSize size,
                                               Texture2D imageTexture)
        {
            Debug.Assert(!String.IsNullOrEmpty(imageFileName));
            Debug.Assert(imageTexture != null);

            string imageFilePath = CacheClient.GenerateModGalleryImageFilePath(modId,
                                                                               imageFileName,
                                                                               size);
            byte[] imageData = imageTexture.EncodeToPNG();

            return LocalDataStorage.WriteFile(imageFilePath, imageData);
        }

        /// <summary>Retrieves a mod gallery image from the cache.</summary>
        public static Texture2D LoadModGalleryImage(int modId,
                                                    string imageFileName,
                                                    ModGalleryImageSize size)
        {
            Debug.Assert(!String.IsNullOrEmpty(imageFileName));

            string imageFilePath = CacheClient.GenerateModGalleryImageFilePath(modId,
                                                                               imageFileName,
                                                                               size);
            byte[] imageData;

            if(LocalDataStorage.ReadFile(imageFilePath, out imageData)
               && imageData != null)
            {
                return IOUtilities.ParseImageData(imageData);
            }
            else
            {
                return null;
            }
        }

        /// <summary>Stores a YouTube thumbnail in the cache.</summary>
        public static bool SaveModYouTubeThumbnail(int modId,
                                                   string youTubeId,
                                                   Texture2D thumbnail)
        {
            Debug.Assert(!String.IsNullOrEmpty(youTubeId));
            Debug.Assert(thumbnail != null);

            string thumbnailFilePath = CacheClient.GenerateModYouTubeThumbnailFilePath(modId,
                                                                                       youTubeId);
            byte[] imageData = thumbnail.EncodeToPNG();

            return LocalDataStorage.WriteFile(thumbnailFilePath, imageData);
        }

        /// <summary>Retrieves a YouTube thumbnail from the cache.</summary>
        public static Texture2D LoadModYouTubeThumbnail(int modId,
                                                        string youTubeId)
        {
            Debug.Assert(!String.IsNullOrEmpty(youTubeId));

            string thumbnailFilePath = CacheClient.GenerateModYouTubeThumbnailFilePath(modId,
                                                                                       youTubeId);
            byte[] imageData;

            if(LocalDataStorage.ReadFile(thumbnailFilePath, out imageData)
               && imageData != null)
            {
                return IOUtilities.ParseImageData(imageData);
            }
            else
            {
                return null;
            }
        }

        // ---------[ MOD TEAMS ]---------
        /// <summary>Generates the file path for a mod team's data.</summary>
        public static string GenerateModTeamFilePath(int modId)
        {
            return IOUtilities.CombinePath(CacheClient.GenerateModDirectoryPath(modId),
                                           "team.data");
        }

        /// <summary>Stores a mod team's data in the cache.</summary>
        public static bool SaveModTeam(int modId,
                                       List<ModTeamMember> modTeam)
        {
            Debug.Assert(modTeam != null);

            string filePath = CacheClient.GenerateModTeamFilePath(modId);
            return LocalDataStorage.WriteJSONFile(filePath, modTeam);
        }

        /// <summary>Retrieves a mod team's data from the cache.</summary>
        public static List<ModTeamMember> LoadModTeam(int modId)
        {
            string filePath = CacheClient.GenerateModTeamFilePath(modId);
            List<ModTeamMember> modTeam;
            LocalDataStorage.ReadJSONFile(filePath, out modTeam);

            return modTeam;
        }

        /// <summary>Deletes a mod team's data from the cache.</summary>
        public static bool DeleteModTeam(int modId)
        {
            string path = CacheClient.GenerateModTeamFilePath(modId);
            return LocalDataStorage.DeleteFile(path);
        }

        // ---------[ USERS ]---------
        /// <summary>Generates the file path for a user's profile.</summary>
        public static string GenerateUserAvatarDirectoryPath(int userId)
        {
            return IOUtilities.CombinePath(CacheClient.cacheDirectory,
                                           "users",
                                           userId + "_avatar");
        }

        /// <summary>Generates the file path for a user's profile.</summary>
        public static string GenerateUserAvatarFilePath(int userId, UserAvatarSize size)
        {
            return IOUtilities.CombinePath(CacheClient.GenerateUserAvatarDirectoryPath(userId),
                                           size.ToString() + ".png");
        }

        /// <summary>Stores a user's avatar in the cache.</summary>
        public static bool SaveUserAvatar(int userId, UserAvatarSize size,
                                          Texture2D avatarTexture)
        {
            Debug.Assert(avatarTexture != null);

            string avatarFilePath = CacheClient.GenerateUserAvatarFilePath(userId, size);
            byte[] imageData = avatarTexture.EncodeToPNG();

            return LocalDataStorage.WriteFile(avatarFilePath, imageData);
        }

        /// <summary>Retrieves a user's avatar from the cache.</summary>
        public static Texture2D LoadUserAvatar(int userId, UserAvatarSize size)
        {
            string avatarFilePath = CacheClient.GenerateUserAvatarFilePath(userId, size);
            byte[] imageData;

            if(LocalDataStorage.ReadFile(avatarFilePath, out imageData)
               && imageData != null)
            {
                return IOUtilities.ParseImageData(imageData);
            }
            else
            {
                return null;
            }
        }

        /// <summary>Delete's a user's avatars from the cache.</summary>
        public static bool DeleteUserAvatar(int userId)
        {
            string path = CacheClient.GenerateUserAvatarDirectoryPath(userId);
            return LocalDataStorage.DeleteDirectory(path);
        }

        // ---------[ OBSOLETE ]---------
        /// <summary>[Obsolete] Retrieves the file paths for the mod logos in the cache.</summary>
        [Obsolete("Use CacheClient.GetModLogoVersionFileNames() instead")]
        public static Dictionary<LogoSize, string> LoadModLogoFilePaths(int modId)
        {
            return CacheClient.GetModLogoVersionFileNames(modId);
        }

        /// <summary>[Obsolete] Generates the file path for a user's profile.</summary>
        [Obsolete("User Profiles are no longer accessible via the mod.io API.")]
        public static string GenerateUserProfileFilePath(int userId)
        {
            return IOUtilities.CombinePath(CacheClient.cacheDirectory,
                                           "users",
                                           userId.ToString(),
                                           "profile.data");
        }

        /// <summary>[Obsolete] Stores a user's profile in the cache.</summary>
        [Obsolete("User Profiles are no longer accessible via the mod.io API.")]
        public static bool SaveUserProfile(UserProfile userProfile)
        {
            Debug.Assert(userProfile != null);

            string filePath = CacheClient.GenerateUserProfileFilePath(userProfile.id);
            return LocalDataStorage.WriteJSONFile(filePath, userProfile);
        }

        /// <summary>[Obsolete] Retrieves a user's profile from the cache.</summary>
        [Obsolete("User Profiles are no longer accessible via the mod.io API.")]
        public static UserProfile LoadUserProfile(int userId)
        {
            string filePath = CacheClient.GenerateUserProfileFilePath(userId);
            UserProfile userProfile;
            LocalDataStorage.ReadJSONFile(filePath, out userProfile);

            return userProfile;
        }

        /// <summary>[Obsolete] Deletes a user's profile from the cache.</summary>
        [Obsolete("User Profiles are no longer accessible via the mod.io API.")]
        public static bool DeleteUserProfile(int userId)
        {
            string path = CacheClient.GenerateUserProfileFilePath(userId);
            return LocalDataStorage.DeleteFile(path);
        }

        /// <summary>[Obsolete] Iterates through all the user profiles in the cache.</summary>
        [Obsolete("User Profiles are no longer accessible via the mod.io API.")]
        public static IEnumerable<UserProfile> IterateAllUserProfiles()
        {
            string profileDirectory = IOUtilities.CombinePath(CacheClient.cacheDirectory,
                                                              "users");

            if(LocalDataStorage.GetDirectoryExists(profileDirectory))
            {
                IList<string> userFiles;
                try
                {
                    userFiles = LocalDataStorage.GetFiles(profileDirectory, null, false);
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
                    UserProfile profile;
                    LocalDataStorage.ReadJSONFile(profileFilePath, out profile);

                    if(profile != null)
                    {
                        yield return profile;
                    }
                }
            }
        }
    }
}
