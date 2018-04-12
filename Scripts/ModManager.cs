// #define DO_NOT_LOAD_CACHE

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using ModIO.API;

using UnityEngine;

namespace ModIO
{
    public delegate void ModEventHandler(ModProfile modProfile);
    public delegate void ModIDEventHandler(int modId);
    public delegate void ModfileEventHandler(int modId, Modfile newModfile);
    public delegate void ModLogoUpdatedEventHandler(int modId, ModLogoVersion version, Texture2D texture);

    public enum ModBinaryStatus
    {
        Missing,
        RequiresUpdate,
        UpToDate
    }

    public class ModManager
    {
        // ---------[ INNER CLASSES ]---------
        [System.Serializable]
        private class UserData
        {
            public string oAuthToken = "";
            public UserProfile userProfile = null;
            public List<int> subscribedModIDs = new List<int>();
        }

        [System.Serializable]
        private class ManifestData
        {
            public TimeStamp lastUpdateTimeStamp;
            public List<ModEvent> unresolvedEvents;
            public GameProfile gameProfile;
        }

        // ---------[ VARIABLES ]---------
        private static ManifestData manifest = null;
        public static GameProfile gameProfile { get { return manifest.gameProfile; }}

        private static UserData userData = null;
        public static UserProfile currentUser { get { return userData == null ? null : userData.userProfile; } }

        public static string cacheDirectory { get; private set; }
        
        private static string manifestPath { get { return cacheDirectory + "manifest.data"; } }
        private static string userdataPath { get { return cacheDirectory + "user.data"; } }

        // --------- [ INITIALIZATION ]---------
        public static void Initialize()
        {
            if(manifest != null)
            {
                return;
            }
            manifest = new ManifestData();

            #pragma warning disable CS0162
            #if DEBUG
            if(GlobalSettings.USE_TEST_SERVER)
            {
                cacheDirectory = Application.persistentDataPath + "/modio_testServer/";
            }
            else
            #endif
            {
                cacheDirectory = Application.persistentDataPath + "/modio/";
            }
            #pragma warning restore CS0162

            Debug.Log("Initializing ModIO.ModManager"
                      + "\nModIO Directory: " + cacheDirectory);

            #if UNITY_EDITOR
            if(Application.isPlaying)
            #endif
            {
                var go = new UnityEngine.GameObject("ModIO.UpdateRunner");
                go.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInBuild;
                go.AddComponent<UpdateRunner>();
            }

            LoadCacheFromDisk();
            FetchAndCacheGameProfile();
            FetchAndCacheAllMods();
        }

        private static void LoadCacheFromDisk()
        {
            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }

            #if DO_NOT_LOAD_CACHE
            {
                manifest = new ManifestData();
                manifest.lastUpdateTimeStamp = new TimeStamp();
                manifest.unresolvedEvents = new List<ModEvent>();

                WriteManifestToDisk();
            }
            #else
            {
                if(!Utility.TryParseJsonFile(manifestPath, out manifest))
                {
                    // --- INITIALIZE FIRST RUN ---
                    manifest = new ManifestData();
                    manifest.lastUpdateTimeStamp = new TimeStamp();
                    manifest.unresolvedEvents = new List<ModEvent>();

                    WriteManifestToDisk();
                }

                // Attempt to load user
                if(Utility.TryParseJsonFile(userdataPath, out userData))
                {
                    Action<WebRequestError> onAuthenticationFail = (error) =>
                    {
                        if(error.responseCode == 401
                            || error.responseCode == 403) // Failed authentication
                        {
                            LogUserOut();
                        }
                    };

                    Client.GetAuthenticatedUser(userData.oAuthToken, 
                                                null, 
                                                onAuthenticationFail);
                }

                // iterate through folders, load ModProfile
                if(!Directory.Exists(cacheDirectory + "mods/"))
                {
                    Directory.CreateDirectory(cacheDirectory + "mods/");
                }

                string[] modDirectories = Directory.GetDirectories(cacheDirectory + "mods/");
                foreach(string modDir in modDirectories)
                {
                    // Load ModProfile from Disk
                    ModProfile profile;
                    if(Utility.TryParseJsonFile(modDir + "/mod_profile.data", out profile))
                    {
                        modCache.Add(profile.id, profile);
                    }
                    else
                    {
                        // TODO(@jackson): better
                        Debug.LogWarning("[mod.io] Unable to parse mod profile at: " + modDir + "/mod_profile.data");
                    }
                }
            }
            #endif
        }

