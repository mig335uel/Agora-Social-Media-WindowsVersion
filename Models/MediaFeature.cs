using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace Agora.Models
{
    [Table("media_feature")]
    public class MediaFeature : BaseModel
    {
        [PrimaryKey("id", false)]
        public string? Id { get; set; }
        [Column("post_id")]
        public string? PostId { get; set; }
        [Column("image")]
        [JsonProperty("image")]
        public string? Image { get; set; }
        [Column("created_at")]
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("user_id")]
        public string? UserId { get; set; }

    }
}
