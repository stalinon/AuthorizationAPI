using AuthorizationAPI.Models.Dto;
using System.Linq.Expressions;

namespace AuthorizationAPI.DataAccess.Repository
{
    public interface IUserRepository
    {
        Task<IEnumerable<UserDto>> GetUsers();
        Task<UserDto> GetUserById(int id);
        Task<UserDto> CreateUpdateUser(UserDto userDto);
        Task<bool> DeleteUser(int id);
        Task<UserDto> FirstOrDefault(Expression<Func<UserDto, bool>> filter, string includeProperties = null, bool isTracking = true);
    }
}
