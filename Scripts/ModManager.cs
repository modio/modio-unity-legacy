// #define DO_NOT_LOAD_CACHE

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using ModIO.API;

using UnityEngine;

using Newtonsoft.Json;

// NOTE(@jackson): Had a weird bug where Initialize authenticated with a user.id of 0?
// TODO(@jackson): UISettings
// TODO(@jackson): ErrorWrapper to handle specific error codes
namespace ModIO
{
    public delegate void GameProfileEventHandler(GameProfile profile);
    public delegate void ModProfileEventHandler(ModProfile modProfile);
    public delegate void AuthenticatedUserEventHandler(AuthenticatedUser user);
    public delegate void ModIDEventHandler(int modId);
    public delegate void ModfileEventHandler(int modId, Modfile newModfile);
    public delegate void ModLogoUpdatedEventHandler(int modId, LogoVersion version, Texture2D texture);
    public delegate void ModGalleryImageUpdatedEventHandler(int modId, string imageFileName, ModGalleryImageVersion version, Texture2D texture);

    public delegate void ModProfilesEventHandler(IEnumerable<ModProfile> modProfiles);
    public delegate void ModIdsEventHandler(IEnumerable<int> modIds);
    public delegate void ModfileStubsEventHandler(IEnumerable<ModfileStub> modfiles);

    public enum ModBinaryStatus
    {
        Missing,
        RequiresUpdate,
        UpToDate
    }

    // TODO(@jackson): -> RequestManager?
    public class ModManager
    {
        // ---------[ INNER CLASSES ]---------
        [System.Serializable]
        private class ManifestData
        {
            public int lastUpdateTimeStamp = ServerTimeStamp.Now;
            public List<ModEvent> unresolvedEvents = new List<ModEvent>();
            public GameProfile gameProfile = new GameProfile();
            public List<string> serializedImageCache = new List<string>();
        }

        // ---------[ EVENTS ]---------
        public static event ModProfilesEventHandler modsAvailable;
        public static event ModProfilesEventHandler modsEdited;
        public static event ModfileStubsEventHandler modReleasesUpdated;
        public static event ModIdsEventHandler modsUnavailable;

        // ---------[ VARIABLES ]---------
        private static ManifestData manifest = null;
        private static AuthenticatedUser authUser = null;

        private static string cacheDirectory    { get { return CacheClient.GetCacheDirectory(); } }
        private static string manifestPath      { get { return cacheDirectory + "manifest.data"; } }
        private static string userdataPath      { get { return cacheDirectory + "user.data"; } }

        // ---------[ COROUTINE HELPERS ]---------
        private static void OnRequestSuccess<T>(T response, ClientRequest<T> request, out bool isDone)
        {
            request.response = response;
            isDone = true;
        }
        private static void OnRequestError<T>(WebRequestError error, ClientRequest<T> request, out bool isDone)
        {
            request.error = error;
            isDone = true;
        }

        // ---------[ GAME PROFILE ]---------
        public static IEnumerator RequestGameProfile(ClientRequest<GameProfile> request)
        {
            bool isDone = false;

            // - Attempt load from cache -
            CacheClient.LoadGameProfile((r) => ModManager.OnRequestSuccess(r, request, out isDone));

            while(!isDone) { yield return null; }

            if(request.response == null)
            {
                isDone = false;

                API.Client.GetGame((r) => ModManager.OnRequestSuccess(r, request, out isDone),
                                   (e) => ModManager.OnRequestError(e, request, out isDone));

                while(!isDone) { yield return null; }

                if(request.error == null)
                {
                    CacheClient.SaveGameProfile(request.response);
                }
            }
        }

        // ---------[ MOD PROFILES ]---------
        // TODO(@jackson): Implement GetModProfiles
        public static void GetModProfile(int modId,
                                         Action<ModProfile> onSuccess,
                                         Action<WebRequestError> onError)
        {
            CacheClient.LoadModProfile(modId,
                                        (cachedProfile) =>
            {
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

                    Client.GetMod(modId,
                                  onGetMod,
                                  onError);
                }
            });
        }

        // TODO(@jackson): Defend everything
        private static void FetchAndRebuildEntireCache(Action onSuccess,
                                                       Action<WebRequestError> onError)
        {
            Action<List<ModProfile>> onModProfilesReceived = (modProfiles) =>
            {
                CacheClient.SaveModProfiles(modProfiles);

                if(onSuccess != null)
                {
                    onSuccess();
                }
            };

            ModManager.FetchAllResultsForQuery<ModProfile>((p,s,e) => Client.GetAllMods(RequestFilter.None, p, s, e),
                                                           onModProfilesReceived,
                                                           onError);
            // TODO(@jackson): Other bits
        }

