// #define TEST_IGNORE_DISK_CACHE

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using ModIO.API;

namespace ModIO
{
    public delegate void ModEventHandler(ModInfo mod);
    public delegate void ModIDEventHandler(int modId);
    public delegate void ModfileEventHandler(int modId, Modfile newModfile);
    public delegate void ModLogoEventHandler(int modId, Texture2D modLogo, ImageVersion logoVersion);

    public enum ModBinaryStatus
    {
        Missing,
        RequiresUpdate,
        UpToDate
    }

    public class ModManager : MonoBehaviour
    {
        private static ModManager instance = null;

        private Action requestHandlerUpdateHandle = null;
        private void Update()
        {
            if(requestHandlerUpdateHandle != null)
            {
                requestHandlerUpdateHandle();
            }
        }

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
            public GameInfo game;
        }

        // ---------[ VARIABLES ]---------
        private static ManifestData manifest = null;
        public static GameInfo gameInfo { get { return manifest.game; }}

        private static UserData userData = null;
        public static User currentUser { get { return userData == null ? null : userData.user; } }

        public static string cacheDirectory { get; private set; }
        
        private static string manifestPath { get { return cacheDirectory + "manifest.data"; } }
        private static string userdataPath { get { return cacheDirectory + "user.data"; } }

        // ---------[ OBJECT WRAPPING ]---------
        private static void OnSuccessWrapper<T_APIObj, T>(T_APIObj apiObject,
                                                          Action<T> successCallback)
        where T_APIObj : struct
        where T : IAPIObjectWrapper<T_APIObj>, new()
        {
            if(successCallback != null)
            {
                T wrapperObject = new T();
                wrapperObject.WrapAPIObject(apiObject);
                successCallback(wrapperObject);
            }
        }

        private static void OnSuccessWrapper<T_APIObj, T>(ObjectArray<T_APIObj> apiObjectArray,
                                                          Action<T[]> successCallback)
        where T_APIObj : struct
        where T : IAPIObjectWrapper<T_APIObj>, new()
        {
            if(successCallback != null)
            {
                T[] wrapperObjectArray = new T[apiObjectArray.data.Length];
                for(int i = 0;
                    i < apiObjectArray.data.Length;
                    ++i)
                {
                    T newObject = new T();
                    newObject.WrapAPIObject(apiObjectArray.data[i]);
                    
                    wrapperObjectArray[i] = newObject;
                }

                successCallback(wrapperObjectArray);
            }
        }

        // --------- [ INITIALIZATION ]---------
        public static void Initialize()
        {
            if(manifest != null)
            {
                return;
            }

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

            // TODO(@jackson): Listen to logo update for caching

            LoadCacheFromDisk();
            FetchAndCacheAllMods();
            FetchAndCacheGameInfo();
        }

        private static void LoadCacheFromDisk()
        {
            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }

            #if TEST_IGNORE_DISK_CACHE
            {
                manifest = new ManifestData();
                manifest.lastUpdateTimeStamp = new TimeStamp();
                manifest.unresolvedEvents = new List<ModEvent>();

                WriteManifestToDisk();
            }
            #else
            {
                if(!File.Exists(manifestPath))
                {
                    // --- INITIALIZE FIRST RUN ---
                    manifest = new ManifestData();
                    manifest.lastUpdateTimeStamp = new TimeStamp();
                    manifest.unresolvedEvents = new List<ModEvent>();

                    WriteManifestToDisk();
                }
                else
                {
                    manifest = JsonUtility.FromJson<ManifestData>(File.ReadAllText(manifestPath));
                }


                // iterate through folders, load ModInfo
                if(!Directory.Exists(cacheDirectory + "mods/"))
                {
                    Directory.CreateDirectory(cacheDirectory + "mods/");
                }

                string[] modDirectories = Directory.GetDirectories(cacheDirectory + "mods/");
                foreach(string modDir in modDirectories)
                {
                    // Load ModInfo from Disk
                    ModInfo mod = JsonUtility.FromJson<ModInfo>(File.ReadAllText(modDir + "/mod.data"));
                    modCache.Add(mod.id, mod);
                }

                // Attempt to load user
                if(File.Exists(userdataPath))
                {
                    userData = JsonUtility.FromJson<UserData>(File.ReadAllText(userdataPath));

                    Action<ErrorInfo> onAuthenticationFail = (error) =>
                    {
                        if(error.httpStatusCode == 401
                            || error.httpStatusCode == 403) // Failed authentication
                        {
                            LogUserOut();
                        }
                    };

                    Client.GetAuthenticatedUser(userData.oAuthToken,
                                                   null,
                                                   onAuthenticationFail);
                }
            }
            #endif
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

