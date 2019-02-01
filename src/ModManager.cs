using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Newtonsoft.Json;

using ModIO.API;

namespace ModIO
{
    public static class ModManager
    {
        // ---------[ NESTED FIELDS ]---------
        private struct PersistentData
        {
            public SimpleVersion lastRunVersion;
            public List<int> subscribedModIds;
            public List<int> enabledModIds;
        }

        // ---------[ CONSTANTS & STATICS ]---------
        /// <summary>Current version of the ModManager/Plugin.</summary>
        public static readonly SimpleVersion VERSION = new SimpleVersion(0, 10);

        /// <summary>File name used to store the persistent data.</summary>
        public const string PERSISTENTDATA_FILENAME = "mod_manager.data";

        /// <summary>Install directory used by the ModManager.</summary>
        public static string installDirectory;

        /// <summary>Data that needs to be stored across sessions.</summary>
        private static PersistentData m_data;

        // ---------[ INITIALIZATION ]---------
        /// <summary>Initialzes the ModManager settings.</summary>
        static ModManager()
        {
            PluginSettings settings = PluginSettings.LoadDefaults();
            ModManager.installDirectory = settings.installDirectory;

            string dataPath = IOUtilities.CombinePath(CacheClient.cacheDirectory, PERSISTENTDATA_FILENAME);

            if(!IOUtilities.TryReadJsonObjectFile(dataPath, out ModManager.m_data))
            {
                ModManager.m_data = new PersistentData()
                {
                    lastRunVersion = ModManager.VERSION,
                    subscribedModIds = new List<int>(),
                    enabledModIds = new List<int>(),
                };
            }

            VersionUpdater.Run(m_data.lastRunVersion);

            m_data.lastRunVersion = VERSION;
            IOUtilities.WriteJsonObjectFile(dataPath, ModManager.m_data);
        }


        // ---------[ MOD MANAGEMENT ]---------
        /// <summary>Returns the subscribed mods.</summary>
        public static List<int> GetSubscribedModIds()
        {
            return m_data.subscribedModIds;
        }
        /// <summary>Sets the subscribed mods and writes the data to disk.</summary>
        public static void SetSubscribedModIds(IEnumerable<int> modIds)
        {
            ModManager.m_data.subscribedModIds = new List<int>(modIds);
            string dataPath = IOUtilities.CombinePath(CacheClient.cacheDirectory, PERSISTENTDATA_FILENAME);
            IOUtilities.WriteJsonObjectFile(dataPath, ModManager.m_data);
        }

        /// <summary>Returns the enabled mods.</summary>
        public static List<int> GetEnabledModIds()
        {
            return m_data.enabledModIds;
        }
        /// <summary>Sets the enabled mods and writes the data to disk.</summary>
        public static void SetEnabledModIds(IEnumerable<int> modIds)
        {
            ModManager.m_data.enabledModIds = new List<int>(modIds);
            string dataPath = IOUtilities.CombinePath(CacheClient.cacheDirectory, PERSISTENTDATA_FILENAME);
            IOUtilities.WriteJsonObjectFile(dataPath, ModManager.m_data);
        }

        /// <summary>Creates the Directory Path for a given modfile install.</summary>
        public static string GetModInstallDirectory(int modId, int modfileId)
        {
            return IOUtilities.CombinePath(ModManager.installDirectory,
                                           modId.ToString() + "_" + modfileId.ToString());
        }

        /// <summary>Extracts a mod archive to the installs folder and removes other installed versions.</summary>
        public static bool TryInstallMod(int modId, int modfileId, bool removeArchiveOnSuccess)
        {
            // Needs to have a valid mod id otherwise we mess with player-added mods!
            Debug.Assert(modId != ModProfile.NULL_ID);

            string zipFilePath = CacheClient.GenerateModBinaryZipFilePath(modId, modfileId);
            if(!File.Exists(zipFilePath))
            {
                Debug.LogWarning("[mod.io] Unable to extract binary to the mod install folder."
                                 + "\nMod Binary ZipFile [" + zipFilePath + "] does not exist.");
                return false;
            }
            if(!ModManager.TryUninstallAllModVersions(modId))
            {
                Debug.LogWarning("[mod.io] Unable to extract binary to the mod install folder."
                                 + "\nFailed to uninstall other versions of this mod.");
                return false;
            }

            string unzipLocation = GetModInstallDirectory(modId, modfileId);
            try
            {
                Directory.CreateDirectory(unzipLocation);

                using (var zip = Ionic.Zip.ZipFile.Read(zipFilePath))
                {
                    zip.ExtractAll(unzipLocation);
                }

                if(removeArchiveOnSuccess)
                {
                    IOUtilities.DeleteFile(zipFilePath);
                }

                return true;
            }
            catch(Exception e)
            {
                Debug.LogWarning("[mod.io] Unable to extract binary to the mod install folder."
                                 + "\nLocation: " + unzipLocation + "\n\n"
                                 + Utility.GenerateExceptionDebugString(e));

                return false;
            }
        }

        /// <summary>Removes all versions of a mod from the installs folder.</summary>
        public static bool TryUninstallAllModVersions(int modId)
        {
            // Don't accidentally uninstall player-added mods!
            Debug.Assert(modId != ModProfile.NULL_ID);

            var installedMods = ModManager.IterateInstalledMods(new int[] { modId });

            bool succeeded = true;
            foreach(var installInfo in installedMods)
            {
                succeeded = IOUtilities.DeleteDirectory(installInfo.Value) && succeeded;
            }

            return succeeded;
        }