        // ---------[ UPDATES ]---------
        public static IEnumerable RequestAndApplyAllModEventsToCache(int fromTimeStamp,
                                                                     int untilTimeStamp,
                                                                     ClientRequest<List<ModEvent>> request)
        {
            bool isDone = false;

            ModManager.GetAndApplyAllModEvents(fromTimeStamp, untilTimeStamp,
                                               (r) => ModManager.OnRequestSuccess(r, request, out isDone),
                                               (e) => ModManager.OnRequestError(e, request, out isDone));

            while(!isDone) { yield return null; }
        }

        public static void GetAndApplyAllModEvents(int fromTimeStamp,
                                                   int untilTimeStamp,
                                                   Action<List<ModEvent>> onSuccess,
                                                   Action<WebRequestError> onError)
        {
            ModManager.GetAllModEvents(fromTimeStamp, untilTimeStamp,
            (modEvents) =>
            {
                ApplyModEventsToCache(modEvents);

                if(onSuccess != null) { onSuccess(modEvents); }
            },
            onError);
        }

        public static void GetAllModEvents(int fromTimeStamp,
                                           int untilTimeStamp,
                                           Action<List<ModEvent>> onSuccess,
                                           Action<WebRequestError> onError)
        {
            // - Filter & Pagination -
            API.RequestFilter modEventFilter = new API.RequestFilter();
            modEventFilter.sortFieldName = API.GetAllModEventsFilterFields.dateAdded;
            modEventFilter.fieldFilters[API.GetAllModEventsFilterFields.dateAdded]
            = new API.RangeFilter<int>()
            {
                min = fromTimeStamp,
                isMinInclusive = false,
                max = untilTimeStamp,
                isMaxInclusive = true,
            };

            // - Get All Events -
            ModManager.FetchAllResultsForQuery<ModEvent>((p,s,e) => API.Client.GetAllModEvents(modEventFilter, p, s, e),
                                                         onSuccess,
                                                         onError);
        }

        // TODO(@jackson): Check updates against ModProfile dateUpdated
        public static void ApplyModEventsToCache(IEnumerable<ModEvent> modEvents)
        {
            List<int> addedIds = new List<int>();
            List<int> editedIds = new List<int>();
            List<int> modfileChangedIds = new List<int>();
            List<int> removedIds = new List<int>();

            // Sort by event type
            foreach(ModEvent modEvent in modEvents)
            {
                switch(modEvent.eventType)
                {
                    case ModEventType.ModAvailable:
                    {
                        addedIds.Add(modEvent.modId);
                    }
                    break;
                    case ModEventType.ModEdited:
                    {
                        editedIds.Add(modEvent.modId);
                    }
                    break;
                    case ModEventType.ModfileChanged:
                    {
                        modfileChangedIds.Add(modEvent.modId);
                    }
                    break;
                    case ModEventType.ModUnavailable:
                    {
                        removedIds.Add(modEvent.modId);
                    }
                    break;
                }
            }

            // --- Process Add/Edit/ModfileChanged ---
            List<int> modsToFetch = new List<int>(addedIds.Count + editedIds.Count + modfileChangedIds.Count);
            modsToFetch.AddRange(addedIds);
            modsToFetch.AddRange(editedIds);
            modsToFetch.AddRange(modfileChangedIds);

            if(modsToFetch.Count > 0)
            {
                // - Filter & Pagination -
                API.RequestFilter modsFilter = new API.RequestFilter();
                modsFilter.fieldFilters[API.GetAllModsFilterFields.id]
                = new API.InArrayFilter<int>()
                {
                    filterArray = modsToFetch.ToArray(),
                };
                // - Get Mods -
                Action<List<ModProfile>> onGetMods = (updatedProfiles) =>
                {
                    // - Create Update Lists -
                    List<ModProfile> addedProfiles = new List<ModProfile>(addedIds.Count);
                    List<ModProfile> editedProfiles = new List<ModProfile>(editedIds.Count);
                    List<ModfileStub> modfileChangedStubs = new List<ModfileStub>(modfileChangedIds.Count);

                    foreach(ModProfile profile in updatedProfiles)
                    {
                        int idIndex;
                        // NOTE(@jackson): If added, ignore everything else
                        if((idIndex = addedIds.IndexOf(profile.id)) >= 0)
                        {
                            addedIds.RemoveAt(idIndex);
                            addedProfiles.Add(profile);
                        }
                        else
                        {
                            if((idIndex = editedIds.IndexOf(profile.id)) >= 0)
                            {
                                editedIds.RemoveAt(idIndex);
                                editedProfiles.Add(profile);
                            }
                            if((idIndex = modfileChangedIds.IndexOf(profile.id)) >= 0)
                            {
                                modfileChangedIds.RemoveAt(idIndex);
                                modfileChangedStubs.Add(profile.currentRelease);
                            }
                        }
                    }

                    // - Save changed to cache -
                    CacheClient.SaveModProfiles(updatedProfiles);

                    // --- Notifications ---
                    if(ModManager.modsAvailable != null
                       && addedProfiles.Count > 0)
                    {
                        ModManager.modsAvailable(addedProfiles);
                    }

                    if(ModManager.modsEdited != null
                       && editedProfiles.Count > 0)
                    {
                        ModManager.modsEdited(editedProfiles);
                    }

                    if(ModManager.modReleasesUpdated != null
                       && modfileChangedStubs.Count > 0)
                    {
                        ModManager.modReleasesUpdated(modfileChangedStubs);
                    }
                };

                ModManager.FetchAllResultsForQuery<ModProfile>((p,s,e) => Client.GetAllMods(modsFilter, p, s, e),
                                                               onGetMods,
                                                               null);
            }

            // --- Process Removed ---
            if(removedIds.Count > 0)
            {
                foreach(int modId in removedIds)
                {
                    CacheClient.UncacheMod(modId);
                }

                // TODO(@jackson): Compare with subscriptions

                if(ModManager.modsUnavailable != null)
                {
                    ModManager.modsUnavailable(removedIds);
                }
            }
        }

