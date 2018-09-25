using System;
using System.Collections.Generic;
using System.Linq;

namespace ModIO
{
    // ---------[ SERIALIZABLE EDITABLE CLASSES ]---------
    [Serializable]
    public class EditableModStatusField : EditableField<ModStatus> {}
    [Serializable]
    public class EditableModVisibilityField : EditableField<ModVisibility> {}
    [Serializable]
    public class EditableKVPArrayField : EditableArrayField<MetadataKVP> {}

    [Serializable]
    public class EditableModProfile
    {
        /// @cond
        [Obsolete]
        public class EditableModStatusField : ModIO.EditableModStatusField {}
        [Obsolete]
        public class EditableModVisibilityField : ModIO.EditableModVisibilityField {}
        [Obsolete]
        public class EditableKVPArrayField : ModIO.EditableKVPArrayField {}
        /// @endcond

        // ---------[ FIELDS ]---------
        public ModIO.EditableModStatusField status =                new ModIO.EditableModStatusField();
        public ModIO.EditableModVisibilityField visibility =        new ModIO.EditableModVisibilityField();
        public EditableStringField name =                           new EditableStringField();
        public EditableStringField nameId =                         new EditableStringField();
        public EditableStringField summary =                        new EditableStringField();
        public EditableStringField description_HTML =               new EditableStringField();
        public EditableStringField homepageURL =                    new EditableStringField();
        public EditableStringArrayField tags =                      new EditableStringArrayField();
        public EditableStringField metadataBlob =                   new EditableStringField();
        public ModIO.EditableKVPArrayField metadataKVPs =           new ModIO.EditableKVPArrayField();
        // - Mod Media -
        public EditableImageLocatorField logoLocator =              new EditableImageLocatorField();
        public EditableStringArrayField youtubeURLs =               new EditableStringArrayField();
        public EditableStringArrayField sketchfabURLs =             new EditableStringArrayField();
        public EditableImageLocatorArrayField galleryImageLocators =new EditableImageLocatorArrayField();

        [Obsolete("Use EditableModProfile.description_HTML instead")]
        public EditableStringField description
        { get { return this.description_HTML; } set { this.description_HTML = value; } }

        // ---------[ VALUE DUPLICATION ]---------
        public static EditableModProfile CreateFromProfile(ModProfile profile)
        {
            EditableModProfile retVal = new EditableModProfile();
            retVal.ApplyBaseProfileChanges(profile);
            return retVal;
        }

        public void ApplyBaseProfileChanges(ModProfile profile)
        {
            if(!this.status.isDirty)
            {
                this.status.value = profile.status;
            }
            if(!this.visibility.isDirty)
            {
                this.visibility.value = profile.visibility;
            }
            if(!this.name.isDirty)
            {
                this.name.value = profile.name;
            }
            if(!this.nameId.isDirty)
            {
                this.nameId.value = profile.nameId;
            }
            if(!this.summary.isDirty)
            {
                this.summary.value = profile.summary;
            }
            if(!this.description_HTML.isDirty)
            {
                this.description_HTML.value = profile.description_HTML;
            }
            if(!this.homepageURL.isDirty)
            {
                this.homepageURL.value = profile.homepageURL;
            }
            if(!this.metadataBlob.isDirty)
            {
                this.metadataBlob.value = profile.metadataBlob;
            }
            if(!this.metadataBlob.isDirty)
            {
                this.metadataKVPs.value = profile.metadataKVPs;
            }
            if(!this.tags.isDirty)
            {
                this.tags.value = profile.tagNames.ToArray();
            }

            // - Media -
            if(!this.logoLocator.isDirty)
            {
                this.logoLocator.value.fileName = profile.logoLocator.fileName;
                this.logoLocator.value.url = profile.logoLocator.GetURL();
            }
            if(!this.youtubeURLs.isDirty)
            {
                this.youtubeURLs.value = profile.media.youtubeURLs;
            }
            if(!this.sketchfabURLs.isDirty)
            {
                this.sketchfabURLs.value = profile.media.sketchfabURLs;
            }
            if(!this.galleryImageLocators.isDirty)
            {
                Utility.SafeMapArraysOrZero(profile.media.galleryImageLocators,
                                            (l) => { return ImageLocatorData.CreateFromImageLocator(l); },
                                            out this.galleryImageLocators.value);
            }
        }
    }
}