            Client.GetAllMods(GetAllModsFilter.None,
                                 result => OnSuccessWrapper<ModObject, ModInfo>(result, AddModsToCache),
                                 Client.LogError);
        }

        private static void FetchAndCacheGameInfo()
        {
            Action<GameInfo> cacheGameInfo = (game) =>
            {
                manifest.game = game;
                WriteManifestToDisk();
            };

            Client.GetGame(result => OnSuccessWrapper(result, cacheGameInfo),
                              null);
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
                instance.StartCoroutine(PollForUpdates());
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

                // - Get Game Updates -
                FetchAndCacheGameInfo();

                // - Get ModInfo Events -
                GetAllModEventsFilter eventFilter = new GetAllModEventsFilter();
                eventFilter.ApplyIntRange(GetAllModEventsFilter.Field.DateAdded,
                                          fromTimeStamp.AsServerTimeStamp(), true,
                                          untilTimeStamp.AsServerTimeStamp(), false);
                eventFilter.ApplyBooleanIs(GetAllModEventsFilter.Field.Latest,
                                           true);

                Action<ModEvent[]> onModEventsReceived = (eventArray) =>
                {
                    manifest.lastUpdateTimeStamp = untilTimeStamp;
                    ProcessModEvents(eventArray);
                };
                Client.GetAllModEvents(eventFilter,
                                          result => OnSuccessWrapper(result, onModEventsReceived),
                                          Client.LogError);

                // TODO(@jackson): Replace with Event Polling
                // - Get Subscription Updates -
                if(userData != null)
                {
                    GetUserSubscriptionsFilter subscriptionFilter = new GetUserSubscriptionsFilter();
                    subscriptionFilter.ApplyIntEquality(GetUserSubscriptionsFilter.Field.GameId, GlobalSettings.GAME_ID);

                    Client.GetUserSubscriptions(userData.oAuthToken,
                                                   subscriptionFilter,
                                                   result => OnSuccessWrapper<ModObject, ModInfo>(result, UpdateSubscriptions),
                                                   Client.LogError);
                }

                yield return new WaitForSeconds(SECONDS_BETWEEN_POLLING);
                isUpdatePollingRunning = false;

                instance.StartCoroutine(PollForUpdates());
            }
        }

        private static void ProcessModEvents(ModEvent[] eventArray)
        {
            // - ModInfo Processing Options -
            Action<ModEvent> processModAvailable = (modEvent) =>
            {
                Action<ModInfo> onGetMod = (mod) =>
                {
                    CacheMod(mod);
                    manifest.unresolvedEvents.Remove(modEvent);

                    if(OnModAdded != null)
                    {
                        OnModAdded(mod);
                    }
                };

                Client.GetMod(modEvent.modId,
                                 result => OnSuccessWrapper(result, onGetMod),
                                 Client.LogError);
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
                Action<ModInfo> onGetMod = (mod) =>
                {
                    CacheMod(mod);
                    manifest.unresolvedEvents.Remove(modEvent);

                    if(OnModUpdated != null)
                    {
                        OnModUpdated(mod.id);
                    }
                };
                Client.GetMod(modEvent.modId,
                                 result => OnSuccessWrapper(result, onGetMod),
                                 Client.LogError);
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
                    Action<ModInfo> onGetMod = (updatedMod) =>
                    {
                        CacheMod(updatedMod);

                        if(OnModfileChanged != null)
                        {
                            OnModfileChanged(updatedMod.id, updatedMod.modfile);
                        }
                    };

                    Client.GetMod(mod.id,
                                     result => OnSuccessWrapper(result, onGetMod),
                                     Client.LogError);

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
                                               Action<APIMessage> onSuccess,
                                               Action<ErrorInfo> onError)
        {
            Client.RequestSecurityCode(emailAddress,
                                          result => OnSuccessWrapper(result, onSuccess),
                                          onError);
        }

        public static void RequestOAuthToken(string securityCode,
                                             Action<string> onSuccess,
                                             Action<ErrorInfo> onError)
        {
            Client.RequestOAuthToken(securityCode,
                                        onSuccess,
                                        onError);
        }

        public static void TryLogUserIn(string userOAuthToken,
                                        Action<User> onSuccess,
                                        Action<ErrorInfo> onError)
        {
            Action<User> onGetUser = (user) =>
            {
                userData = new UserData();
                userData.oAuthToken = userOAuthToken;
                userData.user = user;
                WriteUserDataToDisk();

                onSuccess(user);

                Client.GetUserSubscriptions(userOAuthToken,
                                               GetUserSubscriptionsFilter.None,
                                               result => OnSuccessWrapper<ModObject, ModInfo>(result, UpdateSubscriptions),
                                               Client.LogError);
            };

            Client.GetAuthenticatedUser(userOAuthToken,
                                           result => OnSuccessWrapper(result, onGetUser),
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
            Client.SubscribeToMod(userData.oAuthToken,
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
            Client.UnsubscribeFromMod(userData.oAuthToken,
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
            return cacheDirectory + "mods/" + modId + "/";
        }

        public static ModInfo GetMod(int modId)
        {
            ModInfo mod = null;
            modCache.TryGetValue(modId, out mod);
            return mod;
        }

        private static void CacheMod(ModInfo mod)
        {
            string modDir = GetModDirectory(mod.id);
            Directory.CreateDirectory(modDir);
            
            modCache[mod.id] = mod;
            File.WriteAllText(modDir + "mod.data", JsonUtility.ToJson(mod));

            var modLogoCollection = mod.logo.AsModImageURLCollection(mod.id);
            modLogoURLMap[mod.id] = modLogoCollection;
            File.WriteAllText(modDir + "mod_logo.data", JsonUtility.ToJson(modLogoCollection));
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

        public static ModInfo[] GetMods(GetAllModsFilter filter)
        {
            ModInfo[] retVal = filter.FilterCollection(modCache.Values);
            return retVal;
        }


        public static void DeleteAllDownloadedBinaries(int modId)
        {
            string[] binaryFilePaths = Directory.GetFiles(GetModDirectory(modId), "modfile_*.zip");
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

        // ---------[ MODFILE & BINARY MANAGEMENT ]---------
        public static void LoadOrDownloadModfile(int modId, int modfileId,
                                                 Action<Modfile> onSuccess,
                                                 Action<ErrorInfo> onError)
        {
            string modfileFilePath = (GetModDirectory(modId) + "modfile_"
                                      + modfileId.ToString() + ".data");
            if(File.Exists(modfileFilePath))
            {
                // Load ModInfo from Disk
                Modfile modfile = JsonUtility.FromJson<Modfile>(File.ReadAllText(modfileFilePath));
                onSuccess(modfile);
            }
            else
            {
                Action<ModfileObject> writeModfileToDisk = (m) =>
                {
                    Modfile newModfile = Modfile.CreateFromAPIObject(m);
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

        // ---------[ LOGO MANAGEMENT ]---------
        private static Dictionary<int, ModImageURLCollection> modLogoURLMap = new Dictionary<int, ModImageURLCollection>();

        public static ImageVersion cachedImageVersion = ImageVersion.Thumb_1280x720;
        public static Dictionary<int, Texture2D> modLogoCache = new Dictionary<int, Texture2D>();

        private static Func<ModImageURLCollection, string> GenerateGetModLogoURLFunction(ImageVersion version)
        {
            switch(version)
            {
                case ImageVersion.Original:
                {
                    return (ModImageURLCollection collection) => { return collection.original; };
                }
                case ImageVersion.Thumb_320x180:
                {
                    return (ModImageURLCollection collection) => { return collection.thumb320x180; };
                }
                case ImageVersion.Thumb_640x360:
                {
                    return (ModImageURLCollection collection) => { return collection.thumb640x360; };
                }
                case ImageVersion.Thumb_1280x720:
                {
                    return (ModImageURLCollection collection) => { return collection.thumb1280x720; };
                }
            }
            return null;
        }

        private static string GetLogoFileName(ImageVersion logoVersion)
        {
            switch(logoVersion)
            {
                case ImageVersion.Original:
                {
                    return "logo_original.png";
                }
                case ImageVersion.Thumb_320x180:
                {
                    return "logo_320x180.png";
                }
                case ImageVersion.Thumb_640x360:
                {
                    return "logo_640x360.png";
                }
                case ImageVersion.Thumb_1280x720:
                {
                    return "logo_1280x720.png";
                }
            }
            return null;
        }

        public static Texture2D LoadCachedModLogo(int modId, ImageVersion logoVersion)
        {
            Texture2D logoTexture = null;

            // NOTE(@jackson): Potentially return an off-res version?
            if(cachedImageVersion == logoVersion)
            {
               modLogoCache.TryGetValue(modId, out logoTexture);
            }
            #if !TEST_IGNORE_DISK_CACHE
            if(logoTexture == null)
            {
                string localURL = GetModDirectory(modId) + GetLogoFileName(logoVersion);
                if(File.Exists(localURL))
                {
                    logoTexture = new Texture2D(0, 0);
                    logoTexture.LoadImage(File.ReadAllBytes(localURL));
                }
            }
            #endif

                return logoTexture;
        }

        public static void DownloadModLogo(int modId, ImageVersion logoVersion)
        {
            ModImageURLCollection logoURLCollection = null;
            if(modLogoURLMap.TryGetValue(modId, out logoURLCollection))
            {
                // - Start Download -
                string logoURL = GenerateGetModLogoURLFunction(logoVersion)(logoURLCollection);

                TextureDownload download = new TextureDownload();
                download.sourceURL = logoURL;
                download.OnCompleted += (d) => CacheModLogo(modId, logoVersion, download.texture);

                DownloadManager.AddConcurrentDownload(download);
            }
            else
            {
                Action<ModInfo> cacheModAndDownloadLogo = (mod) =>
                {
                    CacheMod(mod);

                    if(OnModAdded != null)
                    {
                        OnModAdded(mod);
                    }

                    DownloadModLogo(mod.id, logoVersion);
                };

                // TODO(@jackson): Fix up the onError
                Client.GetMod(modId,
                              result => OnSuccessWrapper(result, cacheModAndDownloadLogo),
                              null);
            }
        }

        private static void CacheModLogo(int modId,
                                         ImageVersion logoVersion,
                                         Texture2D logoTexture)
        {
            // - Cache -
            if(cachedImageVersion == logoVersion)
            {
                modLogoCache[modId] = logoTexture;
            }

            // - Save to disk -
            string localURL = GetModDirectory(modId) + GetLogoFileName(logoVersion);
            byte[] bytes = logoTexture.EncodeToPNG();
            File.WriteAllBytes(localURL, bytes);

            // - Notify -
            if(OnModLogoUpdated != null)
            {
                OnModLogoUpdated(modId, logoTexture, logoVersion);
            }
        }

        public static void DownloadModLogos(ModInfo[] modLogosToCache,
                                            ImageVersion logoVersion)
        {
            List<ModInfo> missingLogoList = new List<ModInfo>(modLogosToCache.Length);
            string logoFilename = GetLogoFileName(logoVersion);
            
            // Reset Cache if logoVersion is incorrect
            if(logoVersion != cachedImageVersion)
            {
                modLogoCache = new Dictionary<int, Texture2D>(modLogosToCache.Length);
            }

            // Check which logos are missing
            foreach(ModInfo mod in modLogosToCache)
            {
                if(modLogoCache.ContainsKey(mod.id))
                {
                    continue;
                }

                #if !TEST_IGNORE_DISK_CACHE
                string logoFilepath = GetModDirectory(mod.id) + logoFilename;
                if(File.Exists(logoFilepath))
                {
                    Texture2D logoTexture = new Texture2D(0,0);
                    logoTexture.LoadImage(File.ReadAllBytes(logoFilepath));

                    modLogoCache[mod.id] = logoTexture;

                    if(OnModLogoUpdated != null)
                    {
                        OnModLogoUpdated(mod.id, logoTexture, logoVersion);
                    }
                }
                else
                #endif
                {
                    missingLogoList.Add(mod);
                }
            }

            // Download
            Func<ModImageURLCollection, string> getModLogoURL = GenerateGetModLogoURLFunction(logoVersion);
            foreach(ModInfo mod in missingLogoList)
            {
                string logoURL = getModLogoURL(null);

                TextureDownload download = new TextureDownload();
                download.sourceURL = logoURL;
                download.OnCompleted += (d) => CacheModLogo(mod.id, logoVersion, download.texture);

                DownloadManager.AddConcurrentDownload(download);
            }
        }

        // ---------[ MISC ]------------
        public static void RequestTagCategoryMap(Action<GameTagOption[]> onSuccess,
                                                 Action<ErrorInfo> onError)
        {
            Client.GetAllGameTagOptions(result => OnSuccessWrapper(result, onSuccess),
                                           onError);
        }

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

        public static void SubmitNewMod(EditableModFields modData,
                                        Action<ModInfo> modSubmissionSucceeded,
                                        Action<ErrorInfo> modSubmissionFailed)
        {
            Debug.Assert(modData.name.isDirty && modData.summary.isDirty);
            Debug.Assert(File.Exists(modData.logoFilePath.value));

            var parameters = new AddModParameters();
            parameters.name = modData.name.value;
            parameters.summary = modData.summary.value;
            parameters.logo = BinaryUpload.Create(Path.GetFileName(modData.logoFilePath.value),
                                                  File.ReadAllBytes(modData.logoFilePath.value));
            if(modData.visibility.isDirty)
            {
                parameters.visible = (int)modData.visibility.value;
            }
            if(modData.nameId.isDirty)
            {
                parameters.name_id = modData.nameId.value;
            }
            if(modData.description.isDirty)
            {
                parameters.description = modData.description.value;
            }
            if(modData.homepage.isDirty)
            {
                parameters.name_id = modData.homepage.value;
            }
            if(modData.metadataBlob.isDirty)
            {
                parameters.metadata_blob = modData.metadataBlob.value;
            }
            if(modData.nameId.isDirty)
            {
                parameters.name_id = modData.nameId.value;
            }
            if(modData.tags.isDirty)
            {
                parameters.tags = modData.tags.value;
            }

            Client.AddMod(userData.oAuthToken,
                          parameters,
                          result => OnSuccessWrapper(result, modSubmissionSucceeded),
                          modSubmissionFailed);
        }

        public static void SubmitModChanges(int modId,
                                            EditableModFields modData,
                                            Action<ModInfo> modSubmissionSucceeded,
                                            Action<ErrorInfo> modSubmissionFailed)
        {
            Debug.Assert(modId > 0);

            // TODO(@jackson): Defend this code
            ModInfo mod = GetMod(modId);

            List<Action> submissionActions = new List<Action>();
            int nextActionIndex = 0;
            Action<MessageObject> doNextSubmissionAction = (m) =>
            {
                if(nextActionIndex < submissionActions.Count)
                {
                    submissionActions[nextActionIndex++]();
                }
            };

            if(modData.tags.isDirty)
            {
                var addedTags = new List<string>(modData.tags.value);
                foreach(string tag in mod.GetTagNames())
                {
                    addedTags.Remove(tag);
                }

                var removedTags = new List<string>(mod.GetTagNames());
                foreach(string tag in modData.tags.value)
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

            if(modData.status.isDirty
               || modData.visibility.isDirty
               || modData.name.isDirty
               || modData.nameId.isDirty
               || modData.summary.isDirty
               || modData.description.isDirty
               || modData.homepage.isDirty
               || modData.metadataBlob.isDirty)
            {
                submissionActions.Add(() =>
                {
                    Debug.Log("Submitting Mod Info");

                    var parameters = new EditModParameters();
                    if(modData.status.isDirty)
                    {
                        parameters.status = (int)modData.status.value;
                    }
                    if(modData.visibility.isDirty)
                    {
                        parameters.visible = (int)modData.visibility.value;
                    }
                    if(modData.name.isDirty)
                    {
                        parameters.name = modData.name.value;
                    }
                    if(modData.nameId.isDirty)
                    {
                        parameters.name_id = modData.nameId.value;
                    }
                    if(modData.summary.isDirty)
                    {
                        parameters.summary = modData.summary.value;
                    }
                    if(modData.description.isDirty)
                    {
                        parameters.description = modData.description.value;
                    }
                    if(modData.homepage.isDirty)
                    {
                        parameters.homepage = modData.homepage.value;
                    }
                    if(modData.metadataBlob.isDirty)
                    {
                        parameters.metadata_blob = modData.metadataBlob.value;
                    }

                    Client.EditMod(userData.oAuthToken,
                                   modId, parameters,
                                   result => OnSuccessWrapper(result, modSubmissionSucceeded),
                                   modSubmissionFailed);
                });
            }
            // - Get updated ModInfo -
            else
            {
                submissionActions.Add(() =>
                {
                    Client.GetMod(modId,
                                  result => OnSuccessWrapper(result, modSubmissionSucceeded),
                                  modSubmissionFailed);
                });
            }

            // - Start submission chain -
            doNextSubmissionAction(new MessageObject());
        }

        public static void AddModMedia(ModMediaChanges modMedia,
                                       Action<APIMessage> onSuccess,
                                       Action<ErrorInfo> onError)
        {
            Debug.Assert(modMedia.modId > 0,
                         "Invalid mod id supplied with the mod media changes");

            // - Image Gallery -
            BinaryUpload imageGalleryUpload = null;
            if(modMedia.images.Length > 0)
            {
                string galleryZipLocation = Application.temporaryCachePath + "/modio/imageUpload_" + DateTime.Now.ToFileTime() + ".zip";
                ZipUtil.Zip(galleryZipLocation, modMedia.images);

                imageGalleryUpload = new BinaryUpload()
                {
                    fileName = "images.zip",
                    data = File.ReadAllBytes(galleryZipLocation)
                };
            }

            var parameters = new AddModMediaParameters();
            parameters.youtube = modMedia.youtube;
            parameters.sketchfab = modMedia.sketchfab;
            parameters.images = imageGalleryUpload;

            Client.AddModMedia(userData.oAuthToken, modMedia.modId,
                               parameters,
                               result => OnSuccessWrapper(result, onSuccess),
                               onError);
        }

        public static void DeleteModMedia(ModMediaChanges modMedia,
                                          Action<APIMessage> onSuccess,
                                          Action<ErrorInfo> onError)
        {
            Debug.Assert(modMedia.modId > 0,
                         "Invalid mod id supplied with the mod media changes");

            // Create Parameters
            var parameters = new DeleteModMediaParameters();
            parameters.youtube = modMedia.youtube;
            parameters.sketchfab = modMedia.sketchfab;
            parameters.images = modMedia.images;

            Client.DeleteModMedia(userData.oAuthToken, modMedia.modId,
                                  parameters,
                                  result => OnSuccessWrapper(result, onSuccess),
                                  onError);
        }

        public static void UploadModBinary_Unzipped(int modId,
                                                    string unzippedBinaryLocation,
                                                    ModfileEditableFields modfileValues,
                                                    bool setPrimary,
                                                    Action<Modfile> onSuccess,
                                                    Action<ErrorInfo> onError)
        {
            string binaryZipLocation = Application.temporaryCachePath + "/modio/" + System.IO.Path.GetFileNameWithoutExtension(unzippedBinaryLocation) + DateTime.Now.ToFileTime() + ".zip";

            ZipUtil.Zip(binaryZipLocation, unzippedBinaryLocation);

            UploadModBinary_Zipped(modId, binaryZipLocation, modfileValues, setPrimary, onSuccess, onError);
        }

        public static void UploadModBinary_Zipped(int modId,
                                                  string binaryZipLocation,
                                                  ModfileEditableFields modfileValues,
                                                  bool setPrimary,
                                                  Action<Modfile> onSuccess,
                                                  Action<ErrorInfo> onError)
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
                              (m) => onSuccess(Modfile.CreateFromAPIObject(m)),
                              onError);
        }

        // --- TEMPORARY PASS-THROUGH FUNCTIONS ---
        public static void AddGameMedia(UnsubmittedGameMedia gameMedia,
                                        Action<APIMessage> onSuccess,
                                        Action<ErrorInfo> onError)
        {
            Client.AddGameMedia(userData.oAuthToken,
                                gameMedia.AsAddGameMediaParameters(),
                                result => OnSuccessWrapper(result, onSuccess),
                                onError);
        }

        public static void AddGameTagOption(UnsubmittedGameTagOption tagOption,
                                            Action<APIMessage> onSuccess,
                                            Action<ErrorInfo> onError)
        {
            Client.AddGameTagOption(userData.oAuthToken,
                                    tagOption.AsAddGameTagOptionParameters(),
                                    result => OnSuccessWrapper(result, onSuccess),
                                    onError);
        }

        public static void AddPositiveRating(int modId,
                                             Action<APIMessage> onSuccess,
                                             Action<ErrorInfo> onError)
        {
            Client.AddModRating(userData.oAuthToken,
                                modId, new AddModRatingParameters(1),
                                result => OnSuccessWrapper(result, onSuccess),
                                onError);
        }

        public static void AddModKVPMetadata(int modId, UnsubmittedMetadataKVP[] metadataKVPs,
                                             Action<APIMessage> onSuccess,
                                             Action<ErrorInfo> onError)
        {
            string[] metadataKVPStrings = new string[metadataKVPs.Length];
            for(int i = 0; i < metadataKVPs.Length; ++i)
            {
                metadataKVPStrings[i] = metadataKVPs[i].key + ":" + metadataKVPs[i].value;
            }

            Client.AddModKVPMetadata(userData.oAuthToken,
                                     modId, new AddModKVPMetadataParameters(metadataKVPStrings),
                                     result => OnSuccessWrapper(result, onSuccess),
                                     onError);
        }

        public static void AddModTeamMember(int modId, UnsubmittedTeamMember teamMember,
                                            Action<APIMessage> onSuccess,
                                            Action<ErrorInfo> onError)
        {
            Client.AddModTeamMember(userData.oAuthToken,
                                    modId, teamMember.AsAddModTeamMemberParameters(),
                                    result => OnSuccessWrapper(result, onSuccess),
                                    onError);
        }

        public static void DeleteMod(int modId,
                                     Action<APIMessage> onSuccess,
                                     Action<ErrorInfo> onError)
        {
            Client.DeleteMod(userData.oAuthToken,
                                modId,
                                result => OnSuccessWrapper(result, onSuccess),
                                onError);
        }

        public static void DeleteModTags(int modId, string[] tagsToDelete,
                                         Action<APIMessage> onSuccess,
                                         Action<ErrorInfo> onError)
        {
            Client.DeleteModTags(userData.oAuthToken,
                                 modId, new DeleteModTagsParameters(tagsToDelete),
                                 result => OnSuccessWrapper(result, onSuccess),
                                 onError);
        }

        public static void DeleteModDependencies(int modId, int[] modIdsToRemove,
                                                 Action<APIMessage> onSuccess,
                                                 Action<ErrorInfo> onError)
        {
            Client.DeleteModDependencies(userData.oAuthToken,
                                         modId, new DeleteModDependenciesParameters(modIdsToRemove),
                                         result => OnSuccessWrapper(result, onSuccess),
                                         onError);
        }

        public static void AddModDependencies(int modId, int[] modIdsToAdd,
                                              Action<APIMessage> onSuccess,
                                              Action<ErrorInfo> onError)
        {
            Client.AddModDependencies(userData.oAuthToken,
                                      modId, new AddModDependenciesParameters(modIdsToAdd),
                                      result => OnSuccessWrapper(result, onSuccess),
                                      onError);
        }

        public static void DeleteModKVPMetadata(int modId, UnsubmittedMetadataKVP[] metadataKVPs,
                                                Action<APIMessage> onSuccess,
                                                Action<ErrorInfo> onError)
        {
            string[] kvpStrings = new string[metadataKVPs.Length];
            for(int i = 0; i < metadataKVPs.Length; ++i)
            {
                kvpStrings[i] = metadataKVPs[i].ToString();
            }

            var parameters = new DeleteModKVPMetadataParameters();
            parameters.metadata = kvpStrings;

            Client.DeleteModKVPMetadata(userData.oAuthToken,
                                        modId, parameters,
                                        result => OnSuccessWrapper(result, onSuccess),
                                        onError);
        }

        public static void DeleteModTeamMember(int modId, int teamMemberId,
                                               Action<APIMessage> onSuccess,
                                               Action<ErrorInfo> onError)
        {
            Client.DeleteModTeamMember(userData.oAuthToken,
                                          modId, teamMemberId,
                                          result => OnSuccessWrapper(result, onSuccess),
                                          onError);
        }

        public static void DeleteModComment(int modId, int commentId,
                                            Action<APIMessage> onSuccess,
                                            Action<ErrorInfo> onError)
        {
            Client.DeleteModComment(userData.oAuthToken,
                                       modId, commentId,
                                       result => OnSuccessWrapper(result, onSuccess),
                                       onError);
        }

        public static void GetAllModTeamMembers(int modId,
                                                Action<TeamMember[]> onSuccess,
                                                Action<ErrorInfo> onError)
        {
            Client.GetAllModTeamMembers(modId, GetAllModTeamMembersFilter.None,
                                           result => OnSuccessWrapper(result, onSuccess),
                                           onError);
        }

        public static void DeleteGameTagOption(GameTagOptionToDelete gameTagOption,
                                               Action<APIMessage> onSuccess,
                                               Action<ErrorInfo> onError)
        {
            Client.DeleteGameTagOption(userData.oAuthToken,
                                       gameTagOption.AsDeleteGameTagOptionParameters(),
                                       result => OnSuccessWrapper(result, onSuccess),
                                       onError);
        }
    }
}