        private static void UpdateUserSubscriptions(List<ModProfile> userSubscriptions)
        {
            if(authUser == null) { return; }

            List<int> addedMods = new List<int>();
            List<int> removedMods = authUser.subscribedModIDs;
            authUser.subscribedModIDs = new List<int>(userSubscriptions.Count);

            foreach(ModProfile modProfile in userSubscriptions)
            {
                authUser.subscribedModIDs.Add(modProfile.id);

                if(removedMods.Contains(modProfile.id))
                {
                    removedMods.Remove(modProfile.id);
                }
                else
                {
                    addedMods.Add(modProfile.id);
                }
            }

            WriteUserDataToDisk();

            // - Notify -
            if(OnModSubscriptionAdded != null)
            {
                foreach(int modId in addedMods)
                {
                    OnModSubscriptionAdded(modId);
                }
            }
            if(OnModSubscriptionRemoved != null)
            {
                foreach(int modId in removedMods)
                {
                    OnModSubscriptionRemoved(modId);
                }
            }
        }

        // ---------[ USER MANAGEMENT ]---------
        // NOTE(@jackson): There is "userLoggedIn" event as the only time a
        //  user can be authenticated is via 'TryAuthenticateUser'.
        public static event Action userLoggedOut;
        public static event ModIDEventHandler OnModSubscriptionAdded;
        public static event ModIDEventHandler OnModSubscriptionRemoved;

        public static AuthenticatedUser GetAuthenticatedUser()
        {
            return authUser;
        }

        public static void RequestSecurityCode(string emailAddress,
                                               Action<APIMessage> onSuccess,
                                               Action<WebRequestError> onError)
        {
            Client.RequestSecurityCode(emailAddress,
                                       result => onSuccess(APIMessage.CreateFromMessageObject(result)),
                                       onError);
        }

        public static void RequestOAuthToken(string securityCode,
                                             Action<string> onSuccess,
                                             Action<WebRequestError> onError)
        {
            Client.RequestOAuthToken(securityCode,
                                        onSuccess,
                                        onError);
        }

        public static void TryLogUserIn(string userOAuthToken,
                                        Action<AuthenticatedUser> onSuccess,
                                        Action<WebRequestError> onError)
        {
            Action<UserProfile> onGetUser = (userProfile) =>
            {
                authUser = new AuthenticatedUser();
                authUser.oAuthToken = userOAuthToken;
                authUser.profile = userProfile;
                WriteUserDataToDisk();

                onSuccess(authUser);

                var userSubscriptionFilter = new RequestFilter();
                userSubscriptionFilter.fieldFilters[GetUserSubscriptionsFilterFields.gameId]
                     = new EqualToFilter<int>() { filterValue = GlobalSettings.GAME_ID };

                ModManager.FetchAllResultsForQuery<ModProfile>((p,s,e)=>Client.GetUserSubscriptions(userSubscriptionFilter,
                                                                                                    p, s, e),
                UpdateUserSubscriptions,
                                                    Client.LogError);
            };

            API.Client.SetUserAuthorizationToken(userOAuthToken);

            Client.GetAuthenticatedUser(onGetUser,
                                        (e) =>
                                        {
                                            API.Client.ClearUserAuthorizationToken();
                                            onError(e);
                                        });
        }

