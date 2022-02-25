using AuthorizationAPI.Authorization;
using AuthorizationAPI.DataAccess.Repository;
using AuthorizationAPI.Entities;
using AuthorizationAPI.Helpers;
using AuthorizationAPI.Helpers.Exceptions;
using AuthorizationAPI.Models.Dto;
using AuthorizationAPI.Models.Users;
using Microsoft.Extensions.Options;

namespace AuthorizationAPI.Services
{
    public class UserService : IUserService
    {
        private IUserRepository _users;
        private IJwtUtils _jwtUtils;
        private readonly AuthSettings _appSettings;

        public UserService(
            IUserRepository users,
            IJwtUtils jwtUtils,
            IOptions<AuthSettings> appSettings)
        {
            _users = users;
            _jwtUtils = jwtUtils;
            _appSettings = appSettings.Value;
        }


        public async Task<AuthenticateResponse> AuthenticateAsync(AuthenticateRequest model)
        {
            var user = await _users.FirstOrDefault(filter: (x => x.Username == model.Username));

            // validate
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                throw new AuthException("Username or password is incorrect");

            // authentication successful so generate jwt token
            var jwtToken = _jwtUtils.GenerateJwtToken(user);

            return new AuthenticateResponse(user, jwtToken);
        }

        public async Task<IEnumerable<UserDto>> GetAll()
        {
            return await _users.GetUsers();
        }

        public async Task<UserDto> Get(int id)
        {
            var user = await _users.GetUserById(id);
            if (user == null) throw new KeyNotFoundException("User not found");
            return user;
        }

        public async Task<UserDto> CreateUpdate(UserDto userDto)
        {
            return await _users.CreateUpdateUser(userDto);
        }

        public async Task<bool> Delete(int id)
        {
            return await _users.DeleteUser(id);
        }
    }
}
