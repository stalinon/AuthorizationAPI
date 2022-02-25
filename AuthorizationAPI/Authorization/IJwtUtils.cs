using AuthorizationAPI.Models.Dto;

namespace AuthorizationAPI.Authorization
{
    public interface IJwtUtils
    {
        public string GenerateJwtToken(UserDto user);
        public int? ValidateJwtToken(string token);
    }
}
