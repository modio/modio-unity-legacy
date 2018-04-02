using System.Collections.ObjectModel;

namespace ModIO_TEST.API
{
    [System.Serializable]
    public struct ModObjectRS
    {
        [System.Serializable]
        internal struct ResponseSchema
        {
            public int id;
            public int game_id;
            public int status;
            public int visible;
            public int date_added;
            public int date_updated;
            public int date_live;
            public string homepage;
            public string name;
            public string name_id;
            public string summary;
            public string description;
            public string metadata_blob;
            public string profile_url;
            // public UserObject.ResponseSchema submitted_by;
            // public LogoObject.ResponseSchema logo;
            // public ModfileObject.ResponseSchema modfile;
            // public ModMediaObject.ResponseSchema media;
            // public RatingSummaryObject.ResponseSchema rating_summary;
            // public ModTagObject.ResponseSchema[] tags;
        }

        // ---------[ FIELDS ]---------
        // Unique mod id.
        public readonly int id;
        // Unique game id.
        public readonly int gameId;
        // Status of the mod (see status and visibility for details):
        public readonly int status;
        // Visibility of the mod (see status and visibility for details):
        public readonly int visible;
        // Unix timestamp of date mod was registered.
        public readonly int dateAdded;
        // Unix timestamp of date mod was updated.
        public readonly int dateUpdated;
        // Unix timestamp of date mod was set live.
        public readonly int dateLive;
        // Official homepage of the mod.
        public readonly string homepage;
        // Name of the mod.
        public readonly string name;
        // Path for the mod on mod.io. For example: https://gamename.mod.io/mod-name-id-here
        public readonly string nameId;
        // Summary of the mod.
        public readonly string summary;
        // Detailed description of the mod which allows HTML.
        public readonly string description;
        // Metadata stored by the game developer. Metadata can also be stored as searchable key value pairs, and to individual mod files.
        public readonly string metadataBlob;
        // URL to the mod's mod.io profile.
        public readonly string profileURL;
        // // Contains user data.
        // public readonly UserObject submittedBy;
        // // Contains logo data.
        // public readonly LogoObject logo;
        // // Contains modfile data.
        // public readonly ModfileObject modfile;
        // // Contains mod media data.
        // public readonly ModMediaObject media;
        // // Contains ratings summary.
        // public readonly RatingSummaryObject ratingSummary;
        // // Contains mod tag data.
        // public ReadOnlyCollection<ModTagObject> tags;

        // ---------[ CONSTRUCTOR ]---------
        internal ModObjectRS(ResponseSchema response)
        {
            this.id = response.id;
            this.gameId = response.game_id;
            this.status = response.status;
            this.visible = response.visible;
            this.dateAdded = response.date_added;
            this.dateUpdated = response.date_updated;
            this.dateLive = response.date_live;
            this.homepage = response.homepage;
            this.name = response.name;
            this.nameId = response.name_id;
            this.summary = response.summary;
            this.description = response.description;
            this.metadataBlob = response.metadata_blob;
            this.profileURL = response.profile_url;
            // this.submittedBy = new UserObject(response.submitted_by);
            // this.logo = new LogoObject(response.logo);
            // this.modfile = new ModfileObject(response.modfile);
            // this.media = new ModMediaObject(response.media);
            // this.ratingSummary = new RatingSummaryObject(response.rating_summary);

            // ModTagObject[] tagArray = new ModTagObject[response.tags.Length];
            // for(int i = 0; i < response.tags.Length; ++i)
            // {
            //     tagArray[i] = new ModTagObject(response.tags[i]);
            // }
            // this.tags = new ReadOnlyCollection<ModTagObject>(tagArray);
        }
    }
}