        /// <summary>Removes a specific version of a mod from the installs folder.</summary>
        public static bool TryUninstallModVersion(int modId, int modfileId)
        {
            // Don't accidentally uninstall player-added mods!
            Debug.Assert(modId != ModProfile.NULL_ID);

            var installedMods = ModManager.IterateInstalledMods(new int[] { modId });

            bool succeeded = true;
            foreach(var installInfo in installedMods)
            {
                if(installInfo.Key.modfileId == modfileId)
                {
                    succeeded = IOUtilities.DeleteDirectory(installInfo.Value) && succeeded;
                }
            }

            return succeeded;
        }

        /// <summary>Returns all of the mod directories of installed mods.</summary>
        public static List<string> GetInstalledModDirectories(bool excludeDisabledMods)
        {
            List<int> modIdFilter = null;
            if(excludeDisabledMods)
            {
                modIdFilter = new List<int>(ModManager.GetEnabledModIds());
                // Include drop-ins
                modIdFilter.Add(ModProfile.NULL_ID);
            }

            List<string> directories = new List<string>();
            var installedModInfo = ModManager.IterateInstalledMods(modIdFilter);
            foreach(var kvp in installedModInfo)
            {
                directories.Add(kvp.Value);
            }

            return directories;
        }

        /// <summary>Returns all of the mod version info of installed mods.</summary>
        public static List<ModfileIdPair> GetInstalledModVersions(bool excludeDisabledMods)
        {
            List<int> modIdFilter = null;
            if(excludeDisabledMods)
            {
                modIdFilter = new List<int>(ModManager.GetEnabledModIds());
            }

            List<ModfileIdPair> versions = new List<ModfileIdPair>();
            var installedModInfo = ModManager.IterateInstalledMods(modIdFilter);
            foreach(var kvp in installedModInfo)
            {
                versions.Add(kvp.Key);
            }

            return versions;
        }

        /// <summary>Returns the data of all the mods installed.</summary>
        public static IEnumerable<KeyValuePair<ModfileIdPair, string>> IterateInstalledMods(IList<int> modIdFilter)
        {
            string[] modDirectories = new string[0];
            try
            {
                if(Directory.Exists(ModManager.installDirectory))
                {
                    modDirectories = Directory.GetDirectories(ModManager.installDirectory);
                }
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to read mod installation directory."
                                      + "\nDirectory: " + ModManager.installDirectory + "\n\n");

                Debug.LogWarning(warningInfo
                                 + Utility.GenerateExceptionDebugString(e));
            }

            foreach(string modDirectory in modDirectories)
            {
                string folderName = IOUtilities.GetPathItemName(modDirectory);
                string[] folderNameParts = folderName.Split('_');

                int modId;
                int modfileId;
                if(!(folderNameParts.Length > 0
                     && Int32.TryParse(folderNameParts[0], out modId)))
                {
                    modId = ModProfile.NULL_ID;
                }

                if(modIdFilter == null
                   || modIdFilter.Contains(modId))
                {
                    if(!(modId != ModProfile.NULL_ID
                         && folderNameParts.Length > 1
                         && Int32.TryParse(folderNameParts[1], out modfileId)))
                    {
                        modfileId = Modfile.NULL_ID;
                    }

                    ModfileIdPair idPair = new ModfileIdPair()
                    {
                        modId = modId,
                        modfileId = modfileId,
                    };

                    var info = new KeyValuePair<ModfileIdPair, string>(idPair, modDirectory);
                    yield return info;
                }
            }
        }


        // ---------[ GAME PROFILE ]---------
        /// <summary>Fetches and caches the Game Profile (if not already cached).</summary>
        public static void GetGameProfile(Action<GameProfile> onSuccess,
                                          Action<WebRequestError> onError)
        {
            GameProfile cachedProfile = CacheClient.LoadGameProfile();

            if(cachedProfile != null)
            {
                onSuccess(cachedProfile);
            }
            else
            {
                Action<GameProfile> onGetProfile = (profile) =>
                {
                    CacheClient.SaveGameProfile(profile);
                    if(onSuccess != null) { onSuccess(profile); }
                };

                APIClient.GetGame(onGetProfile,
                                  onError);
            }
        }


        // ---------[ MOD PROFILES ]---------
        /// <summary>Fetches and caches a Mod Profile (if not already cached).</summary>
        public static void GetModProfile(int modId,
                                         Action<ModProfile> onSuccess,
                                         Action<WebRequestError> onError)
        {
            var cachedProfile = CacheClient.LoadModProfile(modId);

            if(cachedProfile != null)
            {
                if(onSuccess != null) { onSuccess(cachedProfile); }
            }
            else
            {
                // - Fetch from Server -
                Action<ModProfile> onGetMod = (profile) =>
                {
                    CacheClient.SaveModProfile(profile);
                    if(onSuccess != null) { onSuccess(profile); }
                };

                APIClient.GetMod(modId,
                                 onGetMod,
                                 onError);
            }
        }

