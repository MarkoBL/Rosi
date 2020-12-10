using System;
using System.Text.Json.Serialization;

namespace Rosi
{
    class GithubRelease
    {
        [JsonPropertyName("html_url")]
        public Uri DownloadUrl { get; set; }
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }

        public Version Version
        {
            get
            {
                return Version.Parse(TagName.Substring(1));
            }
        }
    }
}
