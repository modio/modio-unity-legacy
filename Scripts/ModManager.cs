using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ModIO
{
    public delegate void ModUpdatedEventHandler(int modID);
    public delegate void ModLogoUpdatedEventHandler(int modID,
                                                    Sprite modLogo,
                                                    LogoVersion logoVersion);
    public delegate void ModSubscriptionUpdateEventHandler(int modID);
    public delegate void ModfileChangedEventHandler(int modID, Modfile newModfile);

    public delegate void UserLoggedOutEventHandler();

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

        public static string MODIO_DIR { get { return Application.persistentDataPath + "/modio/"; } }
        public static APIClient APIClient { get { return client; } }
        public static User CurrentUser { get { return userData == null ? null : userData.user; } }

        private static string MANIFEST_URL { get { return MODIO_DIR + "manifest.data"; } }
        private static string USERDATA_URL { get { return MODIO_DIR + "user.data"; } }

        // --------- [ INITIALISATION ]---------
        public static void Initialize(int gameID, string apiKey)
        {
            if(client != null)
            {
                Debug.LogWarning("Cannot re-initialize ModManager");
                return;
            }

            // --- Create Client ---
            GameObject go = new GameObject("ModIO API Client");
            client = go.AddComponent<APIClient>();
            client.SetAccessContext(gameID, apiKey);

            GameObject.DontDestroyOnLoad(go);

            // --- Load To Cache From Disk ---
            if (!Directory.Exists(MODIO_DIR))
            {
                Directory.CreateDirectory(MODIO_DIR);
            }

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

            // iterate through folders, load Mod
            string[] modDirectories = Directory.GetDirectories(MODIO_DIR);
            foreach(string modDir in modDirectories)
            {
                // Load Mod from Disk
                Mod mod = JsonUtility.FromJson<Mod>(File.ReadAllText(modDir + "/mod.data"));
                modCache.Add(mod.ID, mod);
            }

            // Attempt to load user
            if(File.Exists(USERDATA_URL))
            {
                userData = JsonUtility.FromJson<UserData>(File.ReadAllText(USERDATA_URL));

                client.GetAuthenticatedUser(userData.oAuthToken,
                                            APIClient.IgnoreSuccess,
                                            (error) =>
                                            {
                                                if(error.code == 401) // Failed authentication
                                                {
                                                    LogUserOut();
                                                };
                                            });
            }

            // --- Start Update Polling Loop ---
            Debug.Log("ModManager Initialized."
                      + "\nModIO Directory: " + MODIO_DIR);
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

                // - Get Mod Events -
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
                    subscriptionFilter.ApplyIntEquality(GetUserSubscriptionsFilter.Field.GameID, client.gameID);

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
            // - Mod Processing Options -
            Action<ModEvent> processModLive = (modEvent) =>
            {
                client.GetMod(modEvent.modID,
                              (mod) =>
                              {
                                CacheMod(mod);
                                manifest.unresolvedEvents.Remove(modEvent);
                              },
                              APIClient.LogError);
            };

            Action<ModEvent> processModVisibilityChange = (modEvent) =>
            {
                string modVisibilityAfterChange = modEvent.changes[0].after;
                if(modVisibilityAfterChange.ToUpper().Equals("TRUE"))
                {
                    client.GetMod(modEvent.modID,
                                  (mod) =>
                                  {
                                    CacheMod(mod);
                                    manifest.unresolvedEvents.Remove(modEvent);
                                  },
                                  APIClient.LogError);
                }
                else
                {
                    Mod mod = GetMod(modEvent.modID);
                    if(mod != null)
                    {
                        UncacheMod(mod);
                    }
                    manifest.unresolvedEvents.Remove(modEvent);
                }
            };

            Action<ModEvent> processModfileChange = (modEvent) =>
            {
                Mod mod = GetMod(modEvent.modID);

                if(mod == null)
                {
                    Debug.Log("Received Modfile change for uncached mod. Ignoring.");
                    manifest.unresolvedEvents.Remove(modEvent);
                }
                else
                {
                    int modfileID = 0;

                    if(!Int32.TryParse(modEvent.changes[0].after, out modfileID))
                    {
                        Debug.Log("Unable to parse Modfile ID from Mod Event. Updating Mod directly");
                        manifest.unresolvedEvents.Remove(modEvent);

                        client.GetMod(mod.ID,
                                      (updatedMod) =>
                                      {
                                        mod = updatedMod;

                                        string modDir = GetModDirectory(mod.ID);
                                        Directory.CreateDirectory(modDir);
                                        File.WriteAllText(modDir + "mod.data", JsonUtility.ToJson(mod));
                                        modCache[mod.ID] = mod;

                                        if(OnModfileChanged != null)
                                        {
                                            OnModfileChanged(mod.ID, mod.modfile);
                                        }
                                      },
                                      APIClient.LogError);

                        return;
                    }

                    client.GetModfile(mod.ID, modfileID,
                                      (modfile) =>
                                      {
                                        mod.modfile = modfile;
                                        
                                        if(OnModfileChanged != null)
                                        {
                                            OnModfileChanged(mod.ID, modfile);
                                        }
                                      },
                                      APIClient.LogError);
                }
            };

            // - Handle Mod Event -
            foreach(ModEvent modEvent in eventArray)
            {
                string eventSummary = "TimeStamp (Local)=" + modEvent.GetDateAdded().AsLocalDateTime();
                eventSummary += "\nMod=" + modEvent.modID;
                eventSummary += "\nEventType=" + modEvent.GetEventType().ToString();
                
                Debug.Log("[PROCESSING MOD EVENT]\n" + eventSummary);


                manifest.unresolvedEvents.Add(modEvent);

                switch(modEvent.GetEventType())
                {
                    case ModEvent.EventType.ModVisibilityChange:
                    {
                        processModVisibilityChange(modEvent);
                    }
                    break;
                    case ModEvent.EventType.ModLive:
                    {
                        processModLive(modEvent);
                    }
                    break;
                    case ModEvent.EventType.ModfileChange:
                    {
                        processModfileChange(modEvent);
                    }
                    break;
                    default:
                    {
                        Debug.LogError("Unhandled Event Type: " + modEvent.GetEventType().ToString());
                    }
                    break;
                }
            }
        }

        private static void UpdateSubscriptions(Mod[] subscribedMods)
        {
            if(userData == null) { return; }

            List<int> addedMods = new List<int>();
            List<int> removedMods = new List<int>(userData.subscribedModIDs);
            userData.subscribedModIDs = new List<int>(subscribedMods.Length);

            foreach(Mod mod in subscribedMods)
            {
                userData.subscribedModIDs.Add(mod.ID);

                if(removedMods.Contains(mod.ID))
                {
                    removedMods.Remove(mod.ID);
                }
                else
                {
                    addedMods.Add(mod.ID);
                }
            }

            WriteUserDataToDisk();

            // - Notify -
            if(OnModSubscriptionAdded != null)
            {
                foreach(int modID in addedMods)
                {
                    OnModSubscriptionAdded(modID);
                }
            }
            if(OnModSubscriptionRemoved != null)
            {
                foreach(int modID in removedMods)
                {
                    OnModSubscriptionRemoved(modID);
                }
            }
        }

        // ---------[ USER MANAGEMENT ]---------
        public static event UserLoggedOutEventHandler OnUserLoggedOut;
        public static event ModSubscriptionUpdateEventHandler OnModSubscriptionAdded;
        public static event ModSubscriptionUpdateEventHandler OnModSubscriptionRemoved;

        public static void RequestOAuthToken(string securityCode,
                                             ObjectCallback<string> onSuccess,
                                             ErrorCallback onError)
        {
            client.RequestOAuthToken(securityCode,
                                     (authentication) => onSuccess(authentication.accessToken),
                                     onError);
        }

        public static void TryLogUserIn(string userOAuthToken, 
                                        ObjectCallback<User> onSuccess, 
                                        ErrorCallback onError)
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

        public static void SubscribeToMod(int modID,
                                          ObjectCallback<Mod> onSuccess,
                                          ErrorCallback onError)
        {
            client.SubscribeToMod(userData.oAuthToken,
                                  modID,
                                  (message) =>
                                  {
                                    userData.subscribedModIDs.Add(modID);
                                    onSuccess(GetMod(modID));
                                  },
                                  onError);
        }

        public static void UnsubscribeFromMod(int modID,
                                              ObjectCallback<Mod> onSuccess,
                                              ErrorCallback onError)
        {
            client.UnsubscribeFromMod(userData.oAuthToken,
                                      modID,
                                      (message) =>
                                      {
                                        userData.subscribedModIDs.Remove(modID);
                                        onSuccess(GetMod(modID));
                                      },
                                      onError);
        }

        public static bool IsSubscribedToMod(int modID)
        {
            foreach(int subscribedModID in userData.subscribedModIDs)
            {
                if(subscribedModID == modID) { return true; }
            }
            return false;
        }

        // ---------[ MOD MANAGEMENT ]---------
        public static event ModUpdatedEventHandler OnModUpdated;
        public static event ModLogoUpdatedEventHandler OnModLogoUpdated;
        public static event ModfileChangedEventHandler OnModfileChanged;

        private static Dictionary<int, Mod> modCache = new Dictionary<int, Mod>();

        public static string GetModDirectory(int modID)
        {
            return MODIO_DIR + modID + "/";
        }

        public static Mod GetMod(int modID)
        {
            Mod mod = null;
            modCache.TryGetValue(modID, out mod);
            return mod;
        }

        // NOTE(@jackson): Currently dumb. Needs improvement.
        public static void UpdateModCacheFromServer()
        {
            client.GetAllMods(GetAllModsFilter.None, CacheModArray, APIClient.LogError);
        }

        private static void CacheMod(Mod mod)
        {
            string modDir = GetModDirectory(mod.ID);
            Directory.CreateDirectory(modDir);
            File.WriteAllText(modDir + "mod.data", JsonUtility.ToJson(mod));

            modCache[mod.ID] = mod;

            if(OnModUpdated != null)
            {
                OnModUpdated(mod.ID);
            }
        }
        private static void CacheModArray(Mod[] modArray)
        {
            foreach(Mod mod in modArray)
            {
                CacheMod(mod);
            }
        }
        private static void UncacheMod(Mod mod)
        {
            // TODO(@jackson): Check for installs, subscriptions, etc.

            modCache.Remove(mod.ID);

            string modDir = GetModDirectory(mod.ID);
            Directory.Delete(modDir, true);

            // TODO(@jackson): Create Event
        }

        public static void RequestMods(GetAllModsFilter filter, ObjectArrayCallback<Mod> callback)
        {
            Mod[] retVal = filter.FilterCollection(modCache.Values);
            callback(retVal);

            // client.BrowseMods(filter, callback);
        }

        public static FileDownload StartBinaryDownload(int modID, int modfileID)
        {
            FileDownload download = new FileDownload();

            ObjectCallback<Modfile> queueBinaryDownload = (modfile) =>
            {
                download.sourceURL = modfile.downloadURL;
                download.fileURL = GetModDirectory(modID) + "modfile_" + modfile.ID + ".zip";

                DownloadManager.AddQueuedDownload(download);
            };

            client.GetModfile(modID, modfileID,
                              queueBinaryDownload,
                              download.MarkAsFailed);

            return download;
        }

        public static void DeleteDownloadedBinaries(Mod mod)
        {
            string[] binaryFilePaths = Directory.GetFiles(GetModDirectory(mod.ID), "modfile_*.zip");
            foreach(string binaryFilePath in binaryFilePaths)
            {
                File.Delete(binaryFilePath);   
            }
        }

        public static ModBinaryStatus GetBinaryStatus(Mod mod)
        {
            if(File.Exists(GetModDirectory(mod.ID) + "modfile_" + mod.modfile.ID + ".zip"))
            {
                return ModBinaryStatus.UpToDate;
            }
            else
            {
                string[] modfileURLs = Directory.GetFiles(GetModDirectory(mod.ID), "modfile_*.zip");
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

        // ---------[ LOGO MANAGEMENT ]---------
        // TODO(@jackson): Remove W/H. No longer necessary.
        private class LogoTemplate
        {
            static LogoTemplate()
            {
                versionArray = new LogoTemplate[Enum.GetValues(typeof(LogoVersion)).Length];

                versionArray[(int)LogoVersion.Original] = new LogoTemplate();
                versionArray[(int)LogoVersion.Original].version = LogoVersion.Original;
                versionArray[(int)LogoVersion.Original].localFilename = "logo_original.png";
                versionArray[(int)LogoVersion.Original].getServerURL = (Mod m) => { return m.logo.original; };

                versionArray[(int)LogoVersion.Thumb_320x180] = new LogoTemplate();
                versionArray[(int)LogoVersion.Thumb_320x180].version = LogoVersion.Thumb_320x180;
                versionArray[(int)LogoVersion.Thumb_320x180].localFilename = "logo_320x180.png";
                versionArray[(int)LogoVersion.Thumb_320x180].getServerURL = (Mod m) => { return m.logo.thumb_320x180; };

                versionArray[(int)LogoVersion.Thumb_640x360] = new LogoTemplate();
                versionArray[(int)LogoVersion.Thumb_640x360].version = LogoVersion.Thumb_640x360;
                versionArray[(int)LogoVersion.Thumb_640x360].localFilename = "logo_640x360.png";
                versionArray[(int)LogoVersion.Thumb_640x360].getServerURL = (Mod m) => { return m.logo.thumb_640x360; };

                versionArray[(int)LogoVersion.Thumb_1280x720] = new LogoTemplate();
                versionArray[(int)LogoVersion.Thumb_1280x720].version = LogoVersion.Thumb_1280x720;
                versionArray[(int)LogoVersion.Thumb_1280x720].localFilename = "logo_1280x720.png";
                versionArray[(int)LogoVersion.Thumb_1280x720].getServerURL = (Mod m) => { return m.logo.thumb_1280x720; };
            }

            private static LogoTemplate[] versionArray;
            public static LogoTemplate ForLogoVersion(LogoVersion version)
            {
                return versionArray[(int)version];
            }

            // - Fields -
            public LogoVersion version = LogoVersion.Original;
            public string localFilename = "";
            public Func<Mod, string> getServerURL = null;
        }

        public static LogoVersion cachedLogoVersion = LogoVersion.Thumb_1280x720;
        public static Dictionary<int, Sprite> modLogoCache = new Dictionary<int, Sprite>();
        public static Sprite modLogoDownloading;

        public static Sprite GetModLogo(Mod mod, LogoVersion logoVersion)
        {
            Sprite retVal;

            // NOTE(@jackson): Potentially return an off-res version?
            if(cachedLogoVersion == logoVersion
               && modLogoCache.TryGetValue(mod.ID, out retVal))
            {
                return retVal;
            }
            else
            {
                LogoTemplate logoTemplate = LogoTemplate.ForLogoVersion(logoVersion);

                string localURL = GetModDirectory(mod.ID) + logoTemplate.localFilename;
                if(File.Exists(localURL))
                {
                    Texture2D logoTexture = new Texture2D(0, 0);
                    logoTexture.LoadImage(File.ReadAllBytes(localURL));

                    return Sprite.Create(logoTexture,
                                         new Rect(0, 0, logoTexture.width, logoTexture.height),
                                         Vector2.zero);
                }
                else
                {
                    StartLogoDownload(mod, logoTemplate);
                    return modLogoDownloading;
                }
            }
        }

        private static void StartLogoDownload(Mod mod, LogoTemplate logoTemplate)
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
                    modLogoCache[mod.ID]
                        = Sprite.Create(logoTexture,
                                        new Rect(0, 0, logoTexture.width, logoTexture.height),
                                        Vector2.zero);
                }

                // - Save to disk -
                string localURL = GetModDirectory(mod.ID) + logoTemplate.localFilename;
                byte[] bytes = logoTexture.EncodeToPNG();
                File.WriteAllBytes(localURL, bytes);

                // - Notify -
                if(OnModLogoUpdated != null)
                {
                    OnModLogoUpdated(mod.ID, modLogoCache[mod.ID], logoTemplate.version);
                }
            };

            DownloadManager.AddConcurrentDownload(download);
        }

        // TODO(@jackson): Clean up
        public static void PreloadModLogos(Mod[] modLogosToPreload,
                                           LogoVersion logoVersion,
                                           int startingIndex)
        {
            if(logoVersion != cachedLogoVersion)
            {
                modLogoCache = new Dictionary<int, Sprite>(modLogosToPreload.Length);
            }

            Mod initialMod = modLogosToPreload[startingIndex];
            modLogosToPreload[startingIndex] = modLogosToPreload[0];
            modLogosToPreload[0] = initialMod;

            LogoTemplate logoTemplate = LogoTemplate.ForLogoVersion(logoVersion);
            List<Mod> modsMissingLogosList = new List<Mod>(modLogosToPreload.Length);
            foreach(Mod mod in modLogosToPreload)
            {
                if(!modLogoCache.ContainsKey(mod.ID))
                {
                    string localURL = GetModDirectory(mod.ID) + logoTemplate.localFilename;
                    if(File.Exists(localURL))
                    {
                        Texture2D logoTexture = new Texture2D(0,0);
                        logoTexture.LoadImage(File.ReadAllBytes(localURL));

                        modLogoCache[mod.ID]
                            = Sprite.Create(logoTexture,
                                            new Rect(0, 0, logoTexture.width, logoTexture.height),
                                            Vector2.zero);

                        if(OnModLogoUpdated != null)
                        {
                            OnModLogoUpdated(mod.ID, modLogoCache[mod.ID], logoTemplate.version);
                        }
                    }
                    else
                    {
                        modLogoCache.Add(mod.ID, modLogoDownloading);
                        modsMissingLogosList.Add(mod);
                    }
                }
            }

            if(modsMissingLogosList.Count == 0) { return; }

            foreach(Mod mod in modsMissingLogosList)
            {
                StartLogoDownload(mod, logoTemplate);
            }
        }

        // ---------[ MISC ]------------
        public static void RequestTagCategoryMap(ObjectCallback<Dictionary<string, string[]>> onSuccess,
                                                 ErrorCallback onError)
        {
            client.GetGame((Game game) =>
                           {
                            Dictionary<string, string[]> retVal
                                = new Dictionary<string, string[]>();

                            foreach(GameTagOption tagOption in game.tagOptions)
                            {
                                retVal.Add(tagOption.name, tagOption.tags);
                            }

                            onSuccess(retVal);
                           },
                           onError);
        }

        private static void WriteManifestToDisk()
        {
            File.WriteAllText(MANIFEST_URL, JsonUtility.ToJson(manifest));
        }

        private static void WriteUserDataToDisk()
        {
            File.WriteAllText(USERDATA_URL, JsonUtility.ToJson(userData));
        }

        private static void DeleteUserDataFromDisk()
        {
            File.Delete(USERDATA_URL);
        }
    }
}