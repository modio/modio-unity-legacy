#define USING_TEST_SERVER
// #define TEST_IGNORE_CACHE

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ModIO
{
    public delegate void ModEventHandler(ModInfo mod);
    public delegate void ModIDEventHandler(int modId);
    public delegate void ModfileEventHandler(int modId, Modfile newModfile);
    public delegate void ModLogoEventHandler(int modId, Sprite modLogo, LogoVersion logoVersion);

    public enum ModBinaryStatus
    {
        Missing,
        RequiresUpdate,
        UpToDate
    }

    public static class ModManager
    {
        // ---------[ INNER CLASSES ]---------
        [System.Serializable]
        private class UserData
        {
            public string oAuthToken = "";
            public User user = null;
            public List<int> subscribedModIDs = new List<int>();
        }

        [System.Serializable]
        private class ManifestData
        {
            public TimeStamp lastUpdateTimeStamp;
            public List<ModEvent> unresolvedEvents;
        }

        // ---------[ VARIABLES ]---------
        private static APIClient client = null;
        private static ManifestData manifest = null;
        private static UserData userData = null;

        #if USING_TEST_SERVER
        public static string MODIO_DIR { get { return Application.persistentDataPath + "/modio_testServer/"; } }
        #else
        public static string MODIO_DIR { get { return Application.persistentDataPath + "/modio/"; } }
        #endif

        private static string MANIFEST_URL { get { return MODIO_DIR + "manifest.data"; } }
        private static string USERDATA_URL { get { return MODIO_DIR + "user.data"; } }
        
        public static APIClient APIClient { get { return client; } }
        public static User CurrentUser { get { return userData == null ? null : userData.user; } }

        // --------- [ INITIALISATION ]---------
        public static void Initialize(int gameId, string apiKey)
        {
            if(client != null)
            {
                Debug.LogWarning("Cannot re-initialize ModManager");
                return;
            }

            Debug.Log("Initializing ModIO.ModManager"
                      + "\nModIO Directory: " + MODIO_DIR);

            // --- Create Client ---
            GameObject go = new GameObject("ModIO API Client");
            client = go.AddComponent<APIClient>();
            client.InitializeWithCoroutineRequestHandler(client);
            // client.InitializeWithOnUpdateRequestHandler(out client.onUpdate);
            client.SetAccessContext(gameId, apiKey);

            GameObject.DontDestroyOnLoad(go);

            // --- Load To Cache From Disk ---
            if (!Directory.Exists(MODIO_DIR))
            {
                Directory.CreateDirectory(MODIO_DIR);
            }

            #if TEST_IGNORE_CACHE
            {
                manifest = new ManifestData();
                manifest.lastUpdateTimeStamp = new TimeStamp();
                manifest.unresolvedEvents = new List<ModEvent>();

                WriteManifestToDisk();
            }
            #else
            {
                if(!File.Exists(MANIFEST_URL))
                {
                    // --- INITIALIZE FIRST RUN ---
                    manifest = new ManifestData();
                    manifest.lastUpdateTimeStamp = new TimeStamp();
                    manifest.unresolvedEvents = new List<ModEvent>();

                    WriteManifestToDisk();
                }
                else
                {
                    manifest = JsonUtility.FromJson<ManifestData>(File.ReadAllText(MANIFEST_URL));
                }


                // iterate through folders, load ModInfo
                string[] modDirectories = Directory.GetDirectories(MODIO_DIR);
                foreach(string modDir in modDirectories)
                {
                    // Load ModInfo from Disk
                    ModInfo mod = JsonUtility.FromJson<ModInfo>(File.ReadAllText(modDir + "/mod.data"));
                    modCache.Add(mod.id, mod);
                }

                // Attempt to load user
                if(File.Exists(USERDATA_URL))
                {
                    userData = JsonUtility.FromJson<UserData>(File.ReadAllText(USERDATA_URL));

                    client.GetAuthenticatedUser(userData.oAuthToken,
                                                APIClient.IgnoreResponse,
                                                (error) =>
                                                {
                                                    if(error.httpStatusCode == 401
                                                       || error.httpStatusCode == 403) // Failed authentication
                                                    {
                                                        LogUserOut();
                                                    };
                                                });
                }
            }
            #endif

            FetchAndCacheAllMods();
        }

        private static void FetchAndCacheAllMods()
        {
            Action<ModInfo[]> AddModsToCache = (mods) =>
            {
                // TODO(@jackson): Implement mod is unavailable
                // TODO(@jackson): Check for modfile change

                manifest.lastUpdateTimeStamp = TimeStamp.Now();
                WriteManifestToDisk();

                List<ModInfo> updatedMods = new List<ModInfo>();
                List<ModInfo> addedMods = new List<ModInfo>();

                foreach(ModInfo mod in mods)
                {
                    ModInfo cachedMod;
                    if(modCache.TryGetValue(mod.id, out cachedMod)
                       && !cachedMod.Equals(mod))
                    {
                        CacheMod(mod);
                        updatedMods.Add(mod);
                    }
                    else
                    {
                        CacheMod(mod);
                        addedMods.Add(mod);
                    }
                }

                if(OnModAdded != null)
                {
                    foreach(ModInfo mod in addedMods)
                    {
                        OnModAdded(mod);
                    }
                }

                if(OnModUpdated != null)
                {
                    foreach(ModInfo mod in updatedMods)
                    {
                        OnModUpdated(mod.id);
                    }
                }
            };

            client.GetAllMods(GetAllModsFilter.None,
                              AddModsToCache,
                              APIClient.LogError);
        }

        // ---------[ AUTOMATED UPDATING ]---------
        private const float SECONDS_BETWEEN_POLLING = 15.0f;
        private static bool isUpdatePollingEnabled = false;
        private static bool isUpdatePollingRunning = false;

        public static void EnableUpdatePolling()
        {
            isUpdatePollingEnabled = true;
            
            if(!isUpdatePollingRunning)
            {
                client.StartCoroutine(PollForUpdates());
            }
        }
        public static void DisableUpdatePolling()
        {
            isUpdatePollingEnabled = false;
        }

        private static IEnumerator PollForUpdates()
        {
            if(isUpdatePollingEnabled)
            {
                isUpdatePollingRunning = true;
                
                TimeStamp fromTimeStamp = manifest.lastUpdateTimeStamp;
                TimeStamp untilTimeStamp = TimeStamp.Now();

                // - Get ModInfo Events -
                GetAllModEventsFilter eventFilter = new GetAllModEventsFilter();
                eventFilter.ApplyIntRange(GetAllModEventsFilter.Field.DateAdded,
                                          fromTimeStamp.AsServerTimeStamp(), true,
                                          untilTimeStamp.AsServerTimeStamp(), false);
                eventFilter.ApplyBooleanIs(GetAllModEventsFilter.Field.Latest,
                                           true);

                client.GetAllModEvents(eventFilter,
                                       (eventArray) =>
                                       {
                                        manifest.lastUpdateTimeStamp = untilTimeStamp;
                                        ProcessModEvents(eventArray);
                                       },
                                       APIClient.LogError);

                // - Get Subscription Updates -
                if(userData != null)
                {
                    GetUserSubscriptionsFilter subscriptionFilter = new GetUserSubscriptionsFilter();
                    subscriptionFilter.ApplyIntEquality(GetUserSubscriptionsFilter.Field.GameId, client.gameId);

                    APIClient.GetUserSubscriptions(userData.oAuthToken,
                                                   subscriptionFilter,
                                                   UpdateSubscriptions,
                                                   APIClient.LogError);
                }


                yield return new WaitForSeconds(SECONDS_BETWEEN_POLLING);
                isUpdatePollingRunning = false;

                client.StartCoroutine(PollForUpdates());
            }
        }

        private static void ProcessModEvents(ModEvent[] eventArray)
        {
            // - ModInfo Processing Options -
            Action<ModEvent> processModAvailable = (modEvent) =>
            {
                client.GetMod(modEvent.modId,
                              (mod) =>
                              {
                                CacheMod(mod);
                                manifest.unresolvedEvents.Remove(modEvent);

                                if(OnModAdded != null)
                                {
                                    OnModAdded(mod);
                                }
                              },
                              APIClient.LogError);
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
                client.GetMod(modEvent.modId,
                              (mod) =>
                              {
                                CacheMod(mod);
                                manifest.unresolvedEvents.Remove(modEvent);

                                if(OnModUpdated != null)
                                {
                                    OnModUpdated(mod.id);
                                }
                              },
                              APIClient.LogError);
            };


            Action<ModEvent> processModfileChange = (modEvent) =>
            {
                ModInfo mod = GetMod(modEvent.modId);

                if(mod == null)
                {
                    Debug.Log("Received Modfile change for uncached mod. Ignoring.");
                    manifest.unresolvedEvents.Remove(modEvent);
                }
                else
                {
                    client.GetMod(mod.id,
                                  (updatedMod) =>
                                  {
                                    CacheMod(updatedMod);

                                    if(OnModfileChanged != null)
                                    {
                                        OnModfileChanged(updatedMod.id, updatedMod.modfile);
                                    }
                                  },
                                  APIClient.LogError);

                    manifest.unresolvedEvents.Remove(modEvent);
                }
            };


            // - Handle ModInfo Event -
            foreach(ModEvent modEvent in eventArray)
            {
                string eventSummary = "TimeStamp (Local)=" + modEvent.dateAdded.AsLocalDateTime();
                eventSummary += "\nMod=" + modEvent.modId;
                eventSummary += "\nEventType=" + modEvent.eventType.ToString();
                
                Debug.Log("[PROCESSING MOD EVENT]\n" + eventSummary);


                manifest.unresolvedEvents.Add(modEvent);

                switch(modEvent.eventType)
                {
                    case ModEvent.EventType.ModfileChanged:
                    {
                        processModfileChange(modEvent);
                    }
                    break;
                    case ModEvent.EventType.ModAvailable:
                    {
                        processModAvailable(modEvent);
                    }
                    break;
                    case ModEvent.EventType.ModUnavailable:
                    {
                        processModUnavailable(modEvent);
                    }
                    break;
                    case ModEvent.EventType.ModEdited:
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

        private static void UpdateSubscriptions(ModInfo[] subscribedMods)
        {
            if(userData == null) { return; }

            List<int> addedMods = new List<int>();
            List<int> removedMods = new List<int>(userData.subscribedModIDs);
            userData.subscribedModIDs = new List<int>(subscribedMods.Length);

            foreach(ModInfo mod in subscribedMods)
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
                                               Action<ErrorInfo> onError)
        {
            client.RequestSecurityCode(emailAddress,
                                       APIClient.IgnoreResponse,
                                       onError);
        }

        public static void RequestOAuthToken(string securityCode,
                                             Action<string> onSuccess,
                                             Action<ErrorInfo> onError)
        {
            client.RequestOAuthToken(securityCode,
                                     onSuccess,
                                     onError);
        }

        public static void TryLogUserIn(string userOAuthToken,
                                        Action<User> onSuccess,
                                        Action<ErrorInfo> onError)
        {
            Action fetchUserSubscriptions = () =>
            {
                APIClient.GetUserSubscriptions(userOAuthToken,
                                               GetUserSubscriptionsFilter.None,
                                               UpdateSubscriptions,
                                               APIClient.LogError);
            };

            client.GetAuthenticatedUser(userOAuthToken,
                                        user =>
                                        {
                                            userData = new UserData();
                                            userData.oAuthToken = userOAuthToken;
                                            userData.user = user;
                                            WriteUserDataToDisk();

                                            onSuccess(user);

                                            fetchUserSubscriptions();
                                        },
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

        public static User GetActiveUser()
        {
            return (userData == null ? null : userData.user);
        }

        public static void SubscribeToMod(int modId,
                                          Action<ModInfo> onSuccess,
                                          Action<ErrorInfo> onError)
        {
            client.SubscribeToMod(userData.oAuthToken,
                                  modId,
                                  (message) =>
                                  {
                                    userData.subscribedModIDs.Add(modId);
                                    onSuccess(GetMod(modId));
                                  },
                                  onError);
        }

        public static void UnsubscribeFromMod(int modId,
                                              Action<ModInfo> onSuccess,
                                              Action<ErrorInfo> onError)
        {
            client.UnsubscribeFromMod(userData.oAuthToken,
                                      modId,
                                      (message) =>
                                      {
                                        userData.subscribedModIDs.Remove(modId);
                                        onSuccess(GetMod(modId));
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
        public static event ModLogoEventHandler OnModLogoUpdated;

        private static Dictionary<int, ModInfo> modCache = new Dictionary<int, ModInfo>();

        public static string GetModDirectory(int modId)
        {
            return MODIO_DIR + modId + "/";
        }

        public static ModInfo GetMod(int modId)
        {
            ModInfo mod = null;
            modCache.TryGetValue(modId, out mod);
            return mod;
        }

        private static void CacheMod(ModInfo mod)
        {
            modCache[mod.id] = mod;
            WriteModToDisk(mod);
        }
        private static void CacheMods(ModInfo[] modArray)
        {
            foreach(ModInfo mod in modArray)
            {
                CacheMod(mod);
            }
        }
        private static void UncacheMod(int modId)
        {
            string modDir = GetModDirectory(modId);
            Directory.Delete(modDir, true);
        }

        public static void GetMods(GetAllModsFilter filter, Action<ModInfo[]> callback)
        {
            ModInfo[] retVal = filter.FilterCollection(modCache.Values);
            callback(retVal);
        }

        public static FileDownload StartBinaryDownload(int modId, int modfileId)
        {
            string fileURL = GetModDirectory(modId) + "modfile_" + modfileId + ".zip";
            
            FileDownload download = new FileDownload();
            download.OnCompleted += (d) =>
            {
                // Remove any other binaries
                string[] binaryFilePaths = Directory.GetFiles(GetModDirectory(modId), "modfile_*.zip");
                foreach(string binaryFilePath in binaryFilePaths)
                {
                    if(binaryFilePath != fileURL)
                    {
                        File.Delete(binaryFilePath);
                    }
                }
            };

            Action<Modfile> queueBinaryDownload = (modfile) =>
            {
                download.sourceURL = modfile.download.binaryURL;
                download.fileURL = fileURL;
                download.EnableFilehashVerification(modfile.filehash.md5);

                DownloadManager.AddQueuedDownload(download);
            };

            client.GetModfile(modId, modfileId,
                              queueBinaryDownload,
                              download.MarkAsFailed);

            return download;
        }

        public static void DeleteAllDownloadedBinaries(ModInfo mod)
        {
            string[] binaryFilePaths = Directory.GetFiles(GetModDirectory(mod.id), "modfile_*.zip");
            foreach(string binaryFilePath in binaryFilePaths)
            {
                File.Delete(binaryFilePath);
            }
        }

        public static ModBinaryStatus GetBinaryStatus(ModInfo mod)
        {
            if(File.Exists(GetModDirectory(mod.id) + "modfile_" + mod.modfile.id + ".zip"))
            {
                return ModBinaryStatus.UpToDate;
            }
            else
            {
                string[] modfileURLs = Directory.GetFiles(GetModDirectory(mod.id), "modfile_*.zip");
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

        public static string GetBinaryPath(ModInfo mod)
        {
            if(File.Exists(GetModDirectory(mod.id) + "modfile_" + mod.modfile.id + ".zip"))
            {
                return GetModDirectory(mod.id) + "modfile_" + mod.modfile.id + ".zip";
            }
            else
            {
                string[] modfileURLs = Directory.GetFiles(GetModDirectory(mod.id), "modfile_*.zip");
                if(modfileURLs.Length > 0)
                {
                    return modfileURLs[0];
                }
            }
            return null;
        }

        // ---------[ LOGO MANAGEMENT ]---------
        private class LogoTemplate
        {
            static LogoTemplate()
            {
                versionArray = new LogoTemplate[Enum.GetValues(typeof(LogoVersion)).Length];

                versionArray[(int)LogoVersion.Original] = new LogoTemplate();
                versionArray[(int)LogoVersion.Original].version = LogoVersion.Original;
                versionArray[(int)LogoVersion.Original].localFilename = "logo_original.png";
                versionArray[(int)LogoVersion.Original].getServerURL = (ModInfo m) => { return m.logo.original; };

                versionArray[(int)LogoVersion.Thumb_320x180] = new LogoTemplate();
                versionArray[(int)LogoVersion.Thumb_320x180].version = LogoVersion.Thumb_320x180;
                versionArray[(int)LogoVersion.Thumb_320x180].localFilename = "logo_320x180.png";
                versionArray[(int)LogoVersion.Thumb_320x180].getServerURL = (ModInfo m) => { return m.logo.thumb320x180; };

                versionArray[(int)LogoVersion.Thumb_640x360] = new LogoTemplate();
                versionArray[(int)LogoVersion.Thumb_640x360].version = LogoVersion.Thumb_640x360;
                versionArray[(int)LogoVersion.Thumb_640x360].localFilename = "logo_640x360.png";
                versionArray[(int)LogoVersion.Thumb_640x360].getServerURL = (ModInfo m) => { return m.logo.thumb640x360; };

                versionArray[(int)LogoVersion.Thumb_1280x720] = new LogoTemplate();
                versionArray[(int)LogoVersion.Thumb_1280x720].version = LogoVersion.Thumb_1280x720;
                versionArray[(int)LogoVersion.Thumb_1280x720].localFilename = "logo_1280x720.png";
                versionArray[(int)LogoVersion.Thumb_1280x720].getServerURL = (ModInfo m) => { return m.logo.thumb1280x720; };
            }

            private static LogoTemplate[] versionArray;
            public static LogoTemplate ForLogoVersion(LogoVersion version)
            {
                return versionArray[(int)version];
            }

            // - Fields -
            public LogoVersion version = LogoVersion.Original;
            public string localFilename = "";
            public Func<ModInfo, string> getServerURL = null;
        }

        public static LogoVersion cachedLogoVersion = LogoVersion.Thumb_1280x720;
        public static Dictionary<int, Sprite> modLogoCache = new Dictionary<int, Sprite>();
        public static Sprite modLogoDownloadingPlaceholder;

        public static Sprite GetModLogo(ModInfo mod, LogoVersion logoVersion)
        {
            Sprite retVal;

            // NOTE(@jackson): Potentially return an off-res version?
            if(cachedLogoVersion == logoVersion
               && modLogoCache.TryGetValue(mod.id, out retVal))
            {
                return retVal;
            }
            else
            {
                LogoTemplate logoTemplate = LogoTemplate.ForLogoVersion(logoVersion);

                #if !TEST_IGNORE_CACHE
                string localURL = GetModDirectory(mod.id) + logoTemplate.localFilename;
                if(File.Exists(localURL))
                {
                    Texture2D logoTexture = new Texture2D(0, 0);
                    logoTexture.LoadImage(File.ReadAllBytes(localURL));

                    return Sprite.Create(logoTexture,
                                         new Rect(0, 0, logoTexture.width, logoTexture.height),
                                         Vector2.zero);
                }
                else
                #endif
                {
                    StartLogoDownload(mod, logoTemplate);
                    return modLogoDownloadingPlaceholder;
                }
            }
        }

        private static void StartLogoDownload(ModInfo mod, LogoTemplate logoTemplate)
        {
            string logoURL = logoTemplate.getServerURL(mod);

            TextureDownload download = new TextureDownload();
            download.sourceURL = logoURL;
            download.OnCompleted += (d) =>
            {
                TextureDownload textureDownload = download as TextureDownload;
                Texture2D logoTexture = textureDownload.texture;

                // - Cache -
                if(cachedLogoVersion == logoTemplate.version)
                {
                    modLogoCache[mod.id]
                        = Sprite.Create(logoTexture,
                                        new Rect(0, 0, logoTexture.width, logoTexture.height),
                                        Vector2.zero);
                }

                // - Save to disk -
                string localURL = GetModDirectory(mod.id) + logoTemplate.localFilename;
                byte[] bytes = logoTexture.EncodeToPNG();
                File.WriteAllBytes(localURL, bytes);

                // - Notify -
                if(OnModLogoUpdated != null)
                {
                    OnModLogoUpdated(mod.id, modLogoCache[mod.id], logoTemplate.version);
                }
            };

            DownloadManager.AddConcurrentDownload(download);
        }

        public static void CacheModLogos(ModInfo[] modLogosToCache,
                                         LogoVersion logoVersion)
        {
            LogoTemplate logoTemplate = LogoTemplate.ForLogoVersion(logoVersion);
            List<ModInfo> missingLogoList = new List<ModInfo>(modLogosToCache.Length);
            
            // Reset Cache if logoVersion is incorrect
            if(logoVersion != cachedLogoVersion)
            {
                modLogoCache = new Dictionary<int, Sprite>(modLogosToCache.Length);
            }

            // Check which logos are missing
            foreach(ModInfo mod in modLogosToCache)
            {
                if(modLogoCache.ContainsKey(mod.id))
                {
                    continue;
                }

                #if !TEST_IGNORE_CACHE
                string logoFilepath = GetModDirectory(mod.id) + logoTemplate.localFilename;
                if(File.Exists(logoFilepath))
                {
                    Texture2D logoTexture = new Texture2D(0,0);
                    logoTexture.LoadImage(File.ReadAllBytes(logoFilepath));

                    modLogoCache[mod.id]
                        = Sprite.Create(logoTexture,
                                        new Rect(0, 0, logoTexture.width, logoTexture.height),
                                        Vector2.zero);

                    if(OnModLogoUpdated != null)
                    {
                        OnModLogoUpdated(mod.id, modLogoCache[mod.id], logoTemplate.version);
                    }
                }
                else
                #endif
                {
                    modLogoCache.Add(mod.id, modLogoDownloadingPlaceholder);
                    missingLogoList.Add(mod);
                    
                    if(OnModLogoUpdated != null)
                    {
                        OnModLogoUpdated(mod.id, modLogoDownloadingPlaceholder, logoTemplate.version);
                    }
                }
            }

            // Download
            foreach(ModInfo mod in missingLogoList)
            {
                StartLogoDownload(mod, logoTemplate);
            }
        }

        public static string GetModLogoFilepath(ModInfo mod, LogoVersion logoVersion)
        {
            LogoTemplate logoTemplate = LogoTemplate.ForLogoVersion(logoVersion);
            return GetModDirectory(mod.id) + logoTemplate.localFilename;
        }

        // ---------[ MISC ]------------
        public static void RequestTagCategoryMap(Action<GameTagOption[]> onSuccess,
                                                 Action<ErrorInfo> onError)
        {
            client.GetAllGameTagOptions(onSuccess, onError);
        }

        private static void WriteManifestToDisk()
        {
            File.WriteAllText(MANIFEST_URL, JsonUtility.ToJson(manifest));
        }

        private static void WriteUserDataToDisk()
        {
            File.WriteAllText(USERDATA_URL, JsonUtility.ToJson(userData));
        }

        private static void WriteModToDisk(ModInfo mod)
        {
            string modDir = GetModDirectory(mod.id);
            Directory.CreateDirectory(modDir);
            File.WriteAllText(modDir + "mod.data", JsonUtility.ToJson(mod));
        }

        private static void DeleteUserDataFromDisk()
        {
            File.Delete(USERDATA_URL);
        }

        // --- TEMPORARY PASS-THROUGH FUNCTIONS ---
        public static void EditMod(EditableModInfo modInfo,
                                   Action<ModInfo> onSuccess,
                                   Action<ErrorInfo> onError)
        {
            // TODO(@jackson): Force an update poll
            client.EditMod(userData.oAuthToken, modInfo, onSuccess, onError);
        }

        public static void AddMod(AddableModInfo modInfo,
                                  Action<ModInfo> onSuccess,
                                  Action<ErrorInfo> onError)
        {
            client.AddMod(userData.oAuthToken, modInfo, onSuccess, onError);
        }

        public static void AddModMedia(int modId, UnsubmittedModMedia modMedia,
                                       Action<APIMessage> onSuccess,
                                       Action<ErrorInfo> onError)
        {
            client.AddModMedia(userData.oAuthToken,
                                  modId, modMedia,
                                  onSuccess, onError);
        }

        public static void AddModfile(int modId,
                                      UnsubmittedModfile modfile,
                                      Action<Modfile> onSuccess,
                                      Action<ErrorInfo> onError)
        {
            client.AddModfile(userData.oAuthToken, modId, modfile, onSuccess, onError);
        }

        public static void AddGameMedia(UnsubmittedGameMedia gameMedia,
                                        Action<APIMessage> onSuccess,
                                        Action<ErrorInfo> onError)
        {
            client.AddGameMedia(userData.oAuthToken, gameMedia, onSuccess, onError);
        }

        public static void AddGameTagOption(UnsubmittedGameTagOption tagOption,
                                            Action<APIMessage> onSuccess,
                                            Action<ErrorInfo> onError)
        {
            client.AddGameTagOption(userData.oAuthToken, tagOption, onSuccess, onError);
        }

        public static void AddModTags(int modId, string[] tagNames,
                                      Action<APIMessage> onSuccess,
                                      Action<ErrorInfo> onError)
        {
            client.AddModTags(userData.oAuthToken, modId, tagNames,
                              onSuccess, onError);
        }

        public static void AddPositiveRating(int modId,
                                             Action<APIMessage> onSuccess,
                                             Action<ErrorInfo> onError)
        {
            client.AddModRating(userData.oAuthToken, modId, 1, onSuccess, onError);
        }

        public static void AddModKVPMetadata(int modId, UnsubmittedMetadataKVP[] metadataKVPs,
                                             Action<APIMessage> onSuccess,
                                             Action<ErrorInfo> onError)
        {
            client.AddModKVPMetadata(userData.oAuthToken,
                                     modId, metadataKVPs,
                                     onSuccess, onError);
        }

        public static void AddModTeamMember(int modId, UnsubmittedTeamMember teamMember,
                                            Action<APIMessage> onSuccess,
                                            Action<ErrorInfo> onError)
        {
            client.AddModTeamMember(userData.oAuthToken,
                                    modId, teamMember,
                                    onSuccess, onError);
        }

        public static void DeleteMod(int modId,
                                     Action<APIMessage> onSuccess,
                                     Action<ErrorInfo> onError)
        {
            client.DeleteMod(userData.oAuthToken, modId,
                             onSuccess, onError);
        }

        public static void DeleteModMedia(int modId, ModMediaToDelete modMediaToDelete,
                                          Action<APIMessage> onSuccess,
                                          Action<ErrorInfo> onError)
        {
            client.DeleteModMedia(userData.oAuthToken, modId, modMediaToDelete,
                                  onSuccess, onError);
        }

        public static void DeleteModTags(int modId, string[] tagsToDelete,
                                         Action<APIMessage> onSuccess,
                                         Action<ErrorInfo> onError)
        {
            client.DeleteModTags(userData.oAuthToken, modId, tagsToDelete,
                                 onSuccess, onError);
        }

        public static void DeleteModDependencies(int modId, int[] modIdsToRemove,
                                                 Action<APIMessage> onSuccess,
                                                 Action<ErrorInfo> onError)
        {
            client.DeleteModDependencies(userData.oAuthToken, modId, modIdsToRemove,
                                         onSuccess, onError);
        }

        public static void AddModDependencies(int modId, int[] modIdsToRemove,
                                              Action<APIMessage> onSuccess,
                                              Action<ErrorInfo> onError)
        {
            client.AddModDependencies(userData.oAuthToken, modId, modIdsToRemove,
                                      onSuccess, onError);
        }

        public static void DeleteModKVPMetadata(int modId, UnsubmittedMetadataKVP[] metadataKVPs,
                                                Action<APIMessage> onSuccess,
                                                Action<ErrorInfo> onError)
        {
            client.DeleteModKVPMetadata(userData.oAuthToken, modId, metadataKVPs,
                                        onSuccess, onError);
        }

        public static void DeleteModTeamMember(int modId, int teamMemberId,
                                               Action<APIMessage> onSuccess,
                                               Action<ErrorInfo> onError)
        {
            client.DeleteModTeamMember(userData.oAuthToken, modId, teamMemberId,
                                       onSuccess, onError);
        }

        public static void DeleteModComment(int modId, int commentId,
                                            Action<APIMessage> onSuccess,
                                            Action<ErrorInfo> onError)
        {
            client.DeleteModComment(userData.oAuthToken, modId, commentId,
                                    onSuccess, onError);
        }

        public static void GetAllModTeamMembers(int modId,
                                                Action<TeamMember[]> onSuccess,
                                                Action<ErrorInfo> onError)
        {
            client.GetAllModTeamMembers(modId, GetAllModTeamMembersFilter.None,
                                        onSuccess, onError);
        }

        public static void DeleteGameTagOption(GameTagOptionToDelete gameTagOption,
                                               Action<APIMessage> onSuccess,
                                               Action<ErrorInfo> onError)
        {
            client.DeleteGameTagOption(userData.oAuthToken, gameTagOption,
                                       onSuccess, onError);
        }
    }
}