        private static void FetchAndCacheGameProfile()
        {
            Action<API.GameObject> cacheGameProfile = (gameObject) =>
            {
                manifest.gameProfile = GameProfile.CreateFromGameObject(gameObject);
                WriteManifestToDisk();
            };

            Client.GetGame(cacheGameProfile, null);
        }

        private static void FetchAndCacheAllMods()
        {
            Action<ObjectArray<ModObject>> addModsToCache = (modObjects) =>
            {
                // TODO(@jackson): Implement mod is unavailable
                // TODO(@jackson): Check for modfile change

                manifest.lastUpdateTimeStamp = TimeStamp.Now();
                WriteManifestToDisk();

                var profiles = new List<ModProfile>(modObjects.data.Length);
                foreach(ModObject modObject in modObjects.data)
                {
                    ModProfile profile = ModProfile.CreateFromModObject(modObject);
                    profiles.Add(profile);
                }

                List<ModProfile> updatedMods = new List<ModProfile>();
                List<ModProfile> addedMods = new List<ModProfile>();

                foreach(ModProfile profile in profiles)
                {
                    ModProfile cachedMod;
                    if(modCache.TryGetValue(profile.id, out cachedMod)
                       && !cachedMod.Equals(profile))
                    {
                        StoreModData(profile);
                        updatedMods.Add(profile);
                    }
                    else
                    {
                        StoreModData(profile);
                        addedMods.Add(profile);
                    }
                }

                if(OnModAdded != null)
                {
                    foreach(ModProfile mod in addedMods)
                    {
                        OnModAdded(mod);
                    }
                }

                if(OnModUpdated != null)
                {
                    foreach(ModProfile mod in updatedMods)
                    {
                        OnModUpdated(mod.id);
                    }
                }
            };

            Client.GetAllMods(GetAllModsFilter.None,
                              addModsToCache,
                              Client.LogError);
        }

        // ---------[ AUTOMATED UPDATING ]---------
        private const int SECONDS_BETWEEN_POLLING = 15;
        private static bool isUpdatePollingEnabled = false;
        private static bool isUpdatePollingRunning = false;

        public static void EnableUpdatePolling()
        {
            if(!isUpdatePollingEnabled)
            {
                #if UNITY_EDITOR
                if(!Application.isPlaying)
                {
                    UnityEditor.EditorApplication.update += PollForUpdates;
                }
                else
                #endif
                {
                    UpdateRunner.onUpdate += PollForUpdates;
                }
                isUpdatePollingEnabled = true;
            }
        }
        public static void DisableUpdatePolling()
        {
            if(isUpdatePollingEnabled)
            {
                isUpdatePollingEnabled = false;
                #if UNITY_EDITOR
                if(!Application.isPlaying)
                {
                    UnityEditor.EditorApplication.update -= PollForUpdates;
                }
                else
                #endif
                {
                    UpdateRunner.onUpdate -= PollForUpdates;
                }
            }
        }

        private static void PollForUpdates()
        {
            int secondsSinceUpdate = (TimeStamp.Now().AsServerTimeStamp()
                                      - manifest.lastUpdateTimeStamp.AsServerTimeStamp());

            if(secondsSinceUpdate >= SECONDS_BETWEEN_POLLING)
            {
                TimeStamp fromTimeStamp = manifest.lastUpdateTimeStamp;
                TimeStamp untilTimeStamp = TimeStamp.Now();

                // - Get Game Updates -
                FetchAndCacheGameProfile();

                // - Get ModProfile Events -
                GetAllModEventsFilter eventFilter = new GetAllModEventsFilter();
                eventFilter.ApplyIntRange(GetAllModEventsFilter.Field.DateAdded,
                                          fromTimeStamp.AsServerTimeStamp(), true,
                                          untilTimeStamp.AsServerTimeStamp(), false);
                eventFilter.ApplyBooleanIs(GetAllModEventsFilter.Field.Latest,
                                           true);

                Action<ObjectArray<EventObject>> onModEventsReceived = (eventObjectArray) =>
                {
                    manifest.lastUpdateTimeStamp = untilTimeStamp;
                    var modEvents = new ModEvent[eventObjectArray.data.Length];
                    for(int i = 0; i < eventObjectArray.data.Length; ++i)
                    {
                        modEvents[i] = ModEvent.CreateFromEventObject(eventObjectArray.data[i]);
                    }
                    ProcessModEvents(modEvents);
                };
                Client.GetAllModEvents(eventFilter,
                                       onModEventsReceived,
                                       Client.LogError);

                // TODO(@jackson): Replace with Event Polling
                // - Get Subscription Updates -
                if(userData != null)
                {
                    GetUserSubscriptionsFilter subscriptionFilter = new GetUserSubscriptionsFilter();
                    subscriptionFilter.ApplyIntEquality(GetUserSubscriptionsFilter.Field.GameId, GlobalSettings.GAME_ID);

                    Client.GetUserSubscriptions(userData.oAuthToken,
                                                subscriptionFilter,
                                                UpdateSubscriptions,
                                                Client.LogError);
                }
            }
        }