        public static void LogUserOut()
        {
            authUser = null;
            DeleteUserDataFromDisk();

            API.Client.ClearUserAuthorizationToken();

            if(userLoggedOut != null)
            {
                userLoggedOut();
            }
        }

        public static void SubscribeToMod(int modId,
                                          Action<APIMessage> onSuccess,
                                          Action<WebRequestError> onError)
        {
            Client.SubscribeToMod(modId,
                                  (message) =>
                                  {
                                    authUser.subscribedModIDs.Add(modId);
                                    onSuccess(APIMessage.CreateFromMessageObject(message));
                                  },
                                  onError);
        }

        public static void UnsubscribeFromMod(int modId,
                                              Action<APIMessage> onSuccess,
                                              Action<WebRequestError> onError)
        {
            Client.UnsubscribeFromMod(modId,
                                      (message) =>
                                      {
                                        authUser.subscribedModIDs.Remove(modId);
                                        onSuccess(APIMessage.CreateFromMessageObject(message));
                                      },
                                      onError);
        }

        public static bool IsSubscribedToMod(int modId)
        {
            foreach(int subscribedModID in authUser.subscribedModIDs)
            {
                if(subscribedModID == modId) { return true; }
            }
            return false;
        }

        // ---------[ MOD MANAGEMENT ]---------
        public static event ModProfileEventHandler OnModAdded;
        public static event ModIDEventHandler OnModRemoved;
        public static event ModIDEventHandler OnModUpdated;
        public static event ModfileEventHandler OnModfileChanged;

        private static Dictionary<int, ModProfile> modCache = new Dictionary<int, ModProfile>();

        public static string GetModDirectory(int modId)
        {
            return cacheDirectory + "mods/" + modId + "/";
        }

        // TODO(@jackson): Pass other components
        private static void StoreModData(ModProfile modProfile)
        {
            // - Cache -
            modCache[modProfile.id] = modProfile;

            // - Write to disk -
            string modDir = GetModDirectory(modProfile.id);
            Directory.CreateDirectory(modDir);
            File.WriteAllText(modDir + "mod_profile.data", JsonConvert.SerializeObject(modProfile));
            // File.WriteAllText(modDir + "mod_logo.data", JsonConvert.SerializeObject(modProfile.logo.AsImageSet()));
        }

        private static void StoreModDatas(ModProfile[] modArray)
        {
            foreach(ModProfile mod in modArray)
            {
                StoreModData(mod);
            }
        }
        private static void UncacheMod(int modId)
        {
            string modDir = GetModDirectory(modId);
            Directory.Delete(modDir, true);
        }

        public static IEnumerable<ModProfile> GetAllModProfiles()
        {
            // return CacheClient.LoadAllModProfiles();
            return new List<ModProfile>();
        }

        public static void DeleteAllDownloadedBinaries(int modId)
        {
            string[] binaryFilePaths = Directory.GetFiles(GetModDirectory(modId), "modfile_*.zip");
            foreach(string binaryFilePath in binaryFilePaths)
            {
                File.Delete(binaryFilePath);
            }
        }

        public static ModBinaryStatus GetBinaryStatus(ModProfile profile)
        {
            if(File.Exists(GetModDirectory(profile.id) + "modfile_" + profile.currentRelease.id + ".zip"))
            {
                return ModBinaryStatus.UpToDate;
            }
            else
            {
                string[] modfileURLs = Directory.GetFiles(GetModDirectory(profile.id), "modfile_*.zip");
                if(modfileURLs.Length > 0)
                {
                    return ModBinaryStatus.RequiresUpdate;
                }
                else
                {
                    return ModBinaryStatus.Missing;
                }
            }
        }

        public static string GetBinaryPath(ModProfile profile)
        {
            if(File.Exists(GetModDirectory(profile.id) + "modfile_" + profile.currentRelease.id + ".zip"))
            {
                return GetModDirectory(profile.id) + "modfile_" + profile.currentRelease.id + ".zip";
            }
            else
            {
                string[] modfileURLs = Directory.GetFiles(GetModDirectory(profile.id), "modfile_*.zip");
                if(modfileURLs.Length > 0)
                {
                    return modfileURLs[0];
                }
            }
            return null;
        }

        // ---------[ MODFILE & BINARY MANAGEMENT ]---------
        public static void LoadOrDownloadModfile(int modId, int modfileId,
                                                 Action<Modfile> onSuccess,
                                                 Action<WebRequestError> onError)
        {
            string modfileFilePath = (GetModDirectory(modId) + "modfile_"
                                      + modfileId.ToString() + ".data");
            if(File.Exists(modfileFilePath))
            {
                // Load ModProfile from Disk
                Modfile modfile = JsonConvert.DeserializeObject<Modfile>(File.ReadAllText(modfileFilePath));
                onSuccess(modfile);
            }
            else
            {
                Action<Modfile> writeModfileToDisk = (m) =>
                {
                    File.WriteAllText(modfileFilePath,
                                      JsonConvert.SerializeObject(m));
                    onSuccess(m);
                };

                Client.GetModfile(modId, modfileId,
                                  writeModfileToDisk,
                                  onError);
            }
        }