        /// <summary>Fetches and caches Mod Profiles (if not already cached).</summary>
        public static void GetModProfiles(IEnumerable<int> modIds,
                                          Action<List<ModProfile>> onSuccess,
                                          Action<WebRequestError> onError)
        {
            List<int> missingModIds = new List<int>(modIds);
            List<ModProfile> modProfiles = new List<ModProfile>(missingModIds.Count);

            foreach(ModProfile profile in CacheClient.IterateAllModProfiles())
            {
                if(missingModIds.Contains(profile.id))
                {
                    missingModIds.Remove(profile.id);
                    modProfiles.Add(profile);
                }
            }

            if(missingModIds.Count == 0)
            {
                if(onSuccess != null) { onSuccess(modProfiles); }
            }
            else
            {
                // - Filter -
                RequestFilter modFilter = new RequestFilter();
                modFilter.sortFieldName = GetAllModsFilterFields.id;
                modFilter.fieldFilters[GetAllModsFilterFields.id]
                = new InArrayFilter<int>()
                {
                    filterArray = missingModIds.ToArray()
                };

                Action<List<ModProfile>> onGetMods = (profiles) =>
                {
                    modProfiles.AddRange(profiles);

                    CacheClient.SaveModProfiles(profiles);

                    if(onSuccess != null) { onSuccess(modProfiles); }
                };

                // - Get All Events -
                ModManager.FetchAllResultsForQuery<ModProfile>((p,s,e) => APIClient.GetAllMods(modFilter, p, s, e),
                                                               onGetMods,
                                                               onError);
            }
        }


        // ---------[ MODFILES ]---------
        /// <summary>Fetches and caches a Modfile (if not already cached).</summary>
        public static void GetModfile(int modId, int modfileId,
                                      Action<Modfile> onSuccess,
                                      Action<WebRequestError> onError)
        {
            var cachedModfile = CacheClient.LoadModfile(modId, modfileId);

            if(cachedModfile != null)
            {
                if(onSuccess != null) { onSuccess(cachedModfile); }
            }
            else
            {
                // - Fetch from Server -
                Action<Modfile> onGetModfile = (modfile) =>
                {
                    CacheClient.SaveModfile(modfile);
                    if(onSuccess != null) { onSuccess(modfile); }
                };

                APIClient.GetModfile(modId, modfileId,
                                     onGetModfile,
                                     onError);
            }
        }


        // ---------[ MOD STATS ]---------
        /// <summary>Fetches and caches a Mod's Statistics (if not already cached or if expired).</summary>
        public static void GetModStatistics(int modId,
                                            Action<ModStatistics> onSuccess,
                                            Action<WebRequestError> onError)
        {
            var cachedStats = CacheClient.LoadModStatistics(modId);

            if(cachedStats != null
               && cachedStats.dateExpires > ServerTimeStamp.Now)
            {
                if(onSuccess != null) { onSuccess(cachedStats); }
            }
            else
            {
                // - Fetch from Server -
                Action<ModStatistics> onGetStats = (stats) =>
                {
                    CacheClient.SaveModStatistics(stats);
                    if(onSuccess != null) { onSuccess(stats); }
                };

                APIClient.GetModStats(modId,
                                      onGetStats,
                                      onError);
            }
        }


        // ---------[ USERS ]---------
        /// <summary>Fetches and caches a User Profile (if not already cached).</summary>
        public static void GetUserProfile(int userId,
                                          Action<UserProfile> onSuccess,
                                          Action<WebRequestError> onError)
        {
            var cachedProfile = CacheClient.LoadUserProfile(userId);

            if(cachedProfile != null)
            {
                if(onSuccess != null) { onSuccess(cachedProfile); }
            }
            else
            {
                // - Fetch from Server -
                Action<UserProfile> onGetUser = (profile) =>
                {
                    CacheClient.SaveUserProfile(profile);
                    if(onSuccess != null) { onSuccess(profile); }
                };

                APIClient.GetUser(userId,
                                  onGetUser,
                                  onError);
            }
        }

        /// <summary>Fetches and caches a User Avatar (if not already cached).</summary>
        public static void GetUserAvatar(UserProfile profile,
                                         UserAvatarSize size,
                                         Action<Texture2D> onSuccess,
                                         Action<WebRequestError> onError)
        {
            Debug.Assert(profile != null, "[mod.io] User profile must not be null");

            ModManager.GetUserAvatar(profile.id, profile.avatarLocator, size,
                                     onSuccess, onError);
        }

        /// <summary>Fetches and caches a User Avatar (if not already cached).</summary>
        public static void GetUserAvatar(int userId,
                                         AvatarImageLocator avatarLocator,
                                         UserAvatarSize size,
                                         Action<Texture2D> onSuccess,
                                         Action<WebRequestError> onError)
        {
            Debug.Assert(avatarLocator != null);

            var cachedAvatarTexture = CacheClient.LoadUserAvatar(userId, size);
            if(cachedAvatarTexture != null)
            {
                onSuccess(cachedAvatarTexture);
            }
            else
            {
                // - Fetch from Server -
                var download = DownloadClient.DownloadImage(avatarLocator.GetSizeURL(size));

                download.succeeded += (d) =>
                {
                    CacheClient.SaveUserAvatar(userId, size, d.imageTexture);
                };

                download.succeeded += (d) => onSuccess(d.imageTexture);

                if(onError != null)
                {
                    download.failed += (d) => onError(d.error);
                }
            }
        }


        // ---------[ EVENTS ]---------
        /// <summary>Fetches all mod events for the game.</summary>
        public static void FetchAllModEvents(int fromTimeStamp,
                                             int untilTimeStamp,
                                             Action<List<ModEvent>> onSuccess,
                                             Action<WebRequestError> onError)
        {
            ModManager.FetchModEvents(new int[0], fromTimeStamp, untilTimeStamp,
                                      onSuccess, onError);
        }