        private static void ProcessModEvents(ModEvent[] eventArray)
        {
            // - ModProfile Processing Options -
            Action<ModEvent> processModAvailable = (modEvent) =>
            {
                Action<ModObject> onGetMod = (modObject) =>
                {
                    var profile = ModProfile.CreateFromModObject(modObject);

                    StoreModData(profile);
                    manifest.unresolvedEvents.Remove(modEvent);

                    if(OnModAdded != null)
                    {
                        OnModAdded(profile);
                    }
                };

                Client.GetMod(modEvent.modId, onGetMod, Client.LogError);
            };
            Action<ModEvent> processModUnavailable = (modEvent) =>
            {
                // TODO(@jackson): Facilitate marking Mods as installed
                bool isModInstalled = (userData != null
                                       && userData.subscribedModIDs.Contains(modEvent.modId));

                if(!isModInstalled
                   && modCache.ContainsKey(modEvent.modId))
                {
                    UncacheMod(modEvent.modId);

                    if(OnModRemoved != null)
                    {
                        OnModRemoved(modEvent.modId);
                    }
                }
                manifest.unresolvedEvents.Remove(modEvent);
            };

            Action<ModEvent> processModEdited = (modEvent) =>
            {
                Action<ModObject> onGetMod = (modObject) =>
                {
                    var profile = ModProfile.CreateFromModObject(modObject);

                    StoreModData(profile);
                    manifest.unresolvedEvents.Remove(modEvent);

                    if(OnModUpdated != null)
                    {
                        OnModUpdated(profile.id);
                    }
                };

                Client.GetMod(modEvent.modId, onGetMod, Client.LogError);
            };


            Action<ModEvent> processModfileChange = (modEvent) =>
            {
                ModProfile profile = GetModProfile(modEvent.modId);

                if(profile == null)
                {
                    Debug.Log("Received Modfile change for uncached mod. Ignoring.");
                    manifest.unresolvedEvents.Remove(modEvent);
                }
                else
                {
                    Action<ModObject> onGetMod = (modObject) =>
                    {
                        profile.ApplyModObjectValues(modObject);

                        StoreModData(profile);

                        if(OnModfileChanged != null)
                        {
                            throw new System.NotImplementedException();
                            // OnModfileChanged(profile.id, profile.modfile);
                        }
                    };

                    Client.GetMod(profile.id, onGetMod, Client.LogError);

                    manifest.unresolvedEvents.Remove(modEvent);
                }
            };


            // - Handle ModProfile Event -
            foreach(ModEvent modEvent in eventArray)
            {
                string eventSummary = "TimeStamp (Local)=" + modEvent.dateAdded.AsLocalDateTime();
                eventSummary += "\nMod=" + modEvent.modId;
                eventSummary += "\nEventType=" + modEvent.eventType.ToString();
                
                Debug.Log("[PROCESSING MOD EVENT]\n" + eventSummary);


                manifest.unresolvedEvents.Add(modEvent);

                switch(modEvent.eventType)
                {
                    case ModEventType.ModfileChanged:
                    {
                        processModfileChange(modEvent);
                    }
                    break;
                    case ModEventType.ModAvailable:
                    {
                        processModAvailable(modEvent);
                    }
                    break;
                    case ModEventType.ModUnavailable:
                    {
                        processModUnavailable(modEvent);
                    }
                    break;
                    case ModEventType.ModEdited:
                    {
                        processModEdited(modEvent);
                    }
                    break;
                    default:
                    {
                        Debug.LogError("Unhandled Event Type: " + modEvent.eventType.ToString());
                    }
                    break;
                }
            }
        }

