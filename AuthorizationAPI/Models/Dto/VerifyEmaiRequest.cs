using System.ComponentModel.DataAnnotations;

namespace AuthorizationAPI.Models.Dto
{
    public class VerifyEmailRequest
    {
        [Required]
        public string Token { get; set; }
    }
}
