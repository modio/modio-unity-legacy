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
        // ---------[ EVENTS ]---------
        public static event ModUpdatedEventHandler OnModUpdated;
        public static event ModLogoUpdatedEventHandler OnMogLogoUpdated;

        // ---------[ VARIABLES ] ---------
        public static APIClient APIClient { get { return client; } }
        private static APIClient client = null;

        public static string MODIO_DIR
        { get { return Application.persistentDataPath + "/.modio/"; } }

        public static int ModCacheLimit = 100;

        public static IEnumerator DownloadAndStoreFile(string url,
                                                       string targetDirectory_rel,
                                                       string targetFilename,
                                                       DownloadCallback onSuccess,
                                                       ErrorCallback onError)
        {
            byte[] downloadedData = null;
            yield return client.StartCoroutine(APIClient.RequestFileData(url,
                                                                         (byte[] bd) => { downloadedData = bd; },
                                                                         onError));

            string absDir = MODIO_DIR + targetDirectory_rel;
            Directory.CreateDirectory(absDir);
            File.WriteAllBytes(absDir + targetFilename, downloadedData);

            Debug.Log("[ Completed Download of File ]"
                      + "\nSourceURI: " + url
                      + "\nDestinationURI: " + absDir + targetFilename);

            onSuccess(downloadedData);
        }

        // --------- [ INITIALISATION ]---------
        public static void Initialise(int gameID, string apiKey)
        {
            if(client != null)
            {
                Debug.Assert(client.gameID == gameID && client.apiKey == apiKey,
                             "ModIO Initialisation Error: Cannot re-intialise with different data.");
                return;
            }

            GameObject go = new GameObject("ModIO API Client");
            client = go.AddComponent<APIClient>();
            client.gameID = gameID;
            client.apiKey = apiKey;

            GameObject.DontDestroyOnLoad(go);

            // Load To Cache From Disk
            // iterate through folders, load moddata
            if (!Directory.Exists(MODIO_DIR))
            {
                Directory.CreateDirectory(MODIO_DIR);

                // --- INITIALISE FIRST RUN ---
                // Create manifest file?
            }
            else
            {
                string[] modDirectories = Directory.GetDirectories(MODIO_DIR);
                foreach(string modDir in modDirectories)
                {
                    // Load Mod from
                    Mod mod = JsonUtility.FromJson<Mod>(File.ReadAllText(modDir + "/mod.data"));
                    modCache.Add(mod);
                }
            }
        }

        // ---------[ MOD MANAGEMENT ]---------
        private static List<Mod> modCache = new List<Mod>();

        // NOTE(@jackson): Currently dumb. Needs improvement.
        public static void DownloadModDataToDiskAndCache()
        {
            // TODO(@jackson): Handle multiple pages
            client.BrowseMods(ModQueryFilter.EMPTY, SaveModsToDiskAndCache);
        }
        private static void SaveModsToDiskAndCache(Mod[] modData)
        {
            modCache = new List<Mod>(modData);

            foreach(Mod mod in modData)
            {
                string modDir = MODIO_DIR + mod.id + "/";
                Directory.CreateDirectory(modDir);
                File.WriteAllText(MODIO_DIR + mod.id + "/mod.data", JsonUtility.ToJson(mod));

                if(OnModUpdated != null)
                {
                    OnModUpdated(mod.id);
                }
            }
        }

        public static void RequestMods(ModQueryFilter filter, ObjectArrayCallback<Mod> callback)
        {
            Mod[] retVal = filter.FilterModList(modCache.ToArray());
            callback(retVal);

            // client.BrowseMods(filter, callback);
        }

        // ---------[ LOGO MANAGEMENT ]---------
        private class LogoTemplate
        {
            static LogoTemplate()
            {
                versionArray = new LogoTemplate[Enum.GetValues(typeof(LogoVersion)).Length];

                versionArray[(int)LogoVersion.Full] = new LogoTemplate();
                versionArray[(int)LogoVersion.Full].version = LogoVersion.Full;
                versionArray[(int)LogoVersion.Full].localFilename = "logo_full.png";
                // How to handle dimensions?...
                versionArray[(int)LogoVersion.Full].getRemoteLogoURI = (Mod m) => { return m.logo.full; };

                versionArray[(int)LogoVersion.Thumb_320x180] = new LogoTemplate();
                versionArray[(int)LogoVersion.Thumb_320x180].version = LogoVersion.Thumb_320x180;
                versionArray[(int)LogoVersion.Thumb_320x180].localFilename = "logo_320x180.png";
                versionArray[(int)LogoVersion.Thumb_320x180].width = 320;
                versionArray[(int)LogoVersion.Thumb_320x180].height = 180;
                versionArray[(int)LogoVersion.Thumb_320x180].getRemoteLogoURI = (Mod m) => { return m.logo.thumb_320x180; };

                versionArray[(int)LogoVersion.Thumb_640x360] = new LogoTemplate();
                versionArray[(int)LogoVersion.Thumb_640x360].version = LogoVersion.Thumb_640x360;
                versionArray[(int)LogoVersion.Thumb_640x360].localFilename = "logo_640x360.png";
                versionArray[(int)LogoVersion.Thumb_640x360].width = 640;
                versionArray[(int)LogoVersion.Thumb_640x360].height = 360;
                versionArray[(int)LogoVersion.Thumb_640x360].getRemoteLogoURI = (Mod m) => { return m.logo.thumb_640x360; };

                versionArray[(int)LogoVersion.Thumb_1280x720] = new LogoTemplate();
                versionArray[(int)LogoVersion.Thumb_1280x720].version = LogoVersion.Thumb_1280x720;
                versionArray[(int)LogoVersion.Thumb_1280x720].localFilename = "logo_1280x720.png";
                versionArray[(int)LogoVersion.Thumb_1280x720].width = 1280;
                versionArray[(int)LogoVersion.Thumb_1280x720].height = 720;
                versionArray[(int)LogoVersion.Thumb_1280x720].getRemoteLogoURI = (Mod m) => { return m.logo.thumb_1280x720; };
            }

            private static LogoTemplate[] versionArray;

            public LogoVersion version = LogoVersion.Full;
            public string localFilename = "";
            public int width = -1;
            public int height = -1;
            public Func<Mod, string> getRemoteLogoURI = null;

            public static LogoTemplate ForLogoVersion(LogoVersion version)
            {
                return versionArray[(int)version];
            }
        }

        public static LogoVersion cachedLogoVersion = LogoVersion.Thumb_1280x720;
        public static Dictionary<int, Sprite> modLogoCache = new Dictionary<int, Sprite>();
        public static Sprite modLogoDownloading;

        public static Sprite GetModLogo(Mod mod, LogoVersion logoVersion)
        {
            Sprite retVal;

            // TODO(@jackson): Potentially return an off-res version?
            if(cachedLogoVersion == logoVersion
               && modLogoCache.TryGetValue(mod.id, out retVal))
            {
                return retVal;
            }
            else
            {
                LogoTemplate logoTemplate = LogoTemplate.ForLogoVersion(logoVersion);

                string localURI = MODIO_DIR + mod.id + "/" + logoTemplate.localFilename;
                if(File.Exists(localURI))
                {
                    Texture2D logoTexture = new Texture2D(logoTemplate.width, logoTemplate.height);
                    logoTexture.LoadImage(File.ReadAllBytes(localURI));

                    return Sprite.Create(logoTexture,
                                         new Rect(0, 0, logoTemplate.width, logoTemplate.height),
                                         Vector2.zero);
                }
                else
                {
                    client.StartCoroutine(DownloadModLogo(mod, logoTemplate));
                    return modLogoDownloading;
                }
            }
        }
        private static IEnumerator DownloadModLogo(Mod mod, LogoTemplate logoTemplate)
        {
            byte[] imageData = null;
            yield return client.StartCoroutine(DownloadAndStoreFile(logoTemplate.getRemoteLogoURI(mod),
                                                                    mod.id + "/",
                                                                    logoTemplate.localFilename,
                                                                    (byte[] downloadedData) => { imageData = downloadedData; },
                                                                    APIClient.OnAPIRequestError));

            if(imageData != null
               && imageData.Length > 0)
            {
                Texture2D logoTexture = new Texture2D( logoTemplate.width, logoTemplate.height);
                logoTexture.LoadImage(imageData);

                modLogoCache[mod.id]
                    = Sprite.Create(logoTexture,
                                    new Rect(0,0, logoTemplate.width,logoTemplate.height),
                                    Vector2.zero);

                if(OnMogLogoUpdated != null)
                {
                    OnMogLogoUpdated(mod.id, modLogoCache[mod.id], logoTemplate.version);
                }
            }
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
                if(!modLogoCache.ContainsKey(mod.id))
                {
                    string localURI = MODIO_DIR + mod.id + "/" + logoTemplate.localFilename;
                    if(File.Exists(localURI))
                    {
                        Debug.Log("Found Logo: " + localURI);

                        Texture2D logoTexture = new Texture2D(logoTemplate.width, logoTemplate.height);
                        logoTexture.LoadImage(File.ReadAllBytes(localURI));

                        modLogoCache[mod.id]
                            = Sprite.Create(logoTexture,
                                            new Rect(0, 0, logoTemplate.width, logoTemplate.height),
                                            Vector2.zero);

                        if(OnMogLogoUpdated != null)
                        {
                            OnMogLogoUpdated(mod.id, modLogoCache[mod.id], logoTemplate.version);
                        }
                    }
                    else
                    {
                        modLogoCache.Add(mod.id, modLogoDownloading);
                        modsMissingLogosList.Add(mod);
                    }
                }
            }

            if(modsMissingLogosList.Count == 0) { return; }


            client.StartCoroutine(ChainDownloadModLogosToDiskAndCache(modsMissingLogosList,
                                                                      logoTemplate));
        }

        private static IEnumerator ChainDownloadModLogosToDiskAndCache(List<Mod> modList,
                                                                       LogoTemplate logoTemplate)
        {
            foreach(Mod mod in modList)
            {
                yield return DownloadModLogo(mod, logoTemplate);
            }
        }

        // ---------[ MISC ]------------
        public static void RequestTagCategoryMap(ObjectCallback<Dictionary<string, string[]>> callback)
        {
            client.ViewGame((Game game) =>
                            {
                                Dictionary<string, string[]> retVal
                                    = new Dictionary<string, string[]>();

                                foreach(TagCategory category in game.cats)
                                {
                                    retVal.Add(category.name, category.tags);
                                }

                                callback(retVal);
                            });
        }
    }
}