using System;

namespace ModIO.API
{
    [Serializable]
    public struct GameObject : IEquatable<GameObject>
    {
        // - Fields -
        public int id; // Unique game id.
        public int status; // Status of the game (see status and visibility for details):
        public UserObject submitted_by; // Contains user data.
        public int date_added; // Unix timestamp of date game was registered.
        public int date_updated; // Unix timestamp of date game was updated.
        public int date_live; // Unix timestamp of date game was set live.
        public int presentation_option; // Presentation style used on the mod.io website:
        public int submission_option; // Submission process modders must follow:
        public int curation_option; // Curation process used to approve mods:
        public int community_options; // Community features enabled on the mod.io website:
        public int revenue_options; // Revenue capabilities mods can enable:
        public int api_access_options; // Level of API access allowed by this game:
        public string ugc_name; // Word used to describe user-generated content (mods, items, addons etc).
        public IconObject icon; // Contains icon data.
        public LogoObject logo; // Contains logo data.
        public HeaderImageObject header; // Contains header data.
        public string homepage; // Official homepage of the game.
        public string name; // Name of the game.
        public string name_id; // Subdomain for the game on mod.io.
        public string summary; // Summary of the game.
        public string instructions; // A guide about creating and uploading mods for this game to mod.io (applicable if submission_option = 0).
        public string profile_url; // URL to the game's mod.io page.
        public GameTagOptionObject[] tag_options; // Groups of tags configured by the game developer, that mods can select.

        // - Equality Operators -
        public override int GetHashCode()
        {
            return this.id;
        }

        public override bool Equals(object obj)
        {
            return (obj is GameObject
                    && this.Equals((GameObject)obj));
        }

        public bool Equals(GameObject other)
        {
            return(this.id.Equals(other.id)
                   && this.status.Equals(other.status)
                   && this.submitted_by.Equals(other.submitted_by)
                   && this.date_added.Equals(other.date_added)
                   && this.date_updated.Equals(other.date_updated)
                   && this.date_live.Equals(other.date_live)
                   && this.presentation_option.Equals(other.presentation_option)
                   && this.submission_option.Equals(other.submission_option)
                   && this.curation_option.Equals(other.curation_option)
                   && this.community_options.Equals(other.community_options)
                   && this.revenue_options.Equals(other.revenue_options)
                   && this.api_access_options.Equals(other.api_access_options)
                   && this.ugc_name.Equals(other.ugc_name)
                   && this.icon.Equals(other.icon)
                   && this.logo.Equals(other.logo)
                   && this.header.Equals(other.header)
                   && this.homepage.Equals(other.homepage)
                   && this.name.Equals(other.name)
                   && this.name_id.Equals(other.name_id)
                   && this.summary.Equals(other.summary)
                   && this.instructions.Equals(other.instructions)
                   && this.profile_url.Equals(other.profile_url)
                   && this.tag_options.GetHashCode().Equals(other.tag_options.GetHashCode()));
        }
    }
}