        public static FileDownload StartBinaryDownload(int modId, int modfileId)
        {
            string binaryFilePath = (GetModDirectory(modId) + "binary_"
                                     + modfileId.ToString() + ".zip");

            FileDownload download = new FileDownload();
            Action<Modfile> queueBinaryDownload = (m) =>
            {
                download.sourceURL = m.downloadLocator.binaryURL;
                download.fileURL = binaryFilePath;
                download.EnableFilehashVerification(m.fileHash.md5);

                DownloadManager.QueueDownload(download);
            };

            Client.GetModfile(modId, modfileId,
                              queueBinaryDownload,
                              download.MarkAsFailed);

            return download;
        }

        // ---------[ IMAGE MANAGEMENT ]---------
        public static event ModLogoUpdatedEventHandler OnModLogoUpdated;
        public static event ModGalleryImageUpdatedEventHandler OnModGalleryImageUpdated;

        private static Dictionary<string, string> serverToLocalImageURLMap;

        public static string GenerateModLogoFilePath(int modId, LogoVersion version)
        {
            return GetModDirectory(modId) + @"/logo/" + version.ToString() + ".png";
        }

        // TODO(@jackson): Look at reconfiguring params
        public static void GetModLogo(ModProfile profile, LogoVersion version,
                                      Action<Texture2D> onSuccess,
                                      Action<WebRequestError> onError)
        {
            CacheClient.LoadModLogo(profile.id, version,
                                     (logoTexture) =>
            {
                if(logoTexture != null)
                {
                    onSuccess(logoTexture);
                }
                else
                {
                    var download = new TextureDownload();
                    download.sourceURL = profile.logoLocator.GetVersionURL(version);
                    download.OnCompleted += (d) =>
                    {
                        CacheClient.SaveModLogo(profile.id, version, download.texture);
                        onSuccess(download.texture);
                    };
                    download.OnFailed += (d,e) => onError(e);

                    DownloadManager.StartDownload(download);
                }
            });
        }

        public static IEnumerator RequestModLogo(ModProfile profile,
                                                 LogoVersion version,
                                                 ClientRequest<Texture2D> logoRequest)
        {
            bool isDone = false;

            ModManager.GetModLogo(profile, version,
                                  (r) => ModManager.OnRequestSuccess(r, logoRequest, out isDone),
                                  (e) => ModManager.OnRequestError(e, logoRequest, out isDone));

            while(!isDone)
            {
                yield return null;
            }
        }


        public static void GetModGalleryImage(ModProfile profile,
                                              string imageFileName,
                                              ModGalleryImageVersion version,
                                              Action<Texture2D> onSuccess,
                                              Action<WebRequestError> onError)
        {
            CacheClient.LoadModGalleryImage(profile.id,
                                            imageFileName,
                                            version,
                                            (cachedImageTexture) =>
            {
                if(cachedImageTexture != null)
                {
                    if(onSuccess != null) { onSuccess(cachedImageTexture); }
                }
                else
                {
                    // - Fetch from Server -
                    var download = new TextureDownload();
                    download.sourceURL = profile.media.GetGalleryImageWithFileName(imageFileName).GetVersionURL(version);
                    download.OnCompleted += (d) =>
                    {
                        CacheClient.SaveModGalleryImage(profile.id, imageFileName, version, download.texture);
                        if(onSuccess != null) { onSuccess(download.texture); }
                    };
                    download.OnFailed += (d,e) => onError(e);

                    DownloadManager.StartDownload(download);
                }
            });
        }


        public static Texture2D FindSavedImageMatchingServerURL(string serverURL)
        {
            string filePath;
            Texture2D imageTexture;
            if(serverToLocalImageURLMap.TryGetValue(serverURL, out filePath)
               && Utility.TryLoadTextureFromFile(filePath, out imageTexture))
            {
                return imageTexture;
            }
            return null;
        }

