using System.Collections.Generic;

using Newtonsoft.Json;

namespace ModIO
{
    [System.Serializable]
    public class ModProfile
    {
        // ---------[ CONSTANTS ]---------
        /// <summary>An id value indicating an invalid profile.</summary>
        public const int NULL_ID = 0;

        // ---------[ FIELDS ]---------
        /// <summary> Unique mod id. </summary>
        [JsonProperty("id")]
        public int id;

        /// <summary> Unique game id. </summary>
        [JsonProperty("game_id")]
        public int gameId;

        /// <summary>Status of the mod.</summary>
        /// <para>This field is controlled by the game team, rather than the mod team.</para>
        /// <para>See: <a href="https://docs.mod.io/#status-amp-visibility">Status and Visibility
        /// Documentation</a>.</para>
        [JsonProperty("status")]
        public ModStatus status;

        /// <summary>Visibility of the mod.</summary>
        /// <para>This field is controlled by the game team, rather than the mod team.</para>
        /// <para>See: <a href="https://docs.mod.io/#status-amp-visibility">Status and Visibility
        /// Documentation</a>.</para>
        [JsonProperty("visible")]
        public ModVisibility visibility;

        /// <summary> Contains user data. </summary>
        [JsonProperty("submitted_by")]
        public UserProfile submittedBy;

        /// <summary> Unix timestamp of date mod was registered. </summary>
        [JsonProperty("date_added")]
        public int dateAdded;

        /// <summary> Unix timestamp of date mod was updated. </summary>
        [JsonProperty("date_updated")]
        public int dateUpdated;

        /// <summary> Unix timestamp of date mod was set live. </summary>
        [JsonProperty("date_live")]
        public int dateLive;

        /// <summary>
        /// Maturity options flagged by the mod developer, this is only relevant if the parent game
        /// allows mods to be labelled as mature.
        /// </summary>
        [JsonProperty("maturity_option")]
        public ModContentWarnings contentWarnings;

        /// <summary> Contains logo data. </summary>
        [JsonProperty("logo")]
        public LogoImageLocator logoLocator;

        /// <summary> Official homepage of the mod. </summary>
        [JsonProperty("homepage_url")]
        public string homepageURL;

        /// <summary> Name of the mod. </summary>
        [JsonProperty("name")]
        public string name;

        /// <summary>
        /// Path for the mod on mod.io.
        /// For example: https://gamename.mod.io/mod-name-id-here
        /// </summary>
        [JsonProperty("name_id")]
        public string nameId;

        /// <summary> Summary of the mod. </summary>
        [JsonProperty("summary")]
        public string summary;

        /// <summary>Detailed description of the mod which allows HTML.</summary>
        [JsonProperty("description")]
        public string descriptionAsHTML;

        /// <summary>The description of the mod with the HTML formatting removed.</summary>
        [JsonProperty("description_plaintext")]
        public string descriptionAsText;

        /// <summary>
        /// Metadata stored by the game developer.
        /// </summary>
        [JsonProperty("metadata_blob")]
        public string metadataBlob;

        /// <summary> URL to the mod's mod.io profile. </summary>
        [JsonProperty("profile_url")]
        public string profileURL;

        /// <summary>Contains data for the current build.</summary>
        [JsonProperty("modfile")]
        public Modfile currentBuild;

        /// <summary> Contains mod media data. </summary>
        [JsonProperty("media")]
        public ModMediaCollection media;

        /// <summary> Contains key-value metadata. </summary>
        [JsonProperty("metadata_kvp")]
        public MetadataKVP[] metadataKVPs;

        /// <summary> Contains mod tag data. </summary>
        [JsonProperty("tags")]
        public ModTag[] tags;

        /// <summary>Contains stats data.</summary>
        [JsonProperty("stats")]
        public ModStatistics statistics;

        // ---------[ ACCESSORS ]---------
        [JsonIgnore]
        public IEnumerable<string> tagNames
        {
            get {
                if(tags != null)
                {
                    foreach(ModTag tag in tags) { yield return tag.name; }
                }
            }
        }
    }
}
