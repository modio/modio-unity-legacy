/***
 * NOTE(@jackson):
 *  Please do not use this class as it will be removed in the near future.
 *  It is a temporary solution to simplify the complex callback chain of
 *  the ModManager.SubmitNewMod/SubmitModChanges functionality.
 *
 * TODO(@jackson): Remove
 ***/

using System;
using System.Collections.Generic;
using System.Linq;
using Path = System.IO.Path;

using UnityEngine;

using ModIO.API;

namespace ModIO
{
    internal class ModManager_SubmitModOperation
    {
        public Action<ModProfile> onSuccess = null;
        public Action<WebRequestError> onError = null;

        /// <summary>Submits a new mod to the server.</summary>
        public void SubmitNewMod(EditableModProfile newModProfile)
        {
            Debug.Assert(newModProfile != null);

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

            if(error != null)
            {
                if(this.onError != null)
                {
                    this.onError.Invoke(error);
                }
                return;
            }

            // Define callbacks
            LocalDataIOCallbacks.ReadFileCallback onReadLogo = null;

            onReadLogo = (path, success, data) =>
            {
                if(success)
                {
                    error = WebRequestError.GenerateLocal("Mod Profile logo could not be accessed before uploading."
                                                          + "\nLogo Path: " + path);

                    if(this.onError != null)
                    {
                        this.onError.Invoke(error);
                    }
                    return;
                }

                var parameters = new AddModParameters();
                parameters.name = newModProfile.name.value;
                parameters.summary = newModProfile.summary.value;
                parameters.logo = BinaryUpload.Create(Path.GetFileName(newModProfile.logoLocator.value.url), data);

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
                                                                     remainingModEdits),
                                 this.onError);
            };

            // - Initial Mod Submission -
            LocalDataStorage.ReadFile(newModProfile.logoLocator.value.url, onReadLogo);
        }

        /// <summary>Submits changes to a mod to the server.</summary>
        public void SubmitModChanges(int modId, EditableModProfile modEdits)
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
                    (p) => SubmitModChanges_Internal(profile, modEdits),
                    this.onError);
                }
                // - Get updated ModProfile -
                else
                {
                    SubmitModChanges_Internal(profile, modEdits);
                }
            };

            ModManager.GetModProfile(modId, submitChanges, this.onError);
        }

        /// <summary>Calculates changes made to a mod profile and submits them to the servers.</summary>
        private void SubmitModChanges_Internal(ModProfile profile, EditableModProfile modEdits)
        {
            if(profile == null)
            {
                if(this.onError != null)
                {
                    this.onError(WebRequestError.GenerateLocal("ugh"));
                }
            }

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
                   && LocalDataStorage.GetFileExists(modEdits.logoLocator.value.url))
                {
                    addMediaParameters.logo = new BinaryUpload();

                    submissionActions.Add(() =>
                    {
                        LocalDataStorage.ReadFile(modEdits.logoLocator.value.url,
                        (p, success, data) =>
                        {
                            if(success)
                            {
                                addMediaParameters.logo = BinaryUpload.Create(Path.GetFileName(modEdits.logoLocator.value.url), data);
                            }

                            doNextSubmissionAction(null);
                        });
                    });
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
                        if(LocalDataStorage.GetFileExists(locator.url))
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

                        bool archiveCreated = false;

                        try
                        {
                            LocalDataStorage.CreateDirectory(Path.GetDirectoryName(galleryZipLocation));

                            using(var zip = new Ionic.Zip.ZipFile())
                            {
                                foreach(string imageFilePath in addedImageFilePaths)
                                {
                                    zip.AddFile(imageFilePath);
                                }
                                zip.Save(galleryZipLocation);
                            }

                            archiveCreated = true;
                        }
                        catch(Exception e)
                        {
                            Debug.LogError("[mod.io] Unable to zip image gallery prior to uploading.\n\n"
                                           + Utility.GenerateExceptionDebugString(e));
                        }

                        if(archiveCreated)
                        {
                            addMediaParameters.galleryImages = new BinaryUpload();

                            submissionActions.Add(() =>
                            {
                                LocalDataStorage.ReadFile(galleryZipLocation,
                                (p, success, data) =>
                                {
                                    if(success)
                                    {
                                        var imageGalleryUpload = BinaryUpload.Create("images.zip", data);
                                        addMediaParameters.galleryImages = imageGalleryUpload;
                                    }

                                    doNextSubmissionAction(null);
                                });
                            });
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
                                              doNextSubmissionAction, this.onError);
                    });
                }
                if(deleteMediaParameters.stringValues.Count > 0)
                {
                    submissionActions.Add(() =>
                    {
                        APIClient.DeleteModMedia(profile.id,
                                                 deleteMediaParameters,
                                                 () => doNextSubmissionAction(null),
                                                 this.onError);
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
                                                () => doNextSubmissionAction(null), this.onError);
                    });
                }
                if(addedTags.Count > 0)
                {
                    submissionActions.Add(() =>
                    {
                        var parameters = new AddModTagsParameters();
                        parameters.tagNames = addedTags.ToArray();
                        APIClient.AddModTags(profile.id, parameters,
                                             doNextSubmissionAction, this.onError);
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
                                                       this.onError);
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
                                                    this.onError);
                    });
                }
            }

            // - Get Updated Profile -
            submissionActions.Add(() => APIClient.GetMod(profile.id, this.onSuccess, this.onError));

            // - Start submission chain -
            doNextSubmissionAction(new APIMessage());
        }
    }
}
