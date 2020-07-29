using System;
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
        // ---------[ GAME PROFILE ]---------
        /// <summary>File path for the game profile data.</summary>
        public static string gameProfileFilePath
        { get { return IOUtilities.CombinePath(PluginSettings.CACHE_DIRECTORY, "game_profile.data"); } }

        /// <summary>Stores the game's profile in the cache.</summary>
        public static void SaveGameProfile(GameProfile profile, Action<bool> onComplete)
        {
            Debug.Assert(profile != null);

            DataStorage.WriteJSONFile(CacheClient.gameProfileFilePath, profile, (p, success) =>
            {
                if(onComplete != null)
                {
                    onComplete.Invoke(success);
                }
            });
        }

        /// <summary>Retrieves the game's profile from the cache.</summary>
        public static void LoadGameProfile(Action<GameProfile> onComplete)
        {
            Debug.Assert(onComplete != null);

            DataStorage.ReadJSONFile<GameProfile>(CacheClient.gameProfileFilePath, (p, success, data) =>
            {
                if(onComplete != null) { onComplete.Invoke(data); }
            });
        }

        // ---------[ MODS ]---------
        /// <summary>Generates the path for a mod cache directory.</summary>
        public static string GenerateModDirectoryPath(int modId)
        {
            return IOUtilities.CombinePath(PluginSettings.CACHE_DIRECTORY,
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
        public static void SaveModProfile(ModProfile profile, Action<bool> onComplete)
        {
            Debug.Assert(profile != null);
            Debug.Assert(profile.id != ModProfile.NULL_ID);

            string path = GenerateModProfileFilePath(profile.id);
            DataStorage.WriteJSONFile(path, profile, (p, success) =>
            {
                if(onComplete != null)
                {
                    onComplete.Invoke(success);
                }
            });
        }

        /// <summary>Retrieves a mod's profile from the cache.</summary>
        public static void LoadModProfile(int modId, Action<ModProfile> onComplete)
        {
            Debug.Assert(onComplete != null);

            string path = GenerateModProfileFilePath(modId);

            DataStorage.ReadJSONFile<ModProfile>(path, (p, success, data) =>
            {
                if(onComplete != null) { onComplete.Invoke(data); }
            });
        }

        /// <summary>Stores a collection of mod profiles in the cache.</summary>
        public static void SaveModProfiles(IEnumerable<ModProfile> modProfiles, Action<bool> onComplete)
        {
            Debug.Assert(modProfiles != null);

            bool success = true;
            List<ModProfile> profiles = new List<ModProfile>(modProfiles);

            // write
            Action writeNextProfile = null;
            writeNextProfile = () =>
            {
                if(profiles.Count > 0)
                {
                    int index = profiles.Count-1;
                    ModProfile profile = profiles[index];
                    string path = GenerateModProfileFilePath(profile.id);

                    profiles.RemoveAt(index);

                    if(profile != null)
                    {
                        DataStorage.WriteJSONFile(path, profile, (p,s) =>
                        {
                            success &= s;
                            writeNextProfile();
                        });
                    }
                    else
                    {
                        writeNextProfile();
                    }
                }
                else
                {
                    if(onComplete != null)
                    {
                        onComplete.Invoke(success);
                    }
                }
            };

            writeNextProfile();
        }

        /// <summary>Requests all of the mod profiles in the cache.</summary>
        public static void RequestAllModProfiles(Action<IList<ModProfile>> onComplete)
        {
            CacheClient.RequestAllModProfilesFromOffset(0, onComplete);
        }

        /// <summary>Requests all of the mod profiles from the given offset.</summary>
        public static void RequestAllModProfilesFromOffset(int offset, Action<IList<ModProfile>> onComplete)
        {
            const string FILENAME = "profile.data";

            Debug.Assert(IOUtilities.CombinePath(PluginSettings.CACHE_DIRECTORY, "mods", "0")
                         == CacheClient.GenerateModDirectoryPath(0),
                         "[mod.io] This function relies on mod directory path being a generated in"
                         + " a specific way. Changing CacheClient.GenerateModDirectoryPath()"
                         + " necessitates changes in this function.");

            Debug.Assert(IOUtilities.CombinePath(CacheClient.GenerateModDirectoryPath(0), FILENAME)
                         == CacheClient.GenerateModProfileFilePath(0),
                         "[mod.io] This function relies on mod directory profile file path being a generated in"
                         + " a specific way. Changing CacheClient.GenerateModProfileFilePath()"
                         + " necessitates changes in this function.");

            Debug.Assert(onComplete != null);

            List<string> profilePaths = new List<string>();
            List<ModProfile> modProfiles = new List<ModProfile>();
            string profileDirectory = IOUtilities.CombinePath(PluginSettings.CACHE_DIRECTORY, "mods");

            DataStorage.GetDirectories(profileDirectory, (gd_path, gd_exists, modDirectories) =>
            {
                if(gd_exists && modDirectories != null)
                {
                    if(modDirectories.Count - offset > 0)
                    {
                        for(int i = offset; i < modDirectories.Count; ++i)
                        {
                            string profilePath = IOUtilities.CombinePath(modDirectories[i], FILENAME);
                            profilePaths.Add(profilePath);
                        }
                    }
                }
                else
                {
                    string warningInfo = ("[mod.io] Failed to read mod profile directory."
                                          + "\nDirectory: " + profileDirectory);

                    Debug.LogWarning(warningInfo);

                    modDirectories = new string[0];
                }

                // Load Profiles
                Action loadNextProfile = null;

                loadNextProfile = () =>
                {
                    if(profilePaths.Count > 0)
                    {
                        int index = profilePaths.Count-1;
                        string path = profilePaths[index];
                        profilePaths.RemoveAt(index);

                        DataStorage.ReadJSONFile<ModProfile>(path, (p, success, data) =>
                        {
                            if(success)
                            {
                                modProfiles.Add(data);
                                loadNextProfile();
                            }
                            else
                            {
                                DataStorage.DeleteFile(path, (delPath, delSuccess) => loadNextProfile());
                            }
                        });
                    }
                    else
                    {
                        if(onComplete != null)
                        {
                            onComplete.Invoke(modProfiles);
                        }
                    }
                };

                loadNextProfile();
            });
        }

        /// <summary>Requests all of the mod profiles returning only those matching the id filter.</summary>
        public static void RequestFilteredModProfiles(IList<int> idFilter, Action<IList<ModProfile>> onComplete)
        {
            CacheClient.RequestAllModProfilesFromOffset(0, (modProfiles) =>
            {
                List<ModProfile> filteredProfiles = new List<ModProfile>();

                foreach(ModProfile profile in modProfiles)
                {
                    if(profile != null && idFilter.Contains(profile.id))
                    {
                        filteredProfiles.Add(profile);
                    }
                }

                if(onComplete != null)
                {
                    onComplete.Invoke(filteredProfiles);
                }
            });
        }

        /// <summary>Deletes all of a mod's data from the cache.</summary>
        public static void DeleteMod(int modId, Action<bool> onComplete)
        {
            string modDir = CacheClient.GenerateModDirectoryPath(modId);
            DataStorage.DeleteDirectory(modDir, (path, success) =>
            {
                if(onComplete != null)
                {
                    onComplete.Invoke(success);
                }
            });
        }

        // ------[ STATISTICS ]------
        /// <summary>Generates the file path for a mod's statitics data.</summary>
        public static string GenerateModStatisticsFilePath(int modId)
        {
            return IOUtilities.CombinePath(CacheClient.GenerateModDirectoryPath(modId),
                                           "stats.data");
        }

        /// <summary>Stores a mod's statistics in the cache.</summary>
        public static void SaveModStatistics(ModStatistics stats, Action<bool> onComplete)
        {
            Debug.Assert(stats != null);
            Debug.Assert(stats.modId != ModProfile.NULL_ID);

            string path = GenerateModStatisticsFilePath(stats.modId);

            DataStorage.WriteJSONFile(path, stats, (p, success) =>
            {
                if(onComplete != null)
                {
                    onComplete.Invoke(success);
                }
            });
        }

        /// <summary>Retrieves a mod's statistics from the cache.</summary>
        public static void LoadModStatistics(int modId, Action<ModStatistics> onComplete)
        {
            Debug.Assert(modId != ModProfile.NULL_ID);
            Debug.Assert(onComplete != null);

            string path = GenerateModStatisticsFilePath(modId);
            DataStorage.ReadJSONFile<ModStatistics>(path, (p, success, data) =>
            {
                if(onComplete != null)
                {
                    onComplete.Invoke(data);
                }
            });
        }

        /// <summary>Requests all of the mod statistics returning only those matching the id filter.</summary>
        public static void RequestFilteredModStatistics(IList<int> idFilter, Action<IList<ModStatistics>> onComplete)
        {
            const string FILENAME = "stats.data";

            Debug.Assert(IOUtilities.CombinePath(PluginSettings.CACHE_DIRECTORY, "mods", "0")
                         == CacheClient.GenerateModDirectoryPath(0),
                         "[mod.io] This function relies on mod directory path being a generated in"
                         + " a specific way. Changing CacheClient.GenerateModDirectoryPath()"
                         + " necessitates changes in this function.");

            Debug.Assert(IOUtilities.CombinePath(CacheClient.GenerateModDirectoryPath(0), FILENAME)
                         == CacheClient.GenerateModStatisticsFilePath(0),
                         "[mod.io] This function relies on mod directory profile file path being a generated in"
                         + " a specific way. Changing CacheClient.GenerateModStatisticsFilePath()"
                         + " necessitates changes in this function.");

            Debug.Assert(onComplete != null);

            // init
            List<ModStatistics> modStatistics = new List<ModStatistics>();
            List<string> statsPaths = new List<string>();

            // early out
            if(idFilter == null || idFilter.Count == 0)
            {
                onComplete.Invoke(modStatistics);
                return;
            }

            // get statistics
            string statisticsDirectory = IOUtilities.CombinePath(PluginSettings.CACHE_DIRECTORY, "mods");

            DataStorage.GetDirectories(statisticsDirectory, (gd_path, gd_exists, modDirectories) =>
            {
                if(gd_exists && modDirectories != null)
                {
                    foreach(string modDirectory in modDirectories)
                    {
                        string idPart = modDirectory.Substring(statisticsDirectory.Length + 1);
                        int modId = ModProfile.NULL_ID;
                        if(!int.TryParse(idPart, out modId))
                        {
                            modId = ModProfile.NULL_ID;
                        }

                        if(idFilter.Contains(modId))
                        {
                            string statisticsPath = IOUtilities.CombinePath(modDirectory, FILENAME);
                            statsPaths.Add(statisticsPath);
                        }
                    }
                }
                else
                {
                    string warningInfo = ("[mod.io] Failed to read mod statistics directory."
                                          + "\nDirectory: " + statisticsDirectory);

                    Debug.LogWarning(warningInfo);

                    modDirectories = new string[0];
                }

                // Load Statisticss
                Action loadNextStatistics = null;

                loadNextStatistics = () =>
                {
                    if(statsPaths.Count > 0)
                    {
                        int index = statsPaths.Count-1;
                        string path = statsPaths[index];
                        statsPaths.RemoveAt(index);

                        DataStorage.ReadJSONFile<ModStatistics>(path, (p, success, data) =>
                        {
                            if(success)
                            {
                                modStatistics.Add(data);
                                loadNextStatistics();
                            }
                            else
                            {
                                DataStorage.DeleteFile(path, (delPath, delSuccess) => loadNextStatistics());
                            }
                        });
                    }
                    else
                    {
                        if(onComplete != null)
                        {
                            onComplete.Invoke(modStatistics);
                        }
                    }
                };

                loadNextStatistics();
            });
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
        public static void SaveModfile(Modfile modfile, Action<bool> onComplete)
        {
            Debug.Assert(modfile != null);
            Debug.Assert(modfile.modId != ModProfile.NULL_ID);
            Debug.Assert(modfile.id != Modfile.NULL_ID);

            string path = GenerateModfileFilePath(modfile.modId, modfile.id);
            DataStorage.WriteJSONFile(path, modfile, (p, success) =>
            {
                if(onComplete != null)
                {
                    onComplete.Invoke(success);
                }
            });
        }

        /// <summary>Retrieves a modfile from the cache.</summary>
        public static void LoadModfile(int modId, int modfileId, Action<Modfile> onComplete)
        {
            Debug.Assert(modId != ModProfile.NULL_ID);
            Debug.Assert(modfileId != ModProfile.NULL_ID);
            Debug.Assert(onComplete != null);

            string path = GenerateModfileFilePath(modId, modfileId);
            DataStorage.ReadJSONFile<Modfile>(path, (p, success, data) =>
            {
                if(onComplete != null)
                {
                    onComplete.Invoke(data);
                }
            });
        }

        /// <summary>Stores a mod binary's ZipFile data in the cache.</summary>
        public static void SaveModBinaryZip(int modId, int modfileId, byte[] modBinary,
                                            Action<bool> onComplete)
        {
            Debug.Assert(modId != ModProfile.NULL_ID);
            Debug.Assert(modfileId != Modfile.NULL_ID);
            Debug.Assert(modBinary != null);
            Debug.Assert(modBinary.Length > 0);

            string path = GenerateModBinaryZipFilePath(modId, modfileId);
            DataStorage.WriteFile(path, modBinary, (p, success) =>
            {
                if(onComplete != null)
                {
                    onComplete.Invoke(success);
                }
            });
        }

        /// <summary>Retrieves a mod binary's ZipFile data from the cache.</summary>
        public static void LoadModBinaryZip(int modId, int modfileId, Action<byte[]> onComplete)
        {
            Debug.Assert(modId != ModProfile.NULL_ID);
            Debug.Assert(modfileId != Modfile.NULL_ID);
            Debug.Assert(onComplete != null);

            string filePath = GenerateModBinaryZipFilePath(modId, modfileId);
            DataStorage.ReadFile(filePath, (p,s,data) =>
            {
                if(onComplete != null)
                {
                    onComplete.Invoke(data);
                }
            });
        }

        /// <summary>Deletes a modfile and binary from the cache.</summary>
        public static void DeleteModfileAndBinaryZip(int modId, int modfileId, Action<bool> onComplete)
        {
            Debug.Assert(modId != ModProfile.NULL_ID);
            Debug.Assert(modfileId != Modfile.NULL_ID);

            string modfilePath = CacheClient.GenerateModfileFilePath(modId, modfileId);
            string zipPath = CacheClient.GenerateModBinaryZipFilePath(modId, modfileId);

            DataStorage.DeleteFile(modfilePath, (mfP, mfS) =>
            {
                DataStorage.DeleteFile(zipPath, (zP, zS) =>
                {
                    if(onComplete != null)
                    {
                        onComplete.Invoke(mfS && zS);
                    }
                });
            });
        }

        /// <summary>Deletes all modfiles and binaries from the cache.</summary>
        public static void DeleteAllModfileAndBinaryData(int modId, Action<bool> onComplete)
        {
            Debug.Assert(modId != ModProfile.NULL_ID);

            string path = CacheClient.GenerateModBinariesDirectoryPath(modId);
            DataStorage.DeleteDirectory(path, (p, success) =>
            {
                if(onComplete != null)
                {
                    onComplete.Invoke(success);
                }
            });
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
        public static void GetModLogoVersionFileNames(int modId,
                                                      Action<Dictionary<LogoSize, string>> onComplete)
        {
            Debug.Assert(modId != ModProfile.NULL_ID);

            string path = CacheClient.GenerateModLogoVersionInfoFilePath(modId);
            DataStorage.ReadJSONFile<Dictionary<LogoSize, string>>(path, (p, success, data) =>
            {
                if(onComplete != null)
                {
                    onComplete.Invoke(data);
                }
            });
        }

        /// <summary>Stores a mod logo in the cache with the given fileName.</summary>
        public static void SaveModLogo(int modId, string fileName,
                                       LogoSize size, Texture2D logoTexture,
                                       Action<bool> onComplete)
        {
            Debug.Assert(modId != ModProfile.NULL_ID);
            Debug.Assert(!String.IsNullOrEmpty(fileName));
            Debug.Assert(logoTexture != null);

            string path = CacheClient.GenerateModLogoFilePath(modId, size);
            byte[] data = logoTexture.EncodeToPNG();

            // write file
            DataStorage.WriteFile(path, data, (p, success) =>
            {
                // - Update the versioning info -
                CacheClient.GetModLogoVersionFileNames(modId, (versionInfo) =>
                {
                    if(versionInfo == null)
                    {
                        versionInfo = new Dictionary<LogoSize, string>();
                    }
                    versionInfo[size] = fileName;

                    string versionPath = GenerateModLogoVersionInfoFilePath(modId);
                    DataStorage.WriteJSONFile(versionPath, versionInfo, null);
                });

                if(onComplete != null)
                {
                    onComplete.Invoke(success);
                }
            });
        }

        /// <summary>Retrieves a mod logo from the cache.</summary>
        public static void LoadModLogo(int modId, LogoSize size, Action<Texture2D> onComplete)
        {
            Debug.Assert(onComplete != null);

            string filePath = CacheClient.GenerateModLogoFilePath(modId, size);

            DataStorage.ReadFile(filePath, (p, success, data) =>
            {
                Texture2D texture = null;
                if(success && data != null)
                {
                    texture = IOUtilities.ParseImageData(data);
                }

                if(onComplete != null)
                {
                    onComplete.Invoke(texture);
                }
            });
        }

        /// <summary>Retrieves a mod logo from the cache if it matches the given fileName.</summary>
        public static void LoadModLogo(int modId, string fileName, LogoSize size,
                                       Action<Texture2D> onComplete)
        {
            Debug.Assert(modId != ModProfile.NULL_ID);
            Debug.Assert(!String.IsNullOrEmpty(fileName));
            Debug.Assert(onComplete != null);

            CacheClient.GetModLogoFileName(modId, size, (logoFileName) =>
            {
                if(logoFileName == fileName)
                {
                    CacheClient.LoadModLogo(modId, size, onComplete);
                }
                else if(onComplete != null)
                {
                    onComplete.Invoke(null);
                }
            });
        }

        /// <summary>Retrieves the information for the cached mod logos.</summary>
        public static void GetModLogoFileName(int modId, LogoSize size, Action<string> onComplete)
        {
            // - Ensure the logo is the correct version -
            CacheClient.GetModLogoVersionFileNames(modId, (versionInfo) =>
            {
                string logoFileName = null;

                if(versionInfo != null)
                {
                    versionInfo.TryGetValue(size, out logoFileName);
                }

                if(onComplete != null)
                {
                    onComplete.Invoke(logoFileName);
                }
            });
        }

        /// <summary>Stores a mod gallery image in the cache.</summary>
        public static void SaveModGalleryImage(int modId,
                                               string imageFileName,
                                               ModGalleryImageSize size,
                                               Texture2D imageTexture,
                                               Action<bool> onComplete)
        {
            Debug.Assert(modId != ModProfile.NULL_ID);
            Debug.Assert(!String.IsNullOrEmpty(imageFileName));
            Debug.Assert(imageTexture != null);

            string path = CacheClient.GenerateModGalleryImageFilePath(modId, imageFileName, size);
            byte[] data = imageTexture.EncodeToPNG();

            DataStorage.WriteFile(path, data, (p, success) =>
            {
                if(onComplete != null)
                {
                    onComplete.Invoke(success);
                }
            });
        }

        /// <summary>Retrieves a mod gallery image from the cache.</summary>
        public static void LoadModGalleryImage(int modId, string imageFileName, ModGalleryImageSize size,
                                               Action<Texture2D> onComplete)
        {
            Debug.Assert(modId != ModProfile.NULL_ID);
            Debug.Assert(!String.IsNullOrEmpty(imageFileName));
            Debug.Assert(onComplete != null);

            string filePath = CacheClient.GenerateModGalleryImageFilePath(modId, imageFileName, size);

            DataStorage.ReadFile(filePath, (p, success, data) =>
            {
                Texture2D texture = null;
                if(success && data != null)
                {
                    texture = IOUtilities.ParseImageData(data);
                }

                if(onComplete != null)
                {
                    onComplete.Invoke(texture);
                }
            });
        }

        /// <summary>Stores a YouTube thumbnail in the cache.</summary>
        public static void SaveModYouTubeThumbnail(int modId, string youTubeId, Texture2D thumbnail,
                                                   Action<bool> onComplete)
        {
            Debug.Assert(modId != ModProfile.NULL_ID);
            Debug.Assert(!String.IsNullOrEmpty(youTubeId));
            Debug.Assert(thumbnail != null);

            string path = CacheClient.GenerateModYouTubeThumbnailFilePath(modId, youTubeId);
            byte[] data = thumbnail.EncodeToPNG();

            DataStorage.WriteFile(path, data, (p, success) =>
            {
                if(onComplete != null)
                {
                    onComplete.Invoke(success);
                }
            });
        }

        /// <summary>Retrieves a YouTube thumbnail from the cache.</summary>
        public static void LoadModYouTubeThumbnail(int modId, string youTubeId,
                                                   Action<Texture2D> onComplete)
        {
            Debug.Assert(modId != ModProfile.NULL_ID);
            Debug.Assert(!String.IsNullOrEmpty(youTubeId));
            Debug.Assert(onComplete != null);

            string filePath = CacheClient.GenerateModYouTubeThumbnailFilePath(modId, youTubeId);

            DataStorage.ReadFile(filePath, (p, success, data) =>
            {
                Texture2D texture = null;
                if(success && data != null)
                {
                    texture = IOUtilities.ParseImageData(data);
                }

                if(onComplete != null)
                {
                    onComplete.Invoke(texture);
                }
            });
        }

        // ---------[ MOD TEAMS ]---------
        /// <summary>Generates the file path for a mod team's data.</summary>
        public static string GenerateModTeamFilePath(int modId)
        {
            return IOUtilities.CombinePath(CacheClient.GenerateModDirectoryPath(modId),
                                           "team.data");
        }

        /// <summary>Stores a mod team's data in the cache.</summary>
        public static void SaveModTeam(int modId, List<ModTeamMember> modTeam,
                                       Action<bool> onComplete)
        {
            Debug.Assert(modId != ModProfile.NULL_ID);
            Debug.Assert(modTeam != null);

            string path = CacheClient.GenerateModTeamFilePath(modId);
            DataStorage.WriteJSONFile(path, modTeam, (p, success) =>
            {
                if(onComplete != null)
                {
                    onComplete.Invoke(success);
                }
            });
        }

        /// <summary>Retrieves a mod team's data from the cache.</summary>
        public static void LoadModTeam(int modId, Action<List<ModTeamMember>> onComplete)
        {
            Debug.Assert(modId != ModProfile.NULL_ID);
            Debug.Assert(onComplete != null);

            string path = CacheClient.GenerateModTeamFilePath(modId);

            DataStorage.ReadJSONFile<List<ModTeamMember>>(path, (p, success, data) =>
            {
                if(onComplete != null)
                {
                    onComplete.Invoke(data);
                }
            });
        }

        /// <summary>Deletes a mod team's data from the cache.</summary>
        public static void DeleteModTeam(int modId, Action<bool> onComplete)
        {
            Debug.Assert(modId != ModProfile.NULL_ID);

            string path = CacheClient.GenerateModTeamFilePath(modId);
            DataStorage.DeleteFile(path, (p, success) =>
            {
                if(onComplete != null)
                {
                    onComplete.Invoke(success);
                }
            });
        }

        // ---------[ USERS ]---------
        /// <summary>Generates the file path for a user's profile.</summary>
        public static string GenerateUserAvatarDirectoryPath(int userId)
        {
            return IOUtilities.CombinePath(PluginSettings.CACHE_DIRECTORY,
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
        public static void SaveUserAvatar(int userId, UserAvatarSize size, Texture2D avatarTexture,
                                          Action<bool> onComplete)
        {
            Debug.Assert(userId != UserProfile.NULL_ID);
            Debug.Assert(avatarTexture != null);

            string path = CacheClient.GenerateUserAvatarFilePath(userId, size);
            byte[] data = avatarTexture.EncodeToPNG();

            DataStorage.WriteFile(path, data, (p, success) =>
            {
                if(onComplete != null)
                {
                    onComplete.Invoke(success);
                }
            });
        }

        /// <summary>Retrieves a user's avatar from the cache.</summary>
        public static void LoadUserAvatar(int userId, UserAvatarSize size, Action<Texture2D> onComplete)
        {
            Debug.Assert(userId != UserProfile.NULL_ID);
            Debug.Assert(onComplete != null);

            string filePath = CacheClient.GenerateUserAvatarFilePath(userId, size);

            DataStorage.ReadFile(filePath, (p, success, data) =>
            {
                Texture2D texture = null;
                if(success && data != null)
                {
                    texture = IOUtilities.ParseImageData(data);
                }

                if(onComplete != null)
                {
                    onComplete.Invoke(texture);
                }
            });
        }

        /// <summary>Delete's a user's avatars from the cache.</summary>
        public static void DeleteUserAvatar(int userId, Action<bool> onComplete)
        {
            Debug.Assert(userId != UserProfile.NULL_ID);

            string path = CacheClient.GenerateUserAvatarDirectoryPath(userId);
            DataStorage.DeleteDirectory(path, (p, success) =>
            {
                if(onComplete != null)
                {
                    onComplete.Invoke(success);
                }
            });
        }

        // ---------[ OBSOLETE ]---------
        /// <summary>[Obsolete] Directory that the CacheClient uses to store data.</summary>
        [Obsolete("Use PluginSettings.CACHE_DIRECTORY instead")]
        public static string cacheDirectory
        {
            get { return PluginSettings.CACHE_DIRECTORY; }
        }

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
            return IOUtilities.CombinePath(PluginSettings.CACHE_DIRECTORY,
                                           "users",
                                           userId.ToString(),
                                           "profile.data");
        }

        /// <summary>[Obsolete] Stores a user's profile in the cache.</summary>
        [Obsolete("User Profiles are no longer accessible via the mod.io API.")]
        public static bool SaveUserProfile(UserProfile userProfile)
        {
            Debug.Assert(userProfile != null);

            bool result = false;
            string path = CacheClient.GenerateUserProfileFilePath(userProfile.id);
            DataStorage.WriteJSONFile(path, userProfile, (p, s) => result = s);

            return result;
        }

        /// <summary>[Obsolete] Retrieves a user's profile from the cache.</summary>
        [Obsolete("User Profiles are no longer accessible via the mod.io API.")]
        public static UserProfile LoadUserProfile(int userId)
        {
            string path = CacheClient.GenerateUserProfileFilePath(userId);
            UserProfile result = null;

            DataStorage.ReadJSONFile<UserProfile>(path, (p, s, r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Deletes a user's profile from the cache.</summary>
        [Obsolete("User Profiles are no longer accessible via the mod.io API.")]
        public static bool DeleteUserProfile(int userId)
        {
            bool result = false;
            string path = CacheClient.GenerateUserProfileFilePath(userId);
            DataStorage.DeleteFile(path, (p, s) => result = s);
            return result;
        }

        /// <summary>[Obsolete] Iterates through all the user profiles in the cache.</summary>
        [Obsolete("User Profiles are no longer accessible via the mod.io API.")]
        public static IEnumerable<UserProfile> IterateAllUserProfiles()
        {
            return null;
        }

        // --- Obsolete Synchronous Interface ---
        /// <summary>[Obsolete] Stores the game's profile in the cache.</summary>
        [Obsolete("Use SaveGameProfile(GameProfile, Action<bool>) instead.")]
        public static bool SaveGameProfile(GameProfile profile)
        {
            bool result = false;

            CacheClient.SaveGameProfile(profile, (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Retrieves the game's profile from the cache.</summary>
        [Obsolete("Use LoadGameProfile(Action<GameProfile>) instead.")]
        public static GameProfile LoadGameProfile()
        {
            GameProfile result = null;

            CacheClient.LoadGameProfile((r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Stores a mod's profile in the cache.</summary>
        [Obsolete("Use SaveModProfile(ModProfile, Action<bool>) instead.")]
        public static bool SaveModProfile(ModProfile profile)
        {
            bool result = false;

            CacheClient.SaveModProfile(profile, (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Retrieves a mod's profile from the cache.</summary>
        [Obsolete("Use LoadModProfile(int, Action<ModProfile>) instead.")]
        public static ModProfile LoadModProfile(int modId)
        {
            ModProfile result = null;

            CacheClient.LoadModProfile(modId, (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Stores a collection of mod profiles in the cache.</summary>
        [Obsolete("Use SaveModProfile(IEnumerable<ModProfile>, Action<bool>) instead.")]
        public static bool SaveModProfiles(IEnumerable<ModProfile> modProfiles)
        {
            bool result = false;

            CacheClient.SaveModProfiles(modProfiles, (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Iterates through all of the mod profiles in the cache.</summary>
        [Obsolete("Use RequestAllModProfiles(Action<IList<ModProfile>>) instead.")]
        public static IEnumerable<ModProfile> IterateAllModProfiles()
        {
            IList<ModProfile> result = null;

            CacheClient.RequestAllModProfiles((r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Iterates through all of the mod profiles from the given offset.</summary>
        [Obsolete("Use RequestAllModProfilesFromOffset(int, Action<IList<ModProfile>>) instead.")]
        public static IEnumerable<ModProfile> IterateAllModProfilesFromOffset(int offset)
        {
            IList<ModProfile> result = null;

            CacheClient.RequestAllModProfilesFromOffset(offset, (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Iterates through all of the mod profiles returning only those matching the id filter.</summary>
        [Obsolete("Use RequestFilteredModProfiles(IList<int>, Action<IList<ModProfile>>) instead.")]
        public static IEnumerable<ModProfile> IterateFilteredModProfiles(IList<int> idFilter)
        {
            IList<ModProfile> result = null;

            CacheClient.RequestFilteredModProfiles(idFilter, (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Deletes all of a mod's data from the cache.</summary>
        [Obsolete("Use DeleteMod(int modId, Action<bool> onComplete)")]
        public static bool DeleteMod(int modId)
        {
            bool result = false;

            CacheClient.DeleteMod(modId, (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Determines how many ModProfiles are currently stored in the cache.</summary>
        [Obsolete("No longer supported.", true)]
        public static int CountModProfiles()
        {
            return -1;
        }

        /// <summary>[Obsolete] Stores a mod's statistics in the cache.</summary>
        [Obsolete("Use SaveModStatistics(ModStatistics, Action<bool>) instead.")]
        public static bool SaveModStatistics(ModStatistics stats)
        {
            bool result = false;

            CacheClient.SaveModStatistics(stats, (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Retrieves a mod's statistics from the cache.</summary>
        [Obsolete("Use LoadModStatistics(int, Action<ModStatistics>) instead.")]
        public static ModStatistics LoadModStatistics(int modId)
        {
            ModStatistics result = null;

            CacheClient.LoadModStatistics(modId, (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Stores a modfile in the cache.</summary>
        [Obsolete("Use SaveModfile(Modfile, Action<bool>) instead.")]
        public static bool SaveModfile(Modfile modfile)
        {
            bool result = false;

            CacheClient.SaveModfile(modfile, (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Retrieves a modfile from the cache.</summary>
        [Obsolete("Use LoadModfile(int, int, Action<Modfile>) instead.")]
        public static Modfile LoadModfile(int modId, int modfileId)
        {
            Modfile result = null;

            CacheClient.LoadModfile(modId, modfileId, (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Stores a mod binary's ZipFile data in the cache.</summary>
        [Obsolete("Use SaveModBinaryZip(int, int, byte[], Action<bool>) instead.")]
        public static bool SaveModBinaryZip(int modId, int modfileId, byte[] modBinary)
        {
            bool result = false;

            CacheClient.SaveModBinaryZip(modId, modfileId, modBinary,
                                         (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Retrieves a mod binary's ZipFile data from the cache.</summary>
        [Obsolete("Use LoadModBinaryZip(int, int, Action<byte[]>) instead.")]
        public static byte[] LoadModBinaryZip(int modId, int modfileId)
        {
            byte[] result = null;

            CacheClient.LoadModBinaryZip(modId, modfileId, (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Deletes a modfile and binary from the cache.</summary>
        [Obsolete("Use DeleteModfileAndBinaryZip(int, int, Action<bool>) instead.")]
        public static bool DeleteModfileAndBinaryZip(int modId, int modfileId)
        {
            bool result = false;

            CacheClient.DeleteModfileAndBinaryZip(modId, modfileId,
                                                  (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Deletes all modfiles and binaries from the cache.</summary>
        [Obsolete("Use DeleteAllModfileAndBinaryData(int, Action<bool>) instead.")]
        public static bool DeleteAllModfileAndBinaryData(int modId)
        {
            bool result = false;

            CacheClient.DeleteAllModfileAndBinaryData(modId, (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Retrieves the file paths for the mod logos in the cache.</summary>
        [Obsolete("Use GetModLogoVersionFileNames(int, Action<IDictionary<LogoSize, string>>) instead.")]
        public static Dictionary<LogoSize, string> GetModLogoVersionFileNames(int modId)
        {
            Dictionary<LogoSize, string> result = null;

            CacheClient.GetModLogoVersionFileNames(modId, (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Stores a mod logo in the cache with the given fileName.</summary>
        [Obsolete("Use SaveModLogo(int, string, LogoSize, Texture2D, Action<bool>) instead.")]
        public static bool SaveModLogo(int modId, string fileName, LogoSize size, Texture2D logoTexture)
        {
            bool result = false;

            CacheClient.SaveModLogo(modId, fileName,
                                    size, logoTexture,
                                    (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Retrieves a mod logo from the cache.</summary>
        [Obsolete("Use LoadModLogo(int, LogoSize, Action<bool>) instead.")]
        public static Texture2D LoadModLogo(int modId, LogoSize size)
        {
            Texture2D result = null;

            CacheClient.LoadModLogo(modId, size, (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Retrieves a mod logo from the cache if it matches the given fileName.</summary>
        [Obsolete("Use LoadModLogo(int, string, LogoSize, Action<bool>) instead.")]
        public static Texture2D LoadModLogo(int modId, string fileName, LogoSize size)
        {
            Texture2D result = null;

            CacheClient.LoadModLogo(modId, fileName, size, (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Retrieves the information for the cached mod logos.</summary>
        [Obsolete("Use GetModLogoFileName(int, LogoSize, Action<string>) instead.")]
        public static string GetModLogoFileName(int modId, LogoSize size)
        {
            string result = null;

            CacheClient.GetModLogoFileName(modId, size, (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Stores a mod gallery image in the cache.</summary>
        [Obsolete("Use SaveModGalleryImage(int, string, ModGalleryImageSize, Texture2D, Action<bool>) instead.")]
        public static bool SaveModGalleryImage(int modId,
                                               string imageFileName,
                                               ModGalleryImageSize size,
                                               Texture2D imageTexture)
        {
            bool result = false;

            CacheClient.SaveModGalleryImage(modId, imageFileName, size, imageTexture,
                                            (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Retrieves a mod gallery image from the cache.</summary>
        [Obsolete("Use LoadModGalleryImage(int, string, ModGalleryImageSize, Action<Texture2D>) instead.")]
        public static Texture2D LoadModGalleryImage(int modId, string imageFileName, ModGalleryImageSize size)
        {
            Texture2D result = null;

            CacheClient.LoadModGalleryImage(modId, imageFileName, size,
                                            (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Stores a YouTube thumbnail in the cache.</summary>
        [Obsolete("Use SaveModYouTubeThumbnail(int, string, Texture2D, Action<bool>) instead.")]
        public static bool SaveModYouTubeThumbnail(int modId, string youTubeId, Texture2D thumbnail)
        {
            bool result = false;

            CacheClient.SaveModYouTubeThumbnail(modId, youTubeId, thumbnail,
                                                (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Retrieves a YouTube thumbnail from the cache.</summary>
        [Obsolete("Use LoadModYouTubeThumbnail(int, string, Action<Texture2D>) instead.")]
        public static Texture2D LoadModYouTubeThumbnail(int modId, string youTubeId)
        {
            Texture2D result = null;

            CacheClient.LoadModYouTubeThumbnail(modId, youTubeId,
                                                (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Stores a mod team's data in the cache.</summary>
        [Obsolete("Use SaveModTeam(int, List<ModTeamMember>, Action<bool>) instead.")]
        public static bool SaveModTeam(int modId, List<ModTeamMember> modTeam)
        {
            bool result = false;

            CacheClient.SaveModTeam(modId, modTeam,
                                    (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Retrieves a mod team's data from the cache.</summary>
        [Obsolete("Use LoadModTeam(int, Action<List<ModTeamMember>>) instead.")]
        public static List<ModTeamMember> LoadModTeam(int modId)
        {
            List<ModTeamMember> result = null;

            CacheClient.LoadModTeam(modId,
                                    (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Deletes a mod team's data from the cache.</summary>
        [Obsolete("Use DeleteModTeam(int, Action<bool>) instead.")]
        public static bool DeleteModTeam(int modId)
        {
            bool result = false;

            CacheClient.DeleteModTeam(modId,
                                      (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Stores a user's avatar in the cache.</summary>
        [Obsolete("Use SaveUserAvatar(int, UserAvatarSize, Texture2D, Action<bool>) instead.")]
        public static bool SaveUserAvatar(int userId, UserAvatarSize size, Texture2D avatarTexture)
        {
            bool result = false;

            CacheClient.SaveUserAvatar(userId, size, avatarTexture,
                                       (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Retrieves a user's avatar from the cache.</summary>
        [Obsolete("Use LoadUserAvatar(int, UserAvatarSize, Action<Texture2D>) instead.")]
        public static Texture2D LoadUserAvatar(int userId, UserAvatarSize size)
        {
            Texture2D result = null;

            CacheClient.LoadUserAvatar(userId, size,
                                       (r) => result = r);

            return result;
        }

        /// <summary>[Obsolete] Delete's a user's avatars from the cache.</summary>
        [Obsolete("Use DeleteUserAvatar(int, Action<bool>) instead.")]
        public static bool DeleteUserAvatar(int userId)
        {
            bool result = false;

            CacheClient.DeleteUserAvatar(userId,
                                         (r) => result = r);

            return result;
        }
    }
}
