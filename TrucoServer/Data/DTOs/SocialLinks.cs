using Newtonsoft.Json;

namespace TrucoServer.Data.DTOs
{
    public class SocialLinks
    {
        [JsonProperty("facebook")]
        public string FacebookHandle { get; set; }

        [JsonProperty("x")]
        public string XHandle { get; set; }

        [JsonProperty("instagram")]
        public string InstagramHandle { get; set; }
    }
}