        /// <summary>Fetches all mod events for the given mod ids.</summary>
        public static void FetchModEvents(IEnumerable<int> modIdFilter,
                                          int fromTimeStamp,
                                          int untilTimeStamp,
                                          Action<List<ModEvent>> onSuccess,
                                          Action<WebRequestError> onError)
        {
            Debug.Assert(modIdFilter != null);

            // - Filter -
            RequestFilter modEventFilter = new RequestFilter();
            modEventFilter.sortFieldName = GetAllModEventsFilterFields.dateAdded;
            modEventFilter.fieldFilters[GetAllModEventsFilterFields.dateAdded]
            = new RangeFilter<int>()
            {
                min = fromTimeStamp,
                isMinInclusive = false,
                max = untilTimeStamp,
                isMaxInclusive = true,
            };
            modEventFilter.fieldFilters[GetAllModEventsFilterFields.modId]
            = new InArrayFilter<int>()
            {
                filterArray = modIdFilter.ToArray(),
            };

            // - Get All Events -
            ModManager.FetchAllResultsForQuery<ModEvent>((p,s,e) => APIClient.GetAllModEvents(modEventFilter, p, s, e),
                                                         onSuccess,
                                                         onError);
        }

        /// <summary>Fetches all user events for the authenticated user.</summary>
        public static void FetchAllUserEvents(int fromTimeStamp,
                                              int untilTimeStamp,
                                              Action<List<UserEvent>> onSuccess,
                                              Action<WebRequestError> onError)
        {
            // - Filter -
            RequestFilter userEventFilter = new RequestFilter();
            userEventFilter.sortFieldName = GetUserEventsFilterFields.dateAdded;
            userEventFilter.fieldFilters[GetUserEventsFilterFields.dateAdded]
            = new RangeFilter<int>()
            {
                min = fromTimeStamp,
                isMinInclusive = false,
                max = untilTimeStamp,
                isMaxInclusive = true,
            };
            userEventFilter.fieldFilters[GetUserEventsFilterFields.gameId]
            = new EqualToFilter<int>()
            {
                filterValue = APIClient.gameId,
            };

            // - Get All Events -
            ModManager.FetchAllResultsForQuery<UserEvent>((p,s,e) => APIClient.GetUserEvents(userEventFilter, p, s, e),
                                                          onSuccess,
                                                          onError);
        }


        // ---------[ MOD IMAGES ]---------
        public static void GetModLogo(ModProfile profile, LogoSize size,
                                      Action<Texture2D> onSuccess,
                                      Action<WebRequestError> onError)
        {
            GetModLogo(profile.id, profile.logoLocator, size, onSuccess, onError);
        }

        public static void GetModLogo(int modId, LogoImageLocator logoLocator,
                                      LogoSize size,
                                      Action<Texture2D> onSuccess,
                                      Action<WebRequestError> onError)
        {
            Debug.Assert(onSuccess != null);

            var logoTexture = CacheClient.LoadModLogo(modId, logoLocator.fileName, size);
            if(logoTexture != null)
            {
                onSuccess(logoTexture);
            }
            else
            {
                var textureDownload = DownloadClient.DownloadImage(logoLocator.GetSizeURL(size));

                textureDownload.succeeded += (d) =>
                {
                    CacheClient.SaveModLogo(modId, logoLocator.GetFileName(),
                                            size, d.imageTexture);
                };

                textureDownload.succeeded += (d) => onSuccess(d.imageTexture);
                textureDownload.failed += (d) => onError(d.error);
            }
        }

        public static void GetModGalleryImage(ModProfile profile,
                                              string imageFileName,
                                              ModGalleryImageSize size,
                                              Action<Texture2D> onSuccess,
                                              Action<WebRequestError> onError)
        {
            ModManager.GetModGalleryImage(profile.id, profile.media.GetGalleryImageWithFileName(imageFileName), size, onSuccess, onError);
        }

        public static void GetModGalleryImage(int modId,
                                              GalleryImageLocator imageLocator,
                                              ModGalleryImageSize size,
                                              Action<Texture2D> onSuccess,
                                              Action<WebRequestError> onError)
        {
            Debug.Assert(modId > 0, "[mod.io] modId parameter is invalid.");
            Debug.Assert(imageLocator != null, "[mod.io] imageLocator parameter cannot be null.");
            Debug.Assert(!String.IsNullOrEmpty(imageLocator.fileName), "[mod.io] imageFileName parameter needs to be not null or empty (used as identifier for gallery images)");

            var cachedImageTexture = CacheClient.LoadModGalleryImage(modId,
                                                                     imageLocator.fileName,
                                                                     size);

            if(cachedImageTexture != null)
            {
                if(onSuccess != null) { onSuccess(cachedImageTexture); }
            }
            else
            {
                // - Fetch from Server -
                var download = DownloadClient.DownloadModGalleryImage(imageLocator,
                                                                      size);

                download.succeeded += (d) =>
                {
                    CacheClient.SaveModGalleryImage(modId,
                                                    imageLocator.fileName,
                                                    size,
                                                    d.imageTexture);
                };

                download.succeeded += (d) => onSuccess(d.imageTexture);
                download.failed += (d) => onError(d.error);
            }
        }

        public static void GetModYouTubeThumbnail(int modId,
                                                  string youTubeVideoId,
                                                  Action<Texture2D> onSuccess,
                                                  Action<WebRequestError> onError)
        {
            Debug.Assert(modId > 0, "[mod.io] modId parameter must be a valid mod profile id.");
            Debug.Assert(!String.IsNullOrEmpty(youTubeVideoId),
                         "[mod.io] youTubeVideoId parameter must not be null or empty.");

            var cachedYouTubeThumbnail = CacheClient.LoadModYouTubeThumbnail(modId,
                                                                             youTubeVideoId);

            if(cachedYouTubeThumbnail != null)
            {
                if(onSuccess != null) { onSuccess(cachedYouTubeThumbnail); }
            }
            else
            {
                var download = DownloadClient.DownloadYouTubeThumbnail(youTubeVideoId);

                download.succeeded += (d) =>
                {
                    CacheClient.SaveModYouTubeThumbnail(modId, youTubeVideoId, d.imageTexture);
                };

                download.succeeded += (d) => onSuccess(d.imageTexture);
                download.failed += (d) => onError(d.error);
            }
        }

