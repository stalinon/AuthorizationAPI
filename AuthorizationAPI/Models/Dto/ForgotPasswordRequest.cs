using System.ComponentModel.DataAnnotations;

namespace AuthorizationAPI.Models.Dto
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