        // TODO(@jackson): Defend
        // TODO(@jackson): Record whether completed (lest placeholder be accepted)
        public static TextureDownload DownloadAndSaveImageAsPNG(string serverURL,
                                                                string downloadFilePath,
                                                                Texture2D placeholderTexture)
        {
            Debug.Assert(Path.GetExtension(downloadFilePath).Equals(".png"),
                         String.Format("[mod.io] Images can only be saved in PNG format."
                                       + "\n\'{0}\' appears to be in a different format.",
                                       downloadFilePath));

            var download = new TextureDownload();

            Directory.CreateDirectory(Path.GetDirectoryName(downloadFilePath));
            File.WriteAllBytes(downloadFilePath, placeholderTexture.EncodeToPNG());

            serverToLocalImageURLMap[serverURL] = downloadFilePath;

            download.sourceURL = serverURL;
            download.OnCompleted += (d) =>
            {
                File.WriteAllBytes(downloadFilePath, download.texture.EncodeToPNG());
                manifest.serializedImageCache.Add(serverURL + "*" + downloadFilePath);
                WriteManifestToDisk();
            };
            DownloadManager.StartDownload(download);

            return download;
        }

        // TODO(@jackson): param -> ids?
        // TODO(@jackson): defend
        // TODO(@jackson): Add preload function?
        public static void DownloadMissingModLogos(ModProfile[] modProfiles,
                                                   LogoVersion version)
        {
            var missingLogoProfiles = new List<ModProfile>(modProfiles);

            // Check which logos are missing
            foreach(ModProfile profile in modProfiles)
            {
                string serverURL = profile.logoLocator.GetVersionURL(version);
                string filePath;
                if(serverToLocalImageURLMap.TryGetValue(serverURL,
                                                        out filePath))
                {
                    if(File.Exists(filePath))
                    {
                        missingLogoProfiles.Remove(profile);
                    }
                    else
                    {
                        serverToLocalImageURLMap.Remove(serverURL);
                    }
                }
            }

            // Download
            foreach(ModProfile profile in missingLogoProfiles)
            {
                string logoURL = profile.logoLocator.GetVersionURL(version);
                string filePath = GenerateModLogoFilePath(profile.id, version);
                var download = DownloadAndSaveImageAsPNG(logoURL,
                                                         filePath,
                                                         UISettings.Instance.DownloadingPlaceholderImages.modLogo);

                download.OnCompleted += (d) =>
                {
                    if(OnModLogoUpdated != null)
                    {
                        OnModLogoUpdated(profile.id, version, download.texture);
                    }
                };
            }
        }

        // ---------[ MISC ]------------
        public static GameProfile GetGameProfile()
        {
            return manifest.gameProfile;
        }

        private static void WriteManifestToDisk()
        {
            File.WriteAllText(manifestPath, JsonConvert.SerializeObject(manifest));
        }

        private static void WriteUserDataToDisk()
        {
            File.WriteAllText(userdataPath, JsonConvert.SerializeObject(authUser));
        }

        private static void DeleteUserDataFromDisk()
        {
            File.Delete(userdataPath);
        }

