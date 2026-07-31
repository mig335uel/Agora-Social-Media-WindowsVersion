using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace Agora.Models
{
    [Table("devices")]
    public class Devices : BaseModel
    {
        [PrimaryKey("id", false)]
        public string? Id { get; set; }

        [Column("device_identifier")]
        public string? DeviceIdentifier { get; set; }

        [Column("user_id")]
        public string? UserId { get; set; }

        [Column("fcm_token")]
        public string? FcmToken { get; set; }

        [Column("public_device_key")]
        public string? PublicDeviceKey { get; set; }

        [Column("device_name")]
        public string? DeviceName { get; set; }

        [Column("platform")]
        public string? Platform { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("last_seen")]
        public DateTime? LastSeen { get; set; }
    }
}
