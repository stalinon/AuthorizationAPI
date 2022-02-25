using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AuthorizationAPI.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [Required]
        public string Username { get; set; }
        [Required]
        public Role Role { get; set; }

        [JsonIgnore]
        [Required]
        public string PasswordHash { get; set; }
    }
}
