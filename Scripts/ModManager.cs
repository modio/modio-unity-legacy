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

    public static class ModManager
    {
        // ---------[ INNER CLASSES ]---------
        [System.Serializable]
        private class UserData
        {
            public User user = null;
            public List<int> subscribedModIDs = null;
        }

        [System.Serializable]
        private class ManifestData
        {
            public int[] installedMods;
            public string lastOAuthToken;
        }

        // ---------[ VARIABLES ]---------
        private static APIClient client = null;
        private static ManifestData manifest = null;
        private static UserData userData = null;

        public static string MODIO_DIR { get { return Application.persistentDataPath + "/.modio/"; } }
        public static APIClient APIClient { get { return client; } }
        public static User CurrentUser { get { return userData == null ? null : userData.user; } }

        // --------- [ INITIALISATION ]---------
        public static void Initialize(int gameID, string apiKey)
        {
            if(client != null)
            {
                Debug.Assert(client.gameID == gameID && client.apiKey == apiKey,
                             "ModIO Initialization Error: Cannot re-intialise with different data.");
                return;
            }

            // --- Create Client ---
            GameObject go = new GameObject("ModIO API Client");
            client = go.AddComponent<APIClient>();
            client.gameID = gameID;
            client.apiKey = apiKey;

            GameObject.DontDestroyOnLoad(go);

            // --- Load To Cache From Disk ---
            if (!Directory.Exists(MODIO_DIR))
            {
                Directory.CreateDirectory(MODIO_DIR);
            }

            string manifestURL = MODIO_DIR + ".manifest";
            if(!File.Exists(manifestURL))
            {
                // --- INITIALIZE FIRST RUN ---
                manifest = new ManifestData();
                manifest.installedMods = new int[0];
                manifest.lastOAuthToken = "";

                File.WriteAllText(manifestURL, JsonUtility.ToJson(manifest));
            }
            else
            {
                manifest = JsonUtility.FromJson<ManifestData>(File.ReadAllText(manifestURL));
                client.oAuthToken = manifest.lastOAuthToken;
            }

            // iterate through folders, load ModInfo
            string[] modDirectories = Directory.GetDirectories(MODIO_DIR);
            foreach(string modDir in modDirectories)
            {
                // Load Mod from Disk
                Mod mod = JsonUtility.FromJson<Mod>(File.ReadAllText(modDir + "/mod.data"));
                modCache.Add(mod);
            }

            // TODO(@jackson): Load partial downloads
        }

        // ---------[ USER MANAGEMENT ]---------
        public static void RequestAndStoreOAuthToken(string securityCode,
                                                     Action onSuccess,
                                                     ErrorCallback onError)
        {
            client.RequestOAuthToken(securityCode,
                                     (oAuthToken) =>
                                     {
                                        client.oAuthToken = oAuthToken.accessToken;
                                        manifest.lastOAuthToken = oAuthToken.accessToken;
                                        onSuccess();
                                        WriteManifestToDisk();
                                     },
                                     onError);
        }

        public static void AttemptUserInitialization(ObjectCallback<User> onSuccess,
                                                     ErrorCallback onError)
        {
            client.GetAuthenticatedUser(user =>
                                        {
                                            InitializeUser(user, onSuccess, onError);
                                        },
                                        onError);
        }

        private static void InitializeUser(User user,
                                           ObjectCallback<User> onSuccess,
                                           ErrorCallback onError)
        {
            client.GetUserSubscriptions(GetUserSubscriptionsFilter.NONE,
                                        modArray =>
                                        {
                                            userData = new UserData();
                                            userData.user = user;

                                            int[] subscribedModIDs = new int[modArray.Length];
                                            for(int i = 0;
                                                i < modArray.Length;
                                                ++i)
                                            {
                                                subscribedModIDs[i] = modArray[i].ID;
                                            }
                                            userData.subscribedModIDs = new List<int>(subscribedModIDs);
                                            onSuccess(user);
                                        },
                                        onError);
        }

        public static bool IsUserInitialized()
        {
            return userData != null;
        }

        public static void SubscribeToMod(int modID,
                                          ObjectCallback<Mod> onSuccess,
                                          ErrorCallback onError)
        {
            client.SubscribeToMod(modID,
                                  (message) =>
                                  {
                                    userData.subscribedModIDs.Add(modID);
                                    onSuccess(GetModInfoForID(modID));
                                  },
                                  onError);
        }

        public static void UnsubscribeFromMod(int modID,
                                              ObjectCallback<Mod> onSuccess,
                                              ErrorCallback onError)
        {
            client.UnsubscribeFromMod(modID,
                                      (message) =>
                                      {
                                        userData.subscribedModIDs.Remove(modID);
                                        onSuccess(GetModInfoForID(modID));
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
        public static event ModLogoUpdatedEventHandler OnMogLogoUpdated;

        private static List<Mod> modCache = new List<Mod>();

        public static string GetModDirectory(Mod mod)
        {
            return MODIO_DIR + mod.ID + "/";
        }

        public static Mod GetModInfoForID(int modID)
        {
            foreach(Mod mod in modCache)
            {
                if(mod.ID == modID)
                {
                    return mod;
                }
            }

            return null;
        }

        // NOTE(@jackson): Currently dumb. Needs improvement.
        public static void DownloadModInfoToDiskAndCache()
        {
            client.GetAllMods(GetAllModsFilter.NONE, SaveModInfoToDiskAndCache, APIClient.LogError);
        }
        private static void SaveModInfoToDiskAndCache(Mod[] modInfo)
        {
            modCache = new List<Mod>(modInfo);

            foreach(Mod mod in modInfo)
            {
                string modDir = GetModDirectory(mod);
                Directory.CreateDirectory(modDir);
                File.WriteAllText(modDir + "mod.data", JsonUtility.ToJson(mod));

                if(OnModUpdated != null)
                {
                    OnModUpdated(mod.ID);
                }
            }
        }

        public static void RequestMods(GetAllModsFilter filter, ObjectArrayCallback<Mod> callback)
        {
            Mod[] retVal = filter.GetFilteredArray(modCache.ToArray());
            callback(retVal);

            // client.BrowseMods(filter, callback);
        }

        // TODO(@jackson): Add callbacks
        public static FileDownload StartModDownload(Mod mod)
        {
            // TODO(@jackson): Reacquire ModHeader

            FileDownload download = new FileDownload();

            ObjectCallback<Modfile> onModfileReceived = (modfile) =>
            {
                download.sourceURL = modfile.downloadURL;
                download.fileURL = GetModDirectory(mod) + modfile.ID + "_" + modfile.dateAdded + ".zip";
                download.Start();
            };

            // TODO(@jackson): Convert to "Update Modfile" function
            client.GetModfile(mod.ID, mod.modfile.ID,
                              onModfileReceived, APIClient.LogError);

            return download;
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
                // TOOD(@jackson): How to handle dimensions?...
                versionArray[(int)LogoVersion.Original].getRemoteLogoURL = (Mod m) => { return m.logo.original; };

                versionArray[(int)LogoVersion.Thumb_320x180] = new LogoTemplate();
                versionArray[(int)LogoVersion.Thumb_320x180].version = LogoVersion.Thumb_320x180;
                versionArray[(int)LogoVersion.Thumb_320x180].localFilename = "logo_320x180.png";
                versionArray[(int)LogoVersion.Thumb_320x180].width = 320;
                versionArray[(int)LogoVersion.Thumb_320x180].height = 180;
                versionArray[(int)LogoVersion.Thumb_320x180].getRemoteLogoURL = (Mod m) => { return m.logo.thumb_320x180; };

                versionArray[(int)LogoVersion.Thumb_640x360] = new LogoTemplate();
                versionArray[(int)LogoVersion.Thumb_640x360].version = LogoVersion.Thumb_640x360;
                versionArray[(int)LogoVersion.Thumb_640x360].localFilename = "logo_640x360.png";
                versionArray[(int)LogoVersion.Thumb_640x360].width = 640;
                versionArray[(int)LogoVersion.Thumb_640x360].height = 360;
                versionArray[(int)LogoVersion.Thumb_640x360].getRemoteLogoURL = (Mod m) => { return m.logo.thumb_640x360; };

                versionArray[(int)LogoVersion.Thumb_1280x720] = new LogoTemplate();
                versionArray[(int)LogoVersion.Thumb_1280x720].version = LogoVersion.Thumb_1280x720;
                versionArray[(int)LogoVersion.Thumb_1280x720].localFilename = "logo_1280x720.png";
                versionArray[(int)LogoVersion.Thumb_1280x720].width = 1280;
                versionArray[(int)LogoVersion.Thumb_1280x720].height = 720;
                versionArray[(int)LogoVersion.Thumb_1280x720].getRemoteLogoURL = (Mod m) => { return m.logo.thumb_1280x720; };
            }

            private static LogoTemplate[] versionArray;
            public static LogoTemplate ForLogoVersion(LogoVersion version)
            {
                return versionArray[(int)version];
            }

            // - Fields -
            public LogoVersion version = LogoVersion.Original;
            public string localFilename = "";
            public int width = -1;
            public int height = -1;
            public Func<Mod, string> getRemoteLogoURL = null;
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

                string localURL = GetModDirectory(mod) + logoTemplate.localFilename;
                if(File.Exists(localURL))
                {
                    Texture2D logoTexture = new Texture2D(logoTemplate.width, logoTemplate.height);
                    logoTexture.LoadImage(File.ReadAllBytes(localURL));

                    return Sprite.Create(logoTexture,
                                         new Rect(0, 0, logoTemplate.width, logoTemplate.height),
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
            TextureDownload download = new TextureDownload();
            download.sourceURL = logoTemplate.getRemoteLogoURL(mod);
                                 
            download.OnFailed += APIClient.LogError;

            download.OnCompleted += () =>
            {
                Texture2D logoTexture = download.texture;

                // - Cache -
                if(cachedLogoVersion == logoTemplate.version)
                {
                    modLogoCache[mod.ID]
                        = Sprite.Create(logoTexture,
                                        new Rect(0, 0, logoTexture.width, logoTexture.height),
                                        Vector2.zero);
                }

                // - Save to disk -
                string localURL = GetModDirectory(mod) + logoTemplate.localFilename;
                byte[] bytes = logoTexture.EncodeToPNG();
                File.WriteAllBytes(localURL, bytes);

                // - Notify -
                if(OnMogLogoUpdated != null)
                {
                    OnMogLogoUpdated(mod.ID, modLogoCache[mod.ID], logoTemplate.version);
                }
            };

            download.Start();
        }

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
                    string localURL = GetModDirectory(mod) + logoTemplate.localFilename;
                    if(File.Exists(localURL))
                    {
                        Debug.Log("Found Logo: " + localURL);

                        Texture2D logoTexture = new Texture2D(logoTemplate.width, logoTemplate.height);
                        logoTexture.LoadImage(File.ReadAllBytes(localURL));

                        modLogoCache[mod.ID]
                            = Sprite.Create(logoTexture,
                                            new Rect(0, 0, logoTemplate.width, logoTemplate.height),
                                            Vector2.zero);

                        if(OnMogLogoUpdated != null)
                        {
                            OnMogLogoUpdated(mod.ID, modLogoCache[mod.ID], logoTemplate.version);
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

            // TODO(@jackson): Reimplement this with download management
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
            File.WriteAllText(MODIO_DIR + ".manifest", JsonUtility.ToJson(manifest));
        }
    }
}