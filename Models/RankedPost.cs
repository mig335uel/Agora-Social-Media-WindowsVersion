using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Agora.Models
{
    // Este es el "molde" que recibe el JSON aplanado de tu algoritmo RPC
    public class RankedPost
    {
        [JsonProperty("id")]
        public string id { get; set; }

        [JsonProperty("content")]
        public string content { get; set; }

        [JsonProperty("created_at")]
        public DateTime created_at { get; set; }

        [JsonProperty("user_id")]
        public string user_id { get; set; }

        [JsonProperty("username")]
        public string username { get; set; }

        [JsonProperty("display_name")]
        public string display_name { get; set; }

        [JsonProperty("profile_picture_url")]
        public string profile_picture_url { get; set; }

        // El RPC ya te devuelve el array de multimedia incrustado
        [JsonProperty("media")]
        public List<MediaFeature> media { get; set; }

        // (He omitido los likes_count etc. para no hacerlo largo, pero puedes añadirlos igual)
    }
}
