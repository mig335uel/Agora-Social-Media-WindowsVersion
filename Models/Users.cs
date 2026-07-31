
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using Newtonsoft.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
namespace Agora.Models
{


    [Table("users")]
    public class Users : BaseModel
    {
        [PrimaryKey("id", false)]
        public string? Id { get; set; }
        [Column("name")]
        public string? Name { get; set; }
        [Column("last_name")]
        public string? LastName { get; set; }
        [Column("username")]
        public string? Username { get; set; }
        [Column("display_name")]
        public string? DisplayName { get; set; }
        [Column("bio")]
        public string? Bio { get; set; }
        [Column("profile_picture_url")]
        [JsonProperty("profile_picture_url")]
        public string? ProfilePictureUrl { get; set; }
        [Column("is_private")]
        public bool? isPrivate { get; set; }
        [Column("birth_date")]
        public DateOnly? BirthDate { get; set; }
        [Column("gender")]
        public string? Gender { get; set; }
        [Column("is_verified")]
        public bool? IsVerified { get; set; }
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }
        [Column("advertencias")]
        public int? advertencias { get; set; }
        [Column("penalizaciones")]
        public int? penalizaciones { get; set; }

        public Users()
        {
            // Default constructor
        }

        public Users(string? id, string? name, string? lastName, string? username, string? displayName, string? bio, string? profilePictureUrl, DateOnly? birthDate, string? gender, DateTime? createdAt, int advertencias, int penalizaciones)
        {
            Id = id;
            Name = name;
            LastName = lastName;
            Username = username;
            DisplayName = displayName;
            Bio = bio;
            ProfilePictureUrl = profilePictureUrl;

            BirthDate = birthDate;
            Gender = gender;
            CreatedAt = createdAt;
            advertencias = 0;
            penalizaciones = 0;
        }
        public Users(string? id, string? name, string? lastName, string? username, string? displayName, string? bio, string? profilePictureUrl,  string? gender)
        {
            Id = id;
            Name = name;
            LastName = lastName;
            Username = username;
            DisplayName = displayName;
            Bio = bio;
            ProfilePictureUrl = profilePictureUrl;

     
            Gender = gender;
            
            advertencias = 0;
            penalizaciones = 0;
        }
        public Visibility VerifiedVisibility => IsVerified == true ? Visibility.Visible : Visibility.Collapsed;
    }

    // DTOs para los formularios de la interfaz
    // Los sacamos fuera del modelo de Base de Datos para mantenerlo limpio
    public record LoginForm(string Email, string Password);
    
    public record RegisterForm(
        string Email, 
        string Password, 
        string Name, 
        string LastName, 
        string Username, 
        string DisplayName, 
        DateOnly? BirthDate, 
        string? Gender
    );
    
}
