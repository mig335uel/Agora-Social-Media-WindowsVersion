using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Agora.Models
{
    [Table("posts")]
    public class Posts: BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }
        [Column("user_id")]
        public string user_id { get; set; }
        [Column("content")]
        public string? content { get; set; }
        [Column("parent_post_id")]
        public string? parent_post_id { get; set; }
        [Column("likes_count")]
        public int likes_count { get; set; }
        [Column("replies_count")]
        public int replies_count { get; set; }
        [Column("reposts_count")]
        public int reposts_count { get; set; }
        [Column("impressions_count")]
        public int impressions_count { get; set; }
        [Column ("shares_count")]
        public int shares_count { get; set; }
        [Column("created_at")]
        public DateTime created_at { get; set; }
        [Column("is_banned")]
        public bool is_banned { get; set; }
        // El Reference daba conflicto de ambigüedad, lo quitamos y lo rellenamos a mano
        public Users? user { get; set; }
        [Reference(typeof(MediaFeature))]
        public List<MediaFeature>? MediaList { get; set; }
    }
}
