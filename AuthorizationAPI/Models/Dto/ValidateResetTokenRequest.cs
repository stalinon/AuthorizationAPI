using System.ComponentModel.DataAnnotations;

namespace AuthorizationAPI.Models.Dto
{
    public class ValidateResetTokenRequest
    {
        [Required]
        public string Token { get; set; }
    }
}