        // TODO(@jackson): Add MKVPs, Mod Dependencies
        public static void SubmitNewMod(EditableModProfile modEdits,
                                        Action<ModProfile> modSubmissionSucceeded,
                                        Action<WebRequestError> modSubmissionFailed)
        {
            Debug.Assert(modEdits.name.isDirty && modEdits.summary.isDirty);
            Debug.Assert(File.Exists(modEdits.logoLocator.value.url));

            // - Initial Mod Submission -
            var parameters = new AddModParameters();
            parameters.name = modEdits.name.value;
            parameters.summary = modEdits.summary.value;
            parameters.logo = BinaryUpload.Create(Path.GetFileName(modEdits.logoLocator.value.url),
                                                  File.ReadAllBytes(modEdits.logoLocator.value.url));
            if(modEdits.visibility.isDirty)
            {
                parameters.visible = (int)modEdits.visibility.value;
            }
            if(modEdits.nameId.isDirty)
            {
                parameters.name_id = modEdits.nameId.value;
            }
            if(modEdits.description.isDirty)
            {
                parameters.description = modEdits.description.value;
            }
            if(modEdits.homepageURL.isDirty)
            {
                parameters.name_id = modEdits.homepageURL.value;
            }
            if(modEdits.metadataBlob.isDirty)
            {
                parameters.metadata_blob = modEdits.metadataBlob.value;
            }
            if(modEdits.nameId.isDirty)
            {
                parameters.name_id = modEdits.nameId.value;
            }
            if(modEdits.tags.isDirty)
            {
                parameters.tags = modEdits.tags.value;
            }

            // NOTE(@jackson): As add Mod takes more parameters than edit,
            //  we can ignore some of the elements in the EditModParameters
            //  when passing to SubmitModProfileComponents
            var remainingModEdits = new EditableModProfile();
            remainingModEdits.youtubeURLs = modEdits.youtubeURLs;
            remainingModEdits.sketchfabURLs = modEdits.sketchfabURLs;
            remainingModEdits.galleryImageLocators = modEdits.galleryImageLocators;

            Client.AddMod(parameters,
                          result => SubmitModProfileComponents(result,
                                                               remainingModEdits,
                                                               modSubmissionSucceeded,
                                                               modSubmissionFailed),
                          modSubmissionFailed);
        }
        // TODO(@jackson): Add MKVPs, Mod Dependencies
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
                   || modEdits.description.isDirty
                   || modEdits.homepageURL.isDirty
                   || modEdits.metadataBlob.isDirty)
                {
                    var parameters = new EditModParameters();
                    if(modEdits.status.isDirty)
                    {
                        parameters.status = (int)modEdits.status.value;
                    }
                    if(modEdits.visibility.isDirty)
                    {
                        parameters.visible = (int)modEdits.visibility.value;
                    }
                    if(modEdits.name.isDirty)
                    {
                        parameters.name = modEdits.name.value;
                    }
                    if(modEdits.nameId.isDirty)
                    {
                        parameters.name_id = modEdits.nameId.value;
                    }
                    if(modEdits.summary.isDirty)
                    {
                        parameters.summary = modEdits.summary.value;
                    }
                    if(modEdits.description.isDirty)
                    {
                        parameters.description = modEdits.description.value;
                    }
                    if(modEdits.homepageURL.isDirty)
                    {
                        parameters.homepage = modEdits.homepageURL.value;
                    }
                    if(modEdits.metadataBlob.isDirty)
                    {
                        parameters.metadata_blob = modEdits.metadataBlob.value;
                    }

                    Client.EditMod(modId, parameters,
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
            Action<MessageObject> doNextSubmissionAction = (m) =>
            {
                if(nextActionIndex < submissionActions.Count)
                {
                    submissionActions[nextActionIndex++]();
                }
            };

            // - Media -
            if(modEdits.logoLocator.isDirty
               || modEdits.youtubeURLs.isDirty
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

                if(modEdits.youtubeURLs.isDirty)
                {
                    var addedYouTubeLinks = new List<string>(modEdits.youtubeURLs.value);
                    foreach(string youtubeLink in profile.media.youtubeURLs)
                    {
                        addedYouTubeLinks.Remove(youtubeLink);
                    }
                    addMediaParameters.youtube = addedYouTubeLinks.ToArray();

                    var removedTags = new List<string>(profile.media.youtubeURLs);
                    foreach(string youtubeLink in modEdits.youtubeURLs.value)
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
                        string galleryZipLocation = Application.temporaryCachePath + "/modio/imageGallery_" + DateTime.Now.ToFileTime() + ".zip";
                        ZipUtil.Zip(galleryZipLocation, addedImageFilePaths.ToArray());

                        var imageGalleryUpload = BinaryUpload.Create("images.zip",
                                                                     File.ReadAllBytes(galleryZipLocation));

                        addMediaParameters.images = imageGalleryUpload;
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
                        Client.AddModMedia(profile.id,
                                           addMediaParameters,
                                           doNextSubmissionAction, modSubmissionFailed);
                    });
                }
                if(deleteMediaParameters.stringValues.Count > 0)
                {
                    submissionActions.Add(() =>
                    {
                        Client.DeleteModMedia(profile.id,
                                              deleteMediaParameters,
                                              doNextSubmissionAction, modSubmissionFailed);
                    });
                }
            }

            // - Tags -
            if(modEdits.tags.isDirty)
            {
                var addedTags = new List<string>(modEdits.tags.value);
                foreach(string tag in profile.tagNames)
                {
                    addedTags.Remove(tag);
                }

                var removedTags = new List<string>(profile.tagNames);
                foreach(string tag in modEdits.tags.value)
                {
                    removedTags.Remove(tag);
                }

                if(addedTags.Count > 0)
                {
                    submissionActions.Add(() =>
                    {
                        var parameters = new AddModTagsParameters();
                        parameters.tags = addedTags.ToArray();
                        Client.AddModTags(profile.id, parameters,
                                          doNextSubmissionAction, modSubmissionFailed);
                    });
                }
                if(removedTags.Count > 0)
                {
                    submissionActions.Add(() =>
                    {
                        var parameters = new DeleteModTagsParameters();
                        parameters.tags = removedTags.ToArray();
                        Client.DeleteModTags(profile.id, parameters,
                                             doNextSubmissionAction, modSubmissionFailed);
                    });
                }
            }

            // - Metadata KVP -

            // - Mod Dependencies -

            // - Team Members -

            // - Get Updated Profile -
            submissionActions.Add(() => Client.GetMod(profile.id,
                                                      modSubmissionSucceeded,
                                                      modSubmissionFailed));

            // - Start submission chain -
            doNextSubmissionAction(new MessageObject());
        }

        // TODO(@jackson): Convert onError to string!
        public static void UploadModBinary_Unzipped(int modId,
                                                    EditableModfile modfileValues,
                                                    string unzippedBinaryLocation,
                                                    bool setPrimary,
                                                    Action<Modfile> onSuccess,
                                                    Action<WebRequestError> onError)
        {
            string binaryZipLocation = Application.temporaryCachePath + "/modio/" + System.IO.Path.GetFileNameWithoutExtension(unzippedBinaryLocation) + DateTime.Now.ToFileTime() + ".zip";

            ZipUtil.Zip(binaryZipLocation, unzippedBinaryLocation);

            UploadModBinary_Zipped(modId, modfileValues, binaryZipLocation, setPrimary, onSuccess, onError);
        }

        public static void UploadModBinary_Zipped(int modId,
                                                  EditableModfile modfileValues,
                                                  string binaryZipLocation,
                                                  bool setPrimary,
                                                  Action<Modfile> onSuccess,
                                                  Action<WebRequestError> onError)
        {
            string buildFilename = Path.GetFileName(binaryZipLocation);
            byte[] buildZipData = File.ReadAllBytes(binaryZipLocation);

            AddModfileParameters parameters = new AddModfileParameters();
            parameters.filedata = BinaryUpload.Create(buildFilename, buildZipData);
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
                parameters.metadata_blob = modfileValues.metadataBlob.value;
            }

            // TODO(@jackson): parameters.filehash

            Client.AddModfile(modId,
                              parameters,
                              onSuccess,
                              onError);
        }

        // --- TEMPORARY PASS-THROUGH FUNCTIONS ---
        public static void AddPositiveRating(int modId,
                                             Action<APIMessage> onSuccess,
                                             Action<WebRequestError> onError)
        {
            Client.AddModRating(modId, new AddModRatingParameters(1),
                                result => onSuccess(APIMessage.CreateFromMessageObject(result)),
                                onError);
        }

        // public static void AddModTeamMember(int modId, UnsubmittedTeamMember teamMember,
        //                                     Action<APIMessage> onSuccess,
        //                                     Action<WebRequestError> onError)
        // {
        //     Client.AddModTeamMember(modId, teamMember.AsAddModTeamMemberParameters(),
        //                             result => OnRequestSuccessWrapper(result, onSuccess),
        //                             onError);
        // }

        public static void DeleteModFromServer(int modId,
                                               Action<APIMessage> onSuccess,
                                               Action<WebRequestError> onError)
        {
            // TODO(@jackson): Remvoe Mod Locally

            Client.DeleteMod(modId,
                             result => onSuccess(APIMessage.CreateFromMessageObject(result)),
                             onError);
        }

        public static void DeleteModComment(int modId, int commentId,
                                            Action<APIMessage> onSuccess,
                                            Action<WebRequestError> onError)
        {
            Client.DeleteModComment(modId, commentId,
                                    result => onSuccess(APIMessage.CreateFromMessageObject(result)),
                                    onError);
        }
        // ---------[ FETCH ALL RESULTS ]---------
        private delegate void GetAllObjectsQuery<T>(PaginationParameters pagination,
                                                    Action<ResponseArray<T>> onSuccess,
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
                                                            ResponseArray<T> queryResult,
                                                            PaginationParameters pagination,
                                                            List<T> culmativeResults,
                                                            Action<List<T>> onSuccess,
                                                            Action<WebRequestError> onError)
        {
            Debug.Assert(pagination.limit > 0);

            culmativeResults.AddRange(queryResult.Items);

            if(queryResult.Count < queryResult.Limit)
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

        // public static void GetAllModTeamMembers(int modId,
        //                                         Action<TeamMember[]> onSuccess,
        //                                         Action<WebRequestError> onError)
        // {
        //     Client.GetAllModTeamMembers(modId, GetAllModTeamMembersFilter.None,
        //                                    result => OnRequestSuccessWrapper(result, onSuccess),
        //                                    onError);
        // }
    }
}



        // public static void InitializeUsingDirectory(string cacheDirectory)
        // {
        //     CacheClient.cacheDirectory = cacheDirectory;

        //     if (!Directory.Exists(CacheClient.cacheDirectory))
        //     {
        //         Directory.CreateDirectory(CacheClient.cacheDirectory);
        //     }

        //     if(File.Exists(cacheDirectory + "manifest.data"))
        //     {
        //         string manifestFilePath = cacheDirectory + "manifest.data";
        //         try
        //         {
        //             Manifest m = JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(manifestFilePath));
        //             CacheClient._lastUpdate = m.lastUpdateTimeStamp;
        //         }
        //         catch(Exception e)
        //         {
        //             Debug.LogWarning("[mod.io] Failed to read cache manifest from " + cacheDirectory
        //                              + "\n" + e.Message);
        //         }
        //     }
        // }