        private static void UpdateSubscriptions(ObjectArray<ModObject> modObjectArray)
        {
            if(userData == null) { return; }

            var subscribedMods = new List<ModProfile>(modObjectArray.data.Length);
            foreach(ModObject modObject in modObjectArray.data)
            {
                subscribedMods.Add(ModProfile.CreateFromModObject(modObject));
            }

            List<int> addedMods = new List<int>();
            List<int> removedMods = new List<int>(userData.subscribedModIDs);
            userData.subscribedModIDs = new List<int>(subscribedMods.Count);

            foreach(ModProfile mod in subscribedMods)
            {
                userData.subscribedModIDs.Add(mod.id);

                if(removedMods.Contains(mod.id))
                {
                    removedMods.Remove(mod.id);
                }
                else
                {
                    addedMods.Add(mod.id);
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
        public static event Action OnUserLoggedOut;
        public static event ModIDEventHandler OnModSubscriptionAdded;
        public static event ModIDEventHandler OnModSubscriptionRemoved;

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
                                        Action<UserProfile> onSuccess,
                                        Action<WebRequestError> onError)
        {
            Action<API.UserObject> onGetUser = (userObject) =>
            {
                userData = new UserData();
                userData.oAuthToken = userOAuthToken;
                userData.userProfile = UserProfile.CreateFromUserObject(userObject);
                WriteUserDataToDisk();

                onSuccess(userData.userProfile);

                Client.GetUserSubscriptions(userOAuthToken,
                                            GetUserSubscriptionsFilter.None,
                                            UpdateSubscriptions,
                                            Client.LogError);
            };

            Client.GetAuthenticatedUser(userOAuthToken,
                                        onGetUser,
                                        onError);
        }

        public static void LogUserOut()
        {
            userData = null;
            DeleteUserDataFromDisk();
            if(OnUserLoggedOut != null)
            {
                OnUserLoggedOut();
            }
        }

        public static UserProfile GetActiveUser()
        {
            return (userData == null ? null : userData.userProfile);
        }

        public static void SubscribeToMod(int modId,
                                          Action<ModProfile> onSuccess,
                                          Action<WebRequestError> onError)
        {
            Client.SubscribeToMod(userData.oAuthToken,
                                     modId,
                                     (message) =>
                                     {
                                        userData.subscribedModIDs.Add(modId);
                                        onSuccess(GetModProfile(modId));
                                     },
                                     onError);
        }

        public static void UnsubscribeFromMod(int modId,
                                              Action<ModProfile> onSuccess,
                                              Action<WebRequestError> onError)
        {
            Client.UnsubscribeFromMod(userData.oAuthToken,
                                         modId,
                                         (message) =>
                                         {
                                            userData.subscribedModIDs.Remove(modId);
                                            onSuccess(GetModProfile(modId));
                                         },
                                         onError);
        }

        public static bool IsSubscribedToMod(int modId)
        {
            foreach(int subscribedModID in userData.subscribedModIDs)
            {
                if(subscribedModID == modId) { return true; }
            }
            return false;
        }

        // ---------[ MOD MANAGEMENT ]---------
        public static event ModEventHandler OnModAdded;
        public static event ModIDEventHandler OnModRemoved;
        public static event ModIDEventHandler OnModUpdated;
        public static event ModfileEventHandler OnModfileChanged;

        private static Dictionary<int, ModProfile> modCache = new Dictionary<int, ModProfile>();

        public static string GetModDirectory(int modId)
        {
            return cacheDirectory + "mods/" + modId + "/";
        }

        public static ModProfile GetModProfile(int modId)
        {
            ModProfile modInfo;
            modCache.TryGetValue(modId, out modInfo);
            return modInfo;
        }

        // TODO(@jackson): Pass other components
        private static void StoreModData(ModProfile modProfile)
        {
            // - Cache -
            modCache[modProfile.id] = modProfile;
            // modImageMap[modProfile.logoLocator] = modProfile.logo.AsImageSet();
            // modImageMap[modProfile.logoLocator] = new ImageSet();

            // - Write to disk -
            string modDir = GetModDirectory(modProfile.id);
            Directory.CreateDirectory(modDir);
            File.WriteAllText(modDir + "mod_profile.data", JsonUtility.ToJson(modProfile));
            // File.WriteAllText(modDir + "mod_logo.data", JsonUtility.ToJson(modProfile.logo.AsImageSet()));
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

        public static ModProfile[] GetModProfiles(GetAllModsFilter filter)
        {
            // return filter.FilterCollection(modCache.Values);
            return new ModProfile[0];
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
            if(File.Exists(GetModDirectory(profile.id) + "modfile_" + profile.primaryModfileId + ".zip"))
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
            if(File.Exists(GetModDirectory(profile.id) + "modfile_" + profile.primaryModfileId + ".zip"))
            {
                return GetModDirectory(profile.id) + "modfile_" + profile.primaryModfileId + ".zip";
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
                Modfile modfile = JsonUtility.FromJson<Modfile>(File.ReadAllText(modfileFilePath));
                onSuccess(modfile);
            }
            else
            {
                Action<ModfileObject> writeModfileToDisk = (m) =>
                {
                    Modfile newModfile = Modfile.CreateFromModfileObject(m);
                    File.WriteAllText(modfileFilePath,
                                      JsonUtility.ToJson(newModfile));
                    onSuccess(newModfile);
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
            Action<ModfileObject> queueBinaryDownload = (m) =>
            {
                download.sourceURL = m.download.binary_url;
                download.fileURL = binaryFilePath;
                download.EnableFilehashVerification(m.filehash.md5);

                DownloadManager.AddQueuedDownload(download);
            };

            Client.GetModfile(modId, modfileId,
                              queueBinaryDownload,
                              download.MarkAsFailed);

            return download;
        }

        // ---------[ IMAGE MANAGEMENT ]---------
        public static event ModLogoUpdatedEventHandler OnModLogoUpdated;
        
        private static Dictionary<string, string> serverToLocalImageURLMap = new Dictionary<string, string>();

        public static string GenerateModLogoFilePath(int modId, ModLogoVersion version)
        {
            return GetModDirectory(modId) + @"/logo/" + version.ToString() + ".png";
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
        public static TextureDownload DownloadAndSaveImage(string serverURL,
                                                           string downloadTarget,
                                                           Texture2D placeholderTexture)
        {
            var download = new TextureDownload();

            serverToLocalImageURLMap[serverURL] = downloadTarget;
            File.WriteAllBytes(downloadTarget, placeholderTexture.EncodeToPNG());

            download.sourceURL = serverURL;
            DownloadManager.AddConcurrentDownload(download);

            return download;
        }

        public static Texture2D LoadOrDownloadModLogo(int modId, ModLogoVersion version)
        {
            // TODO(@jackson): Defend
            ModProfile profile = GetModProfile(modId);

            Texture2D texture = null;
            string filePath = string.Empty;
            string serverURL = profile.logoLocator.GetVersionSource(version);
            if(serverToLocalImageURLMap.TryGetValue(serverURL, out filePath))
            {
                Utility.TryLoadTextureFromFile(filePath, out texture);
            }

            if(texture == null)
            {
                texture = UISettings.Instance.DownloadingPlaceholderImages.modLogo;

                var download = DownloadAndSaveImage(serverURL,
                                                    GenerateModLogoFilePath(profile.id, version),
                                                    texture);
                download.OnCompleted += (d) =>
                {
                    if(OnModLogoUpdated != null)
                    {
                        OnModLogoUpdated(modId, version, download.texture);
                    }
                };
            }

            return texture;
        }

        // TODO(@jackson): param -> ids?
        // TODO(@jackson): defend
        // TODO(@jackson): Add preload function?
        public static void DownloadMissingModLogos(ModProfile[] modProfiles,
                                                   ModLogoVersion version)
        {
            var missingLogoProfiles = new List<ModProfile>(modProfiles);

            // Check which logos are missing
            foreach(ModProfile profile in modProfiles)
            {
                string serverURL = profile.logoLocator.GetVersionSource(version);
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
                var download = DownloadAndSaveImage(profile.logoLocator.GetVersionSource(version),
                                                    GenerateModLogoFilePath(profile.id, version),
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
        private static void WriteManifestToDisk()
        {
            File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest));
        }

        private static void WriteUserDataToDisk()
        {
            File.WriteAllText(userdataPath, JsonUtility.ToJson(userData));
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
            Debug.Assert(File.Exists(modEdits.logoLocator.value.source));

            var parameters = new AddModParameters();
            parameters.name = modEdits.name.value;
            parameters.summary = modEdits.summary.value;
            parameters.logo = BinaryUpload.Create(Path.GetFileName(modEdits.logoLocator.value.source),
                                                  File.ReadAllBytes(modEdits.logoLocator.value.source));
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

            Client.AddMod(userData.oAuthToken,
                          parameters,
                          result => modSubmissionSucceeded(ModProfile.CreateFromModObject(result)),
                          modSubmissionFailed);
        }
        // TODO(@jackson): Add MKVPs, Mod Dependencies
        public static void SubmitModChanges(int modId,
                                            EditableModProfile modEdits,
                                            Action<ModProfile> modSubmissionSucceeded,
                                            Action<WebRequestError> modSubmissionFailed)
        {
            Debug.Assert(modId > 0);

            // TODO(@jackson): Defend this code
            ModProfile profile = GetModProfile(modId);

            List<Action> submissionActions = new List<Action>();
            int nextActionIndex = 0;
            Action<MessageObject> doNextSubmissionAction = (m) =>
            {
                if(nextActionIndex < submissionActions.Count)
                {
                    submissionActions[nextActionIndex++]();
                }
            };

            if(modEdits.tags.isDirty)
            {
                var addedTags = new List<string>(modEdits.tags.value);
                foreach(string tag in profile.tags)
                {
                    addedTags.Remove(tag);
                }

                var removedTags = new List<string>(profile.tags);
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
                        Client.AddModTags(userData.oAuthToken,
                                          modId, parameters,
                                          doNextSubmissionAction, modSubmissionFailed);
                    });
                }
                if(removedTags.Count > 0)
                {
                    submissionActions.Add(() =>
                    {
                        var parameters = new DeleteModTagsParameters();
                        parameters.tags = removedTags.ToArray();
                        Client.DeleteModTags(userData.oAuthToken,
                                             modId, parameters,
                                             doNextSubmissionAction, modSubmissionFailed);
                    });
                }
            }

            if(modEdits.logoLocator.isDirty
               || modEdits.youtubeURLs.isDirty
               || modEdits.sketchfabURLs.isDirty
               || modEdits.galleryImageLocators.isDirty)
            {
                var addMediaParameters = new AddModMediaParameters();
                var deleteMediaParameters = new DeleteModMediaParameters();
                
                if(modEdits.logoLocator.isDirty
                   && File.Exists(modEdits.logoLocator.value.source))
                {
                    addMediaParameters.logo = BinaryUpload.Create(Path.GetFileName(modEdits.logoLocator.value.source),
                                                                  File.ReadAllBytes(modEdits.logoLocator.value.source));
                }
                
                if(modEdits.youtubeURLs.isDirty)
                {
                    var addedYouTubeLinks = new List<string>(modEdits.youtubeURLs.value);
                    foreach(string youtubeLink in profile.youtubeURLs)
                    {
                        addedYouTubeLinks.Remove(youtubeLink);
                    }
                    addMediaParameters.youtube = addedYouTubeLinks.ToArray();

                    var removedTags = new List<string>(profile.youtubeURLs);
                    foreach(string youtubeLink in modEdits.youtubeURLs.value)
                    {
                        removedTags.Remove(youtubeLink);
                    }
                    deleteMediaParameters.youtube = addedYouTubeLinks.ToArray();
                }
                
                if(modEdits.sketchfabURLs.isDirty)
                {
                    var addedSketchfabLinks = new List<string>(modEdits.sketchfabURLs.value);
                    foreach(string sketchfabLink in profile.sketchfabURLs)
                    {
                        addedSketchfabLinks.Remove(sketchfabLink);
                    }
                    addMediaParameters.sketchfab = addedSketchfabLinks.ToArray();

                    var removedTags = new List<string>(profile.sketchfabURLs);
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
                        if(File.Exists(locator.source))
                        {
                            addedImageFilePaths.Add(locator.source);
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
                    foreach(var locator in profile.galleryImageLocators)
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
            }

            if(modEdits.status.isDirty
               || modEdits.visibility.isDirty
               || modEdits.name.isDirty
               || modEdits.nameId.isDirty
               || modEdits.summary.isDirty
               || modEdits.description.isDirty
               || modEdits.homepageURL.isDirty
               || modEdits.metadataBlob.isDirty)
            {
                submissionActions.Add(() =>
                {
                    Debug.Log("Submitting Mod Info");

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

                    Client.EditMod(userData.oAuthToken,
                                   modId, parameters,
                                   result => modSubmissionSucceeded(ModProfile.CreateFromModObject(result)),
                                   modSubmissionFailed);
                });
            }
            // - Get updated ModProfile -
            else
            {
                submissionActions.Add(() =>
                {
                    Client.GetMod(modId,
                                  result => modSubmissionSucceeded(ModProfile.CreateFromModObject(result)),
                                  modSubmissionFailed);
                });
            }

            // - Start submission chain -
            doNextSubmissionAction(new MessageObject());
        }

        public static void UploadModBinary_Unzipped(int modId,
                                                    string unzippedBinaryLocation,
                                                    EditableModfile modfileValues,
                                                    bool setPrimary,
                                                    Action<Modfile> onSuccess,
                                                    Action<WebRequestError> onError)
        {
            string binaryZipLocation = Application.temporaryCachePath + "/modio/" + System.IO.Path.GetFileNameWithoutExtension(unzippedBinaryLocation) + DateTime.Now.ToFileTime() + ".zip";

            ZipUtil.Zip(binaryZipLocation, unzippedBinaryLocation);

            UploadModBinary_Zipped(modId, binaryZipLocation, modfileValues, setPrimary, onSuccess, onError);
        }

        public static void UploadModBinary_Zipped(int modId,
                                                  string binaryZipLocation,
                                                  EditableModfile modfileValues,
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

            Client.AddModfile(userData.oAuthToken,
                              modId,
                              parameters,
                              (m) => onSuccess(Modfile.CreateFromModfileObject(m)),
                              onError);
        }

        // --- TEMPORARY PASS-THROUGH FUNCTIONS ---
        public static void AddPositiveRating(int modId,
                                             Action<APIMessage> onSuccess,
                                             Action<WebRequestError> onError)
        {
            Client.AddModRating(userData.oAuthToken,
                                modId, new AddModRatingParameters(1),
                                result => onSuccess(APIMessage.CreateFromMessageObject(result)),
                                onError);
        }

        // public static void AddModTeamMember(int modId, UnsubmittedTeamMember teamMember,
        //                                     Action<APIMessage> onSuccess,
        //                                     Action<WebRequestError> onError)
        // {
        //     Client.AddModTeamMember(userData.oAuthToken,
        //                             modId, teamMember.AsAddModTeamMemberParameters(),
        //                             result => OnSuccessWrapper(result, onSuccess),
        //                             onError);
        // }

        public static void DeleteModFromServer(int modId,
                                               Action<APIMessage> onSuccess,
                                               Action<WebRequestError> onError)
        {
            // TODO(@jackson): Remvoe Mod Locally

            Client.DeleteMod(userData.oAuthToken,
                             modId,
                             result => onSuccess(APIMessage.CreateFromMessageObject(result)),
                             onError);
        }

        public static void DeleteModComment(int modId, int commentId,
                                            Action<APIMessage> onSuccess,
                                            Action<WebRequestError> onError)
        {
            Client.DeleteModComment(userData.oAuthToken,
                                    modId, commentId,
                                    result => onSuccess(APIMessage.CreateFromMessageObject(result)),
                                    onError);
        }

        // public static void GetAllModTeamMembers(int modId,
        //                                         Action<TeamMember[]> onSuccess,
        //                                         Action<WebRequestError> onError)
        // {
        //     Client.GetAllModTeamMembers(modId, GetAllModTeamMembersFilter.None,
        //                                    result => OnSuccessWrapper(result, onSuccess),
        //                                    onError);
        // }
    }
}