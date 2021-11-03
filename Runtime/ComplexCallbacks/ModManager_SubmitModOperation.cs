/***
 * NOTE(@jackson):
 *  Please do not use this class as it will be removed in the near future.
 *  It is a temporary solution to simplify the complex callback chain of
 *  the ModManager.SubmitNewMod/SubmitModChanges functionality.
 *
 * NOTE(@jackson):
 *  If the process of submitting a new mod succeeds in the initial APIClient.AddMod request,
 *  but fails in the second half when uploading media, the *whole process* returns an error,
 *  which can be unhelpful as the mod will now exist on the server.
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
        private int modId = ModProfile.NULL_ID;
        private EditableModProfile eModProfile = null;

        private AddModParameters addModParams = null;


        private string logoPath = null;
        private byte[] logoData = null;
        private byte[] imageArchiveData = null;

        private List<string> removedImageFileNames = null;
        private List<string> removedYouTubeURLs = null;
        private List<string> removedSketchfabURLs = null;
        private List<string> addedImageFilePaths = null;
        private List<string> addedYouTubeURLs = null;
        private List<string> addedSketchfabURLs = null;
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
            string errorMessage = null;
            if(String.IsNullOrEmpty(newModProfile.name.value))
            {
                errorMessage = "Mod Profile needs to be named before it can be uploaded";
            }
            else if(String.IsNullOrEmpty(newModProfile.summary.value))
            {
                errorMessage = "Mod Profile needs to be given a summary before it can be uploaded";
            }

            // Send data
            if(errorMessage == null)
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
                if(newModProfile.youTubeURLs.isDirty || newModProfile.sketchfabURLs.isDirty
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
                DataStorage.ReadFile(newModProfile.logoLocator.value.url,
                                     this.SubmitNewMod_OnReadLogo);
            }
            else
            {
                this.SubmissionError_Local(errorMessage);
            }
        }

        /// <summary>Submits changes to a mod to the server.</summary>
        public void SubmitModChanges(int modId, EditableModProfile modEdits)
        {
            Debug.Assert(modId != ModProfile.NULL_ID);

            this.eModProfile = modEdits;

            if(this.eModProfile.status.isDirty || this.eModProfile.visibility.isDirty
               || this.eModProfile.name.isDirty || this.eModProfile.nameId.isDirty
               || this.eModProfile.summary.isDirty || this.eModProfile.descriptionAsHTML.isDirty
               || this.eModProfile.homepageURL.isDirty || this.eModProfile.metadataBlob.isDirty)
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

                APIClient.EditMod(modId, parameters, this.SubmitModChanges_Internal,
                                  this.SubmissionComplete_Error);
            }
            // - Get updated ModProfile -
            else
            {
                ModManager.GetModProfile(modId, this.SubmitModChanges_Internal,
                                         this.SubmissionComplete_Error);
            }
        }

        /// <summary>Calculates changes made to a mod profile and submits them to the
        /// servers.</summary>
        private void SubmitModChanges_Internal(ModProfile profile)
        {
            // early outs
            if(profile == null)
            {
                this.SubmissionError_Local(
                    "Profile parameter passed to ModManager_SubmitModOperation.SubmitModChanges_Internal"
                    + " was null. This was an unexpected error, please try submitting the mod again.");
                return;
            }
            if(profile.id == ModProfile.NULL_ID)
            {
                this.SubmissionError_Local(
                    "Profile parameter passed to ModManager_SubmitModOperation.SubmitModChanges_Internal"
                    + " has a NULL_ID. This was an unexpected error, please try submitting the mod again.");
                return;
            }

            // --- Collect Submission Information ---
            this.modId = profile.id;

            // - Media -
            if(this.eModProfile.logoLocator.isDirty
               && !string.IsNullOrEmpty(this.eModProfile.logoLocator.value.url))
            {
                this.logoPath = this.eModProfile.logoLocator.value.url;
            }

            if(this.eModProfile.galleryImageLocators.isDirty)
            {
                this.removedImageFileNames = new List<string>();
                foreach(var locator in profile.media.galleryImageLocators)
                {
                    this.removedImageFileNames.Add(locator.fileName);
                }
                foreach(var locator in this.eModProfile.galleryImageLocators.value)
                {
                    this.removedImageFileNames.Remove(locator.fileName);
                }

                this.addedImageFilePaths = new List<string>();
                foreach(var locator in this.eModProfile.galleryImageLocators.value)
                {
                    this.addedImageFilePaths.Add(locator.url);
                }
                foreach(var locator in profile.media.galleryImageLocators)
                {
                    this.addedImageFilePaths.Remove(locator.GetURL());
                }
            }

            if(this.eModProfile.sketchfabURLs.isDirty)
            {
                this.removedSketchfabURLs = new List<string>(profile.media.sketchfabURLs);
                foreach(string url in this.eModProfile.sketchfabURLs.value)
                {
                    this.removedSketchfabURLs.Remove(url);
                }

                this.addedSketchfabURLs = new List<string>(this.eModProfile.sketchfabURLs.value);
                foreach(string url in profile.media.sketchfabURLs)
                {
                    this.addedSketchfabURLs.Remove(url);
                }
            }

            if(this.eModProfile.youTubeURLs.isDirty)
            {
                this.removedYouTubeURLs = new List<string>(profile.media.youTubeURLs);
                foreach(string url in this.eModProfile.youTubeURLs.value)
                {
                    this.removedYouTubeURLs.Remove(url);
                }

                this.addedYouTubeURLs = new List<string>(this.eModProfile.youTubeURLs.value);
                foreach(string url in profile.media.youTubeURLs)
                {
                    this.addedYouTubeURLs.Remove(url);
                }
            }

            // - Tags -
            if(this.eModProfile.tags.isDirty)
            {
                this.removedTags = new List<string>(profile.tagNames);
                foreach(string tag in this.eModProfile.tags.value) { this.removedTags.Remove(tag); }

                this.addedTags = new List<string>(this.eModProfile.tags.value);
                foreach(string tag in profile.tagNames) { this.addedTags.Remove(tag); }
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
                    if(this.addedKVPs.TryGetValue(kvp.key, out editValue) && editValue == kvp.value)
                    {
                        this.addedKVPs.Remove(kvp.key);
                    }
                }
            }

            // - Start submission chain -
            if(this.logoPath != null)
            {
                DataStorage.ReadFile(this.logoPath, this.SubmitModChanges_Internal_OnReadLogo);
            }
            else
            {
                this.SubmitModChanges_Internal_ZipImages();
            }
        }

        // ---------[ Internal Callbacks ]---------
        private void SubmissionError_Local(string errorMessage)
        {
            if(this != null && this.onError != null)
            {
                WebRequestError error = WebRequestError.GenerateLocal(errorMessage);
                this.SubmissionComplete_Error(error);
            }
        }

        private void SubmitNewMod_OnReadLogo(string path, bool success, byte[] data)
        {
            if(!success)
            {
                this.SubmissionError_Local("Mod Profile logo file could not be read for uploading."
                                           + "\nLogo Path: " + path);
            }
            else
            {
                this.addModParams.logo = BinaryUpload.Create(Path.GetFileName(path), data);

                if(this.eModProfile == null)
                {
                    APIClient.AddMod(this.addModParams, this.SubmissionComplete_Success,
                                     this.SubmissionComplete_Error);
                }
                else
                {
                    APIClient.AddMod(this.addModParams, this.SubmitModChanges_Internal,
                                     this.SubmissionComplete_Error);
                }
            }
        }

        private void SubmitModChanges_Internal_OnReadLogo(string path, bool success, byte[] data)
        {
            if(success)
            {
                this.logoData = data;
                this.SubmitModChanges_Internal_ZipImages();
            }
            else
            {
                this.SubmissionError_Local("Mod Profile logo file could not be read for uploading."
                                           + "\nLogo Path: " + path);
            }
        }

        private void SubmitModChanges_Internal_ZipImages()
        {
            if(this.addedImageFilePaths != null && this.addedImageFilePaths.Count > 0)
            {
                string imageArchivePath =
                    IOUtilities.CombinePath(Application.temporaryCachePath, "modio",
                                            "imageGallery_" + DateTime.Now.ToFileTime() + ".zip");

                DataStorage.CreateDirectory(Path.GetDirectoryName(imageArchivePath), (path,
                                                                                      success) => {
                    if(success)
                    {
                        bool didZip = CompressionModule.CompressFileCollection(
                            null, this.addedImageFilePaths, imageArchivePath);

                        if(didZip)
                        {
                            DataStorage.ReadFile(imageArchivePath,
                                                 this.SubmitModChanges_Internal_OnReadImageArchive);
                        }
                        else
                        {
                            this.SubmissionError_Local(
                                "Unable to zip image gallery prior to uploading.");
                        }
                    }
                    else
                    {
                        this.SubmissionError_Local(
                            "Unable to create temp directory for image gallery prior to uploading.");
                    }
                });
            }
            else
            {
                this.SubmitNextParameter();
            }
        }

        private void SubmitModChanges_Internal_OnReadImageArchive(string path, bool success,
                                                                  byte[] data)
        {
            if(success)
            {
                this.imageArchiveData = data;
                this.SubmitNextParameter();
            }
        }

        private void SubmissionComplete_Success(ModProfile profile)
        {
            RequestCache.Clear();

            if(this.onSuccess != null)
            {
                this.onSuccess.Invoke(profile);
            }
        }

        private void SubmissionComplete_Error(WebRequestError error)
        {
            if(this.onError != null)
            {
                this.onError.Invoke(error);
            }
        }

        // ---------[ Submission Loop Functions ]---------
        private void SubmitNextParameter()
        {
            this.SubmitNextParameter(null);
        }

        private void SubmitNextParameter(object o)
        {
            // - Media -
            if((this.removedImageFileNames != null && this.removedImageFileNames.Count > 0)
               || (this.removedSketchfabURLs != null && this.removedSketchfabURLs.Count > 0)
               || (this.removedYouTubeURLs != null && this.removedYouTubeURLs.Count > 0))
            {
                var parameters = new DeleteModMediaParameters();

                if(this.removedImageFileNames != null)
                {
                    parameters.images = this.removedImageFileNames.ToArray();
                }
                if(this.removedSketchfabURLs != null)
                {
                    parameters.sketchfab = this.removedSketchfabURLs.ToArray();
                }
                if(this.removedYouTubeURLs != null)
                {
                    parameters.youtube = this.removedYouTubeURLs.ToArray();
                }

                APIClient.DeleteModMedia(this.modId, parameters, this.SubmitNextParameter,
                                         this.SubmissionComplete_Error);

                this.removedImageFileNames = null;
                this.removedSketchfabURLs = null;
                this.removedYouTubeURLs = null;
            }
            else if((this.logoData != null) || (this.imageArchiveData != null)
                    || (this.addedSketchfabURLs != null && this.addedSketchfabURLs.Count > 0)
                    || (this.addedYouTubeURLs != null && this.addedYouTubeURLs.Count > 0))
            {
                var parameters = new AddModMediaParameters();

                if(this.logoData != null)
                {
                    parameters.logo =
                        BinaryUpload.Create(Path.GetFileName(this.logoPath), this.logoData);
                }
                if(this.imageArchiveData != null)
                {
                    parameters.galleryImages =
                        BinaryUpload.Create("images.zip", this.imageArchiveData);
                }
                if(this.addedSketchfabURLs != null && this.addedSketchfabURLs.Count > 0)
                {
                    parameters.sketchfab = this.addedSketchfabURLs.ToArray();
                }
                if(this.addedYouTubeURLs != null && this.addedYouTubeURLs.Count > 0)
                {
                    parameters.youtube = this.addedYouTubeURLs.ToArray();
                }

                APIClient.AddModMedia(this.modId, parameters, this.SubmitNextParameter,
                                      this.SubmissionComplete_Error);

                this.logoData = null;
                this.imageArchiveData = null;
                this.addedSketchfabURLs = null;
                this.addedYouTubeURLs = null;
            }
            // - Tags -
            else if(this.removedTags != null && this.removedTags.Count > 0)
            {
                var parameters = new DeleteModTagsParameters();
                parameters.tagNames = this.removedTags.ToArray();

                APIClient.DeleteModTags(this.modId, parameters, this.SubmitNextParameter,
                                        this.SubmissionComplete_Error);

                this.removedTags = null;
            }
            else if(this.addedTags != null && this.addedTags.Count > 0)
            {
                var parameters = new AddModTagsParameters();
                parameters.tagNames = this.addedTags.ToArray();

                APIClient.AddModTags(this.modId, parameters, this.SubmitNextParameter,
                                     this.SubmissionComplete_Error);

                this.addedTags = null;
            }
            // - KVPs -
            else if(this.removedKVPs != null && this.removedKVPs.Count > 0)
            {
                var parameters = new DeleteModKVPMetadataParameters();
                parameters.metadataKeys = this.removedKVPs.Keys.ToArray();

                APIClient.DeleteModKVPMetadata(this.modId, parameters, this.SubmitNextParameter,
                                               this.SubmissionComplete_Error);
                this.removedKVPs = null;
            }
            else if(this.addedKVPs != null && this.addedKVPs.Count > 0)
            {
                string[] addedKVPStrings =
                    AddModKVPMetadataParameters.ConvertMetadataKVPsToAPIStrings(
                        MetadataKVP.DictionaryToArray(this.addedKVPs));

                var parameters = new AddModKVPMetadataParameters();
                parameters.metadata = addedKVPStrings;

                APIClient.AddModKVPMetadata(this.modId, parameters, this.SubmitNextParameter,
                                            this.SubmissionComplete_Error);

                this.addedKVPs = null;
            }
            // - FINALIZE -
            else if(o != null && o is ModProfile && ((ModProfile)o).id == this.modId)
            {
                this.SubmissionComplete_Success((ModProfile)o);
            }
            else
            {
                APIClient.GetMod(this.modId, this.SubmissionComplete_Success,
                                 this.SubmissionComplete_Error);
            }
        }
    }
}
