using AuthorizationAPI.Entities;
using AuthorizationAPI.Models.Dto;

namespace AuthorizationAPI.Authorization
{
    public interface IJwtUtils
    {
        public string GenerateJwtToken(Account account);
        public int? ValidateJwtToken(string token);
        public RefreshToken GenerateRefreshToken(string ipAddress);
    }
}