        // ---------[ MOD TEAMS ]---------
        public static void GetModTeam(int modId,
                                      Action<List<ModTeamMember>> onSuccess,
                                      Action<WebRequestError> onError)
        {
            List<ModTeamMember> cachedModTeam = CacheClient.LoadModTeam(modId);

            if(cachedModTeam != null)
            {
                if(onSuccess != null) { onSuccess(cachedModTeam); }
            }
            else
            {
                // - Get All Team Members -
                Action<List<ModTeamMember>> onGetModTeam = (modTeam) =>
                {
                    CacheClient.SaveModTeam(modId, modTeam);
                    if(onSuccess != null) { onSuccess(modTeam); }
                };

                ModManager.FetchAllResultsForQuery<ModTeamMember>((p,s,e) => APIClient.GetAllModTeamMembers(modId, RequestFilter.None,
                                                                                                            p, s, e),
                                                                  onGetModTeam,
                                                                  onError);
            }
        }


        // ---------[ UPLOADING ]---------
        public static void SubmitNewMod(EditableModProfile modEdits,
                                        Action<ModProfile> modSubmissionSucceeded,
                                        Action<WebRequestError> modSubmissionFailed)
        {
            // - Client-Side error-checking -
            WebRequestError error = null;
            if(String.IsNullOrEmpty(modEdits.name.value))
            {
                error = WebRequestError.GenerateLocal("Mod Profile needs to be named before it can be uploaded");
            }
            else if(String.IsNullOrEmpty(modEdits.summary.value))
            {
                error = WebRequestError.GenerateLocal("Mod Profile needs to be given a summary before it can be uploaded");
            }
            else if(!File.Exists(modEdits.logoLocator.value.url))
            {
                error = WebRequestError.GenerateLocal("Mod Profile needs to be assigned a logo before it can be uploaded");
            }

            if(error != null)
            {
                modSubmissionFailed(error);
                return;
            }

            // - Initial Mod Submission -
            var parameters = new AddModParameters();
            parameters.name = modEdits.name.value;
            parameters.summary = modEdits.summary.value;
            parameters.logo = BinaryUpload.Create(Path.GetFileName(modEdits.logoLocator.value.url),
                                                      File.ReadAllBytes(modEdits.logoLocator.value.url));
            if(modEdits.visibility.isDirty)
            {
                parameters.visibility = modEdits.visibility.value;
            }
            if(modEdits.nameId.isDirty)
            {
                parameters.nameId = modEdits.nameId.value;
            }
            if(modEdits.descriptionAsHTML.isDirty)
            {
                parameters.descriptionAsHTML = modEdits.descriptionAsHTML.value;
            }
            if(modEdits.homepageURL.isDirty)
            {
                parameters.nameId = modEdits.homepageURL.value;
            }
            if(modEdits.metadataBlob.isDirty)
            {
                parameters.metadataBlob = modEdits.metadataBlob.value;
            }
            if(modEdits.nameId.isDirty)
            {
                parameters.nameId = modEdits.nameId.value;
            }
            if(modEdits.tags.isDirty)
            {
                parameters.tags = modEdits.tags.value;
            }

            // NOTE(@jackson): As add Mod takes more parameters than edit,
            //  we can ignore some of the elements in the EditModParameters
            //  when passing to SubmitModProfileComponents
            var remainingModEdits = new EditableModProfile();
            remainingModEdits.youTubeURLs = modEdits.youTubeURLs;
            remainingModEdits.sketchfabURLs = modEdits.sketchfabURLs;
            remainingModEdits.galleryImageLocators = modEdits.galleryImageLocators;

            APIClient.AddMod(parameters,
                             result => SubmitModProfileComponents(result,
                                                                  remainingModEdits,
                                                                  modSubmissionSucceeded,
                                                                  modSubmissionFailed),
                             modSubmissionFailed);
        }

        public static void SubmitModChanges(int modId,
                                            EditableModProfile modEdits,
                                            Action<ModProfile> modSubmissionSucceeded,
                                            Action<WebRequestError> modSubmissionFailed)
        {
            Debug.Assert(modId > 0);

            Action<ModProfile> submitChanges = (profile) =>
            {
                if(modEdits.status.isDirty
                   || modEdits.visibility.isDirty
                   || modEdits.name.isDirty
                   || modEdits.nameId.isDirty
                   || modEdits.summary.isDirty
                   || modEdits.descriptionAsHTML.isDirty
                   || modEdits.homepageURL.isDirty
                   || modEdits.metadataBlob.isDirty)
                {
                    var parameters = new EditModParameters();
                    if(modEdits.status.isDirty)
                    {
                        parameters.status = modEdits.status.value;
                    }
                    if(modEdits.visibility.isDirty)
                    {
                        parameters.visibility = modEdits.visibility.value;
                    }
                    if(modEdits.name.isDirty)
                    {
                        parameters.name = modEdits.name.value;
                    }
                    if(modEdits.nameId.isDirty)
                    {
                        parameters.nameId = modEdits.nameId.value;
                    }
                    if(modEdits.summary.isDirty)
                    {
                        parameters.summary = modEdits.summary.value;
                    }
                    if(modEdits.descriptionAsHTML.isDirty)
                    {
                        parameters.descriptionAsHTML = modEdits.descriptionAsHTML.value;
                    }
                    if(modEdits.homepageURL.isDirty)
                    {
                        parameters.homepageURL = modEdits.homepageURL.value;
                    }
                    if(modEdits.metadataBlob.isDirty)
                    {
                        parameters.metadataBlob = modEdits.metadataBlob.value;
                    }

                    APIClient.EditMod(modId, parameters,
                                   (p) => SubmitModProfileComponents(profile, modEdits,
                                                                     modSubmissionSucceeded,
                                                                     modSubmissionFailed),
                                   modSubmissionFailed);
                }
                // - Get updated ModProfile -
                else
                {
                    SubmitModProfileComponents(profile,
                                               modEdits,
                                               modSubmissionSucceeded,
                                               modSubmissionFailed);
                }
            };

            ModManager.GetModProfile(modId,
                                     submitChanges,
                                     modSubmissionFailed);
        }

