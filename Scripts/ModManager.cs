// #define DO_NOT_LOAD_CACHE

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Newtonsoft.Json;

using ModIO.API;

// NOTE(@jackson): Had a weird bug where Initialize authenticated with a user.id of 0?
// TODO(@jackson): UISettings
// TODO(@jackson): ErrorWrapper to handle specific error codes
namespace ModIO
{
    public delegate void GameProfileEventHandler(GameProfile profile);
    public delegate void ModProfileEventHandler(ModProfile modProfile);
    public delegate void AuthenticatedUserEventHandler(UserProfile user);
    public delegate void ModIDEventHandler(int modId);
    public delegate void ModfileEventHandler(int modId, Modfile newModfile);
    public delegate void ModLogoUpdatedEventHandler(int modId, LogoSize size, Texture2D texture);
    public delegate void ModGalleryImageUpdatedEventHandler(int modId, string imageFileName, ModGalleryImageSize size, Texture2D texture);

    public delegate void ModProfilesEventHandler(IEnumerable<ModProfile> modProfiles);
    public delegate void ModIdsEventHandler(IEnumerable<int> modIds);
    public delegate void ModfileStubsEventHandler(IEnumerable<ModfileStub> modfiles);

    // TODO(@jackson): -> RequestManager?
    public static class ModManager
    {
        // ---------[ MEMBERS ]---------

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

        // ---------[ AUTHENTICATED USER ]---------
        public static void GetAuthenticatedUserProfile(Action<UserProfile> onSuccess,
                                                       Action<WebRequestError> onError)
        {
            UserProfile cachedProfile = CacheClient.LoadAuthenticatedUserProfile();

            if(cachedProfile != null)
            {
                if(onSuccess != null) { onSuccess(cachedProfile); }
            }
            else
            {
                // - Fetch from Server -
                Action<UserProfile> onGetUser = (profile) =>
                {
                    CacheClient.SaveAuthenticatedUserProfile(profile);
                    if(onSuccess != null) { onSuccess(profile); }
                };

                APIClient.GetAuthenticatedUser(onGetUser, onError);
            }
        }

        public static void GetAuthenticatedUserMods(Action<List<ModProfile>> onSuccess,
                                                    Action<WebRequestError> onError)
        {
            List<int> cachedModIds = CacheClient.LoadAuthenticatedUserMods();

            if(cachedModIds != null)
            {
                ModManager.GetModProfiles(cachedModIds,
                                          onSuccess,
                                          onError);
            }
            else
            {
                RequestFilter userModsFilter = new RequestFilter();
                userModsFilter.fieldFilters[GetUserModFilterFields.gameId]
                = new EqualToFilter<int>() { filterValue = APIClient.gameId };

                Action<List<ModProfile>> onGetMods = (modProfiles) =>
                {
                    CacheClient.SaveModProfiles(modProfiles);

                    List<int> modIds = new List<int>(modProfiles.Count);
                    foreach(ModProfile profile in modProfiles)
                    {
                        modIds.Add(profile.id);
                    }
                    CacheClient.SaveAuthenticatedUserMods(modIds);

                    if(onSuccess != null) { onSuccess(modProfiles); }
                };

                // - Get All Events -
                ModManager.FetchAllResultsForQuery<ModProfile>((p,s,e) => APIClient.GetUserMods(userModsFilter, p, s, e),
                                                               onGetMods,
                                                               onError);
            }
        }

        // ---------[ GAME PROFILE ]---------
        public static IEnumerator RequestGameProfile(ClientRequest<GameProfile> request)
        {

            // - Attempt load from cache -
            GameProfile cachedProfile = CacheClient.LoadGameProfile();
            request.response = cachedProfile;

            if(request.response == null)
            {
                bool isDone = false;

                APIClient.GetGame((r) => ModManager.OnRequestSuccess(r, request, out isDone),
                                   (e) => ModManager.OnRequestError(e, request, out isDone));

                while(!isDone) { yield return null; }

                if(request.error == null)
                {
                    CacheClient.SaveGameProfile(request.response);
                }
            }
        }

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

