using AuthorizationAPI.Models.Dto;
using AuthorizationAPI.Models.Users;

namespace AuthorizationAPI.Services
{
    public interface IUserService
    {
        Task<AuthenticateResponse> AuthenticateAsync(AuthenticateRequest model);
        Task<IEnumerable<UserDto>> GetAll();
        Task<UserDto> Get(int id);
        Task<UserDto> CreateUpdate(UserDto userDto);
        Task<bool> Delete(int id);
    }
}
