using System;
using System.IO;
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
        /// @cond
        /// <summary>A structure used to store data on disk.</summary>
        private struct PersistentData
        {
            public SimpleVersion lastRunVersion;
            public int[] subscribedModIds;
            public int[] enabledModIds;
        }

        /// <summary>Data that needs to be stored across sessions.</summary>
        private static PersistentData m_data;
        /// @endcond

        // ---------[ CONSTANTS & STATICS ]---------
        /// <summary>Current version of the ModManager/Plugin.</summary>
        /// <para>Updates the last run version is saved is checked by the static
        /// initialization and determines if any updates to the locally stored
        /// data need to be made.</para>
        public static readonly SimpleVersion VERSION = new SimpleVersion(2, 0);

        /// <summary>File name used to store the persistent data.</summary>
        /// <para>This file is used to stored the values in the PersistentData
        /// struct between sessions.</para>
        public const string PERSISTENTDATA_FILENAME = "mod_manager.data";

        /// <summary>File path used to store the persistent data.</summary>
        /// <para>This file is used to stored the values in the PersistentData
        /// struct between sessions.</para>
        public static readonly string PERSISTENTDATA_FILEPATH;

        /// <summary>Install directory used by the ModManager.</summary>
        /// <para>Mods installed using [ModManager.TryInstallMod](ModIO.ModManager.TryInstallMod)
        /// are unzipped to this folder, in an unique directory determined by
        /// [ModManager.GetModInstallDirectory](ModIO.ModManager.GetModInstallDirectory).</para>
        /// <para>Any folders located in this directory will be presumed to be
        /// user mods not managed by this mod.io, and will thus be returned in
        /// the various mod installation management functions.</para>
        /// <para>See also: [[ModIO.ModManager.GetInstalledModDirectories]],
        /// [[ModIO.ModManager.GetInstalledModVersions]],
        /// [[ModIO.ModManager.IterateInstalledMods]]</para>
        public static string installationDirectory;

        // ---------[ INITIALIZATION ]---------
        /// <summary>Initializes the ModManager settings.</summary>
        /// <para>Loads/creates the persistent data, and makes any necessary
        /// versioning updates to the persistent data used by this plugin.</para>
        static ModManager()
        {
            PluginSettings.Data settings = PluginSettings.data;
            ModManager.installationDirectory = settings.installationDirectory;
            ModManager.PERSISTENTDATA_FILEPATH = IOUtilities.CombinePath(settings.cacheDirectory, PERSISTENTDATA_FILENAME);

            if(!IOUtilities.TryReadJsonObjectFile(PERSISTENTDATA_FILEPATH, out ModManager.m_data))
            {
                ModManager.m_data = new PersistentData()
                {
                    lastRunVersion = ModManager.VERSION,
                    subscribedModIds = new int[0],
                    enabledModIds = new int[0],
                };
            }

            VersionUpdater.Run(m_data.lastRunVersion);

            m_data.lastRunVersion = VERSION;
            IOUtilities.WriteJsonObjectFile(PERSISTENTDATA_FILEPATH, ModManager.m_data);
        }


        // ---------[ MOD MANAGEMENT ]---------
        /// <summary>Returns the subscribed mods.</summary>
        /// <para>This value is a locally stored representation of the mods that
        /// the current user is subscribed to, and is stored in the persistent
        /// data file. To get the subscriptions on the mod.io servers, use
        /// [APIClient.GetUserSubscriptions](ModIO.APIClient.GetUserSubscriptions).
        /// <para>This functionality is provided to allow for offline and
        /// user-less mod management.</para>
        /// <para>See also: [[ModIO.ModManager.SetSubscribedModIds]]</para>
        public static List<int> GetSubscribedModIds()
        {
            return new List<int>(m_data.subscribedModIds);
        }
        /// <summary>Sets the subscribed mods and writes the data to disk.</summary>
        /// <para>This value is a locally stored representation of the mods that
        /// the current user is subscribed to, and is stored in the persistent
        /// data file. To set the subscriptions on the mod.io servers, use
        /// [APIClient.SubscribeToMod](ModIO.APIClient.SubscribeToMod) /
        /// [APIClient.UnsubscribeFromMod](ModIO.APIClient.UnsubscribeFromMod).
        /// <para>This functionality is provided to allow for offline and
        /// user-less mod management.</para>
        /// <para>See also: [[ModIO.ModManager.GetSubscribedModIds]]</para>
        /// <param name="modIds">New value to set as the subscribed mod collection</param>
        public static void SetSubscribedModIds(IEnumerable<int> modIds)
        {
            int[] modIdArray;

            if(modIds == null)
            {
                modIdArray = new int[0];
            }
            else
            {
                modIdArray = modIds.ToArray();
            }

            ModManager.m_data.subscribedModIds = modIdArray;
            IOUtilities.WriteJsonObjectFile(PERSISTENTDATA_FILEPATH, ModManager.m_data);
        }

        /// <summary>Returns the enabled mods.</summary>
        /// <para>This value represents the collection of mod ids that should be
        /// loaded and activate when the game is run.</para>
        /// <para>See also: [[ModIO.ModManager.SetEnabledModIds]]</para>
        public static List<int> GetEnabledModIds()
        {
            return new List<int>(m_data.enabledModIds);
        }
        /// <summary>Sets the enabled mods and writes the data to disk.</summary>
        /// <para>This value represents the collection of mod ids that should be
        /// loaded and activate when the game is run.</para>
        /// <para>See also: [[ModIO.ModManager.GetEnabledModIds]]</para>
        /// <param name="modIds">New value to set as the enabled mod collection</param>
        public static void SetEnabledModIds(IEnumerable<int> modIds)
        {
            int[] modIdArray;

            if(modIds == null)
            {
                modIdArray = new int[0];
            }
            else
            {
                modIdArray = modIds.ToArray();
            }

            ModManager.m_data.enabledModIds = modIdArray;
            string dataPath = IOUtilities.CombinePath(CacheClient.cacheDirectory, PERSISTENTDATA_FILENAME);
            IOUtilities.WriteJsonObjectFile(dataPath, ModManager.m_data);
        }

        /// <summary>Generates the path for a given modfile install directory.</summary>
        /// <para>The mod io Unity Plugin extracts each mod to a unique folder
        /// based on the mod id and modfile id, that is determined by the return
        /// value of this function.</para>
        /// <param name="modId">Mod identifier to use for install path generation</param>
        /// <param name="modfileId">Modfile identifier to use for install path generation</param>
        /// <returns>The unique path of the directory for the given mod and
        /// modfile identifiers</returns>
        public static string GetModInstallDirectory(int modId, int modfileId)
        {
            return IOUtilities.CombinePath(ModManager.installationDirectory,
                                           modId.ToString() + "_" + modfileId.ToString());
        }

        /// <summary>Extracts a mod archive to the installs folder and removes other installed versions.</summary>
        /// <para>This function does not manage the downloading of mod binaries,
        /// merely extracts the mod binary zip file stored at the location
        /// described by
        /// [CacheClient.GenerateModBinaryZipFilePath](ModIO.CacheClient.GenerateModBinaryZipFilePath).
        /// If this file does not exist, or is not an extractable archive, this
        /// this function will fail. Additionally, if any existing modfile
        /// installations fail to uninstall, this function will also fail.</para>
        /// <param name="modId">Mod identifier of the mod binary to install</param>
        /// <param name="modfileId">Modfile identifier of the mod binary to install</param>
        /// <param name="removeArchiveOnSuccess">Remove the mod binary archive
        /// be removed if the installation succeeds</param>
        /// <returns>**TRUE** indicates the entire installation completed successfully,
        /// (although the failure/success of the achive deletion is unchecked).
        /// **FALSE** indicates that either the mod binary archive does not exist,
        /// the uninstallation of previous versions failed, or that the
        /// extraction process failed.</returns>
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

            // extract
            string tempLocation = Path.Combine(CacheClient.GenerateModBinariesDirectoryPath(modId),
                                               modfileId.ToString());
            try
            {
                if(Directory.Exists(tempLocation))
                {
                    Directory.Delete(tempLocation, true);
                }

                Directory.CreateDirectory(tempLocation);

                using (var zip = Ionic.Zip.ZipFile.Read(zipFilePath))
                {
                    zip.ExtractAll(tempLocation);
                }
            }
            catch(Exception e)
            {
                Debug.LogWarning("[mod.io] Unable to extract binary to a temporary folder."
                                 + "\nLocation: " + tempLocation + "\n\n"
                                 + Utility.GenerateExceptionDebugString(e));

                if(!IOUtilities.DeleteDirectory(tempLocation))
                {
                    Debug.LogWarning("[mod.io] Failed to remove the temporary folder."
                                     + "\nLocation: " + tempLocation + "\n\n");
                }

                return false;
            }

            // Remove old versions
            bool uninstallSucceeded = ModManager.TryUninstallAllModVersions(modId);

            if(!uninstallSucceeded)
            {
                Debug.LogWarning("[mod.io] Unable to extract binary to the mod install folder."
                                 + "\nFailed to uninstall other versions of this mod.");

                if(!IOUtilities.DeleteDirectory(tempLocation))
                {
                    Debug.LogWarning("[mod.io] Failed to remove the temporary folder."
                                     + "\nLocation: " + tempLocation + "\n\n");
                }

                return false;
            }

            // Move to permanent folder
            string installDirectory = ModManager.GetModInstallDirectory(modId,
                                                                        modfileId);
            try
            {
                if(Directory.Exists(installDirectory))
                {
                    Directory.Delete(installDirectory, true);
                }
                else
                {
                    Directory.CreateDirectory(ModManager.installationDirectory);
                }

                Directory.Move(tempLocation, installDirectory);
            }
            catch(Exception e)
            {
                Debug.LogWarning("[mod.io] Unable to move binary to the mod installation folder."
                                 + "\nSrc: " + tempLocation
                                 + "\nDest: " + installDirectory + "\n\n"
                                 + Utility.GenerateExceptionDebugString(e));

                if(!IOUtilities.DeleteDirectory(tempLocation))
                {
                    Debug.LogWarning("[mod.io] Failed to remove the temporary folder."
                                     + "\nLocation: " + tempLocation + "\n\n");
                }

                return false;
            }

            if(removeArchiveOnSuccess)
            {
                IOUtilities.DeleteFile(zipFilePath);
            }

            return true;
        }

        /// <summary>Removes all versions of a mod from the installs folder.</summary>
        /// <para>See also: [[ModIO.ModManager.TryInstallMod]],
        /// [[ModIO.ModManager.GetModInstallDirectory]]</para>
        /// <param name="modId">Mod identifier of the mod to uninstall</param>
        /// <returns>**TRUE** indicates that all of the detected versions of the
        /// mod installation were successfully removed.</returns>
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
        /// <para>See also: [[ModIO.ModManager.TryInstallMod]],
        /// [[ModIO.ModManager.GetModInstallDirectory]]</para>
        /// <param name="modId">Mod identifier of the mod version to uninstall</param>
        /// <param name="modfileId">Modfile identifier of the mod version to uninstall</param>
        /// <returns>**TRUE** indicates that the mod version was successfully
        /// uninstalled.</returns>
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
        /// <para>**NOTE:** This function will additionally return the paths of
        /// _any_ folder located in the
        /// [ModManager.installationDirectory](ModIO.ModManager.installationDirectory)
        /// and thus we recommend checking the validity of any mods assumed to
        /// be contained within the returned directories before loading.</para>
        /// <para>See also: [[ModIO.ModManager.GetInstalledModVersions]],
        /// [[ModIO.ModManager.IterateInstalledMods]]</para>
        /// <param name="excludeDisabledMods">Passing **TRUE** causes this
        /// function to omit any disabled mods in the returned collection</param>
        /// <returns>A collection containing the paths of all the installed mod
        /// binary directories.</returns>
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
        /// <para>See also: [[ModIO.ModManager.GetInstalledModDirectories]],
        /// [[ModIO.ModManager.IterateInstalledMods]]</para>
        /// <param name="excludeDisabledMods">Passing **TRUE** causes this
        /// function to omit any disabled mods in the returned collection</param>
        /// <returns>A collection containing the a
        /// [ModfileIdPair](ModIO.ModfileIdPair) for each installed mod version.</returns>
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
                if(kvp.Key.modId != ModProfile.NULL_ID)
                {
                    versions.Add(kvp.Key);
                }
            }

            return versions;
        }

        /// <summary>Returns the data of all the mods installed.</summary>
        /// <para>Iterates over all of the folders in the
        /// [mod installation directory](ModIO.ModManager.installationDirectory)
        /// and parses the folder name to generate the accompanying
        /// [ModfileIdPair](ModIO.ModfileIdPair) to return as a KeyValuePair.</para>
        /// <para>NOTE: It cannot be assumed that the Key of the KeyValuePair is
        /// unique as there can be directories that cannot be parsed as mod and
        /// modfile identifier pairs.</para>
        /// <para>See also: [[ModIO.ModManager.GetInstalledModDirectories]],
        /// [[ModIO.ModManager.GetInstalledModVersions]]</para>
        /// <param name="modIdFilter">Mods to include in the enumeration. A
        /// value of **NULL** includes all results in the enumeration</param>
        /// <returns>An enumerator of all the detected
        /// [ModfileIdPairs](ModIO.ModfileIdPair) and their accompanying
        /// directory paths. A folder name with an unparseable mod identifier
        /// element generates a [ModfileIdPair](ModIO.ModfileIdPair) with a
        /// modId set to [ModProfile.NULL_ID](ModIO.ModProfile.NULL_ID) and a
        /// modfileId set to [Modfile.NULL_ID](ModIO.Modfile.NULL_ID). If only
        /// the modfile identifier cannot be parsed, the mod id will be set to
        /// the parsed value and the modfileId will be set to
        /// [Modfile.NULL_ID](ModIO.Modfile.NULL_ID).</returns>
        public static IEnumerable<KeyValuePair<ModfileIdPair, string>> IterateInstalledMods(IList<int> modIdFilter)
        {
            string[] modDirectories = new string[0];
            try
            {
                if(Directory.Exists(ModManager.installationDirectory))
                {
                    modDirectories = Directory.GetDirectories(ModManager.installationDirectory);
                }
            }
            catch(Exception e)
            {
                string warningInfo = ("[mod.io] Failed to read mod installation directory."
                                      + "\nDirectory: " + ModManager.installationDirectory + "\n\n");

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

        /// <summary>Downloads and installs any new mods binaries.</summary>
        /// <para>This complex coroutine fetches collects the list of currently
        /// installed mods, fetches all of the corresponding mod profiles from
        /// the server, then sequentially attempts to download and install any
        /// mod binaries that don't match the
        /// [ModProfile.currentBuild](ModIO.ModProfile.currentBuild) value.</para>
        /// <para>Errors (other than an unresolvable request, or failed
        /// validation) will trigger a delay and reattempt. (Max 2 attempts per
        /// mod.)</para>
        /// <returns>A Unity coroutine that needs to be run using
        /// [StartCoroutine](https://docs.unity3d.com/2018.2/Documentation/ScriptReference/MonoBehaviour.StartCoroutine.html)
        /// on a Game Object component.</returns>
        public static System.Collections.IEnumerator UpdateAllInstalledMods_Coroutine()
        {
            List<ModfileIdPair> installedModVersions = ModManager.GetInstalledModVersions(false);
            List<Modfile> updatedModVersions = new List<Modfile>();

            bool isRequestResolved = false;
            int attemptCount = 0;
            int attemptLimit = 2;

            // reattempt delay calculator
            Func<WebRequestError, int> calcReattemptDelay = (requestError) =>
            {
                if(requestError.limitedUntilTimeStamp > 0)
                {
                    return (requestError.limitedUntilTimeStamp - ServerTimeStamp.Now);
                }
                else if(!requestError.isRequestUnresolvable)
                {
                    if(requestError.isServerUnreachable)
                    {
                        return 60;
                    }
                    else
                    {
                        return 15;
                    }
                }
                else
                {
                    return 0;
                }
            };

            // get all current versions
            List<int> modIds = new List<int>(installedModVersions.Count);
            foreach(ModfileIdPair idPair in installedModVersions)
            {
                modIds.Add(idPair.modId);
            }

            // - fetch and compare all the installed versions to the remote mod profiles -
            RequestFilter modFilter = new RequestFilter();
            modFilter.fieldFilters[GetAllModsFilterFields.id]
            = new InArrayFilter<int>()
            {
                filterArray = modIds.ToArray()
            };

            while(!isRequestResolved
                  && attemptCount < attemptLimit)
            {
                bool isDone = false;
                WebRequestError error = null;
                List<ModProfile> profiles = null;

                ModManager.FetchAllResultsForQuery<ModProfile>((p,s,e) => APIClient.GetAllMods(modFilter,p,s,e),
                (r) =>
                {
                    profiles = r;
                    isDone = true;
                },
                (e) =>
                {
                    error = e;
                    isDone = true;
                });

                while(!isDone) { yield return null; }

                if(error != null)
                {
                    WebRequestError.LogAsWarning(error);

                    if(error.isAuthenticationInvalid)
                    {
                        yield break;
                    }
                    else if(error.isRequestUnresolvable)
                    {
                        isRequestResolved = true;
                    }
                    else
                    {
                        ++attemptCount;

                        int reattemptDelay = calcReattemptDelay(error);
                        yield return new WaitForSeconds(reattemptDelay);
                    }
                }
                else
                {
                    foreach(ModProfile profile in profiles)
                    {
                        foreach(ModfileIdPair idPair in installedModVersions)
                        {
                            if(idPair.modId == profile.id)
                            {
                                if(idPair.modfileId != profile.currentBuild.id)
                                {
                                    updatedModVersions.Add(profile.currentBuild);
                                }
                                break;
                            }
                        }
                    }

                    isRequestResolved = true;
                }
            }

            // - download and install any updates sequentially -
            foreach(Modfile updatedBuild in updatedModVersions)
            {
                isRequestResolved = false;
                attemptCount = 0;

                int modId = updatedBuild.modId;
                int modfileId = updatedBuild.id;

                while(!isRequestResolved
                      && attemptCount < 2)
                {
                    bool isDone = false;
                    WebRequestError error = null;

                    string zipFilePath = CacheClient.GenerateModBinaryZipFilePath(modId, modfileId);
                    bool isDownloadedAndValid = (System.IO.File.Exists(zipFilePath)
                                                 && updatedBuild.fileSize == IOUtilities.GetFileSize(zipFilePath)
                                                 && updatedBuild.fileHash != null
                                                 && updatedBuild.fileHash.md5 == IOUtilities.CalculateFileMD5Hash(zipFilePath));

                    // is installed
                    if(Directory.Exists(GetModInstallDirectory(modId, modfileId)))
                    {
                        isRequestResolved = true;
                        break;
                    }
                    // is downloaded
                    else if(isDownloadedAndValid)
                    {
                        isRequestResolved = ModManager.TryInstallMod(modId, modfileId, true);
                        ++attemptCount;
                    }
                    // is the downloadLocator valid?
                    else if(updatedBuild.downloadLocator.dateExpires <= ServerTimeStamp.Now)
                    {
                        Modfile modfile = null;

                        APIClient.GetModfile(modId, modfileId,
                        (r) =>
                        {
                            modfile = r;
                            isDone = true;
                        },
                        (e) =>
                        {
                            error = e;
                            isDone = true;
                        });

                        while(!isDone) { yield return null; }

                        if(error != null)
                        {
                            WebRequestError.LogAsWarning(error);

                            if(error.isAuthenticationInvalid)
                            {
                                yield break;
                            }
                            else if(error.isRequestUnresolvable)
                            {
                                isRequestResolved = true;
                            }
                            else
                            {
                                ++attemptCount;

                                int reattemptDelay = calcReattemptDelay(error);
                                yield return new WaitForSeconds(reattemptDelay);
                            }
                        }
                        else
                        {
                            updatedBuild.downloadLocator = modfile.downloadLocator;
                        }

                        continue;
                    }
                    // downloadLocator is valid, but not downloaded
                    else
                    {
                        // check if already downloading
                        if(DownloadClient.GetActiveModBinaryDownload(modId, modfileId) != null)
                        {
                            isRequestResolved = true;
                            break;
                        }

                        FileDownloadInfo downloadInfo = DownloadClient.StartModBinaryDownload(updatedBuild, zipFilePath);

                        while(!downloadInfo.isDone) { yield return null; }

                        if(downloadInfo.error != null)
                        {
                            ++attemptCount;

                            WebRequestError.LogAsWarning(downloadInfo.error);

                            if(downloadInfo.error.isAuthenticationInvalid)
                            {
                                yield break;
                            }
                            else if(downloadInfo.error.isRequestUnresolvable)
                            {
                                isRequestResolved = true;
                            }
                            else
                            {
                                int reattemptDelay = calcReattemptDelay(downloadInfo.error);
                                yield return new WaitForSeconds(reattemptDelay);
                            }
                        }
                    }
                }
            }
        }


        // ---------[ GAME PROFILE ]---------
        /// <summary>Fetches and caches the Game Profile (if not already cached).</summary>
        /// <para>As with all similar ModManager functions, this checks the
        /// cache for the [GameProfile](ModIO.GameProfile) first, and if not
        /// found, fetches it from the server and stores it in the cache. As
        /// such, there is the potential for the data returned by this function
        /// to be obsolete.</para>
        /// <para>See also: [[ModIO.CacheClient.LoadGameProfile]],
        /// [[ModIO.APIClient.GetGame]]</para>
        /// <param name="onSuccess">Action to execute if the request succeeds</param>
        /// <param name="onError">Action to execute if the request returns an error</param>
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
        /// <para>As with all similar ModManager functions, this checks the
        /// cache for the relevant [ModProfile](ModIO.ModProfile) first, and if
        /// not found, fetches it from the server and stores it in the cache. As
        /// such, there is the potential for the data returned by this function
        /// to be obsolete.</para>
        /// <para>See also: [[ModIO.CacheClient.LoadModProfile]],
        /// [[ModIO.APIClient.GetMod]]</para>
        /// <param name="modId">Identifier for the mod profile to get</param>
        /// <param name="onSuccess">Action to execute if the request succeeds</param>
        /// <param name="onError">Action to execute if the request returns an error</param>
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
        /// <para>As with all similar ModManager functions, this checks the
        /// cache for the relevant [ModProfiles](ModIO.ModProfile) first, and
        /// for any that aren't found, fetches the profiles from the server and
        /// stores them in the cache. As such, there is the potential for the
        /// data returned by this function to be obsolete.</para>
        /// <para>See also: [[ModIO.CacheClient.IterateModProfiles]],
        /// [[ModIO.APIClient.GetAllMods]]</para>
        /// <param name="modIds">Identifiers for the mod profiles to get</param>
        /// <param name="onSuccess">Action to execute if the request succeeds</param>
        /// <param name="onError">Action to execute if the request returns an error</param>
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

        // ---------[ MOD IMAGES ]---------
        /// <summary>Fetches and caches a Mod Logo (if not already cached).</summary>
        /// <para>As with all similar ModManager functions, this checks the
        /// cache for the mod logo first, and if not found, fetches it from the
        /// server and stores it in the cache. As such, there is the potential
        /// for the data returned by this function to be obsolete.</para>
        /// <para>See also: [[ModIO.DownloadClient.DownloadModLogo]],
        /// [[ModIO.CacheClient.LoadModLogo]]</para>
        /// <param name="profile">Profile to fetch the logo for</param>
        /// <param name="size">Size of image to fetch</param>
        /// <param name="onSuccess">Action to execute if the request succeeds</param>
        /// <param name="onError">Action to execute if the request returns an error</param>
        public static void GetModLogo(ModProfile profile, LogoSize size,
                                      Action<Texture2D> onSuccess,
                                      Action<WebRequestError> onError)
        {
            Debug.Assert(profile != null);

            GetModLogo(profile.id, profile.logoLocator, size, onSuccess, onError);
        }

        /// <summary>Fetches and caches a Mod Logo (if not already cached).</summary>
        /// <para>As with all similar ModManager functions, this checks the
        /// cache for the mod logo first, and if not found, fetches it from the
        /// server and stores it in the cache. As such, there is the potential
        /// for the data returned by this function to be obsolete.</para>
        /// <para>See also: [[ModIO.DownloadClient.DownloadModLogo]],
        /// [[ModIO.CacheClient.LoadModLogo]]</para>
        /// <param name="modId">Mod identifier for the logo</param>
        /// <param name="logoLocator">Server data for the logo</param>
        /// <param name="size">Size of image to fetch</param>
        /// <param name="onSuccess">Action to execute if the request succeeds</param>
        /// <param name="onError">Action to execute if the request returns an error</param>
        public static void GetModLogo(int modId, LogoImageLocator logoLocator,
                                      LogoSize size,
                                      Action<Texture2D> onSuccess,
                                      Action<WebRequestError> onError)
        {
            Debug.Assert(logoLocator != null);

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

        /// <summary>Fetches and caches a Mod Gallery Image (if not already cached).</summary>
        /// <para>As with all similar ModManager functions, this checks the
        /// cache for the mod gallery image first, and if not found, fetches it
        /// from the server and stores it in the cache. As such, there is the
        /// potential for the data returned by this function to be obsolete.</para>
        /// <para>See also: [[ModIO.DownloadClient.DownloadModGalleryImage]],
        /// [[ModIO.CacheClient.LoadModGalleryImage]]</para>
        /// <param name="profile">Profile to fetch the gallery image for</param>
        /// <param name="imageFileName">Identifier for the gallery image</param>
        /// <param name="size">Size of image to fetch</param>
        /// <param name="onSuccess">Action to execute if the request succeeds</param>
        /// <param name="onError">Action to execute if the request returns an error</param>
        public static void GetModGalleryImage(ModProfile profile,
                                              string imageFileName,
                                              ModGalleryImageSize size,
                                              Action<Texture2D> onSuccess,
                                              Action<WebRequestError> onError)
        {
            Debug.Assert(profile != null);

            ModManager.GetModGalleryImage(profile.id, profile.media.GetGalleryImageWithFileName(imageFileName), size, onSuccess, onError);
        }

        /// <summary>Fetches and caches a Mod Gallery Image (if not already cached).</summary>
        /// <para>As with all similar ModManager functions, this checks the
        /// cache for the mod gallery image first, and if not found, fetches it
        /// from the server and stores it in the cache. As such, there is the
        /// potential for the data returned by this function to be obsolete.</para>
        /// <para>See also: [[ModIO.DownloadClient.DownloadModGalleryImage]],
        /// [[ModIO.CacheClient.LoadModGalleryImage]]</para>
        /// <param name="modId">Mod identifier for the gallery image</param>
        /// <param name="imageLocator">Server data for the gallery image</param>
        /// <param name="size">Size of image to fetch</param>
        /// <param name="onSuccess">Action to execute if the request succeeds</param>
        /// <param name="onError">Action to execute if the request returns an error</param>
        public static void GetModGalleryImage(int modId,
                                              GalleryImageLocator imageLocator,
                                              ModGalleryImageSize size,
                                              Action<Texture2D> onSuccess,
                                              Action<WebRequestError> onError)
        {
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

        /// <summary>Fetches and caches a Mod YouTube Thumbnail (if not already cached).</summary>
        /// <para>As with all similar ModManager functions, this checks the
        /// cache for the mod gallery image first, and if not found, fetches it
        /// from the server and stores it in the cache. As such, there is the
        /// potential for the data returned by this function to be obsolete.</para>
        /// <para>See also: [[ModIO.DownloadClient.DownloadModGalleryImage]],
        /// [[ModIO.CacheClient.LoadModGalleryImage]]</para>
        /// <param name="modId">Mod identifier for the gallery image</param>
        /// <param name="youTubeVideoId">Identifier for the YouTube Video</param>
        /// <param name="onSuccess">Action to execute if the request succeeds</param>
        /// <param name="onError">Action to execute if the request returns an error</param>
        public static void GetModYouTubeThumbnail(int modId,
                                                  string youTubeVideoId,
                                                  Action<Texture2D> onSuccess,
                                                  Action<WebRequestError> onError)
        {
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


        // ---------[ MODFILES ]---------
        /// <summary>Fetches and caches a Modfile (if not already cached).</summary>
        /// <para>As with all similar ModManager functions, this checks the
        /// cache for the [Modfile](ModIO.Modfile), and if not found, fetches it
        /// from the server and stores it in the cache. As such, there is the
        /// potential for the data returned by this function to be obsolete.</para>
        /// <para>See also: [[ModIO.APIClient.GetModfile]],
        /// [[ModIO.CacheClient.LoadModfile]]</para>
        /// <param name="modId">Mod identifier for the modfile</param>
        /// <param name="modfileId">Identifier for the modfile</param>
        /// <param name="onSuccess">Action to execute if the request succeeds</param>
        /// <param name="onError">Action to execute if the request returns an error</param>
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
        /// <para>As with all similar ModManager functions, this checks the
        /// cache for the [Modfile](ModIO.Modfile), and if not found, fetches it
        /// from the server and stores it in the cache. Unlike most other
        /// similar ModManager functions, this endpoint will not return obsolete
        /// data as [ModStatistics](ModIO.ModStatistics) objects contain an
        /// [expiration date](ModIO.ModStatistics.dateExpires) and will be
        /// re-fetched if expired.</para>
        /// <para>See also: [[ModIO.APIClient.GetModStats]],
        /// [[ModIO.CacheClient.LoadModStatistics]]</para>
        /// <param name="modId">Mod identifier for the mod statistics</param>
        /// <param name="onSuccess">Action to execute if the request succeeds</param>
        /// <param name="onError">Action to execute if the request returns an error</param>
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
        /// <para>As with all similar ModManager functions, this checks the
        /// cache for the [UserProfile](ModIO.UserProfile), and if not found,
        /// fetches it from the server and stores it in the cache.
        /// As such, there is the potential for the data returned by this
        /// function to be obsolete.</para>
        /// <para>See also: [[ModIO.APIClient.GetUser]],
        /// [[ModIO.CacheClient.LoadUserProfile]]</para>
        /// <param name="userId">Identifier for the user profile</param>
        /// <param name="onSuccess">Action to execute if the request succeeds</param>
        /// <param name="onError">Action to execute if the request returns an error</param>
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
        /// <para>As with all similar ModManager functions, this checks the
        /// cache for the user avatar,
        /// and if not found, fetches it from the server and stores it in the cache.
        /// As such, there is the potential for the data returned by this
        /// function to be obsolete.</para>
        /// <para>See also: [[ModIO.DownloadClient.DownloadUserAvatar]],
        /// [[ModIO.CacheClient.LoadUserAvatar]]</para>
        /// <param name="profile">User profile to request the avatar for</param>
        /// <param name="size">Image size of the avatar</param>
        /// <param name="onSuccess">Action to execute if the request succeeds</param>
        /// <param name="onError">Action to execute if the request returns an error</param>
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
        /// <para>As with all similar ModManager functions, this checks the
        /// cache for the user avatar,
        /// and if not found, fetches it from the server and stores it in the cache.
        /// As such, there is the potential for the data returned by this
        /// function to be obsolete.</para>
        /// <para>See also: [[ModIO.DownloadClient.DownloadUserAvatar]],
        /// [[ModIO.CacheClient.LoadUserAvatar]]</para>
        /// <param name="userId">User identifier for the user avatar</param>
        /// <param name="avatarLocator">Server data for the user avatar</param>
        /// <param name="size">Image size of the avatar</param>
        /// <param name="onSuccess">Action to execute if the request succeeds</param>
        /// <param name="onError">Action to execute if the request returns an error</param>
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
        /// <para>Primarily wraps
        /// [APIClient.GetAllModEvents](ModIO.APIClient.GetAllModEvents)
        /// providing a convenient interface for fetching _all_ of the results
        /// as it automatically fetches each page sequentially until all have
        /// been received.</para>
        /// <para>See also: [[ModIO.APIClient.GetAllModEvents]]</para>
        /// <param name="fromTimeStamp">Start of the time period to filter by **(exclusively)**</param>
        /// <param name="untilTimeStamp">End of the time period to filter by **(inclusively)**</param>
        /// <param name="onSuccess">Action to execute if the request succeeds</param>
        /// <param name="onError">Action to execute if the request returns an error</param>
        public static void FetchAllModEvents(int fromTimeStamp,
                                             int untilTimeStamp,
                                             Action<List<ModEvent>> onSuccess,
                                             Action<WebRequestError> onError)
        {
            ModManager.FetchModEvents(null, fromTimeStamp, untilTimeStamp,
                                      onSuccess, onError);
        }

        /// <summary>Fetches all mod events for the given mod ids.</summary>
        /// <para>Primarily wraps
        /// [APIClient.GetAllModEvents](ModIO.APIClient.GetAllModEvents)
        /// providing a convenient interface for fetching _all_ of the results
        /// as it automatically fetches each page sequentially until all have
        /// been received.</para>
        /// <para>See also: [[ModIO.APIClient.GetAllModEvents]]</para>
        /// <param name="modIdFilter">Filter of mod identifiers to include in the result</param>
        /// <param name="fromTimeStamp">Start of the time period to filter by **(exclusively)**</param>
        /// <param name="untilTimeStamp">End of the time period to filter by **(inclusively)**</param>
        /// <param name="onSuccess">Action to execute if the request succeeds</param>
        /// <param name="onError">Action to execute if the request returns an error</param>
        public static void FetchModEvents(IEnumerable<int> modIdFilter,
                                          int fromTimeStamp,
                                          int untilTimeStamp,
                                          Action<List<ModEvent>> onSuccess,
                                          Action<WebRequestError> onError)
        {
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
            if(modIdFilter != null)
            {
                modEventFilter.fieldFilters[GetAllModEventsFilterFields.modId]
                = new InArrayFilter<int>()
                {
                    filterArray = modIdFilter.ToArray(),
                };
            }

            // - Get All Events -
            ModManager.FetchAllResultsForQuery<ModEvent>((p,s,e) => APIClient.GetAllModEvents(modEventFilter, p, s, e),
                                                         onSuccess,
                                                         onError);
        }

        /// <summary>Fetches all user events for the authenticated user.</summary>
        /// <para>Primarily wraps
        /// [APIClient.GetUserEvents](ModIO.APIClient.GetUserEvents)
        /// providing a convenient interface for fetching _all_ of the events
        /// that involve the authenticated user and the stored game id,
        /// as it automatically fetches each page sequentially until all have
        /// been received.</para>
        /// <para>See also: [[ModIO.APIClient.GetUserEvents]],
        /// [[ModIO.UserAuthenticationData]]</para>
        /// <param name="fromTimeStamp">Start of the time period to filter by **(exclusively)**</param>
        /// <param name="untilTimeStamp">End of the time period to filter by **(inclusively)**</param>
        /// <param name="onSuccess">Action to execute if the request succeeds</param>
        /// <param name="onError">Action to execute if the request returns an error</param>
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

        // ---------[ UPLOADING ]---------
        /// <summary>Submits a new mod to the server.</summary>
        public static void SubmitNewMod(EditableModProfile newModProfile,
                                        Action<ModProfile> onSuccess,
                                        Action<WebRequestError> onError)
        {
            // - Client-Side error-checking -
            WebRequestError error = null;
            if(String.IsNullOrEmpty(newModProfile.name.value))
            {
                error = WebRequestError.GenerateLocal("Mod Profile needs to be named before it can be uploaded");
            }
            else if(String.IsNullOrEmpty(newModProfile.summary.value))
            {
                error = WebRequestError.GenerateLocal("Mod Profile needs to be given a summary before it can be uploaded");
            }
            else if(!File.Exists(newModProfile.logoLocator.value.url))
            {
                error = WebRequestError.GenerateLocal("Mod Profile needs to be assigned a logo before it can be uploaded");
            }

            if(error != null)
            {
                onError(error);
                return;
            }

            // - Initial Mod Submission -
            var parameters = new AddModParameters();
            parameters.name = newModProfile.name.value;
            parameters.summary = newModProfile.summary.value;
            parameters.logo = BinaryUpload.Create(Path.GetFileName(newModProfile.logoLocator.value.url),
                                                      File.ReadAllBytes(newModProfile.logoLocator.value.url));
            if(newModProfile.visibility.isDirty)
            {
                parameters.visibility = newModProfile.visibility.value;
            }
            if(newModProfile.nameId.isDirty)
            {
                parameters.nameId = newModProfile.nameId.value;
            }
            if(newModProfile.descriptionAsHTML.isDirty)
            {
                parameters.descriptionAsHTML = newModProfile.descriptionAsHTML.value;
            }
            if(newModProfile.homepageURL.isDirty)
            {
                parameters.nameId = newModProfile.homepageURL.value;
            }
            if(newModProfile.metadataBlob.isDirty)
            {
                parameters.metadataBlob = newModProfile.metadataBlob.value;
            }
            if(newModProfile.nameId.isDirty)
            {
                parameters.nameId = newModProfile.nameId.value;
            }
            if(newModProfile.tags.isDirty)
            {
                parameters.tags = newModProfile.tags.value;
            }

            // NOTE(@jackson): As add Mod takes more parameters than edit,
            //  we can ignore some of the elements in the EditModParameters
            //  when passing to SubmitModChanges_Internal
            var remainingModEdits = new EditableModProfile();
            remainingModEdits.youTubeURLs = newModProfile.youTubeURLs;
            remainingModEdits.sketchfabURLs = newModProfile.sketchfabURLs;
            remainingModEdits.galleryImageLocators = newModProfile.galleryImageLocators;

            APIClient.AddMod(parameters,
                             result => SubmitModChanges_Internal(result,
                                                                 remainingModEdits,
                                                                 onSuccess,
                                                                 onError),
                             onError);
        }

        /// <summary>Submits changes to a mod to the server.</summary>
        public static void SubmitModChanges(int modId,
                                            EditableModProfile modEdits,
                                            Action<ModProfile> onSuccess,
                                            Action<WebRequestError> onError)
        {
            Debug.Assert(modId != ModProfile.NULL_ID);

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
                    (p) => SubmitModChanges_Internal(profile, modEdits,
                                                     onSuccess,
                                                     onError),
                    onError);
                }
                // - Get updated ModProfile -
                else
                {
                    SubmitModChanges_Internal(profile,
                                              modEdits,
                                              onSuccess,
                                              onError);
                }
            };

            ModManager.GetModProfile(modId, submitChanges, onError);
        }

        /// <summary>Calculates changes made to a mod profile and submits them to the servers.</summary>
        private static void SubmitModChanges_Internal(ModProfile profile,
                                                      EditableModProfile modEdits,
                                                      Action<ModProfile> onSuccess,
                                                      Action<WebRequestError> onError)
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
                                              doNextSubmissionAction, onError);
                    });
                }
                if(deleteMediaParameters.stringValues.Count > 0)
                {
                    submissionActions.Add(() =>
                    {
                        APIClient.DeleteModMedia(profile.id,
                                                 deleteMediaParameters,
                                                 () => doNextSubmissionAction(null),
                                                 onError);
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
                                                () => doNextSubmissionAction(null), onError);
                    });
                }
                if(addedTags.Count > 0)
                {
                    submissionActions.Add(() =>
                    {
                        var parameters = new AddModTagsParameters();
                        parameters.tagNames = addedTags.ToArray();
                        APIClient.AddModTags(profile.id, parameters,
                                             doNextSubmissionAction, onError);
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
                                                       () => doNextSubmissionAction(null),
                                                       onError);
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
                                                    onError);
                    });
                }
            }

            // - Get Updated Profile -
            submissionActions.Add(() => APIClient.GetMod(profile.id, onSuccess, onError));

            // - Start submission chain -
            doNextSubmissionAction(new APIMessage());
        }

        /// <summary>Zips and uploads a mod data directory as a new build to the servers.</summary>
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
                    WebRequestError error = WebRequestError.GenerateLocal("Unable to zip mod binary prior to uploading");

                    onError(error);
                }
            }

            if(zipSucceeded)
            {
                UploadModBinary_Zipped(modId, modfileValues, binaryZipLocation, setActiveBuild, onSuccess, onError);
            }
        }

        /// <summary>Zips and uploads a mod data file as a new build to the servers.</summary>
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
                    WebRequestError error = WebRequestError.GenerateLocal("Unable to zip mod binary prior to uploading");

                    onError(error);
                }
            }

            if(zipSucceeded)
            {
                UploadModBinary_Zipped(modId, modfileValues, binaryZipLocation, setActiveBuild, onSuccess, onError);
            }
        }

        /// <summary>Uploads a zipped mod binary as a new build to the servers.</summary>
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
        // ---------[ USER DATA ]---------
        /// <summary>Fetches and caches the User Profile for the values in UserAuthenticationData.</summary>
        public static void GetAuthenticatedUserProfile(Action<UserProfile> onSuccess,
                                                       Action<WebRequestError> onError)
        {
            if(UserAuthenticationData.instance.userId != UserProfile.NULL_ID)
            {
                ModManager.GetUserProfile(UserAuthenticationData.instance.userId,
                                          onSuccess,
                                          onError);
            }
            else if(!string.IsNullOrEmpty(UserAuthenticationData.instance.token))
            {
                APIClient.GetAuthenticatedUser(
                (p) =>
                {
                    CacheClient.SaveUserProfile(p);

                    if(onSuccess != null)
                    {
                        onSuccess(p);
                    }
                },
                onError);
            }
            else if(onSuccess != null)
            {
                onSuccess(null);
            }
        }

        /// <summary>Fetches the list of mods associated with the  User Profile matching the UserAuthenticationData.</summary>
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
    }
}