        private static void SubmitModProfileComponents(ModProfile profile,
                                                       EditableModProfile modEdits,
                                                       Action<ModProfile> modSubmissionSucceeded,
                                                       Action<WebRequestError> modSubmissionFailed)
        {
            List<Action> submissionActions = new List<Action>();
            int nextActionIndex = 0;
            Action<APIMessage> doNextSubmissionAction = (m) =>
            {
                if(nextActionIndex < submissionActions.Count)
                {
                    submissionActions[nextActionIndex++]();
                }
            };

            // - Media -
            if(modEdits.logoLocator.isDirty
               || modEdits.youTubeURLs.isDirty
               || modEdits.sketchfabURLs.isDirty
               || modEdits.galleryImageLocators.isDirty)
            {
                var addMediaParameters = new AddModMediaParameters();
                var deleteMediaParameters = new DeleteModMediaParameters();

                if(modEdits.logoLocator.isDirty
                   && File.Exists(modEdits.logoLocator.value.url))
                {
                    addMediaParameters.logo = BinaryUpload.Create(Path.GetFileName(modEdits.logoLocator.value.url),
                                                                  File.ReadAllBytes(modEdits.logoLocator.value.url));
                }

                if(modEdits.youTubeURLs.isDirty)
                {
                    var addedYouTubeLinks = new List<string>(modEdits.youTubeURLs.value);
                    foreach(string youtubeLink in profile.media.youTubeURLs)
                    {
                        addedYouTubeLinks.Remove(youtubeLink);
                    }
                    addMediaParameters.youtube = addedYouTubeLinks.ToArray();

                    var removedTags = new List<string>(profile.media.youTubeURLs);
                    foreach(string youtubeLink in modEdits.youTubeURLs.value)
                    {
                        removedTags.Remove(youtubeLink);
                    }
                    deleteMediaParameters.youtube = addedYouTubeLinks.ToArray();
                }

                if(modEdits.sketchfabURLs.isDirty)
                {
                    var addedSketchfabLinks = new List<string>(modEdits.sketchfabURLs.value);
                    foreach(string sketchfabLink in profile.media.sketchfabURLs)
                    {
                        addedSketchfabLinks.Remove(sketchfabLink);
                    }
                    addMediaParameters.sketchfab = addedSketchfabLinks.ToArray();

                    var removedTags = new List<string>(profile.media.sketchfabURLs);
                    foreach(string sketchfabLink in modEdits.sketchfabURLs.value)
                    {
                        removedTags.Remove(sketchfabLink);
                    }
                    deleteMediaParameters.sketchfab = addedSketchfabLinks.ToArray();
                }

                if(modEdits.galleryImageLocators.isDirty)
                {
                    var addedImageFilePaths = new List<string>();
                    foreach(var locator in modEdits.galleryImageLocators.value)
                    {
                        if(File.Exists(locator.url))
                        {
                            addedImageFilePaths.Add(locator.url);
                        }
                    }
                    // - Create Images.Zip -
                    if(addedImageFilePaths.Count > 0)
                    {
                        string galleryZipLocation = IOUtilities.CombinePath(Application.temporaryCachePath,
                                                                            "modio",
                                                                            "imageGallery_" + DateTime.Now.ToFileTime() + ".zip");

                        try
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(galleryZipLocation));

                            using(var zip = new Ionic.Zip.ZipFile())
                            {
                                foreach(string imageFilePath in addedImageFilePaths)
                                {
                                    zip.AddFile(imageFilePath);
                                }
                                zip.Save(galleryZipLocation);
                            }

                            var imageGalleryUpload = BinaryUpload.Create("images.zip",
                                                                         File.ReadAllBytes(galleryZipLocation));

                            addMediaParameters.galleryImages = imageGalleryUpload;
                        }
                        catch(Exception e)
                        {
                            Debug.LogError("[mod.io] Unable to zip image gallery prior to uploading.\n\n"
                                           + Utility.GenerateExceptionDebugString(e));
                        }
                    }

                    var removedImageFileNames = new List<string>();
                    foreach(var locator in profile.media.galleryImageLocators)
                    {
                        removedImageFileNames.Add(locator.fileName);
                    }
                    foreach(var locator in modEdits.galleryImageLocators.value)
                    {
                        removedImageFileNames.Remove(locator.fileName);
                    }

                    if(removedImageFileNames.Count > 0)
                    {
                        deleteMediaParameters.images = removedImageFileNames.ToArray();
                    }
                }

