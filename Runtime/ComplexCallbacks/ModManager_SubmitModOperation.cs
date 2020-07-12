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

        // - operation vars -
        private EditableModProfile eModProfile = null;

        private AddModParameters addModParams = null;
        private DeleteModMediaParameters deleteMediaParams = null;
        private AddModMediaParameters addMediaParams = null;

        private int modId = ModProfile.NULL_ID;
        private List<string> removedTags = null;
        private List<string> addedTags = null;
        private Dictionary<string, string> removedKVPs = null;
        private Dictionary<string, string> addedKVPs = null;

        // ---------[ Submission Functions ]---------
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
                this.SubmissionError(error);
            }
            else
            {
                // - string params -
                this.addModParams = new AddModParameters();
                this.addModParams.name = newModProfile.name.value;
                this.addModParams.summary = newModProfile.summary.value;

                if(newModProfile.visibility.isDirty)
                {
                    this.addModParams.visibility = newModProfile.visibility.value;
                }
                if(newModProfile.nameId.isDirty)
                {
                    this.addModParams.nameId = newModProfile.nameId.value;
                }
                if(newModProfile.descriptionAsHTML.isDirty)
                {
                    this.addModParams.descriptionAsHTML = newModProfile.descriptionAsHTML.value;
                }
                if(newModProfile.homepageURL.isDirty)
                {
                    this.addModParams.nameId = newModProfile.homepageURL.value;
                }
                if(newModProfile.metadataBlob.isDirty)
                {
                    this.addModParams.metadataBlob = newModProfile.metadataBlob.value;
                }
                if(newModProfile.nameId.isDirty)
                {
                    this.addModParams.nameId = newModProfile.nameId.value;
                }
                if(newModProfile.tags.isDirty)
                {
                    this.addModParams.tags = newModProfile.tags.value;
                }

                // - editable params -
                if(newModProfile.youTubeURLs.isDirty
                   || newModProfile.sketchfabURLs.isDirty
                   || newModProfile.galleryImageLocators.isDirty)
                {
                    // NOTE(@jackson): As add Mod takes more parameters than edit,
                    //  we can ignore some of the elements in the EditModParameters
                    //  when passing to SubmitModChanges_Internal
                    this.eModProfile = new EditableModProfile();
                    this.eModProfile.youTubeURLs = newModProfile.youTubeURLs;
                    this.eModProfile.sketchfabURLs = newModProfile.sketchfabURLs;
                    this.eModProfile.galleryImageLocators = newModProfile.galleryImageLocators;
                }

                // - data params -
                LocalDataStorage.ReadFile(newModProfile.logoLocator.value.url, this.SubmitNewMod_OnReadLogo);
            }
        }

        /// <summary>Submits changes to a mod to the server.</summary>
        public void SubmitModChanges(int modId, EditableModProfile modEdits)
        {
            Debug.Assert(modId != ModProfile.NULL_ID);

            this.eModProfile = modEdits;

            if(this.eModProfile.status.isDirty
               || this.eModProfile.visibility.isDirty
               || this.eModProfile.name.isDirty
               || this.eModProfile.nameId.isDirty
               || this.eModProfile.summary.isDirty
               || this.eModProfile.descriptionAsHTML.isDirty
               || this.eModProfile.homepageURL.isDirty
               || this.eModProfile.metadataBlob.isDirty)
            {
                var parameters = new EditModParameters();
                if(this.eModProfile.status.isDirty)
                {
                    parameters.status = this.eModProfile.status.value;
                }
                if(this.eModProfile.visibility.isDirty)
                {
                    parameters.visibility = this.eModProfile.visibility.value;
                }
                if(this.eModProfile.name.isDirty)
                {
                    parameters.name = this.eModProfile.name.value;
                }
                if(this.eModProfile.nameId.isDirty)
                {
                    parameters.nameId = this.eModProfile.nameId.value;
                }
                if(this.eModProfile.summary.isDirty)
                {
                    parameters.summary = this.eModProfile.summary.value;
                }
                if(this.eModProfile.descriptionAsHTML.isDirty)
                {
                    parameters.descriptionAsHTML = this.eModProfile.descriptionAsHTML.value;
                }
                if(this.eModProfile.homepageURL.isDirty)
                {
                    parameters.homepageURL = this.eModProfile.homepageURL.value;
                }
                if(this.eModProfile.metadataBlob.isDirty)
                {
                    parameters.metadataBlob = this.eModProfile.metadataBlob.value;
                }

                APIClient.EditMod(modId, parameters, this.SubmitModChanges_Internal, this.SubmissionError);
            }
            // - Get updated ModProfile -
            else
            {
                ModManager.GetModProfile(modId, this.SubmitModChanges_Internal, this.SubmissionError);
            }
        }

        /// <summary>Calculates changes made to a mod profile and submits them to the servers.</summary>
        private void SubmitModChanges_Internal(ModProfile profile)
        {
            // early outs
            if(profile == null)
            {
                if(this.onError != null)
                {
                    WebRequestError error = WebRequestError.GenerateLocal("Profile parameter passed to ModManager_SubmitModOperation.SubmitModChanges_Internal"
                                                                          + " was null. This was an unexpected error, please try submitting the mod again.");
                    this.onError(error);
                }

                return;
            }
            if(profile.id == ModProfile.NULL_ID)
            {
                if(this.onError != null)
                {
                    WebRequestError error = WebRequestError.GenerateLocal("Profile parameter passed to ModManager_SubmitModOperation.SubmitModChanges_Internal"
                                                                          + " has a NULL_ID. This was an unexpected error, please try submitting the mod again.");
                    this.onError(error);
                }

                return;
            }

            this.modId = profile.id;

            // - start process -
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
            if(this.eModProfile.logoLocator.isDirty
               || this.eModProfile.youTubeURLs.isDirty
               || this.eModProfile.sketchfabURLs.isDirty
               || this.eModProfile.galleryImageLocators.isDirty)
            {
                this.addMediaParams = new AddModMediaParameters();
                this.deleteMediaParams = new DeleteModMediaParameters();

                // - string values -
                if(this.eModProfile.youTubeURLs.isDirty)
                {
                    var addedYouTubeLinks = new List<string>(this.eModProfile.youTubeURLs.value);
                    foreach(string youtubeLink in profile.media.youTubeURLs)
                    {
                        addedYouTubeLinks.Remove(youtubeLink);
                    }
                    this.addMediaParams.youtube = addedYouTubeLinks.ToArray();

                    var removedTags = new List<string>(profile.media.youTubeURLs);
                    foreach(string youtubeLink in this.eModProfile.youTubeURLs.value)
                    {
                        removedTags.Remove(youtubeLink);
                    }
                    this.deleteMediaParams.youtube = addedYouTubeLinks.ToArray();
                }

                if(this.eModProfile.sketchfabURLs.isDirty)
                {
                    var addedSketchfabLinks = new List<string>(this.eModProfile.sketchfabURLs.value);
                    foreach(string sketchfabLink in profile.media.sketchfabURLs)
                    {
                        addedSketchfabLinks.Remove(sketchfabLink);
                    }
                    this.addMediaParams.sketchfab = addedSketchfabLinks.ToArray();

                    var removedTags = new List<string>(profile.media.sketchfabURLs);
                    foreach(string sketchfabLink in this.eModProfile.sketchfabURLs.value)
                    {
                        removedTags.Remove(sketchfabLink);
                    }
                    this.deleteMediaParams.sketchfab = addedSketchfabLinks.ToArray();
                }

                if(this.eModProfile.logoLocator.isDirty
                   && LocalDataStorage.GetFileExists(this.eModProfile.logoLocator.value.url))
                {
                    this.addMediaParams.logo = new BinaryUpload();

                    submissionActions.Add(() =>
                    {
                        LocalDataStorage.ReadFile(this.eModProfile.logoLocator.value.url,
                        (path, success, data) =>
                        {
                            if(success)
                            {
                                this.addMediaParams.logo = BinaryUpload.Create(Path.GetFileName(path), data);
                            }

                            doNextSubmissionAction(null);
                        });
                    });
                }

                if(this.eModProfile.galleryImageLocators.isDirty)
                {
                    var addedImageFilePaths = new List<string>();
                    foreach(var locator in this.eModProfile.galleryImageLocators.value)
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
                            this.addMediaParams.galleryImages = new BinaryUpload();

                            submissionActions.Add(() =>
                            {
                                LocalDataStorage.ReadFile(galleryZipLocation,
                                (p, success, data) =>
                                {
                                    if(success)
                                    {
                                        var imageGalleryUpload = BinaryUpload.Create("images.zip", data);
                                        this.addMediaParams.galleryImages = imageGalleryUpload;
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
                    foreach(var locator in this.eModProfile.galleryImageLocators.value)
                    {
                        removedImageFileNames.Remove(locator.fileName);
                    }

                    if(removedImageFileNames.Count > 0)
                    {
                        this.deleteMediaParams.images = removedImageFileNames.ToArray();
                    }
                }

                if(this.addMediaParams.stringValues.Count > 0
                   || this.addMediaParams.binaryData.Count > 0)
                {
                    submissionActions.Add(() =>
                    {
                        APIClient.AddModMedia(profile.id,
                                              this.addMediaParams,
                                              doNextSubmissionAction, this.onError);
                    });
                }
                if(this.deleteMediaParams.stringValues.Count > 0)
                {
                    submissionActions.Add(() =>
                    {
                        APIClient.DeleteModMedia(profile.id,
                                                 this.deleteMediaParams,
                                                 () => doNextSubmissionAction(null),
                                                 this.onError);
                    });
                }
            }

            // - Tags -
            if(this.eModProfile.tags.isDirty)
            {
                this.removedTags = new List<string>(profile.tagNames);
                foreach(string tag in this.eModProfile.tags.value)
                {
                    this.removedTags.Remove(tag);
                }

                this.addedTags = new List<string>(this.eModProfile.tags.value);
                foreach(string tag in profile.tagNames)
                {
                    this.addedTags.Remove(tag);
                }
            }

            // - Metadata KVP -
            if(this.eModProfile.metadataKVPs.isDirty)
            {
                this.removedKVPs = MetadataKVP.ArrayToDictionary(profile.metadataKVPs);
                foreach(MetadataKVP kvp in this.eModProfile.metadataKVPs.value)
                {
                    string profileValue;

                    // if edited kvp is exact match it's not removed
                    if(this.removedKVPs.TryGetValue(kvp.key, out profileValue)
                        && profileValue == kvp.value)
                    {
                        this.removedKVPs.Remove(kvp.key);
                    }
                }

                this.addedKVPs = MetadataKVP.ArrayToDictionary(this.eModProfile.metadataKVPs.value);
                foreach(MetadataKVP kvp in profile.metadataKVPs)
                {
                    string editValue;

                    // if profile kvp is exact match it's not new
                    if(this.addedKVPs.TryGetValue(kvp.key, out editValue)
                        && editValue == kvp.value)
                    {
                        this.addedKVPs.Remove(kvp.key);
                    }
                }
            }

            // - Get Updated Profile -
            submissionActions.Add(this.SubmitNextParameter);

            // - Start submission chain -
            doNextSubmissionAction(new APIMessage());
        }

        // ---------[ Internal Callbacks ]---------
        private void SubmissionSuccess(ModProfile profile)
        {
            if(this != null && this.onSuccess != null)
            {
                this.onSuccess.Invoke(profile);
            }
        }

        private void SubmissionError(WebRequestError error)
        {
            if(this != null && this.onError != null)
            {
                this.onError.Invoke(error);
            }
        }

        private void SubmitNewMod_OnReadLogo(string path, bool success, byte[] data)
        {
            if(!success)
            {
                WebRequestError error = WebRequestError.GenerateLocal("Mod Profile logo could not be accessed before uploading."
                                                                      + "\nLogo Path: " + path);
                this.SubmissionError(error);
            }
            else
            {
                this.addModParams.logo = BinaryUpload.Create(Path.GetFileName(path), data);

                if(this.eModProfile == null)
                {
                    APIClient.AddMod(this.addModParams,
                                     this.SubmissionSuccess,
                                     this.onError);
                }
                else
                {
                    APIClient.AddMod(this.addModParams,
                                     this.SubmitModChanges_Internal,
                                     this.onError);
                }
            }
        }

        private void SubmitNextParameter(object o)
        {
            this.SubmitNextParameter();
        }

        private void SubmitNextParameter()
        {
            if(this.removedTags != null && this.removedTags.Count > 0)
            {
                var parameters = new DeleteModTagsParameters();
                parameters.tagNames = this.removedTags.ToArray();

                APIClient.DeleteModTags(this.modId, parameters,
                                        this.SubmitNextParameter,
                                        this.onError);

                this.removedTags = null;
            }
            else if(this.addedTags != null && this.addedTags.Count > 0)
            {
                var parameters = new AddModTagsParameters();
                parameters.tagNames = this.addedTags.ToArray();

                APIClient.AddModTags(this.modId, parameters,
                                     this.SubmitNextParameter,
                                     this.onError);

                this.addedTags = null;
            }
            else if(this.removedKVPs != null && this.removedKVPs.Count > 0)
            {
                var parameters = new DeleteModKVPMetadataParameters();
                parameters.metadataKeys = this.removedKVPs.Keys.ToArray();

                APIClient.DeleteModKVPMetadata(this.modId, parameters,
                                               this.SubmitNextParameter,
                                               this.onError);
                this.removedKVPs = null;
            }
            else if(this.addedKVPs != null && this.addedKVPs.Count > 0)
            {
                string[] addedKVPStrings = AddModKVPMetadataParameters.ConvertMetadataKVPsToAPIStrings(MetadataKVP.DictionaryToArray(this.addedKVPs));

                var parameters = new AddModKVPMetadataParameters();
                parameters.metadata = addedKVPStrings;

                APIClient.AddModKVPMetadata(this.modId, parameters,
                                            this.SubmitNextParameter,
                                            this.onError);

                this.addedKVPs = null;
            }
            else
            {
                APIClient.GetMod(this.modId, this.onSuccess, this.onError);
            }
        }
    }
}