                    APIClient.GetMod(modId,
                                     onGetMod,
                                     onError);
                }
            });
        }

        public static void GetModProfiles(IEnumerable<int> modIds,
                                          Action<List<ModProfile>> onSuccess,
                                          Action<WebRequestError> onError)
        {
            List<int> missingModIds = new List<int>(modIds);
            List<ModProfile> modProfiles = new List<ModProfile>(missingModIds.Count);

            foreach(ModProfile profile in CacheClient.AllModProfiles())
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

            ModManager.FetchAllResultsForQuery<ModProfile>((p,s,e) => APIClient.GetAllMods(RequestFilter.None, p, s, e),
                                                           onModProfilesReceived,
                                                           onError);
            // TODO(@jackson): Other bits
        }

        // ---------[ EVENTS ]---------
        public static event ModProfilesEventHandler     modsAvailable;
        public static event ModProfilesEventHandler     modsEdited;
        public static event ModfileStubsEventHandler    modReleasesUpdated;
        public static event ModIdsEventHandler          modsUnavailable;

        public static IEnumerator RequestAndApplyAllModEventsToCache(int fromTimeStamp,
                                                                     int untilTimeStamp,
                                                                     ClientRequest<List<ModEvent>> request)
        {
            bool isDone = false;

            ModManager.FetchAndApplyAllModEvents(fromTimeStamp, untilTimeStamp,
                                                 (r) => ModManager.OnRequestSuccess(r, request, out isDone),
                                                 (e) => ModManager.OnRequestError(e, request, out isDone));

            while(!isDone) { yield return null; }
        }

        public static void FetchAndApplyAllModEvents(int fromTimeStamp,
                                                     int untilTimeStamp,
                                                     Action<List<ModEvent>> onSuccess,
                                                     Action<WebRequestError> onError)
        {
            ModManager.FetchAllModEvents(fromTimeStamp, untilTimeStamp,
            (modEvents) =>
            {
                ApplyModEventsToCache(modEvents);

                if(onSuccess != null) { onSuccess(modEvents); }
            },
            onError);
        }

        public static void FetchAllModEvents(int fromTimeStamp,
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

            // - Get All Events -
            ModManager.FetchAllResultsForQuery<ModEvent>((p,s,e) => APIClient.GetAllModEvents(modEventFilter, p, s, e),
                                                         onSuccess,
                                                         onError);
        }

        // TODO(@jackson): Check updates against ModProfile dateUpdated
        // TODO(@jackson): Return Profiles as lists?
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
                RequestFilter modsFilter = new RequestFilter();
                modsFilter.fieldFilters[GetAllModsFilterFields.id]
                = new InArrayFilter<int>()
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
                                modfileChangedStubs.Add(profile.activeBuild);
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

                ModManager.FetchAllResultsForQuery<ModProfile>((p,s,e) => APIClient.GetAllMods(modsFilter, p, s, e),
                                                               onGetMods,
                                                               null);
            }

            // --- Process Removed ---
            if(removedIds.Count > 0)
            {
                foreach(int modId in removedIds)
                {
                    CacheClient.DeleteMod(modId);
                }

                // TODO(@jackson): Compare with subscriptions

                if(ModManager.modsUnavailable != null)
                {
                    ModManager.modsUnavailable(removedIds);
                }
            }
        }

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

            // - Get All Events -
            ModManager.FetchAllResultsForQuery<UserEvent>((p,s,e) => APIClient.GetUserEvents(userEventFilter, p, s, e),
                                                          onSuccess,
                                                          onError);
        }

        public static void ApplyUserEventsToCache(IEnumerable<UserEvent> userEvents)
        {
            List<int> subscriptionsAdded = new List<int>();
            List<int> subscriptionsRemoved = new List<int>();

            foreach(UserEvent ue in userEvents)
            {
                switch(ue.eventType)
                {
                    case UserEventType.ModSubscribed:
                    {
                        subscriptionsAdded.Add(ue.modId);
                    }
                    break;
                    case UserEventType.ModUnsubscribed:
                    {
                        subscriptionsRemoved.Add(ue.modId);
                    }
                    break;
                }
            }

            List<int> subscriptions = CacheClient.LoadAuthenticatedUserSubscriptions();

            subscriptions.RemoveAll(s => subscriptionsRemoved.Contains(s));
            subscriptions.AddRange(subscriptionsAdded);

            CacheClient.SaveAuthenticatedUserSubscriptions(subscriptions);
        }

        // ---------[ MOD IMAGES ]---------
        // TODO(@jackson): Look at reconfiguring params
        public static void GetModLogo(ModProfile profile, LogoSize size,
                                      Action<Texture2D> onSuccess,
                                      Action<WebRequestError> onError)
        {
            Debug.Assert(onSuccess != null);

            CacheClient.LoadModLogo(profile.id, size,
                                    (logoTexture) =>
            {
                if(logoTexture != null)
                {
                    onSuccess(logoTexture);
                }

                var versionInfo = CacheClient.LoadModLogoVersionInfo(profile.id);

                if(logoTexture == null
                   || versionInfo[size] != profile.logoLocator.GetFileName())
                {
                    var textureDownload = DownloadClient.DownloadModLogo(profile, size);

                    textureDownload.succeeded += (d) =>
                    {
                        CacheClient.SaveModLogo(profile.id, profile.logoLocator.GetFileName(),
                                                size, d.imageTexture);
                    };

                    textureDownload.succeeded += (d) => onSuccess(d.imageTexture);
                    textureDownload.failed += (d) => onError(d.error);
                }
            });
        }

        public static void GetModGalleryImage(ModProfile profile,
                                              string imageFileName,
                                              ModGalleryImageSize size,
                                              Action<Texture2D> onSuccess,
                                              Action<WebRequestError> onError)
        {
            CacheClient.LoadModGalleryImage(profile.id,
                                            imageFileName,
                                            size,
                                            (cachedImageTexture) =>
            {
                if(cachedImageTexture != null)
                {
                    if(onSuccess != null) { onSuccess(cachedImageTexture); }
                }
                else
                {
                    // - Fetch from Server -
                    var download = DownloadClient.DownloadModGalleryImage(profile,
                                                                          imageFileName,
                                                                          size);

                    download.succeeded += (d) =>
                    {
                        CacheClient.SaveModGalleryImage(profile.id, imageFileName, size, d.imageTexture);
                    };

                    download.succeeded += (d) => onSuccess(d.imageTexture);
                    download.failed += (d) => onError(d.error);
                }
            });
        }

        public static IEnumerator RequestModLogo(ModProfile profile,
                                                 LogoSize size,
                                                 ClientRequest<Texture2D> logoRequest)
        {
            bool isDone = false;

            ModManager.GetModLogo(profile, size,
                                  (r) => ModManager.OnRequestSuccess(r, logoRequest, out isDone),
                                  (e) => ModManager.OnRequestError(e, logoRequest, out isDone));

            while(!isDone)
            {
                yield return null;
            }
        }

        // ---------[ MODFILES ]---------
        public static void GetModfile(int modId, int modfileId,
                                      Action<Modfile> onSuccess,
                                      Action<WebRequestError> onError)
        {
            CacheClient.LoadModfile(modId, modfileId,
                                    (cachedModfile) =>
            {
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
            });
        }

        public static void GetDownloadedBinaryStatus(ModfileStub modfile,
                                                     Action<ModBinaryStatus> callback)
        {
            string binaryFilePath = CacheClient.GenerateModBinaryZipFilePath(modfile.modId, modfile.id);
            ModBinaryStatus status;

            if(File.Exists(binaryFilePath))
            {
                try
                {
                    if((new FileInfo(binaryFilePath)).Length != modfile.fileSize)
                    {
                        status = ModBinaryStatus.Error_FileSizeMismatch;
                    }
                    else
                    {
                        using (var md5 = System.Security.Cryptography.MD5.Create())
                        {
                            using (var stream = System.IO.File.OpenRead(binaryFilePath))
                            {
                                var hash = md5.ComputeHash(stream);
                                string hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                                bool isValidHash = (hashString == modfile.fileHash.md5);

                                Debug.Log("Hash Validation = [" + isValidHash + "]"
                                          + "\nExpected Hash: " + modfile.fileHash.md5
                                          + "\nDownload Hash: " + hashString);

                                if(isValidHash)
                                {
                                    status = ModBinaryStatus.CompleteAndVerified;
                                }
                                else
                                {
                                    status = ModBinaryStatus.Error_HashCheckFailed;
                                }
                            }
                        }
                    }
                }
                #pragma warning disable 0168

                catch(Exception e)
                {
                    status = ModBinaryStatus.Error_UnableToReadFile;
                }
                #pragma warning restore 0168
            }
            else if(File.Exists(binaryFilePath + ".download"))
            {
                status = ModBinaryStatus.PartiallyDownloaded;
            }
            else
            {
                status = ModBinaryStatus.Missing;
            }

            if(callback != null) { callback(status); }
        }

        public static ModBinaryRequest GetActiveModBinary(ModProfile profile)
        {
            string zipFilePath = CacheClient.GenerateModBinaryZipFilePath(profile.id,
                                                                          profile.activeBuild.id);
            ModBinaryRequest request;

            if(File.Exists(zipFilePath))
            {
                request = new ModBinaryRequest();
                request.isDone = true;
                request.binaryFilePath = zipFilePath;
            }
            else
            {
                request = DownloadClient.DownloadModBinary(profile.id,
                                                           profile.activeBuild.id,
                                                           CacheClient.GenerateModBinaryZipFilePath(profile.id, profile.activeBuild.id));

                request.succeeded += (r) => CacheClient.SaveModfile(r.modfile);
            }

            return request;
        }

        public static void DeleteInactiveBuilds(ModProfile profile)
        {
            string buildDir = CacheClient.GenerateModBuildsDirectoryPath(profile.id);
            string[] buildFilePaths = Directory.GetFiles(buildDir, "*.*");

            foreach(string buildFile in buildFilePaths)
            {
                if(Path.GetFileNameWithoutExtension(buildFile)
                   != profile.activeBuild.id.ToString())
                {
                    CacheClient.DeleteFile(buildFile);
                }
            }
        }

        public static void UnzipModBinaryToLocation(ModfileStub modfile,
                                                    string unzipLocation)
        {
            string zipFilePath = CacheClient.GenerateModBinaryZipFilePath(modfile.modId, modfile.id);

            if(File.Exists(zipFilePath))
            {
                try
                {
                    Directory.CreateDirectory(unzipLocation);

                    using (var zip = Ionic.Zip.ZipFile.Read(zipFilePath))
                    {
                        zip.ExtractAll(unzipLocation);
                    }
                }
                catch(Exception e)
                {
                    Utility.LogExceptionAsWarning("[mod.io] Unable to extract binary to given location."
                                                  + "\nLocation: " + unzipLocation,
                                                  e);
                }
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

            APIClient.AddMod(parameters,
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
                        APIClient.AddModTags(profile.id, parameters,
                                          doNextSubmissionAction, modSubmissionFailed);
                    });
                }
                if(removedTags.Count > 0)
                {
                    submissionActions.Add(() =>
                    {
                        var parameters = new DeleteModTagsParameters();
                        parameters.tags = removedTags.ToArray();
                        APIClient.DeleteModTags(profile.id, parameters,
                                             doNextSubmissionAction, modSubmissionFailed);
                    });
                }
            }

            // - Metadata KVP -

            // - Mod Dependencies -

            // - Team Members -

            // - Get Updated Profile -
            submissionActions.Add(() => APIClient.GetMod(profile.id,
                                                      modSubmissionSucceeded,
                                                      modSubmissionFailed));

            // - Start submission chain -
            doNextSubmissionAction(new APIMessage());
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
            bool zipSucceeded = false;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(binaryZipLocation));

                using(var zip = new Ionic.Zip.ZipFile())
                {
                    zip.AddFile(unzippedBinaryLocation);
                    zip.Save(binaryZipLocation);
                }

                zipSucceeded = true;
            }
            catch(Exception e)
            {
                string warning = "[mod.io] Unable to zip mod binary prior to uploading.\n";
                Utility.LogExceptionAsWarning(warning, e);
            }

            if(zipSucceeded)
            {
                UploadModBinary_Zipped(modId, modfileValues, binaryZipLocation, setPrimary, onSuccess, onError);
            }
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

            var parameters = new AddModfileParameters();
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

            // - Generate Hash -
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = System.IO.File.OpenRead(binaryZipLocation))
                {
                    var hash = md5.ComputeHash(stream);
                    parameters.filehash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }

            APIClient.AddModfile(modId, parameters, onSuccess, onError);
        }






        // ---------------------------[ OLD OLD OLD OLD OLD !!! ]------------------------




        private static string cacheDirectory    { get { return CacheClient.GetCacheDirectory(); } }
        private static string manifestPath      { get { return cacheDirectory + "manifest.data"; } }
        private static string userdataPath      { get { return cacheDirectory + "user.data"; } }

        // --- TEMPORARY PASS-THROUGH FUNCTIONS ---
        public static void AddPositiveRating(int modId,
                                             Action<APIMessage> onSuccess,
                                             Action<WebRequestError> onError)
        {
            APIClient.AddModRating(modId, new AddModRatingParameters(1),
                                   onSuccess,
                                   onError);
        }

        // public static void AddModTeamMember(int modId, UnsubmittedTeamMember teamMember,
        //                                     Action<APIMessage> onSuccess,
        //                                     Action<WebRequestError> onError)
        // {
        //     APIClient.AddModTeamMember(modId, teamMember.AsAddModTeamMemberParameters(),
        //                             result => OnRequestSuccessWrapper(result, onSuccess),
        //                             onError);
        // }

        public static void DeleteModFromServer(int modId,
                                               Action<APIMessage> onSuccess,
                                               Action<WebRequestError> onError)
        {
            // TODO(@jackson): Remvoe Mod Locally

            APIClient.DeleteMod(modId,
                                onSuccess,
                                onError);
        }

        public static void DeleteModComment(int modId, int commentId,
                                            Action<APIMessage> onSuccess,
                                            Action<WebRequestError> onError)
        {
            APIClient.DeleteModComment(modId, commentId,
                                       onSuccess,
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
        //     APIClient.GetAllModTeamMembers(modId, GetAllModTeamMembersFilter.None,
        //                                    result => OnRequestSuccessWrapper(result, onSuccess),
        //                                    onError);
        // }
    }
}