                if(addMediaParameters.stringValues.Count > 0
                   || addMediaParameters.binaryData.Count > 0)
                {
                    submissionActions.Add(() =>
                    {
                        APIClient.AddModMedia(profile.id,
                                              addMediaParameters,
                                              doNextSubmissionAction, modSubmissionFailed);
                    });
                }
                if(deleteMediaParameters.stringValues.Count > 0)
                {
                    submissionActions.Add(() =>
                    {
                        APIClient.DeleteModMedia(profile.id,
                                                 deleteMediaParameters,
                                                 () => doNextSubmissionAction(null),
                                                 modSubmissionFailed);
                    });
                }
            }

            // - Tags -
            if(modEdits.tags.isDirty)
            {
                var removedTags = new List<string>(profile.tagNames);
                foreach(string tag in modEdits.tags.value)
                {
                    removedTags.Remove(tag);
                }
                var addedTags = new List<string>(modEdits.tags.value);
                foreach(string tag in profile.tagNames)
                {
                    addedTags.Remove(tag);
                }

                if(removedTags.Count > 0)
                {
                    submissionActions.Add(() =>
                    {
                        var parameters = new DeleteModTagsParameters();
                        parameters.tagNames = removedTags.ToArray();
                        APIClient.DeleteModTags(profile.id, parameters,
                                             doNextSubmissionAction, modSubmissionFailed);
                    });
                }
                if(addedTags.Count > 0)
                {
                    submissionActions.Add(() =>
                    {
                        var parameters = new AddModTagsParameters();
                        parameters.tagNames = addedTags.ToArray();
                        APIClient.AddModTags(profile.id, parameters,
                                          doNextSubmissionAction, modSubmissionFailed);
                    });
                }
            }

            // - Metadata KVP -
            if(modEdits.metadataKVPs.isDirty)
            {
                var removedKVPs = MetadataKVP.ArrayToDictionary(profile.metadataKVPs);
                var addedKVPs = MetadataKVP.ArrayToDictionary(modEdits.metadataKVPs.value);

                foreach(MetadataKVP kvp in modEdits.metadataKVPs.value)
                {
                    string profileValue;

                    // if edited kvp is exact match it's not removed
                    if(removedKVPs.TryGetValue(kvp.key, out profileValue)
                        && profileValue == kvp.value)
                    {
                        removedKVPs.Remove(kvp.key);
                    }
                }

                foreach(MetadataKVP kvp in profile.metadataKVPs)
                {
                    string editValue;

                    // if profile kvp is exact match it's not new
                    if(addedKVPs.TryGetValue(kvp.key, out editValue)
                        && editValue == kvp.value)
                    {
                        addedKVPs.Remove(kvp.key);
                    }
                }

                if(removedKVPs.Count > 0)
                {
                    submissionActions.Add(() =>
                    {
                        var parameters = new DeleteModKVPMetadataParameters();
                        parameters.metadataKeys = removedKVPs.Keys.ToArray();
                        APIClient.DeleteModKVPMetadata(profile.id, parameters,
                                                       doNextSubmissionAction,
                                                       modSubmissionFailed);
                    });
                }

                if(addedKVPs.Count > 0)
                {
                    string[] addedKVPStrings = AddModKVPMetadataParameters.ConvertMetadataKVPsToAPIStrings(MetadataKVP.DictionaryToArray(addedKVPs));

                    submissionActions.Add(() =>
                    {
                        var parameters = new AddModKVPMetadataParameters();
                        parameters.metadata = addedKVPStrings;
                        APIClient.AddModKVPMetadata(profile.id, parameters,
                                                    doNextSubmissionAction,
                                                    modSubmissionFailed);
                    });
                }
            }

            // - Get Updated Profile -
            submissionActions.Add(() => APIClient.GetMod(profile.id,
                                                      modSubmissionSucceeded,
                                                      modSubmissionFailed));

            // - Start submission chain -
            doNextSubmissionAction(new APIMessage());
        }

        public static void UploadModBinaryDirectory(int modId,
                                                    EditableModfile modfileValues,
                                                    string binaryDirectory,
                                                    bool setActiveBuild,
                                                    Action<Modfile> onSuccess,
                                                    Action<WebRequestError> onError)
        {
            if(!Directory.Exists(binaryDirectory))
            {
                if(onError != null)
                {
                    onError(WebRequestError.GenerateLocal("Mod Binary directory [" + binaryDirectory + "] doesn't exist"));
                }
                return;
            }

            char lastCharacter = binaryDirectory[binaryDirectory.Length - 1];
            if(lastCharacter == Path.DirectorySeparatorChar
               || lastCharacter == Path.DirectorySeparatorChar)
            {
                binaryDirectory = binaryDirectory.Remove(binaryDirectory.Length - 1);
            }

            // - Zip Directory -
            string folderName = IOUtilities.GetPathItemName(binaryDirectory);
            string binaryZipLocation = IOUtilities.CombinePath(Application.temporaryCachePath,
                                                               "modio",
                                                               folderName + "_" + DateTime.Now.ToFileTime() + ".zip");
            bool zipSucceeded = false;
            int binaryDirectoryPathLength = binaryDirectory.Length + 1;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(binaryZipLocation));

                using(var zip = new Ionic.Zip.ZipFile())
                {
                    foreach(string filePath in Directory.GetFiles(binaryDirectory, "*.*", SearchOption.AllDirectories))
                    {
                        string relativeFilePath = filePath.Substring(binaryDirectoryPathLength);
                        string relativeDirectory = Path.GetDirectoryName(relativeFilePath);

                        zip.AddFile(filePath, relativeDirectory);
                        zip.Save(binaryZipLocation);
                    }
                }

                zipSucceeded = true;
            }
            catch(Exception e)
            {
                Debug.LogError("[mod.io] Unable to zip mod binary prior to uploading.\n\n"
                               + Utility.GenerateExceptionDebugString(e));

                if(onError != null)
                {
                    WebRequestError error = new WebRequestError()
                    {
                        message = "Unable to zip mod binary prior to uploading",
                        url = binaryZipLocation,
                        timeStamp = ServerTimeStamp.Now,
                        responseCode = 0,
                    };

                    onError(error);
                }
            }

            if(zipSucceeded)
            {
                UploadModBinary_Zipped(modId, modfileValues, binaryZipLocation, setActiveBuild, onSuccess, onError);
            }
        }

        public static void UploadModBinary_Unzipped(int modId,
                                                    EditableModfile modfileValues,
                                                    string unzippedBinaryLocation,
                                                    bool setActiveBuild,
                                                    Action<Modfile> onSuccess,
                                                    Action<WebRequestError> onError)
        {
            string binaryZipLocation = IOUtilities.CombinePath(Application.temporaryCachePath,
                                                               "modio",
                                                               Path.GetFileNameWithoutExtension(unzippedBinaryLocation) + "_" + DateTime.Now.ToFileTime() + ".zip");
            bool zipSucceeded = false;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(binaryZipLocation));

                using(var zip = new Ionic.Zip.ZipFile())
                {
                    zip.AddFile(unzippedBinaryLocation, "");
                    zip.Save(binaryZipLocation);
                }

                zipSucceeded = true;
            }
            catch(Exception e)
            {
                Debug.LogError("[mod.io] Unable to zip mod binary prior to uploading.\n\n"
                               + Utility.GenerateExceptionDebugString(e));

                if(onError != null)
                {
                    WebRequestError error = new WebRequestError()
                    {
                        message = "Unable to zip mod binary prior to uploading",
                        url = binaryZipLocation,
                        timeStamp = ServerTimeStamp.Now,
                        responseCode = 0,
                    };

                    onError(error);
                }
            }

            if(zipSucceeded)
            {
                UploadModBinary_Zipped(modId, modfileValues, binaryZipLocation, setActiveBuild, onSuccess, onError);
            }
        }

        public static void UploadModBinary_Zipped(int modId,
                                                  EditableModfile modfileValues,
                                                  string binaryZipLocation,
                                                  bool setActiveBuild,
                                                  Action<Modfile> onSuccess,
                                                  Action<WebRequestError> onError)
        {
            string buildFilename = Path.GetFileName(binaryZipLocation);
            byte[] buildZipData = File.ReadAllBytes(binaryZipLocation);

            var parameters = new AddModfileParameters();
            parameters.zippedBinaryData = BinaryUpload.Create(buildFilename, buildZipData);
            if(modfileValues.version.isDirty)
            {
                parameters.version = modfileValues.version.value;
            }
            if(modfileValues.changelog.isDirty)
            {
                parameters.changelog = modfileValues.changelog.value;
            }
            if(modfileValues.metadataBlob.isDirty)
            {
                parameters.metadataBlob = modfileValues.metadataBlob.value;
            }

            parameters.isActiveBuild = setActiveBuild;

            // - Generate Hash -
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = System.IO.File.OpenRead(binaryZipLocation))
                {
                    var hash = md5.ComputeHash(stream);
                    parameters.fileHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }

            APIClient.AddModfile(modId, parameters, onSuccess, onError);
        }

        // ---------[ FETCH ALL RESULTS HELPER ]---------
        private delegate void GetAllObjectsQuery<T>(APIPaginationParameters pagination,
                                                    Action<RequestPage<T>> onSuccess,
                                                    Action<WebRequestError> onError);

        private static void FetchAllResultsForQuery<T>(GetAllObjectsQuery<T> query,
                                                       Action<List<T>> onSuccess,
                                                       Action<WebRequestError> onError)
        {
            var pagination = new APIPaginationParameters()
            {
                limit = APIPaginationParameters.LIMIT_MAX,
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
                                                            RequestPage<T> queryResult,
                                                            APIPaginationParameters pagination,
                                                            List<T> culmativeResults,
                                                            Action<List<T>> onSuccess,
                                                            Action<WebRequestError> onError)
        {
            Debug.Assert(pagination.limit > 0);

            culmativeResults.AddRange(queryResult.items);

            if(queryResult.items.Length < queryResult.size)
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

        // ---------[ USER DATA ]---------
        [Obsolete]
        public static void GetAuthenticatedUserProfile(Action<UserProfile> onSuccess,
                                                       Action<WebRequestError> onError)
        {
            if(UserAuthenticationData.instance.userId != UserProfile.NULL_ID)
            {
                ModManager.GetUserProfile(UserAuthenticationData.instance.userId,
                                          onSuccess,
                                          onError);
            }
            else if(onSuccess != null)
            {
                onSuccess(null);
            }
        }

        public static void GetAuthenticatedUserMods(Action<List<ModProfile>> onSuccess,
                                                    Action<WebRequestError> onError)
        {
            RequestFilter userModsFilter = new RequestFilter();
            userModsFilter.fieldFilters[GetUserModFilterFields.gameId]
            = new EqualToFilter<int>() { filterValue = APIClient.gameId };

            Action<List<ModProfile>> onGetMods = (modProfiles) =>
            {
                List<int> modIds = new List<int>(modProfiles.Count);
                foreach(ModProfile profile in modProfiles)
                {
                    modIds.Add(profile.id);
                }

                if(onSuccess != null) { onSuccess(modProfiles); }
            };

            // - Get All Events -
            ModManager.FetchAllResultsForQuery<ModProfile>((p,s,e) => APIClient.GetUserMods(userModsFilter, p, s, e),
                                                           onGetMods,
                                                           onError);
        }
    }
}
